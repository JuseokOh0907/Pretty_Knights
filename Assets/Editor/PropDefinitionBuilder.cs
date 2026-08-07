using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PrettyKnights.Data;
using UnityEditor;
using UnityEngine;

namespace PrettyKnights.EditorTools
{
    /// <summary>
    /// <see cref="PropDefinition"/> 18종을 만든다.
    ///
    /// 값의 출처는 <c>docs/design/map-objects.md</c> 의 실측표다.
    /// 접지폭과 <c>Visual</c> Y 는 <b>프리팹에 굽는 값이라 여기 넣지 않는다</b> —
    /// 에디터에서 눈으로 맞춰야 하는 것이기 때문이다. 대신 로그에 함께 찍어
    /// 프리팹을 만들 때 옮겨 적을 수 있게 한다.
    ///
    /// HP·경험치·인구 지분은 <b>임시값</b>이다. 플레이어 DPS 가 정해지면 다시 잡는다.
    /// </summary>
    public static class PropDefinitionBuilder
    {
        private const string MenuRoot = "Pretty Knights/Props/";
        private const string TargetFolder = "Assets/Data/Props";

        private readonly struct Row
        {
            public readonly string Id;
            public readonly string Name;
            public readonly string Theme;
            public readonly PropRole Role;
            public readonly float MaxHp;
            public readonly int Exp;
            public readonly int Share;

            /// <summary>실측값. 에셋에는 안 들어가고 프리팹 제작용으로 로그에만 찍는다.</summary>
            public readonly float Footprint;
            public readonly float VisualY;

            public Row(string id, string name, string theme, PropRole role,
                float maxHp, int exp, int share, float footprint, float visualY)
            {
                Id = id; Name = name; Theme = theme; Role = role;
                MaxHp = maxHp; Exp = exp; Share = share;
                Footprint = footprint; VisualY = visualY;
            }
        }

        // docs/design/map-objects.md §1 (역할) · §2 (실측)
        //                id                          표시명            테마        역할                     HP   경험치 지분  접지폭 VisualY
        private static readonly Row[] Rows =
        {
            new Row("goblin_01_twisted_stump",     "뒤틀린 그루터기",   "Goblin",  PropRole.SubTotem,      120f,  40,  4,  1.75f, 0.828f),
            new Row("goblin_02_moss_boulder",      "이끼 바위",         "Goblin",  PropRole.Destructible,   60f,   6,  0,  1.52f, 0.813f),
            new Row("goblin_03_gold_vein_rock",    "금맥 바위",         "Goblin",  PropRole.Destructible,   60f,  12,  0,  1.28f, 0.641f),
            new Row("goblin_04_mushroom_cluster",  "버섯 무리",         "Goblin",  PropRole.Destructible,   25f,   4,  0,  1.25f, 0.609f),
            new Row("goblin_05_supply_pile",       "보급 더미",         "Goblin",  PropRole.Destructible,   35f,   8,  0,  1.48f, 0.609f),
            new Row("goblin_06_scrap_totem",       "고철 토템",         "Goblin",  PropRole.MainTotem,     300f, 100,  4,  0.80f, 0.875f),

            new Row("orc_01_scorched_stump",       "그을린 그루터기",   "Orc",     PropRole.SubTotem,      120f,  40,  4,  1.62f, 0.891f),
            new Row("orc_02_iron_boulder",         "쇠사슬 바위",       "Orc",     PropRole.Destructible,   70f,   6,  0,  1.47f, 0.891f),
            new Row("orc_03_weapon_rack",          "무기 거치대",       "Orc",     PropRole.Destructible,   40f,  10,  0,  1.59f, 0.891f),
            new Row("orc_04_supply_pile",          "보급 더미",         "Orc",     PropRole.Destructible,   35f,   8,  0,  1.52f, 0.891f),
            new Row("orc_05_war_totem",            "전쟁 토템",         "Orc",     PropRole.MainTotem,     300f, 100,  4,  0.84f, 0.891f),
            new Row("orc_06_fire_pit",             "화덕",              "Orc",     PropRole.Destructible,   30f,   6,  0,  1.23f, 0.891f),

            new Row("vampire_01_dead_tree",        "고사목",            "Vampire", PropRole.Destructible,   55f,   6,  0,  1.27f, 0.859f),
            new Row("vampire_02_gravestone",       "묘비 무리",         "Vampire", PropRole.Destructible,   65f,   8,  0,  1.55f, 0.766f),
            new Row("vampire_03_sarcophagus",      "석관",              "Vampire", PropRole.SubTotem,      120f,  40,  4,  1.69f, 0.531f),
            new Row("vampire_04_candle_cluster",   "촛대",              "Vampire", PropRole.Destructible,   20f,   4,  0,  0.48f, 0.594f),
            new Row("vampire_05_thorned_roses",    "가시 장미",         "Vampire", PropRole.Destructible,   25f,   4,  0,  1.39f, 0.375f),
            new Row("vampire_06_bloodstone_altar", "혈석 제단",         "Vampire", PropRole.MainTotem,     300f, 100,  4,  1.69f, 0.750f)
        };

