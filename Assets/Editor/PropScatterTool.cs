using System.Collections.Generic;
using System.Linq;
using System.Text;
using PrettyKnights.Data;
using PrettyKnights.World;
using UnityEditor;
using UnityEngine;

namespace PrettyKnights.EditorTools
{
    /// <summary>
    /// 층에 맵 오브젝트를 뿌린다. <b>에디터 베이크</b>이며 런타임 랜덤이 아니다.
    ///
    /// 탈출 스킬로 나갔다 돌아왔을 때 지형이 바뀌면 길을 처음부터 다시 찾아야 한다.
    /// 오브젝트가 통행 경로를 만드는 이상 배치는 그 층의 지형 그 자체다
    /// (docs/design/map-objects.md §4).
    ///
    /// <b>손으로 배치한 것은 건드리지 않는다.</b> 도구는 자기가 만든
    /// <c>AutoProps</c> 컨테이너만 관리하고, 그 밖의 오브젝트는 회피 대상으로만 읽는다.
    /// 히든 방 입구처럼 의도적으로 막아둔 배치를 지우면 설계가 깨진다.
    /// </summary>
    public static class PropScatterTool
    {
        private const string MenuRoot = "Pretty Knights/Props/";
        internal const string ContainerName = "AutoProps";

        /// <summary>오브젝트가 차지하는 칸. 128px · PPU 64 라 2 × 2 다.</summary>
        private static readonly Vector2Int PropFootprint = new Vector2Int(2, 2);

        // ── 점검 ──────────────────────────────────────────────────────────

        [MenuItem(MenuRoot + "0. 배치 미리보기 (변경 없음)", priority = 500)]
        public static void Preview() => Run(dryRun: true);

        [MenuItem(MenuRoot + "1. 오브젝트 뿌리기", priority = 501)]
        public static void Scatter() => Run(dryRun: false);

        [MenuItem(MenuRoot + "2. 자동 배치분만 지우기", priority = 502)]
        public static void ClearGenerated()
        {
            AreaAnchor[] anchors = Object.FindObjectsByType<AreaAnchor>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            int removed = 0;

            foreach (AreaAnchor anchor in anchors)
            {
                Transform container = anchor.transform.Find(ContainerName);
                if (container == null) continue;

                removed += container.childCount;
                Undo.DestroyObjectImmediate(container.gameObject);
            }

            Debug.Log($"[PropScatter] 자동 배치분 {removed}개를 지웠습니다. 손으로 놓은 것은 그대로입니다.");
        }

        // ── 본체 ──────────────────────────────────────────────────────────

        private static void Run(bool dryRun)
        {
            AreaAnchor[] anchors = Object.FindObjectsByType<AreaAnchor>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (anchors.Length == 0)
            {
                Debug.LogWarning("[PropScatter] 열린 씬에서 AreaAnchor 를 찾지 못했습니다.");
                return;
            }

            var report = new StringBuilder(dryRun ? "[PropScatter] 미리보기 (변경 없음)\n" : "[PropScatter] 배치\n");
            int totalPlaced = 0;

            NoSpawnZone[] zones = Object.FindObjectsByType<NoSpawnZone>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (AreaAnchor anchor in anchors.OrderBy(a => a.AreaId))
            {
                anchor.Resolve(force: true);

                AreaDefinition definition = anchor.Definition;
                FloorScatterProfile profile = definition != null ? definition.ScatterProfile : null;

                if (profile == null) continue;

                if (anchor.Walkable == null || anchor.Walkable.Floor == null)
                {
                    report.AppendLine($"  ✗ #{anchor.AreaId} '{anchor.name}' — WalkableArea 또는 Floor 가 없어 건너뜁니다");
                    continue;
                }

                int placed = ScatterFloor(anchor, definition, profile, zones, dryRun, report);
                totalPlaced += placed;
            }

            if (totalPlaced == 0) report.AppendLine("  놓을 것이 없었습니다. AreaDefinition 의 Scatter Profile 을 확인하세요.");

            report.AppendLine();
            report.Append(dryRun
                ? "  실제로 놓으려면 \"1. 오브젝트 뿌리기\" 를 실행하세요."
                : $"  총 {totalPlaced}개를 놓았습니다. 씬을 저장하세요.");

            Debug.Log(report.ToString());
        }

