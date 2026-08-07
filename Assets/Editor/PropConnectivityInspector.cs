using System.Collections.Generic;
using System.Linq;
using System.Text;
using PrettyKnights.Data;
using PrettyKnights.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PrettyKnights.EditorTools
{
    /// <summary>
    /// 뿌려 놓은 오브젝트가 길을 막지 않는지 검사한다.
    ///
    /// <b>배치와 분리한 이유</b> — 배치 중에 매번 flood fill 을 돌리면
    /// 오브젝트 하나 놓을 때마다 2만 칸을 훑게 된다. 따로 돌리면 비용도 한 번이고,
    /// "무엇이 무엇을 막는지" 까지 짚어줄 수 있다.
    ///
    /// <b>필수 지점만 본다</b> — 도착 지점 → 메인 토템 → 포탈.
    /// 히든 방은 필수 지점이 아니므로 오브젝트로 막혀 있어도 통과다.
    /// 그게 히든 방의 정의다 (docs/design/map-objects.md §4).
    ///
    /// 길 찾기는 가중 탐색이다. 자동 배치분은 <b>비싸지만 지나갈 수 있는</b> 칸으로 두고,
    /// 최소 비용 경로가 그 칸들을 지나면 <b>그게 곧 치워야 할 목록</b>이 된다.
    /// 하나씩 빼보며 다시 검사하는 것보다 한 번에 답이 나온다.
    /// </summary>
    public static class PropConnectivityInspector
    {
        private const string MenuRoot = "Pretty Knights/Props/";

        /// <summary>자동 배치분을 지나는 비용. 빈 칸 1 에 비해 압도적으로 크면 된다.</summary>
        private const int AutoPropCost = 10000;

        [MenuItem(MenuRoot + "3. 연결성 검사 (변경 없음)", priority = 503)]
        public static void Inspect() => Run(fix: false);

        [MenuItem(MenuRoot + "4. 길을 막는 자동 배치분 치우기", priority = 504)]
        public static void Fix() => Run(fix: true);

        private static void Run(bool fix)
        {
            AreaAnchor[] anchors = Object.FindObjectsByType<AreaAnchor>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (anchors.Length == 0)
            {
                Debug.LogWarning("[PropConnectivity] 열린 씬에서 AreaAnchor 를 찾지 못했습니다.");
                return;
            }

            var report = new StringBuilder(
                fix ? "[PropConnectivity] 막는 것 치우기\n" : "[PropConnectivity] 검사 (변경 없음)\n");

            int problems = 0;
            int removed = 0;

            foreach (AreaAnchor anchor in anchors.OrderBy(a => a.AreaId))
            {
                anchor.Resolve(force: true);

                if (anchor.Walkable == null || anchor.Walkable.Floor == null) continue;
                if (anchor.AllArrivals == null || anchor.AllArrivals.Length == 0) continue;

                problems += InspectFloor(anchor, fix, report, ref removed);
            }

            report.AppendLine();

            if (problems == 0) report.Append("  전부 도달 가능합니다.");
            else if (fix) report.Append($"  문제 {problems}건 · 오브젝트 {removed}개를 치웠습니다. 씬을 저장하세요.");
            else report.Append($"  문제 {problems}건. \"4. 길을 막는 자동 배치분 치우기\" 로 자동 정리할 수 있습니다.");

            if (problems == 0) Debug.Log(report.ToString());
            else Debug.LogWarning(report.ToString());
        }

        private static int InspectFloor(
            AreaAnchor anchor, bool fix, StringBuilder report, ref int removed)
        {
            Tilemap floor = anchor.Walkable.Floor;
            Tilemap guide = anchor.Walkable.Guide;

            // 1) 통행 가능한 칸. 바닥이 있고 벽이 없는 곳.
            var open = new HashSet<Vector3Int>();
            floor.CompressBounds();

            foreach (Vector3Int cell in floor.cellBounds.allPositionsWithin)
            {
                if (!floor.HasTile(cell)) continue;
                if (guide != null && guide.HasTile(cell)) continue;
                open.Add(cell);
            }

            if (open.Count == 0)
            {
                report.AppendLine($"  ✗ #{anchor.AreaId} — 통행 가능한 칸이 하나도 없습니다");
                return 1;
            }

            // 2) 오브젝트가 덮은 칸. 자동 배치분은 치울 수 있고 수동 배치분은 못 치운다.
            Transform container = anchor.transform.Find(PropScatterTool.ContainerName);

            var autoCells = new Dictionary<Vector3Int, GameObject>();
            var manualCells = new HashSet<Vector3Int>();

            foreach (Collider2D collider in anchor.GetComponentsInChildren<Collider2D>(includeInactive: true))
            {
                if (collider == null || collider.isTrigger) continue;

                // 타일맵 콜라이더는 위에서 이미 벽으로 처리했다.
                if (collider is TilemapCollider2D || collider is CompositeCollider2D) continue;

                bool isAuto = container != null && collider.transform.IsChildOf(container);
                GameObject owner = FindPropRoot(collider.transform, container);

                foreach (Vector3Int cell in CellsUnder(collider, floor))
                {
                    if (!open.Contains(cell)) continue;

                    if (isAuto) autoCells[cell] = owner;
                    else manualCells.Add(cell);
                }
            }

            // 3) 반드시 도달해야 하는 지점
            var starts = anchor.AllArrivals
                .Where(a => a != null)
                .Select(a => floor.WorldToCell(a.Position))
                .Where(open.Contains)
                .ToList();

            if (starts.Count == 0)
            {
                report.AppendLine($"  ✗ #{anchor.AreaId} — 도착 지점이 전부 통행 불가 칸 위에 있습니다");
                return 1;
            }

            var targets = new List<(string label, Vector3Int cell)>();

            foreach (SpawnTotem totem in anchor.GetComponentsInChildren<SpawnTotem>(includeInactive: true))
                if (totem != null) targets.Add(($"토템 '{totem.name}'", floor.WorldToCell(totem.transform.position)));

            foreach (Portal portal in anchor.GetComponentsInChildren<Portal>(includeInactive: true))
                if (portal != null) targets.Add(($"포탈 '{portal.name}'", floor.WorldToCell(portal.transform.position)));

            // 도착 지점끼리도 서로 오갈 수 있어야 한다.
            for (int i = 1; i < starts.Count; i++) targets.Add(("도착 지점", starts[i]));

            if (targets.Count == 0)
            {
                report.AppendLine($"  · #{anchor.AreaId} — 확인할 필수 지점이 없습니다 (토템·포탈 미배치)");
                return 0;
            }

            // 4) 가중 탐색. 자동 배치분은 비싸지만 지나갈 수 있다.
            Dictionary<Vector3Int, int> cost = Search(open, autoCells.Keys.ToHashSet(), manualCells, starts);

            int problems = 0;
            var toRemove = new HashSet<GameObject>();

            foreach ((string label, Vector3Int cell) in targets)
            {
                if (!cost.TryGetValue(cell, out int c))
                {
                    report.AppendLine(
                        $"  ✗ #{anchor.AreaId} {label} — 도달 불가. " +
                        "벽 또는 손으로 놓은 오브젝트가 막고 있어 자동으로 치울 수 없습니다");
                    problems++;
                    continue;
                }

                if (c < AutoPropCost) continue;

                int blockers = c / AutoPropCost;
                report.AppendLine(
                    $"  △ #{anchor.AreaId} {label} — 자동 배치분 {blockers}개를 지나야 닿습니다");
                problems++;

                if (!fix) continue;

                foreach (Vector3Int blocked in TracePath(cost, cell, starts))
                    if (autoCells.TryGetValue(blocked, out GameObject owner) && owner != null)
                        toRemove.Add(owner);
            }

            if (fix && toRemove.Count > 0)
            {
                foreach (GameObject go in toRemove)
                {
                    report.AppendLine($"      ↳ '{go.name}' 제거");
                    Undo.DestroyObjectImmediate(go);
                    removed++;
                }
            }

            if (problems == 0)
                report.AppendLine($"  · #{anchor.AreaId} — 필수 지점 {targets.Count}곳 전부 도달 가능 " +
                                  $"(통행 {open.Count}칸)");

            return problems;
        }

        /// <summary>
        /// 다익스트라. 자동 배치분이 덮은 칸은 <see cref="AutoPropCost"/> 를 문다.
        /// 최소 비용 경로가 그 칸을 지나면 그 오브젝트가 곧 병목이다.
        /// </summary>
        private static Dictionary<Vector3Int, int> Search(
            HashSet<Vector3Int> open, HashSet<Vector3Int> auto, HashSet<Vector3Int> manual,
            List<Vector3Int> starts)
        {
            var cost = new Dictionary<Vector3Int, int>();
            var frontier = new SortedSet<(int cost, int x, int y)>();

            foreach (Vector3Int start in starts)
            {
                if (manual.Contains(start)) continue;
                cost[start] = 0;
                frontier.Add((0, start.x, start.y));
            }

            Vector3Int[] steps =
            {
                new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
                new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0)
            };

            while (frontier.Count > 0)
            {
                (int c, int x, int y) = frontier.Min;
                frontier.Remove(frontier.Min);

                var here = new Vector3Int(x, y, 0);
                if (cost.TryGetValue(here, out int known) && c > known) continue;

                foreach (Vector3Int step in steps)
                {
                    Vector3Int next = here + step;

                    if (!open.Contains(next)) continue;
                    if (manual.Contains(next)) continue;   // 손으로 놓은 것은 못 치운다

                    int stepCost = auto.Contains(next) ? AutoPropCost : 1;
                    int total = c + stepCost;

                    if (cost.TryGetValue(next, out int old) && old <= total) continue;

                    cost[next] = total;
                    frontier.Add((total, next.x, next.y));
                }
            }

            return cost;
        }

        /// <summary>비용 지도를 거슬러 올라가며 지나온 칸을 모은다.</summary>
        private static IEnumerable<Vector3Int> TracePath(
            Dictionary<Vector3Int, int> cost, Vector3Int from, List<Vector3Int> starts)
        {
            var path = new List<Vector3Int>();
            var startSet = new HashSet<Vector3Int>(starts);
            Vector3Int here = from;

            Vector3Int[] steps =
            {
                new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
                new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0)
            };

            // 무한 루프 방지. 경로가 맵 전체보다 길 수는 없다.
            for (int guard = 0; guard < 100000; guard++)
            {
                path.Add(here);
                if (startSet.Contains(here)) break;
                if (!cost.TryGetValue(here, out int c)) break;

                Vector3Int best = here;
                int bestCost = c;

                foreach (Vector3Int step in steps)
                {
                    Vector3Int next = here + step;
                    if (!cost.TryGetValue(next, out int nc) || nc >= bestCost) continue;

                    bestCost = nc;
                    best = next;
                }

                if (best == here) break;
                here = best;
            }

            return path;
        }

        /// <summary>콜라이더가 덮는 칸들.</summary>
        private static IEnumerable<Vector3Int> CellsUnder(Collider2D collider, Tilemap floor)
        {
            Bounds bounds = collider.bounds;

            Vector3Int min = floor.WorldToCell(bounds.min);
            Vector3Int max = floor.WorldToCell(bounds.max);

            for (int y = min.y; y <= max.y; y++)
                for (int x = min.x; x <= max.x; x++)
                    yield return new Vector3Int(x, y, 0);
        }

        /// <summary>콜라이더가 자식에 있을 수 있으므로 컨테이너 바로 아래까지 올라간다.</summary>
        private static GameObject FindPropRoot(Transform from, Transform container)
        {
            if (container == null) return from.gameObject;

            Transform here = from;
            while (here != null && here.parent != container) here = here.parent;

            return here != null ? here.gameObject : from.gameObject;
        }
    }
}
