using System.Collections;
using PrettyKnights.Characters;
using PrettyKnights.Core;
using PrettyKnights.Data;
using PrettyKnights.Save;
using PrettyKnights.UI;
using UnityEngine;

namespace PrettyKnights.World
{
    /// <summary>
    /// 구역 전환의 유일한 실행 주체. <b>Boot 씬에 상주한다.</b>
    /// 포탈은 "어디로" 만 말하고, 순서와 잠금은 전부 여기서 다룬다.
    ///
    /// <code>
    /// 입력 잠금 → 페이드 아웃 → 구역 교체(+카메라 경계) → 도착 지점으로 이동
    ///           → 방향 세팅 → 카메라 스냅 → 세이브 → 페이드 인 → 잠금 해제
    /// </code>
    ///
    /// 구역 교체가 페이드로 완전히 가려진 뒤에 일어나야 한다.
    /// 층 오브젝트 하나가 2만 칸짜리 타일맵이라 켜는 순간 한 프레임이 튄다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AreaTransition : MonoBehaviour
    {
        [Header("도착 보정")]
        [SerializeField, Min(0f), Tooltip("도착 지점이 벽이면 이 반경 안에서 설 수 있는 자리를 찾는다")]
        private float landingSearchRadius = 3f;

        [Header("디버그")]
        [SerializeField] private bool logTransitions = true;

        /// <summary>전환 중인지. 중복 요청을 막는 데 쓴다.</summary>
        public bool IsTransitioning { get; private set; }

        private void Awake() => ServiceRegistry.Register(this);

        private void OnDestroy()
        {
            if (ServiceRegistry.TryGet(out AreaTransition current) && current == this)
                ServiceRegistry.Unregister<AreaTransition>();
        }

        /// <summary>포탈이 부른다. 목적지 구역의 <paramref name="arrivalId"/> 지점으로 옮긴다.</summary>
        public void Request(AreaDefinition destination, string arrivalId)
        {
            if (destination == null)
            {
                Debug.LogError("[AreaTransition] 목적지가 비어 있습니다. 포탈의 Destination 을 확인하세요.");
                return;
            }

            if (IsTransitioning)
            {
                // 전환 중에 또 눌린 경우. 조용히 무시한다 — 경고를 남기면 연타할 때마다 로그가 쌓인다.
                return;
            }

            if (!ServiceRegistry.TryGet(out AreaRegistry registry) || registry == null)
            {
                Debug.LogError(
                    "[AreaTransition] AreaRegistry 가 없습니다. Map 루트에 AreaRegistry 를 붙였는지 확인하세요.");
                return;
            }

            if (!registry.TryGet(destination, out AreaAnchor anchor))
            {
                // 대개는 그 층의 AreaAnchor 에 AreaDefinition 을 안 꽂은 경우다.
                // 등록된 번호를 함께 보여주면 무엇이 빠졌는지 바로 보인다.
                Debug.LogError(
                    $"[AreaTransition] areaId #{destination.AreaId} ({destination.DisplayName}) 에 해당하는 " +
                    "층 오브젝트를 찾지 못했습니다.\n  " +
                    "그 층의 AreaAnchor 에 이 정의가 연결되어 있는지 확인하세요.\n  " +
                    $"지금 등록된 구역: {string.Join(", ", registry.RegisteredIds)}");
                return;
            }

            StartCoroutine(Run(registry, anchor, arrivalId));
        }

        /// <summary>
        /// 던전 탈출. 포탈은 단방향이라 되돌아갈 수 없고, 탈출 스킬만이 밖으로 나가는 길이다.
        /// 목적지는 현재 구역의 <see cref="AreaDefinition.EscapeTo"/> 가 정한다.
        /// </summary>
        public bool RequestEscape()
        {
            if (!ServiceRegistry.TryGet(out AreaRegistry registry) || registry?.Active == null) return false;

            AreaDefinition here = registry.Active.Definition;
            if (here == null || !here.CanEscape)
            {
                if (logTransitions) Debug.Log("[AreaTransition] 이 구역에서는 탈출할 수 없습니다.");
                return false;
            }

            Request(here.EscapeTo, here.EscapeArrivalId);
            return true;
        }

        private IEnumerator Run(AreaRegistry registry, AreaAnchor destination, string arrivalId)
        {
            IsTransitioning = true;

            ServiceRegistry.TryGet(out PlayerController player);
            ServiceRegistry.TryGet(out InteractionHub hub);
            ServiceRegistry.TryGet(out ScreenFader fader);

            // 1) 잠금. 페이드보다 먼저 걸어야 어두워지는 동안 걸어 나가지 못한다.
            if (player != null) player.InputEnabled = false;
            if (hub != null) hub.Locked = true;

            // 2) 페이드 아웃
            if (fader != null) yield return fader.FadeOut();

            // 3) 떠나는 구역을 먼저 정산한다. 구역을 바꾸고 나면 어디서 왔는지 알 수 없다.
            AreaDefinition leaving = registry.Active != null ? registry.Active.Definition : null;
            SettleDeparture(leaving, destination.Definition);

            // 4) 구역 교체. 카메라 경계도 여기서 함께 바뀐다.
            registry.Activate(destination);

            // 4) 도착. 지정 지점이 벽이면 주변에서 설 수 있는 칸을 찾는다.
            //    맵을 다시 그렸을 때 조용히 벽 속에 박히는 것을 막는다.
            ArrivalPoint arrival = destination.ResolveArrival(arrivalId);
            if (arrival == null)
            {
                Debug.LogError(
                    $"[AreaTransition] '{destination.name}' 에 도착 지점이 하나도 없습니다. " +
                    "ArrivalPoint 를 최소 하나 두어야 합니다.");
            }
            else if (player != null && player.Motor != null)
            {
                Vector2 landing = arrival.Position;

                WalkableArea area = destination.Walkable;
                if (area != null && !area.TryFindWalkable(landing, landingSearchRadius, out landing))
                {
                    Debug.LogWarning(
                        $"[AreaTransition] 도착 지점 '{arrival.ArrivalId}' 주변에서 설 수 있는 자리를 찾지 못했습니다. " +
                        "지정된 좌표를 그대로 씁니다.");
                    landing = arrival.Position;
                }

                player.Motor.Warp(landing);
                player.AnimatorDriver?.ForceFacing(arrival.Facing);
            }

            // 5) 카메라를 즉시 붙인다. 보간에 맡기면 페이드 인 후에 화면이 흘러간다.
            if (ServiceRegistry.TryGet(out CameraFollow camera) && camera != null) camera.SnapToTarget();

            // 6) 어디에 있는지를 세이브에 남긴다.
            //    이게 없으면 재시작 시 옛 구역의 같은 좌표에 서게 된다.
            if (ServiceRegistry.TryGet(out GameRoot root) && root != null)
            {
                root.Location.SetArea(destination.AreaId);
                root.SaveNow();
            }

            if (logTransitions)
                Debug.Log($"[AreaTransition] 이동 완료 — {destination.Definition} / 지점 '{arrivalId}'");

            // 7) 페이드 인과 잠금 해제
            if (fader != null) yield return fader.FadeIn();

            if (player != null) player.InputEnabled = true;
            if (hub != null) hub.Locked = false;

            IsTransitioning = false;
        }

        /// <summary>
        /// 구역을 떠날 때의 정산.
        ///
        /// <b>보상방을 나가면 그 테마를 완전 클리어한 것으로 본다.</b> 파괴 기록을 지우고
        /// 클리어 횟수를 올리므로 다음에 들어가면 배치가 새로 뽑힌다 — 새 회차다.
        ///
        /// <b>다른 테마로 넘어가면 떠난 테마를 되살린다.</b> 단 메인 토템은 부서진 채 둔다 —
        /// 되살아나면 이미 뚫어둔 포탈이 다시 닫혀 같은 일을 반복해야 한다.
        /// 이때는 시드가 그대로라 <b>위치도 그대로</b>다.
        /// </summary>
        private void SettleDeparture(AreaDefinition leaving, AreaDefinition entering)
        {
            if (leaving == null || entering == null) return;
            if (!ServiceRegistry.TryGet(out WorldProgress progress) || progress == null) return;

            if (leaving.IsRewardRoom)
            {
                progress.CompleteTheme(leaving.ThemeNumber);

                if (logTransitions)
                    Debug.Log($"[AreaTransition] 테마 {leaving.ThemeNumber} 완전 클리어 — " +
                              $"{progress.ClearCountOf(leaving.ThemeNumber)}회차. 다음 입장 시 재배치됩니다");
                return;
            }

            // 테마 안에서 층만 옮기는 것은 정산 대상이 아니다.
            if (leaving.ThemeNumber == entering.ThemeNumber) return;

            progress.SoftResetTheme(leaving.ThemeNumber);

            if (logTransitions)
                Debug.Log($"[AreaTransition] 테마 {leaving.ThemeNumber} 을(를) 떠났습니다 — " +
                          "오브젝트와 서브 토템이 되살아납니다 (포탈은 열린 채)");
        }

        // ── 검증용 ────────────────────────────────────────────────────────
        // 포탈은 단방향이라 한 번 3F 로 올라가면 걸어서 되돌아올 수 없다.
        // 배치를 고쳐가며 반복 확인하려면 임의 이동 수단이 필요하다.
        // 재생 중 인스펙터에서 이 컴포넌트를 우클릭한다.

        [Header("디버그 이동")]
        [SerializeField, Tooltip("아래 컨텍스트 메뉴가 보낼 구역")]
        private AreaDefinition debugDestination;

        [SerializeField, Tooltip("디버그 이동이 쓸 도착 지점. 비우면 그 구역의 대체 지점")]
        private string debugArrivalId = string.Empty;

        [ContextMenu("디버그 이동")]
        private void DebugJump()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[AreaTransition] 재생 중에만 동작합니다.");
                return;
            }

            Request(debugDestination, debugArrivalId);
        }

        [ContextMenu("디버그 탈출")]
        private void DebugEscape()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[AreaTransition] 재생 중에만 동작합니다.");
                return;
            }

            RequestEscape();
        }
    }
}
