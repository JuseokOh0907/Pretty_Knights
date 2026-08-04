using PrettyKnights.Characters;
using PrettyKnights.Data;
using PrettyKnights.Save;
using UnityEngine;

namespace PrettyKnights.Core
{
    /// <summary>
    /// Boot 씬에 하나만 놓는 상주 오브젝트.
    /// 세이브·플레이어 상태·씬 전환을 들고 있으며 게임이 끝날 때까지 파괴되지 않는다.
    ///
    /// 이 구조 덕분에 세로/가로 씬이 갈아 끼워져도
    /// <see cref="PlayerRuntimeState"/> 는 같은 인스턴스로 유지된다 (기획서 §15-6).
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class GameRoot : MonoBehaviour
    {
        [Header("정의 에셋")]
        [SerializeField] private PlayerStatsDefinition playerStats;

        [Header("시작 설정")]
        [SerializeField, Tooltip("신규 플레이일 때의 시작 모드. 저장된 위치가 있으면 그쪽이 우선한다")]
        private GameMode startMode = GameMode.Horizontal;
        [SerializeField, Tooltip("모바일에서 배터리를 아끼기 위한 상한. 0이면 건드리지 않음")]
        private int targetFrameRate = 60;

        [Header("디버그")]
        [SerializeField, Tooltip("시작과 씬 전환을 콘솔에 남긴다")]
        private bool logLifecycle = true;

        public static GameRoot Instance { get; private set; }

        public PlayerRuntimeState PlayerState { get; private set; }
        public SaveService Saves { get; private set; }
        public SceneFlow Scenes { get; private set; }

        /// <summary>마지막으로 있었던 자리. 씬이 올라온 뒤 여기로 몸을 옮긴다.</summary>
        public WorldLocation Location { get; private set; }

        /// <summary>이번 실행이 신규 플레이인지 (세이브 파일이 없었는지).</summary>
        public bool IsNewGame { get; private set; }

        /// <summary>디버그로 세이브를 지운 뒤 자동 저장이 되살리는 것을 막는다.</summary>
        private bool suppressAutoSave;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // 게임플레이 씬을 단독 실행했다가 Boot 로 돌아온 경우 등 중복 방지.
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (targetFrameRate > 0) Application.targetFrameRate = targetFrameRate;

            InitializeServices();
        }

        private void Start()
        {
            // 저장된 모드가 있으면 그쪽으로 복귀한다. 없으면 인스펙터의 시작 모드.
            GameMode target = Location.HasValue ? Location.Mode : startMode;
            StartCoroutine(Scenes.SwitchTo(target));
        }

        /// <summary>
        /// 씬이 올라온 직후 저장된 자리로 몸을 옮긴다.
        /// 몸(<see cref="PlayerController"/>)은 게임플레이 씬에 있고 씬마다 새로 생기므로
        /// 매 전환마다 다시 찾아야 한다.
        /// </summary>
        private void RestorePlayerPosition(GameMode mode)
        {
            if (!Location.HasValue || Location.Mode != mode) return;

            // 좌표를 되돌리기 전에 구역부터 켠다. 층 9개가 좌표계를 공유하므로
            // 순서가 바뀌면 옛 구역의 같은 자리에 서게 된다.
            if (Location.AreaId != 0 && ServiceRegistry.TryGet(out World.AreaRegistry areas) && areas != null)
            {
                if (!areas.Activate(Location.AreaId))
                    Debug.LogWarning(
                        $"[GameRoot] 저장된 구역 #{Location.AreaId} 을 씬에서 찾지 못했습니다. " +
                        "켜져 있는 층을 그대로 씁니다.");
            }

            if (!ServiceRegistry.TryGet(out PlayerController body) || body.Motor == null)
            {
                Debug.LogWarning(
                    "[GameRoot] 씬에서 플레이어를 찾지 못해 위치를 복원하지 못했습니다. " +
                    "Player 프리팹이 씬에 있는지 확인하세요.");
                return;
            }

            // 저장된 좌표가 맵 밖일 수 있다. 구역이 바뀌었거나 맵을 다시 그렸다면 그렇다.
            Vector2 destination = Location.Position;
            if (ServiceRegistry.TryGet(out World.WalkableArea area) && area != null)
            {
                if (!area.TryFindWalkable(destination, 3f, out destination))
                {
                    Debug.LogWarning(
                        $"[GameRoot] 저장된 자리 {Location.Position} 주변에서 설 수 있는 곳을 찾지 못했습니다. " +
                        "씬의 기본 위치를 그대로 씁니다.");
                    return;
                }
            }

            body.Motor.Warp(destination);

            // 바라보던 방향까지 되돌린다. snap 이라 보간 없이 즉시 그 방향으로 선다.
            // 이게 없으면 재시작 때마다 정면(기본값)을 보게 되어 latch 규칙이 끊긴다.
            body.AnimatorDriver?.ForceFacing(Location.Facing);

            if (logLifecycle) Debug.Log($"[GameRoot] 위치 복원 — {Location}");
        }

        /// <summary>현재 몸의 좌표와 방향을 <see cref="Location"/> 에 담는다. 저장 직전에 호출한다.</summary>
        private void CaptureLocation()
        {
            if (Scenes?.CurrentMode == null) return;
            if (!ServiceRegistry.TryGet(out PlayerController body)) return;

            Vector2 facing = body.AnimatorDriver != null
                ? body.AnimatorDriver.FacingVector
                : Location.Facing;

            Location.Set(Scenes.CurrentMode.Value, body.transform.position, facing);
        }

