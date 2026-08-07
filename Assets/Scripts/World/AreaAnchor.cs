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
        private bool resolved;

        public AreaDefinition Definition => definition;

        /// <summary>정의가 비어 있으면 <see cref="AreaDefinition.NoArea"/>. 그런 층은 포탈이 찾지 못한다.</summary>
        public int AreaId => definition != null ? definition.AreaId : AreaDefinition.NoArea;
        public Tilemap Floor => floor;
        public WalkableArea Walkable => walkable;

        private void Awake() => Resolve();

        /// <summary>
        /// 자식 참조를 채운다. <c>AreaRegistry</c> 가 비활성 층까지 훑어야 하므로
        /// <see cref="Awake"/> 를 기다리지 않고 밖에서도 부를 수 있게 열어 둔다.
        ///
        /// <b>에디터에서는 <paramref name="force"/> 를 켜야 한다.</b>
        /// 결과를 한 번 캐시하면 그 뒤에 자식을 추가해도 다시 스캔하지 않는데,
        /// 런타임에는 계층이 고정이라 맞지만 배치 작업 중에는 옛 결과가 남는다.
        /// 점검 도구가 "도착 지점이 하나도 없음" 을 잘못 보고하는 원인이 이것이었다.
        /// </summary>
        public void Resolve(bool force = false)
        {
            if (resolved && !force) return;
            resolved = true;

            // 손으로 연결한 것은 덮어쓰지 않는다. 비어 있는 칸만 채운다.
            if (floor == null) floor = FindFloorTilemap();
            if (walkable == null) walkable = GetComponent<WalkableArea>();

            // 꺼져 있는 층에서도 도착 지점을 찾을 수 있어야 한다.
            // 포탈이 목적지를 물을 때 그 층은 아직 비활성이다.
            arrivals = GetComponentsInChildren<ArrivalPoint>(includeInactive: true);

            if (definition == null)
                Debug.LogError($"[AreaAnchor] '{name}' 에 AreaDefinition 이 비어 있습니다. 이 층은 포탈로 이동할 수 없습니다.");

            if (floor == null)
                Debug.LogWarning($"[AreaAnchor] '{name}' 에서 바닥 타일맵을 찾지 못했습니다. 카메라 경계가 잡히지 않습니다.");
        }

        /// <summary>
        /// 바닥 타일맵을 자동으로 찾는다. <b>콜라이더가 없는 쪽을 고른다.</b>
        /// 층은 Floor 와 Guide 두 타일맵을 갖는데 Guide 에만 콜라이더가 붙는다.
        /// 그냥 첫 번째를 집으면 벽 타일맵이 잡혀 카메라 경계가 엉뚱해진다.
        /// </summary>
        private Tilemap FindFloorTilemap()
        {
            Tilemap[] found = GetComponentsInChildren<Tilemap>(includeInactive: true);

            foreach (Tilemap map in found)
                if (map != null && map.GetComponent<TilemapCollider2D>() == null)
                    return map;

            return found.Length > 0 ? found[0] : null;
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

        /// <summary>
        /// 에디터 점검용. 배치 중에는 계층이 계속 바뀌므로 <b>매번 다시 스캔한다.</b>
        /// 런타임 경로(<see cref="ResolveArrival"/>)는 캐시를 그대로 쓴다.
        /// </summary>
        public ArrivalPoint[] AllArrivals
        {
            get
            {
                Resolve(force: !Application.isPlaying);
                return arrivals;
            }
        }
    }
}
