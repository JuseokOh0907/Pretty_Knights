using System.Collections.Generic;
using PrettyKnights.Core;
using PrettyKnights.Data;
using PrettyKnights.Save;
using UnityEngine;

namespace PrettyKnights.World
{
    /// <summary>
    /// 층에 들어갈 때 오브젝트를 만든다. 층 루트에 <see cref="AreaAnchor"/> 와 나란히 붙인다.
    ///
    /// <b>씬에 굽지 않고 런타임에 만드는 이유</b>는 재배치 때문이다.
    /// 테마를 완전 클리어하면 그 테마가 초기화되고 배치가 새로 뽑혀야 하는데
    /// (docs/decisions/006-area-transition.md §3-1), 씬에 구워두면 그럴 수 없다.
    ///
    /// 덤으로 파괴 상태를 <b>생성 순서 인덱스</b>로 저장할 수 있게 된다.
    /// 시드가 같으면 순서가 같으므로 i번째는 언제나 같은 것이다.
    /// 좌표로 저장하는 것보다 짧고 정확하다.
    ///
    /// 진입 비용은 페이드가 가린다. 층 전환은 0.25초 페이드 뒤에 일어난다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AreaAnchor))]
    public sealed class FloorProps : MonoBehaviour
    {
        private const string ContainerName = "AutoProps";

        [SerializeField, Tooltip("비우면 같은 오브젝트에서 찾는다")]
        private AreaAnchor anchor;

        [SerializeField, Tooltip("층에 들어갈 때마다 다시 만들지. 끄면 한 번만 만든다")]
        private bool rebuildOnEnable;

        [Header("디버그")]
        [SerializeField] private bool logBuild = true;

        /// <summary>지금 살아 있는 것들. 순서가 곧 세이브 인덱스다.</summary>
        private readonly List<Destructible> spawned = new List<Destructible>();

        private Transform container;
        private bool built;

        public IReadOnlyList<Destructible> Spawned => spawned;

        private void Awake()
        {
            if (anchor == null) anchor = GetComponent<AreaAnchor>();
        }

        private void OnEnable()
        {
            if (built && !rebuildOnEnable) return;
            Build();
        }

        /// <summary>
        /// 배치를 계산해 실제로 만든다.
        /// <paramref name="seedOffset"/> 은 재배치용이다 — 완전 클리어 횟수를 넣으면
        /// 회차마다 다른 배치가 나오면서도 같은 회차 안에서는 결정적이다.
        /// </summary>
        public void Build(int seedOffset = 0)
        {
            Clear();

            anchor.Resolve();

            AreaDefinition definition = anchor.Definition;
            FloorScatterProfile profile = definition != null ? definition.ScatterProfile : null;

            if (profile == null || profile.PropPrefab == null) return;
            if (anchor.Walkable == null) return;

            container = new GameObject(ContainerName).transform;
            container.SetParent(transform, worldPositionStays: false);

            var zones = new List<NoSpawnZone>(GetComponentsInChildren<NoSpawnZone>(includeInactive: true));
            List<Vector2> avoid = PropScatterer.CollectAvoidPoints(anchor, container);

            ServiceRegistry.TryGet(out WorldProgress progress);

            // 완전 클리어 횟수가 시드에 들어간다. 같은 회차면 몇 번을 들어와도 같은 배치이고,
            // 클리어할 때마다 새로 뽑힌다 — "새 회차" 라는 경계에서만 지형이 바뀐다.
            int clears = progress != null ? progress.ClearCountOf(definition.ThemeNumber) : 0;
            int seed = profile.Seed + definition.AreaId + seedOffset + clears * 7919;
            List<PropScatterer.Placement> plan =
                PropScatterer.Plan(anchor.Walkable, profile, seed, zones, avoid);

            for (int i = 0; i < plan.Count; i++)
            {
                Destructible created = Create(plan[i], profile);
                spawned.Add(created);

                if (created == null || progress == null) continue;

                bool isMain = plan[i].Definition.Role == PropRole.MainTotem;
                int index = i;

                // 부순 것을 기록한다. 인덱스는 생성 순서이고 시드가 같으면 순서도 같다.
                created.Broken += _ => progress.MarkPropDestroyed(definition.AreaId, index, isMain);

                // 이미 부숴둔 것은 조용히 부서진 모습으로 되돌린다.
                // 보상을 다시 주면 같은 것을 반복해서 벌 수 있다.
                if (progress.IsPropDestroyed(definition.AreaId, index)) created.Break(grantRewards: false);
            }

            built = true;

            if (logBuild)
                Debug.Log($"[FloorProps] {definition} — 오브젝트 {spawned.Count}개 생성 (시드 {seed})");
        }

