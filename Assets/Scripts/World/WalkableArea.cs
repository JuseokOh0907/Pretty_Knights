using PrettyKnights.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PrettyKnights.World
{
    /// <summary>
    /// 그 구역에서 실제로 설 수 있는 자리를 판정한다.
    /// 스폰 위치 · 순간이동 목적지 · 포탈 도착 지점이 전부 이걸 거친다.
    ///
    /// <b>"벽과 겹치지 않는다"로 판정하면 안 된다.</b>
    /// 맵 바깥이나 칠하지 않은 구멍도 벽 콜라이더가 없어 통과해 버린다.
    /// <c>cellBounds</c> 는 직사각형이라 실제 바닥보다 훨씬 넓다
    /// (Goblin 1F 는 직사각형 23,814칸 중 바닥이 12,147칸뿐이다).
    ///
    /// 그래서 <b>"여기에 바닥 타일이 있는가"</b> 라는 긍정형으로 판정한다.
    /// 물리 쿼리가 아니라 딕셔너리 조회라 훨씬 싸다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WalkableArea : MonoBehaviour
    {
        [Header("타일맵")]
        [SerializeField, Tooltip("바닥. 여기에 타일이 있어야 설 수 있다")]
        private Tilemap floor;

        [SerializeField, Tooltip("벽. 여기에 타일이 있으면 설 수 없다")]
        private Tilemap guide;

        [SerializeField, Tooltip(
            "부술 수 있는 벽(히든 방). 부수기 전까지는 벽이므로 여기도 봐야 한다. " +
            "빠뜨리면 히든 방 안에 몬스터가 스폰되고 도착 지점이 벽 안에 잡힌다. " +
            "부서지면 타일이 사라져 자동으로 통행 가능이 된다")]
        private Tilemap breakable;

        [Header("탐색")]
        [SerializeField, Min(1), Tooltip("무작위 지점을 몇 번까지 다시 뽑을지")]
        private int maxAttempts = 24;

        public Tilemap Floor => floor;

        /// <summary>벽. 점검 도구가 "바닥이 없다" 와 "벽 안이다" 를 갈라 보고하는 데 쓴다.</summary>
        public Tilemap Guide => guide;

        /// <summary>부술 수 있는 벽. 히든 방 입구를 막는다.</summary>
        public Tilemap Breakable => breakable;

        /// <summary>
        /// 설 수 없는 이유를 문장으로 돌려준다. 설 수 있으면 <c>null</c>.
        ///
        /// <b>"설 수 없다" 만으로는 어디를 고쳐야 할지 알 수 없다.</b>
        /// 바닥 타일이 없는 것과 벽 타일 위에 있는 것은 고치는 방법이 정반대다 —
        /// 전자는 바닥을 칠하거나 안쪽으로 옮기고, 후자는 벽에서 비켜야 한다.
        /// </summary>
        public string DescribeBlockage(Vector2 world)
        {
            if (floor == null) return "WalkableArea 의 Floor 가 비어 있음";

            if (!floor.HasTile(floor.WorldToCell(world)))
                return "그 칸에 바닥 타일이 없음";

            if (guide != null && guide.HasTile(guide.WorldToCell(world)))
                return "벽(Guide) 타일 위에 있음";

            if (breakable != null && breakable.HasTile(breakable.WorldToCell(world)))
                return "부술 수 있는 벽(히든 방) 위에 있음";

            return null;
        }

        private void OnEnable()
        {
            // 활성화된 구역이 곧 현재 구역이다. 층을 켜고 끄는 것만으로 교체된다.
            ServiceRegistry.Register(this);
        }

        private void OnDisable()
        {
            if (ServiceRegistry.TryGet(out WalkableArea current) && current == this)
                ServiceRegistry.Unregister<WalkableArea>();
        }

        /// <summary>이 자리에 설 수 있는가.</summary>
        public bool IsWalkable(Vector2 world)
        {
            if (floor == null) return false;

            Vector3Int cell = floor.WorldToCell(world);

            if (!floor.HasTile(cell)) return false;
            if (guide != null && guide.HasTile(guide.WorldToCell(world))) return false;

            // 부수기 전까지는 이것도 벽이다.
            if (breakable != null && breakable.HasTile(breakable.WorldToCell(world))) return false;

            return true;
        }

        /// <summary>
        /// 가로 <paramref name="size"/> 칸이 전부 설 수 있는 자리인가.
        ///
        /// <b>오브젝트는 2 × 2칸을 차지한다.</b> <see cref="IsWalkable"/> 는 점 하나만 보므로
        /// 오브젝트 배치에 쓰면 모서리가 벽에 걸친 채로 놓인다.
        /// <paramref name="center"/> 는 오브젝트의 중심이다.
        /// </summary>
        public bool IsAreaWalkable(Vector2 center, Vector2Int size)
        {
            if (floor == null) return false;

            // 중심에서 좌우·상하로 반씩 펼친다. 2×2 면 (-1,-1)~(0,0) 네 칸이다.
            int minX = -(size.x / 2);
            int minY = -(size.y / 2);

            for (int dy = 0; dy < size.y; dy++)
            {
                for (int dx = 0; dx < size.x; dx++)
                {
                    var offset = new Vector2(minX + dx, minY + dy);
                    if (!IsWalkable(center + offset)) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// <paramref name="origin"/> 주변 <paramref name="radius"/> 안에서
        /// 설 수 있는 자리를 찾는다. 스폰과 순간이동이 함께 쓴다.
        /// </summary>
        public bool TryFindWalkable(Vector2 origin, float radius, out Vector2 result)
        {
            // 원점이 이미 유효하면 그대로 쓴다.
            if (IsWalkable(origin))
            {
                result = origin;
                return true;
            }

            for (int i = 0; i < maxAttempts; i++)
            {
                Vector2 candidate = origin + Random.insideUnitCircle * radius;
                if (!IsWalkable(candidate)) continue;

                result = SnapToCellCenter(candidate);
                return true;
            }

            result = origin;
            return false;
        }

        /// <summary>
        /// 칸 한가운데로 맞춘다. 칸 경계에 걸치면 콜라이더에 끼일 수 있다.
        /// </summary>
        public Vector2 SnapToCellCenter(Vector2 world)
        {
            if (floor == null) return world;

            Vector3Int cell = floor.WorldToCell(world);
            return floor.GetCellCenterWorld(cell);
        }
    }
}