        private static int ScatterFloor(
            AreaAnchor anchor, AreaDefinition definition, FloorScatterProfile profile,
            NoSpawnZone[] zones, bool dryRun, StringBuilder report)
        {
            WalkableArea walkable = anchor.Walkable;
            var floorCells = CollectFloorCells(anchor);

            if (floorCells.Count == 0)
            {
                report.AppendLine($"  ✗ #{anchor.AreaId} — 바닥 타일이 하나도 없습니다");
                return 0;
            }

            // 1) 피해야 할 지점 — 도착 지점 · 포탈 · 손으로 놓은 오브젝트
            var avoid = new List<Vector2>();

            foreach (ArrivalPoint arrival in anchor.AllArrivals)
                if (arrival != null) avoid.Add(arrival.Position);

            foreach (Portal portal in anchor.GetComponentsInChildren<Portal>(includeInactive: true))
                if (portal != null) avoid.Add(portal.transform.position);

            Transform container = anchor.transform.Find(ContainerName);

            foreach (Destructible manual in anchor.GetComponentsInChildren<Destructible>(includeInactive: true))
            {
                // 자동 배치분은 지우고 다시 놓으므로 회피 대상이 아니다.
                if (container != null && manual.transform.IsChildOf(container)) continue;
                avoid.Add(manual.transform.position);
            }

            // 2) 기존 자동 배치분을 걷어낸다. 손으로 놓은 것은 그대로 둔다.
            if (!dryRun && container != null) Undo.DestroyObjectImmediate(container.gameObject);

            // 3) 순서가 중요하다 — 메인 토템을 먼저 놓아야 좋은 자리를 차지한다.
            var queue = new List<FloorScatterProfile.Entry>();
            queue.AddRange(profile.Entries.Where(e => Role(e) == PropRole.MainTotem));
            queue.AddRange(profile.Entries.Where(e => Role(e) == PropRole.SubTotem));
            queue.AddRange(profile.Entries.Where(e => Role(e) != PropRole.MainTotem && Role(e) != PropRole.SubTotem));

            var random = new System.Random(profile.Seed + anchor.AreaId);
            var placedPoints = new List<Vector2>();

            Transform newContainer = null;
            if (!dryRun)
            {
                var go = new GameObject(ContainerName);
                Undo.RegisterCreatedObjectUndo(go, "오브젝트 뿌리기");
                go.transform.SetParent(anchor.transform, worldPositionStays: false);
                newContainer = go.transform;
            }

            int placedHere = 0;

            foreach (FloorScatterProfile.Entry entry in queue)
            {
                if (entry.definition == null || entry.prefab == null || entry.count <= 0) continue;

                int want = entry.count;
                int got = 0;

                // 자리 찾기는 확률적이라 실패할 수 있다. 시도 상한을 둔다.
                int attempts = want * 200 + 200;

                while (got < want && attempts-- > 0)
                {
                    Vector3Int cell = floorCells[random.Next(floorCells.Count)];
                    Vector2 point = walkable.Floor.GetCellCenterWorld(cell);

                    if (!IsPlaceable(point, walkable, profile, zones, avoid, placedPoints)) continue;

                    placedPoints.Add(point);
                    got++;
                    placedHere++;

                    if (dryRun) continue;

                    GameObject instance = Place(entry, point, newContainer);

                    if (Role(entry) == PropRole.MainTotem)
                        AttachPortal(instance, definition, profile, newContainer);
                }

                if (got < want)
                {
                    report.AppendLine(
                        $"  △ #{anchor.AreaId} '{entry.definition.DisplayName}' — {want}개 중 {got}개만 놓았습니다. " +
                        "간격이나 보호 반경을 줄이거나 개수를 낮추세요");
                }
            }

            report.AppendLine($"  · #{anchor.AreaId} {definition.DisplayName} — {placedHere}개 " +
                              $"(바닥 {floorCells.Count}칸)");

            return placedHere;
        }

        private static PropRole Role(FloorScatterProfile.Entry entry) =>
            entry.definition != null ? entry.definition.Role : PropRole.Decoration;

