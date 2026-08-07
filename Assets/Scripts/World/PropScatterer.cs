using System.Collections.Generic;
using PrettyKnights.Data;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PrettyKnights.World
{
    /// <summary>
    /// 오브젝트를 어디에 놓을지 <b>계산만 한다.</b> 생성은 하지 않는다.
    ///
    /// 에디터 미리보기와 런타임 생성이 **같은 결과**를 내야 하므로 계산을 한 곳에 둔다.
    /// 두 벌이 되면 에디터에서 본 배치와 실제 배치가 달라진다.
    ///
    /// <b>시드가 같으면 결과가 같다.</b> 이 성질이 두 가지를 동시에 준다 —
    /// 탈출했다 돌아와도 지형이 그대로이고, 파괴 상태를 <b>생성 순서 인덱스</b>로
    /// 저장할 수 있다. i번째 항목은 언제나 같은 것이기 때문이다.
    /// </summary>
    public static class PropScatterer
    {
        /// <summary>오브젝트가 차지하는 칸. 128px · PPU 64 라 2 × 2 다.</summary>
        public static readonly Vector2Int Footprint = new Vector2Int(2, 2);

        public readonly struct Placement
        {
            public readonly PropDefinition Definition;
            public readonly Vector2 Position;

            public Placement(PropDefinition definition, Vector2 position)
            {
                Definition = definition;
                Position = position;
            }
        }

        /// <summary>
        /// 배치를 계산한다. 결과 순서가 곧 인덱스이므로 <b>순서를 바꾸면 세이브가 깨진다.</b>
        ///
        /// 메인 토템을 먼저 놓는다. 나중에 놓으면 잡동사니가 좋은 자리를 다 차지한 뒤라
        /// 층 한구석에 몰린다.
        /// </summary>
        public static List<Placement> Plan(
            WalkableArea walkable, FloorScatterProfile profile, int seed,
            IReadOnlyList<NoSpawnZone> zones, IReadOnlyList<Vector2> avoid)
        {
            var result = new List<Placement>();

            if (walkable == null || walkable.Floor == null || profile == null) return result;

            List<Vector3Int> cells = CollectFloorCells(walkable.Floor);
            if (cells.Count == 0) return result;

            var random = new System.Random(seed);
            var placed = new List<Vector2>();

            foreach (FloorScatterProfile.Entry entry in Ordered(profile))
            {
                if (entry.definition == null || entry.count <= 0) continue;

                int got = 0;

                // 자리 찾기는 확률적이라 실패할 수 있다. 시도 상한을 둔다.
                int attempts = entry.count * 200 + 200;

                while (got < entry.count && attempts-- > 0)
                {
                    Vector3Int cell = cells[random.Next(cells.Count)];
                    Vector2 point = walkable.Floor.GetCellCenterWorld(cell);

                    if (!IsPlaceable(point, walkable, profile, zones, avoid, placed)) continue;

                    placed.Add(point);
                    result.Add(new Placement(entry.definition, point));
                    got++;
                }
            }

            return result;
        }

        /// <summary>메인 토템 → 서브 토템 → 나머지. 좋은 자리를 토템이 먼저 가져간다.</summary>
        private static IEnumerable<FloorScatterProfile.Entry> Ordered(FloorScatterProfile profile)
        {
            foreach (FloorScatterProfile.Entry e in profile.Entries)
                if (Role(e) == PropRole.MainTotem) yield return e;

            foreach (FloorScatterProfile.Entry e in profile.Entries)
                if (Role(e) == PropRole.SubTotem) yield return e;

            foreach (FloorScatterProfile.Entry e in profile.Entries)
                if (Role(e) != PropRole.MainTotem && Role(e) != PropRole.SubTotem) yield return e;
        }

        private static PropRole Role(FloorScatterProfile.Entry entry) =>
            entry.definition != null ? entry.definition.Role : PropRole.Decoration;

        /// <summary>이 자리에 놓아도 되는가. 싼 검사부터 순서대로 거른다.</summary>
        private static bool IsPlaceable(
            Vector2 point, WalkableArea walkable, FloorScatterProfile profile,
            IReadOnlyList<NoSpawnZone> zones, IReadOnlyList<Vector2> avoid, List<Vector2> placed)
        {
            // 2×2 칸 전부가 바닥이어야 한다. 점 하나만 보면 모서리가 벽에 걸친다.
            if (!walkable.IsAreaWalkable(point, Footprint)) return false;

            // 벽에서 띄운다. 벽에 붙으면 통로가 실질적으로 좁아진다.
            if (profile.WallClearance > 0f && !HasClearance(point, walkable, profile.WallClearance))
                return false;

            // 히든 방 안은 비워 둔다. 손으로 꾸미는 공간이다.
            if (NoSpawnZone.BlocksPropAt(point, zones)) return false;

            float protectedSqr = profile.ProtectedRadius * profile.ProtectedRadius;
            for (int i = 0; i < avoid.Count; i++)
                if ((avoid[i] - point).sqrMagnitude < protectedSqr) return false;

            float spacingSqr = profile.MinSpacing * profile.MinSpacing;
            for (int i = 0; i < placed.Count; i++)
                if ((placed[i] - point).sqrMagnitude < spacingSqr) return false;

            return true;
        }

        private static bool HasClearance(Vector2 point, WalkableArea walkable, float radius)
        {
            int steps = Mathf.CeilToInt(radius);

            for (int dy = -steps; dy <= steps; dy++)
            {
                for (int dx = -steps; dx <= steps; dx++)
                {
                    var offset = new Vector2(dx, dy);
                    if (offset.sqrMagnitude > radius * radius) continue;
                    if (!walkable.IsWalkable(point + offset)) return false;
                }
            }

            return true;
        }

        private static List<Vector3Int> CollectFloorCells(Tilemap floor)
        {
            var cells = new List<Vector3Int>();
            floor.CompressBounds();

            foreach (Vector3Int cell in floor.cellBounds.allPositionsWithin)
                if (floor.HasTile(cell)) cells.Add(cell);

            return cells;
        }

        /// <summary>
        /// 피해야 할 지점을 모은다. 도착 지점 · 포탈 · <b>손으로 놓은 오브젝트</b>.
        /// 자동 생성분은 매번 새로 만들어지므로 회피 대상이 아니다.
        /// </summary>
        public static List<Vector2> CollectAvoidPoints(AreaAnchor anchor, Transform generatedRoot)
        {
            var avoid = new List<Vector2>();
            if (anchor == null) return avoid;

            foreach (ArrivalPoint arrival in anchor.AllArrivals)
                if (arrival != null) avoid.Add(arrival.Position);

            foreach (Portal portal in anchor.GetComponentsInChildren<Portal>(includeInactive: true))
                if (portal != null) avoid.Add(portal.transform.position);

            foreach (Destructible manual in anchor.GetComponentsInChildren<Destructible>(includeInactive: true))
            {
                if (generatedRoot != null && manual.transform.IsChildOf(generatedRoot)) continue;
                avoid.Add(manual.transform.position);
            }

            return avoid;
        }
    }
}