        private void InitializeServices()
        {
            Saves = new SaveService();
            Scenes = new SceneFlow();

            IsNewGame = !Saves.TryLoad(out SaveData data);
            PlayerState = data.Player;
            Location = data.Location;

            if (playerStats == null)
            {
                Debug.LogError(
                    "[GameRoot] PlayerStatsDefinition 이 비어 있습니다. " +
                    "인스펙터에서 연결해야 스탯 계산이 동작합니다.");
            }
            else
            {
                PlayerState.Bind(playerStats);
            }

            ServiceRegistry.Register(this);
            ServiceRegistry.Register(PlayerState);
            ServiceRegistry.Register(Saves);
            ServiceRegistry.Register(Scenes);

            // 씬이 올라온 뒤라야 몸이 존재한다. 전환 완료 시점에 자리를 되돌린다.
            Scenes.ModeChanged += RestorePlayerPosition;

            if (!logLifecycle) return;

            Scenes.ModeChanged += mode => Debug.Log($"[GameRoot] 씬 전환 완료 — {mode}");
            Debug.Log($"[GameRoot] 시작 — {(IsNewGame ? "신규 플레이" : "이어하기")}\n{BuildStateReport()}");
        }

        /// <summary>상태 한 덩어리. 시작 로그와 컨텍스트 메뉴가 함께 쓴다.</summary>
        private string BuildStateReport()
        {
            if (PlayerState == null || !PlayerState.IsBound)
                return "  (PlayerStatsDefinition 이 연결되지 않아 스탯을 계산할 수 없습니다)";

            return
                $"  Lv {PlayerState.Level}  EXP {PlayerState.Exp}/{PlayerState.ExpToNextLevel}  " +
                $"HP {PlayerState.CurrentHp:0.#}/{PlayerState.MaxHp:0.#}\n" +
                $"  스탯 : {PlayerState.Stats}\n" +
                $"  위치 : {Location}\n" +
                $"  세이브 : {Saves.SavePath}  (존재 {Saves.Exists})";
        }

        /// <summary>현재 상태를 파일에 쓴다. 저장 지점마다 호출한다.</summary>
        public void SaveNow()
        {
            if (Saves == null || PlayerState == null || suppressAutoSave) return;

            CaptureLocation();
            Saves.TrySave(SaveData.From(PlayerState, Location));
        }

        public void RequestMode(GameMode mode)
        {
            if (Scenes == null || !isActiveAndEnabled) return;
            StartCoroutine(Scenes.SwitchTo(mode));
        }

        // 모바일은 홈 버튼 한 번으로 앱이 회수될 수 있다.
        // 종료 콜백을 기다리지 않고 백그라운드로 내려가는 시점에 저장한다.
        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveNow();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) SaveNow();
        }

        private void OnApplicationQuit() => SaveNow();

        private void OnDestroy()
        {
            if (Instance != this) return;

            ServiceRegistry.Clear();
            Instance = null;
        }

        // ── 검증용 ────────────────────────────────────────────────────────
        // HUD 가 아직 없어 상태를 볼 방법이 없다.
        // 재생 중 인스펙터에서 GameRoot 컴포넌트 우클릭 → 아래 항목으로 확인한다.

        [ContextMenu("상태 로그")]
        private void LogState()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[GameRoot] 재생 중에만 동작합니다.");
                return;
            }

            if (PlayerState == null || !PlayerState.IsBound)
            {
                Debug.LogError("[GameRoot] PlayerState 가 정의에 연결되지 않았습니다. PlayerStatsDefinition 을 확인하세요.");
                return;
            }

            Debug.Log($"[GameRoot] 현재 상태 — {(IsNewGame ? "신규 플레이" : "이어하기")}\n{BuildStateReport()}");
        }

        [ContextMenu("경험치 +100")]
        private void DebugAddExp()
        {
            if (!Application.isPlaying) return;

            PlayerState?.AddExp(100);
            LogState();
        }

        [ContextMenu("피해 10")]
        private void DebugDamage()
        {
            if (!Application.isPlaying) return;

            PlayerState?.ApplyDamage(10f);
            LogState();
        }

        [ContextMenu("지금 저장")]
        private void DebugSave()
        {
            if (!Application.isPlaying) return;

            suppressAutoSave = false;   // 명시적 저장은 억제를 푼다
            SaveNow();
            Debug.Log($"[GameRoot] 저장했습니다 → {Saves.SavePath}");
        }

        [ContextMenu("세이브 삭제")]
        private void DebugDeleteSave()
        {
            Saves ??= new SaveService();
            Saves.Delete();
            Location?.Clear();

            // 이걸 안 하면 재생을 멈추는 순간 OnApplicationQuit 이 다시 저장해
            // 방금 지운 것이 되살아난다.
            suppressAutoSave = true;

            Debug.Log(
                "[GameRoot] 세이브를 삭제했습니다. 다음 실행이 신규 플레이가 됩니다.\n" +
                "  이번 세션의 자동 저장은 꺼졌습니다 (지금 저장 메뉴로 다시 켤 수 있습니다).");
        }
    }
}