        /// <summary>이 자리에 놓아도 되는가. 실패 사유가 여럿이라 순서대로 걸러낸다.</summary>
        private static bool IsPlaceable(
            Vector2 point, WalkableArea walkable, FloorScatterProfile profile,
            NoSpawnZone[] zones, List<Vector2> avoid, List<Vector2> placed)
        {
            // 2×2 칸 전부가 바닥이어야 한다. 점 하나만 보면 모서리가 벽에 걸친다.
            if (!walkable.IsAreaWalkable(point, PropFootprint)) return false;

            // 벽에서 띄운다. 벽에 붙으면 통로가 실질적으로 좁아진다.
            if (profile.WallClearance > 0f && !HasClearance(point, walkable, profile.WallClearance))
                return false;

            // 히든 방 안은 비워 둔다. 손으로 꾸미는 공간이다.
            if (NoSpawnZone.BlocksPropAt(point, zones)) return false;

            // 도착 지점·포탈·수동 배치 주변은 막지 않는다.
            float protectedSqr = profile.ProtectedRadius * profile.ProtectedRadius;
            foreach (Vector2 p in avoid)
                if ((p - point).sqrMagnitude < protectedSqr) return false;

            float spacingSqr = profile.MinSpacing * profile.MinSpacing;
            foreach (Vector2 p in placed)
                if ((p - point).sqrMagnitude < spacingSqr) return false;

            return true;
        }

        /// <summary>주변 <paramref name="radius"/> 안이 전부 바닥인가. 벽에서 띄우는 데 쓴다.</summary>
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

        private static GameObject Place(FloorScatterProfile.Entry entry, Vector2 point, Transform parent)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(entry.prefab, parent);
            instance.transform.position = point;
            instance.name = entry.definition.PropId;

            Undo.RegisterCreatedObjectUndo(instance, "오브젝트 뿌리기");
            return instance;
        }

        /// <summary>
        /// 메인 토템 자리에 <b>꺼진 포탈</b>을 함께 만든다.
        ///
        /// 런타임 생성으로 미루면 <c>포탈 링크 점검</c> 이 부숴보기 전에 오타를 잡아주던 것이
        /// 사라진다. 그 점검은 비활성 포탈까지 훑으므로 꺼진 채로 두어도 검사된다.
        /// </summary>
        private static void AttachPortal(
            GameObject totem, AreaDefinition definition, FloorScatterProfile profile, Transform parent)
        {
            if (profile.PortalPrefab == null)
            {
                Debug.LogError(
                    $"[PropScatter] '{definition.DisplayName}' 의 프로필에 Portal Prefab 이 없습니다. " +
                    "메인 토템을 부숴도 다음 층으로 갈 길이 생기지 않습니다.");
                return;
            }

            if (!definition.HasNextArea)
            {
                Debug.LogError(
                    $"[PropScatter] '{definition.DisplayName}' 의 Next Area 가 비어 있습니다. " +
                    "메인 토템이 열 포탈의 목적지를 정할 수 없습니다.");
                return;
            }

            var portal = (GameObject)PrefabUtility.InstantiatePrefab(profile.PortalPrefab, parent);
            portal.transform.position = totem.transform.position;
            portal.name = $"Portal_from_{definition.AreaId}";
            Undo.RegisterCreatedObjectUndo(portal, "오브젝트 뿌리기");

            var portalComponent = portal.GetComponent<Portal>();
            if (portalComponent != null)
            {
                var so = new SerializedObject(portalComponent);
                so.FindProperty("destination").objectReferenceValue = definition.NextArea;
                so.FindProperty("destinationArrivalId").stringValue = definition.NextArrivalId;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // 토템을 부술 때까지 보이지 않는다.
            portal.SetActive(false);

            var totemComponent = totem.GetComponent<SpawnTotem>();
            if (totemComponent == null) return;

            var totemSo = new SerializedObject(totemComponent);
            totemSo.FindProperty("portalToOpen").objectReferenceValue = portal;
            totemSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static List<Vector3Int> CollectFloorCells(AreaAnchor anchor)
        {
            var cells = new List<Vector3Int>();
            var floor = anchor.Walkable.Floor;

            floor.CompressBounds();

            foreach (Vector3Int cell in floor.cellBounds.allPositionsWithin)
                if (floor.HasTile(cell)) cells.Add(cell);

            return cells;
        }
    }
}
