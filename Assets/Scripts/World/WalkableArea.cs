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

        [Header("탐색")]
        [SerializeField, Min(1), Tooltip("무작위 지점을 몇 번까지 다시 뽑을지")]
        private int maxAttempts = 24;

        public Tilemap Floor => floor;

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