        [MenuItem(MenuRoot + "5. 오브젝트 정의 점검 (변경 없음)", priority = 510)]
        public static void Report()
        {
            var report = new StringBuilder($"[PropDefinition] 표 {Rows.Length}종\n");
            report.AppendLine("  ID                              테마     역할           HP  경험치 지분 | 접지폭 VisualY");

            foreach (Row row in Rows)
            {
                report.AppendLine(
                    $"  {row.Id,-31} {row.Theme,-8} {row.Role,-13} {row.MaxHp,4:0} {row.Exp,5} {row.Share,4} | " +
                    $"{row.Footprint,6:0.00} {row.VisualY,7:0.000}");
            }

            report.AppendLine();
            report.AppendLine("  접지폭·VisualY 는 에셋이 아니라 프리팹에 넣는 값이다.");
            report.AppendLine("  콜라이더 = 접지폭 × 0.5칸, Visual 자식의 Y = 위 값.");
            report.AppendLine();
            report.Append(BuildDifference());

            Debug.Log(report.ToString());
        }

        private static string BuildDifference()
        {
            var found = LoadExisting();
            if (found.Count == 0) return "  에셋이 아직 없습니다. \"6. 생성/갱신\" 을 실행하세요.";

            var report = new StringBuilder($"  기존 에셋 {found.Count}개와 대조\n");
            int mismatches = 0;

            foreach (Row row in Rows)
            {
                if (!found.TryGetValue(row.Id, out PropDefinition asset))
                {
                    report.AppendLine($"    ✗ {row.Id} — 에셋 없음");
                    mismatches++;
                    continue;
                }

                if (asset.Role != row.Role)
                {
                    report.AppendLine($"    △ {row.Id} — 역할 {asset.Role} (표는 {row.Role})");
                    mismatches++;
                }
            }

            foreach (string extra in found.Keys.Where(k => Rows.All(r => r.Id != k)))
            {
                report.AppendLine($"    ? {extra} — 표에 없는 에셋");
                mismatches++;
            }

            if (mismatches == 0) report.AppendLine("    표와 일치합니다.");
            return report.ToString();
        }

        [MenuItem(MenuRoot + "6. PropDefinition 생성/갱신", priority = 511)]
        public static void Build()
        {
            EnsureFolder();

            var existing = LoadExisting();
            int created = 0, updated = 0;

            foreach (Row row in Rows)
            {
                string path = $"{TargetFolder}/Prop_{row.Id}.asset";

                // 이미 있으면 지우지 않고 값만 덮어쓴다.
                // 지웠다 다시 만들면 프리팹·프로필이 물고 있던 참조가 전부 끊긴다.
                PropDefinition asset = existing.TryGetValue(row.Id, out PropDefinition found)
                    ? found
                    : AssetDatabase.LoadAssetAtPath<PropDefinition>(path);

                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<PropDefinition>();
                    AssetDatabase.CreateAsset(asset, path);
                    created++;
                }
                else
                {
                    updated++;
                }

                Apply(asset, row);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[PropDefinition] 생성 {created}개 · 갱신 {updated}개 → {TargetFolder}\n" +
                "  Broken Sprite 와 Drop Table 은 건드리지 않았습니다. 인스펙터에서 채우세요.\n" +
                "  HP·경험치·지분은 임시값입니다 — 플레이어 DPS 가 정해지면 다시 잡습니다.");
        }

        private static void Apply(PropDefinition asset, Row row)
        {
            var so = new SerializedObject(asset);

            Set(so, "propId", p => p.stringValue = row.Id);
            Set(so, "displayName", p => p.stringValue = row.Name);
            Set(so, "theme", p => p.stringValue = row.Theme);
            Set(so, "role", p => p.enumValueIndex = (int)row.Role);
            Set(so, "maxHp", p => p.floatValue = row.MaxHp);
            Set(so, "expReward", p => p.intValue = row.Exp);
            Set(so, "populationShare", p => p.intValue = row.Share);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void Set(SerializedObject so, string path, System.Action<SerializedProperty> write)
        {
            SerializedProperty property = so.FindProperty(path);

            if (property == null)
            {
                Debug.LogError(
                    $"[PropDefinition] '{path}' 필드를 찾지 못했습니다. " +
                    "PropDefinition 의 필드 이름이 바뀌었다면 이 도구도 함께 고쳐야 합니다.");
                return;
            }

            write(property);
        }

        private static Dictionary<string, PropDefinition> LoadExisting()
        {
            var map = new Dictionary<string, PropDefinition>();

            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(PropDefinition)}"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<PropDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset == null || string.IsNullOrEmpty(asset.PropId)) continue;
                if (!map.ContainsKey(asset.PropId)) map.Add(asset.PropId, asset);
            }

            return map;
        }

        private static void EnsureFolder()
        {
            if (AssetDatabase.IsValidFolder(TargetFolder)) return;

            string parent = Path.GetDirectoryName(TargetFolder)!.Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parent)) AssetDatabase.CreateFolder("Assets", "Data");

            AssetDatabase.CreateFolder(parent, Path.GetFileName(TargetFolder));
        }
    }
}