        /// <summary>만들어둔 것을 전부 지운다. 재배치 전에 부른다.</summary>
        public void Clear()
        {
            spawned.Clear();

            if (container == null) return;

            if (Application.isPlaying) Destroy(container.gameObject);
            else DestroyImmediate(container.gameObject);

            container = null;
            built = false;
        }

        private Destructible Create(PropScatterer.Placement placement, FloorScatterProfile profile)
        {
            GameObject instance = Instantiate(
                profile.PropPrefab, placement.Position, Quaternion.identity, container);

            instance.name = placement.Definition.PropId;

            // 겉모습(스프라이트 · 콜라이더 · Visual Y)이 전부 정의에서 온다.
            // 프리팹이 하나이므로 여기서 채우지 않으면 전부 같은 모습이 된다.
            var destructible = instance.GetComponent<Destructible>();
            if (destructible != null) destructible.Bind(placement.Definition);

            if (!placement.Definition.IsTotem) return destructible;

            // 토템은 컴포넌트가 하나 더 필요하다. 공용 프리팹에는 없으므로 여기서 붙인다.
            SpawnTotem totem = instance.GetComponent<SpawnTotem>();
            if (totem == null) totem = instance.AddComponent<SpawnTotem>();

            if (placement.Definition.Role == PropRole.MainTotem) AttachPortal(totem, instance);

            return destructible;
        }

        /// <summary>
        /// 메인 토템 자리에 꺼진 포탈을 함께 만든다.
        /// 목적지는 <see cref="AreaDefinition.NextArea"/> 가 정하므로
        /// 토템이 어디에 뽑히든 링크가 어긋나지 않는다.
        /// </summary>
        private void AttachPortal(SpawnTotem totem, GameObject totemObject)
        {
            AreaDefinition definition = anchor.Definition;
            FloorScatterProfile profile = definition.ScatterProfile;

            if (profile.PortalPrefab == null)
            {
                Debug.LogError(
                    $"[FloorProps] {definition} 의 프로필에 Portal Prefab 이 없습니다. " +
                    "메인 토템을 부숴도 다음 층으로 갈 길이 생기지 않습니다.");
                return;
            }

            if (!definition.HasNextArea)
            {
                Debug.LogError(
                    $"[FloorProps] {definition} 의 Next Area 가 비어 있습니다. " +
                    "메인 토템이 열 포탈의 목적지를 정할 수 없습니다.");
                return;
            }

            GameObject portal = Instantiate(
                profile.PortalPrefab, totemObject.transform.position, Quaternion.identity, container);

            portal.name = $"Portal_from_{definition.AreaId}";

            var component = portal.GetComponent<Portal>();
            if (component != null) component.SetDestination(definition.NextArea, definition.NextArrivalId);

            // 토템을 부술 때까지 보이지 않는다.
            portal.SetActive(false);
            totem.SetPortal(portal);
        }

        [ContextMenu("다시 배치 (다음 회차 시드)")]
        private void DebugRebuild()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[FloorProps] 재생 중에만 동작합니다.");
                return;
            }

            Build(Random.Range(1, 10000));
        }
    }
}
