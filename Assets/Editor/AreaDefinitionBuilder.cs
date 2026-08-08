using System.Collections.Generic;
using System.IO;
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
    /// <see cref="AreaDefinition"/> 13개와 <see cref="FloorScatterProfile"/> 9개를 만든다.
    ///
    /// <b>손으로 만들면 안 되는 이유는 번호와 링크다.</b> 구역은 areaId(숫자)와
    /// arrivalId(문자열)로만 이어져 있어 오타를 컴파일러가 잡지 못하고,
    /// 잘못된 링크는 그 포탈을 실제로 밟기 전까지 드러나지 않는다.
    /// 13개 × (다음 층 · 탈출 · 배치 프로필)을 인스펙터로 채우면 반드시 하나는 어긋난다.
    ///
    /// <b>배치 개수는 표에 박지 않고 씬에서 잰다.</b> 층마다 바닥 넓이가
    /// 1,950칸부터 20,035칸까지 10배 넘게 차이 나므로 고정 개수는 의미가 없다.
    /// 밀도(칸/개)만 상수로 두고 실측 칸 수로 나눈다 — 이 밀도는
    /// 이미 검증된 Goblin 3층의 개수를 그대로 재현하도록 맞춘 값이다.
    ///
    /// <code>
    /// Goblin 1F  12,147칸 / 145 = 84   (기존 83)
    /// Goblin 2F  20,035칸 / 145 = 138  (기존 139)
    /// Goblin 3F   5,742칸 / 287 = 20   (기존 20)
    /// </code>
    ///
    /// <b>이미 있는 배치 프로필은 건드리지 않는다.</b> 개수·시드·간격은 손으로 다듬는 값이고,
    /// 덮어쓰면 받아들인 배치가 조용히 다시 뽑힌다. 비어 있는 프리팹 칸만 채운다.
    /// </summary>
    public static class AreaDefinitionBuilder
    {
        private const string MenuRoot = "Pretty Knights/Areas/";
        private const string AreaFolder = "Assets/Data/Areas";
        private const string ScatterFolder = "Assets/Data/Scatter";

        private const string PropPrefabPath = "Assets/Prefabs/Props/Prop.prefab";
        private const string BluePortalPath = "Assets/Prefabs/Portals/Blue_Portal.prefab";
        private const string RedPortalPath = "Assets/Prefabs/Portals/Red_Portal.prefab";
        private const string GoldPortalPath = "Assets/Prefabs/Portals/Gold_Portal.prefab";

        /// <summary>일반 층의 밀도. 바닥 이만큼마다 오브젝트 하나.</summary>
        private const int CellsPerPropNormal = 145;

        /// <summary>보스 층은 절반 밀도. 넓게 트여 있어야 보스 패턴을 피할 수 있다.</summary>
        private const int CellsPerPropBoss = 287;

        /// <summary>파괴용 오브젝트 4종의 배분. 앞에서부터 propId 오름차순이다.</summary>
        private static readonly int[] NormalWeights = { 25, 19, 31, 25 };
        private static readonly int[] BossWeights = { 30, 20, 25, 25 };

        // ── 표 ────────────────────────────────────────────────────────────

        private readonly struct Row
        {
            public readonly int AreaId;
            public readonly string Name;
            public readonly string DisplayName;
            public readonly string Theme;
            public readonly bool IsBoss;
            public readonly bool IsReward;

            /// <summary>메인 토템을 부수면 열릴 포탈의 목적지. 0 이면 없다.</summary>
            public readonly int NextAreaId;
            public readonly string NextArrivalId;

            /// <summary>탈출 스킬이 데려다 놓을 곳. 0 이면 탈출 불가(거점).</summary>
            public readonly int EscapeToId;

            /// <summary>배치 프로필 이름. null 이면 자동 배치 대상이 아니다(거점·보상방).</summary>
            public readonly string Profile;

            public Row(int areaId, string name, string displayName, string theme,
                bool isBoss, bool isReward, int nextAreaId, string nextArrivalId,
                int escapeToId, string profile)
            {
                AreaId = areaId; Name = name; DisplayName = displayName; Theme = theme;
                IsBoss = isBoss; IsReward = isReward;
                NextAreaId = nextAreaId; NextArrivalId = nextArrivalId;
                EscapeToId = escapeToId; Profile = profile;
            }

            /// <summary>층 번호. 거점·보상방은 0 이다.</summary>
            public int Floor => AreaId < 100 || IsReward ? 0 : AreaId % 100;
        }

        /// <summary>던전 입구에서 탈출·귀환이 도착하는 지점 이름.</summary>
        private const string EscapeArrivalId = "from_escape";

        //                      #   파일 이름                표시명              테마        보스   보상   다음  다음 도착지     탈출  배치 프로필
        private static readonly Row[] Rows =
        {
            new Row(  3, "Area_Dungeon_Entrance", "던전 입구",        "Base",    false, false,   0, "",            0, null),

            new Row(101, "Area_Goblin_1F",     "고블린 소굴 1층",     "Goblin",  false, false, 102, "from_1f",     3, "Goblin_1F"),
            new Row(102, "Area_Goblin_2F",     "고블린 소굴 2층",     "Goblin",  false, false, 103, "from_2f",     3, "Goblin_2F"),
            new Row(103, "Area_Goblin_3F",     "고블린 소굴 3층",     "Goblin",  true,  false, 190, "from_boss",   3, "Goblin_3F"),
            new Row(190, "Area_Goblin_Reward", "고블린 보상방",       "Goblin",  false, true,    0, "",            3, null),

            new Row(201, "Area_Orc_1F",        "오크 주둔지 1층",     "Orc",     false, false, 202, "from_1f",     3, "Orc_1F"),
            new Row(202, "Area_Orc_2F",        "오크 주둔지 2층",     "Orc",     false, false, 203, "from_2f",     3, "Orc_2F"),
            new Row(203, "Area_Orc_3F",        "오크 주둔지 3층",     "Orc",     true,  false, 290, "from_boss",   3, "Orc_3F"),
            new Row(290, "Area_Orc_Reward",    "오크 보상방",         "Orc",     false, true,    0, "",            3, null),

            new Row(301, "Area_Vampire_1F",    "흡혈귀 성 1층",       "Vampire", false, false, 302, "from_1f",     3, "Vampire_1F"),
            new Row(302, "Area_Vampire_2F",    "흡혈귀 성 2층",       "Vampire", false, false, 303, "from_2f",     3, "Vampire_2F"),
            new Row(303, "Area_Vampire_3F",    "흡혈귀 성 3층",       "Vampire", true,  false, 390, "from_boss",   3, "Vampire_3F"),
            new Row(390, "Area_Vampire_Reward","흡혈귀 보상방",       "Vampire", false, true,    0, "",            3, null)
        };

        // ── 점검 ──────────────────────────────────────────────────────────

        [MenuItem(MenuRoot + "2. 구역 정의 점검 (변경 없음)", priority = 302)]
        public static void Report()
        {
            var report = new StringBuilder($"[AreaDefinition] 표 {Rows.Length}개\n");

            Dictionary<int, AreaDefinition> assets = LoadDefinitions();
            Dictionary<int, AreaAnchor> anchors = LoadAnchorsByName();

            report.AppendLine("  #     표시명            에셋  씬 앵커         바닥 칸수   권장 배치");
            report.AppendLine("  ────────────────────────────────────────────────────────────────");

            int missingAssets = 0, missingAnchors = 0, unmeasured = 0;

            foreach (Row row in Rows)
            {
                bool hasAsset = assets.ContainsKey(row.AreaId);
                if (!hasAsset) missingAssets++;

                anchors.TryGetValue(row.AreaId, out AreaAnchor anchor);
                if (anchor == null) missingAnchors++;

                int cells = CountFloorCells(anchor);
                if (anchor != null && cells == 0) unmeasured++;

                string counts = row.Profile == null
                    ? "(배치 없음)"
                    : cells > 0
                        ? $"{Plan(row, cells).Sum()}개"
                        : "잴 수 없음";

                report.AppendLine(
                    $"  #{row.AreaId,-4} {row.DisplayName,-16} {(hasAsset ? "있음" : "없음")}  " +
                    $"{(anchor != null ? anchor.name : "(못 찾음)"),-14} " +
                    $"{(cells > 0 ? cells.ToString("N0") : "-"),9}   {counts}");
            }

            report.AppendLine();
            report.Append(Summarize(missingAssets, missingAnchors, unmeasured, anchors));

            if (missingAssets == 0 && missingAnchors == 0) Debug.Log(report.ToString());
            else Debug.LogWarning(report.ToString());
        }

        private static string Summarize(
            int missingAssets, int missingAnchors, int unmeasured, Dictionary<int, AreaAnchor> anchors)
        {
            var text = new StringBuilder();

            if (missingAssets > 0)
                text.AppendLine($"  · 에셋 {missingAssets}개가 없습니다 — \"3. 생성/갱신\" 을 실행하세요.");

            if (anchors.Count == 0)
            {
                text.AppendLine(
                    "  · 씬에서 AreaAnchor 를 찾지 못했습니다. Ingame_Horizontal 을 열고 다시 실행하세요.\n" +
                    "    배치 개수는 씬의 바닥 칸 수로 계산하므로 씬이 열려 있어야 프로필을 만들 수 있습니다.");
                return text.ToString();
            }

            if (missingAnchors > 0)
                text.AppendLine(
                    $"  · 씬에서 짝을 못 찾은 구역이 {missingAnchors}개입니다. " +
                    "층 오브젝트 이름이 표와 다르거나 아직 만들지 않은 층입니다.");

            if (unmeasured > 0)
                text.AppendLine(
                    $"  · 바닥 칸을 세지 못한 구역이 {unmeasured}개입니다. " +
                    "WalkableArea 의 Floor 가 비었거나 타일을 아직 칠하지 않았습니다.");

            AreaAnchor[] blank = Object.FindObjectsByType<AreaAnchor>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(a => a.Definition == null)
                .ToArray();

            if (blank.Length > 0)
            {
                // 이게 비면 AreaRegistry 가 등록조차 하지 않아 포탈도 디버그 이동도 못 찾는다.
                text.AppendLine(
                    $"  · AreaDefinition 이 비어 있는 층 {blank.Length}개 — 인스펙터에서 연결해야 합니다:\n    " +
                    string.Join("\n    ", blank.Select(a => a.name)));
            }

            if (text.Length == 0) text.Append("  표와 씬이 일치합니다.");
            return text.ToString();
        }

        // ── 생성 ──────────────────────────────────────────────────────────

        [MenuItem(MenuRoot + "3. AreaDefinition · 배치 프로필 생성/갱신", priority = 303)]
        public static void Build()
        {
            EnsureFolder(AreaFolder);
            EnsureFolder(ScatterFolder);

            Dictionary<int, AreaAnchor> anchors = LoadAnchorsByName();
            Dictionary<string, PropDefinition> props = LoadProps();

            if (props.Count == 0)
            {
                Debug.LogError(
                    "[AreaDefinition] PropDefinition 이 하나도 없습니다. " +
                    "\"Pretty Knights > Props > 6. PropDefinition 생성/갱신\" 을 먼저 실행하세요.");
                return;
            }

            // 1차 — 에셋을 전부 만들어 둔다. 링크를 걸려면 상대가 이미 있어야 한다.
            var definitions = new Dictionary<int, AreaDefinition>();
            var profiles = new Dictionary<int, FloorScatterProfile>();

            int createdAreas = 0, createdProfiles = 0, keptProfiles = 0, skipped = 0;

            foreach (Row row in Rows)
            {
                definitions[row.AreaId] = LoadOrCreate<AreaDefinition>(
                    $"{AreaFolder}/{row.Name}.asset", out bool madeArea);
                if (madeArea) createdAreas++;

                if (row.Profile == null) continue;

                string path = $"{ScatterFolder}/{row.Profile}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<FloorScatterProfile>(path);

                if (existing != null)
                {
                    // 개수·시드·간격은 손으로 다듬는 값이다. 비어 있는 프리팹 칸만 채운다.
                    FillPrefabsIfEmpty(existing, row);
                    profiles[row.AreaId] = existing;
                    keptProfiles++;
                    continue;
                }

                anchors.TryGetValue(row.AreaId, out AreaAnchor anchor);
                int cells = CountFloorCells(anchor);

                if (cells == 0)
                {
                    // 개수를 지어내면 좁은 층이 순식간에 막힌다. 만들지 않고 알린다.
                    Debug.LogWarning(
                        $"[AreaDefinition] #{row.AreaId} {row.DisplayName} — 바닥 칸을 세지 못해 " +
                        $"배치 프로필 '{row.Profile}' 을 만들지 않았습니다.\n" +
                        "  Ingame_Horizontal 을 열고, 그 층의 WalkableArea 에 Floor 타일맵을 연결한 뒤 다시 실행하세요.");
                    skipped++;
                    continue;
                }

                var profile = LoadOrCreate<FloorScatterProfile>(path, out _);
                ApplyProfile(profile, row, cells, props);
                profiles[row.AreaId] = profile;
                createdProfiles++;
            }

            // 2차 — 링크를 건다. 이제 모든 상대가 존재한다.
            foreach (Row row in Rows) ApplyDefinition(definitions[row.AreaId], row, definitions, profiles);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[AreaDefinition] 구역 {Rows.Length}개 (새로 {createdAreas}개) · " +
                $"배치 프로필 새로 {createdProfiles}개 · 기존 유지 {keptProfiles}개" +
                (skipped > 0 ? $" · 건너뜀 {skipped}개" : "") + "\n" +
                $"  {AreaFolder} · {ScatterFolder}\n" +
                "  기존 프로필의 개수·시드·간격은 건드리지 않았습니다 — 다시 뽑히면 받아들인 배치가 사라집니다.\n" +
                "  다만 비어 있던 Prop/Portal Prefab 칸은 채웠습니다 (Goblin 3F 의 Gold 포탈 등).\n" +
                "  씬의 AreaAnchor 에 정의를 연결하는 것은 손으로 해야 합니다 (docs/guides/all-maps-setup.md).");
        }

        private static void ApplyDefinition(
            AreaDefinition asset, Row row,
            Dictionary<int, AreaDefinition> definitions,
            Dictionary<int, FloorScatterProfile> profiles)
        {
            var so = new SerializedObject(asset);

            Set(so, "areaId", p => p.intValue = row.AreaId);
            Set(so, "displayName", p => p.stringValue = row.DisplayName);
            Set(so, "theme", p => p.stringValue = row.Theme);
            Set(so, "isBossFloor", p => p.boolValue = row.IsBoss);
            Set(so, "isRewardRoom", p => p.boolValue = row.IsReward);

            AreaDefinition next = row.NextAreaId != 0 ? definitions[row.NextAreaId] : null;
            Set(so, "nextArea", p => p.objectReferenceValue = next);
            Set(so, "nextArrivalId", p => p.stringValue = row.NextArrivalId);

            AreaDefinition escape = row.EscapeToId != 0 ? definitions[row.EscapeToId] : null;
            Set(so, "escapeTo", p => p.objectReferenceValue = escape);
            Set(so, "escapeArrivalId", p => p.stringValue = escape != null ? EscapeArrivalId : "");

            // 프로필을 못 만든 층은 비워 둔다. 엉뚱한 층의 프로필을 물리면 배치가 통째로 어긋난다.
            profiles.TryGetValue(row.AreaId, out FloorScatterProfile profile);
            if (profile != null) Set(so, "scatterProfile", p => p.objectReferenceValue = profile);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void ApplyProfile(
            FloorScatterProfile asset, Row row, int cells, Dictionary<string, PropDefinition> props)
        {
            List<PropDefinition> ordered = OrderedProps(row.Theme, props);
            int[] counts = Plan(row, cells);

            var so = new SerializedObject(asset);

            SerializedProperty entries = so.FindProperty("entries");
            entries.arraySize = ordered.Count;

            for (int i = 0; i < ordered.Count; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("definition").objectReferenceValue = ordered[i];
                entry.FindPropertyRelative("count").intValue = i < counts.Length ? counts[i] : 0;
            }

            Set(so, "propPrefab", p => p.objectReferenceValue = Load<GameObject>(PropPrefabPath));
            Set(so, "portalPrefab", p => p.objectReferenceValue = Load<GameObject>(PortalPathOf(row)));

            // 보스 층은 넓게 트여 있어야 예고를 보고 피할 수 있다.
            Set(so, "minSpacing", p => p.floatValue = row.IsBoss ? 5f : 3f);
            Set(so, "wallClearance", p => p.floatValue = row.IsBoss ? 2f : 1f);
            Set(so, "protectedRadius", p => p.floatValue = row.IsBoss ? 6f : 4f);

            // areaId 가 섞여 있어야 같은 시드로도 층마다 다른 배치가 나온다.
            Set(so, "seed", p => p.intValue = 480480 + row.AreaId);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        /// <summary>
        /// 이미 있는 프로필은 개수를 건드리지 않되, <b>비어 있는 프리팹 칸만</b> 채운다.
        /// Portal Prefab 이 비면 메인 토템을 부숴도 다음 층으로 갈 길이 생기지 않고,
        /// 그 사실은 재생해서 토템을 부술 때까지 드러나지 않는다.
        /// </summary>
        private static void FillPrefabsIfEmpty(FloorScatterProfile asset, Row row)
        {
            var so = new SerializedObject(asset);
            bool changed = false;

            SerializedProperty prop = so.FindProperty("propPrefab");
            if (prop != null && prop.objectReferenceValue == null)
            {
                prop.objectReferenceValue = Load<GameObject>(PropPrefabPath);
                changed = true;
            }

            SerializedProperty portal = so.FindProperty("portalPrefab");
            if (portal != null && portal.objectReferenceValue == null)
            {
                portal.objectReferenceValue = Load<GameObject>(PortalPathOf(row));
                changed = true;
            }

            if (!changed) return;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        /// <summary>1F 는 Blue · 2F 는 Red · 3F 는 Gold. 결정 006 §3-1 의 표 그대로다.</summary>
        private static string PortalPathOf(Row row) => row.Floor switch
        {
            1 => BluePortalPath,
            2 => RedPortalPath,
            3 => GoldPortalPath,
            _ => BluePortalPath
        };

        // ── 개수 계산 ─────────────────────────────────────────────────────

        /// <summary>
        /// 그 층에 무엇을 몇 개 놓을지. 순서는 <see cref="OrderedProps"/> 와 같다 —
        /// 메인 토템 · 서브 토템 · 파괴용 4종(propId 오름차순).
        /// </summary>
        private static int[] Plan(Row row, int cells)
        {
            int density = row.IsBoss ? CellsPerPropBoss : CellsPerPropNormal;
            int target = Mathf.Max(0, Mathf.RoundToInt(cells / (float)density));

            // 보스 층에는 토템이 없다. 보스를 잡는 것이 곧 진행이다.
            int main = row.IsBoss ? 0 : 1;
            int sub = row.IsBoss ? 0 : row.Floor == 1 ? 2 : 3;

            int rest = Mathf.Max(0, target - main - sub);
            int[] weights = row.IsBoss ? BossWeights : NormalWeights;

            var counts = new List<int> { main, sub };
            counts.AddRange(Distribute(rest, weights));
            return counts.ToArray();
        }

        /// <summary>
        /// 가중치대로 나누되 <b>합이 정확히 <paramref name="total"/> 이 되게</b> 한다.
        /// 반올림만 하면 넷을 더한 값이 총량과 어긋나 밀도가 조용히 틀어진다.
        /// 최대 잉여법 — 내림한 뒤 소수부가 큰 쪽부터 하나씩 얹는다.
        /// </summary>
        private static int[] Distribute(int total, IReadOnlyList<int> weights)
        {
            int sum = weights.Sum();
            var counts = new int[weights.Count];
            var remainders = new List<(int Index, float Fraction)>();

            int assigned = 0;

            for (int i = 0; i < weights.Count; i++)
            {
                float exact = total * weights[i] / (float)sum;
                counts[i] = Mathf.FloorToInt(exact);
                assigned += counts[i];
                remainders.Add((i, exact - counts[i]));
            }

            foreach ((int index, float _) in remainders.OrderByDescending(r => r.Fraction))
            {
                if (assigned >= total) break;

                counts[index]++;
                assigned++;
            }

            return counts;
        }

        /// <summary>
        /// 그 테마의 오브젝트 6종을 <b>메인 토템 · 서브 토템 · 파괴용(propId 오름차순)</b> 순으로.
        /// 순서가 곧 배치 개수 배열의 순서라 여기서 한 번만 정한다.
        /// </summary>
        private static List<PropDefinition> OrderedProps(
            string theme, Dictionary<string, PropDefinition> props)
        {
            List<PropDefinition> themed = props.Values
                .Where(p => string.Equals(p.Theme, theme, System.StringComparison.OrdinalIgnoreCase))
                .ToList();

            var ordered = new List<PropDefinition>();
            ordered.AddRange(themed.Where(p => p.Role == PropRole.MainTotem).OrderBy(p => p.PropId));
            ordered.AddRange(themed.Where(p => p.Role == PropRole.SubTotem).OrderBy(p => p.PropId));
            ordered.AddRange(themed.Where(p => p.Role == PropRole.Destructible).OrderBy(p => p.PropId));

            if (ordered.Count != 6)
                Debug.LogWarning(
                    $"[AreaDefinition] 테마 '{theme}' 의 오브젝트가 {ordered.Count}종입니다 (6종이어야 함). " +
                    "PropDefinition 이 덜 만들어졌거나 theme 문자열이 다릅니다.");

            return ordered;
        }

        // ── 씬 읽기 ───────────────────────────────────────────────────────

        /// <summary>
        /// 그 층의 바닥 칸 수. <b>벽은 빼지 않는다</b> — Guide 는 Floor 와 겹치지 않게 그려져 있고,
        /// 겹친 부분은 <c>PropScatterer</c> 가 놓을 때 걸러낸다.
        /// </summary>
        private static int CountFloorCells(AreaAnchor anchor)
        {
            if (anchor == null) return 0;

            Tilemap floor = FindFloor(anchor);
            if (floor == null) return 0;

            int count = 0;

            // CompressBounds 를 부르지 않는다. 그건 타일맵을 실제로 바꿔 씬을 더럽힌다.
            foreach (Vector3Int cell in floor.cellBounds.allPositionsWithin)
                if (floor.HasTile(cell)) count++;

            return count;
        }

        /// <summary>
        /// 그 층의 바닥 타일맵. <b><c>AreaAnchor.Resolve</c> 를 부르지 않는다</b> —
        /// 그쪽은 정의가 비면 에러를 찍는데, 정의를 아직 안 채운 층을 재는 것이 이 도구의 일이라
        /// 점검할 때마다 에러가 8줄씩 쏟아진다.
        ///
        /// 마지막 수단은 <c>AreaAnchor</c> 와 같은 규칙이다 — <b>콜라이더가 없는 쪽이 바닥</b>.
        /// 층은 Floor 와 Guide 두 타일맵을 갖는데 Guide 에만 콜라이더가 붙는다.
        /// </summary>
        private static Tilemap FindFloor(AreaAnchor anchor)
        {
            WalkableArea walkable = anchor.GetComponent<WalkableArea>();
            if (walkable != null && walkable.Floor != null) return walkable.Floor;
            if (anchor.Floor != null) return anchor.Floor;

            foreach (Tilemap map in anchor.GetComponentsInChildren<Tilemap>(includeInactive: true))
                if (map != null && map.GetComponent<TilemapCollider2D>() == null)
                    return map;

            return null;
        }

        /// <summary>
        /// 씬의 앵커를 areaId 로 찾는다. <b>정의가 아직 비어 있어도 찾아야 하므로</b>
        /// 오브젝트 이름으로도 짚는다 — 정의를 연결하려면 개수부터 재야 하는데
        /// 그 개수를 재려면 앵커를 찾아야 하는 순환을 여기서 끊는다.
        /// </summary>
        private static Dictionary<int, AreaAnchor> LoadAnchorsByName()
        {
            var map = new Dictionary<int, AreaAnchor>();

            AreaAnchor[] anchors = Object.FindObjectsByType<AreaAnchor>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (AreaAnchor anchor in anchors)
            {
                int id = anchor.Definition != null ? anchor.Definition.AreaId : GuessAreaId(anchor);
                if (id == 0 || map.ContainsKey(id)) continue;

                map.Add(id, anchor);
            }

            return map;
        }

        /// <summary>
        /// 계층 경로에서 번호를 짐작한다. <c>Map/Orc/Orc2F</c> → 202,
        /// <c>Map/Vampire/Rewards</c> → 390. 어디까지나 정의를 연결하기 전까지의 임시 수단이다.
        /// </summary>
        private static int GuessAreaId(AreaAnchor anchor)
        {
            string path = anchor.name;
            for (Transform t = anchor.transform.parent; t != null; t = t.parent) path = t.name + "/" + path;

            int theme =
                path.Contains("Goblin") ? 1 :
                path.Contains("Orc") ? 2 :
                path.Contains("Vampire") ? 3 : 0;

            if (path.Contains("Dungeon")) return 3;
            if (theme == 0) return 0;
            if (anchor.name.Contains("Reward")) return theme * 100 + 90;

            for (int floor = 1; floor <= 3; floor++)
                if (anchor.name.Contains($"{floor}F")) return theme * 100 + floor;

            return 0;
        }

        // ── 에셋 다루기 ───────────────────────────────────────────────────

        private static Dictionary<int, AreaDefinition> LoadDefinitions()
        {
            var map = new Dictionary<int, AreaDefinition>();

            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(AreaDefinition)}"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<AreaDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset == null || map.ContainsKey(asset.AreaId)) continue;

                map.Add(asset.AreaId, asset);
            }

            return map;
        }

        private static Dictionary<string, PropDefinition> LoadProps()
        {
            var map = new Dictionary<string, PropDefinition>();

            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(PropDefinition)}"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<PropDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset == null || string.IsNullOrEmpty(asset.PropId) || map.ContainsKey(asset.PropId)) continue;

                map.Add(asset.PropId, asset);
            }

            return map;
        }

        private static T LoadOrCreate<T>(string path, out bool created) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            created = false;

            if (asset != null) return asset;

            // 지웠다 다시 만들지 않는다. 프리팹·씬이 물고 있던 참조가 전부 끊긴다.
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            created = true;

            return asset;
        }

        private static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);

            if (asset == null)
                Debug.LogWarning($"[AreaDefinition] '{path}' 를 찾지 못했습니다. 해당 칸은 비어 있습니다.");

            return asset;
        }

        private static void Set(SerializedObject so, string path, System.Action<SerializedProperty> write)
        {
            SerializedProperty property = so.FindProperty(path);

            if (property == null)
            {
                Debug.LogError(
                    $"[AreaDefinition] '{path}' 필드를 찾지 못했습니다. " +
                    "필드 이름이 바뀌었다면 이 도구도 함께 고쳐야 합니다.");
                return;
            }

            write(property);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;

            string parent = Path.GetDirectoryName(folder)!.Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parent)) AssetDatabase.CreateFolder("Assets", "Data");

            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
