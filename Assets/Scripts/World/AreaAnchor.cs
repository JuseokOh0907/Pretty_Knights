using PrettyKnights.Data;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PrettyKnights.World
{
    /// <summary>
    /// 층 루트에 붙는 컴포넌트. <c>AreaDefinition</c>(에셋)과 씬의 실물을 잇는 유일한 지점이다.
    ///
    /// <b>층 루트에 직접 붙인다.</b> 빈 자식 오브젝트를 따로 만들지 않는다.
    /// 이 오브젝트를 켜고 끄는 것이 곧 구역 교체이므로,
    /// <see cref="WalkableArea"/> 도 같은 오브젝트에 두어야 활성 상태가 정확히 맞물린다.
    ///
    /// <code>
    /// Map [Grid]
    ///  └ Goblin
    ///      └ Goblin1F   [AreaAnchor] [WalkableArea]   ← 여기
    ///          ├ 1Floor  [Tilemap]
    ///          ├ 1FGuide [Tilemap + Collider]
    ///          ├ Arrivals └ from_entrance [ArrivalPoint]
    ///          └ Portals └ Portal_to_2F  [Portal]
    /// </code>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AreaAnchor : MonoBehaviour
    {
        [Header("정의")]
        [SerializeField, Tooltip("이 층이 어떤 구역인지. 비면 아무것도 이 층을 찾을 수 없다")]
        private AreaDefinition definition;

        [Header("연결 (비우면 자식에서 자동으로 찾는다)")]
        [SerializeField, Tooltip("바닥 타일맵. 카메라 경계로도 쓰인다")]
        private Tilemap floor;

        [SerializeField] private WalkableArea walkable;

        [Header("도착 지점")]
        [SerializeField, Tooltip("포탈이 arrivalId 를 못 찾았을 때 쓸 자리. 비우면 첫 번째 지점")]
        private ArrivalPoint fallbackArrival;

        private ArrivalPoint[] arrivals;

        public AreaDefinition Definition => definition;

        /// <summary>정의가 비어 있으면 <see cref="AreaDefinition.NoArea"/>. 그런 층은 포탈이 찾지 못한다.</summary>
        public int AreaId => definition != null ? definition.AreaId : AreaDefinition.NoArea;
        public Tilemap Floor => floor;
        public WalkableArea Walkable => walkable;

        private void Awake() => Resolve();

        /// <summary>
        /// 자식 참조를 채운다. <c>AreaRegistry</c> 가 비활성 층까지 훑어야 하므로
        /// <see cref="Awake"/> 를 기다리지 않고 밖에서도 부를 수 있게 열어 둔다.
        /// </summary>
        public void Resolve()
        {
            if (arrivals != null) return;

            if (floor == null) floor = GetComponentInChildren<Tilemap>(includeInactive: true);
            if (walkable == null) walkable = GetComponent<WalkableArea>();

            // 꺼져 있는 층에서도 도착 지점을 찾을 수 있어야 한다.
            // 포탈이 목적지를 물을 때 그 층은 아직 비활성이다.
            arrivals = GetComponentsInChildren<ArrivalPoint>(includeInactive: true);

            if (definition == null)
                Debug.LogError($"[AreaAnchor] '{name}' 에 AreaDefinition 이 비어 있습니다. 이 층은 포탈로 이동할 수 없습니다.");

            if (floor == null)
                Debug.LogWarning($"[AreaAnchor] '{name}' 에서 바닥 타일맵을 찾지 못했습니다. 카메라 경계가 잡히지 않습니다.");
        }

        /// <summary><paramref name="arrivalId"/> 에 해당하는 도착 지점. 없으면 대체 지점을 돌려준다.</summary>
        public ArrivalPoint ResolveArrival(string arrivalId)
        {
            Resolve();

            if (!string.IsNullOrEmpty(arrivalId))
            {
                foreach (ArrivalPoint point in arrivals)
                    if (point != null && point.ArrivalId == arrivalId)
                        return point;

                Debug.LogWarning(
                    $"[AreaAnchor] '{AreaId}' 에 도착 지점 '{arrivalId}' 가 없습니다. 대체 지점을 씁니다. " +
                    "포탈의 목적지 arrivalId 오타를 확인하세요.");
            }

            if (fallbackArrival != null) return fallbackArrival;

            foreach (ArrivalPoint point in arrivals)
                if (point != null) return point;

            return null;
        }

        /// <summary>에디터 점검용. 같은 구역 안에 중복된 arrivalId 가 있으면 알려준다.</summary>
        public ArrivalPoint[] AllArrivals
        {
            get
            {
                Resolve();
                return arrivals;
            }
        }
    }
}
