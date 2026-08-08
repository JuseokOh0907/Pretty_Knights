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
    /// <see cref="DropTable"/> 6종을 만들고 몬스터·오브젝트 정의에 연결한다.
    ///
    /// <b>표를 손으로 물리지 않는 이유는 개수다.</b> 몬스터 10 + 오브젝트 18 = 28곳이고,
    /// 빠뜨린 곳은 "왜 이것만 경험치가 적지" 로 나타나 원인을 찾기 어렵다.
    /// 등급과 역할에서 기계적으로 정해지므로 규칙을 코드에 둔다.
    ///
    /// <b>아이템 시스템이 아직 없다.</b> 그래서 표는 지금 경험치만 돌려준다 —
    /// 확정 보상 위에 얹히는 <b>변동분</b>이다. 같은 몬스터를 잡아도 수확이 달라지는 것이
    /// 파밍의 결을 만든다. 아이템이 생기면 <see cref="DropTable.Entry"/> 에 참조를 더하면 되고
    /// 이 도구도 그때 라벨을 아이템으로 바꾸면 된다.
    ///
    /// <b>이미 연결된 표는 덮어쓰지 않는다.</b> 손으로 특별하게 물린 것을 되돌리지 않으려는 것이다.
    /// </summary>
    public static class DropTableBuilder
    {
        private const string MenuRoot = "Pretty Knights/Data/";
        private const string TargetFolder = "Assets/Data/Drops";

        private readonly struct Row
        {
            public readonly string Name;
            public readonly (string Label, float Chance, int MinExp, int MaxExp)[] Entries;

            public Row(string name, params (string, float, int, int)[] entries)
            {
                Name = name;
                Entries = entries;
            }
        }

        // 확률은 "얼마나 자주 기분이 좋은가" 다. 흔한 항목은 자주·조금,
        // 희귀한 항목은 드물게·크게. 두 항목을 따로 굴리므로 둘 다 나오는 날도 있다.
        private static readonly Row[] Rows =
        {
            new Row("Drop_Monster_Normal",
                ("잡동사니", 0.35f, 1, 3),
                ("이빨",     0.10f, 4, 8)),

            new Row("Drop_Monster_Elite",
                ("전리품",   0.50f,  8, 16),
                ("희귀 재료", 0.15f, 20, 35)),

            new Row("Drop_Monster_Boss",
                ("보스 전리품", 1.00f,  60, 120),
                ("희귀 재료",   0.50f, 100, 200)),

            new Row("Drop_Prop_Common",
                ("부스러기", 0.25f, 1, 2)),

            new Row("Drop_Prop_Rich",
                ("광석",     0.60f,  5, 12),
                ("숨은 한 줌", 0.10f, 15, 25)),

            new Row("Drop_Prop_Totem",
                ("토템 조각", 0.80f, 20, 40))
        };

        /// <summary>흔한 것보다 수확이 큰 오브젝트. 이름으로 짚는다.</summary>
        private static readonly string[] RichProps =
        {
            "gold_vein_rock", "supply_pile", "weapon_rack", "iron_boulder", "gravestone_cluster"
        };

        // ── 점검 ──────────────────────────────────────────────────────────

        [MenuItem(MenuRoot + "2. 드랍 표 점검 (변경 없음)", priority = 210)]
        public static void Report()
        {
            var report = new StringBuilder($"[DropTable] 표 {Rows.Length}종\n");

            foreach (Row row in Rows)
            {
                float min = row.Entries.Sum(e => e.Chance * e.MinExp);
                float max = row.Entries.Sum(e => e.Chance * e.MaxExp);

                report.AppendLine(
                    $"  {row.Name,-22} 항목 {row.Entries.Length}개 · 기대값 {min:0.#}~{max:0.#} 경험치");

                foreach ((string label, float chance, int lo, int hi) in row.Entries)
                    report.AppendLine($"      {label,-12} {chance * 100,3:0}% · {lo}~{hi}");
            }

            report.AppendLine();
            report.Append(BuildDifference());

            Debug.Log(report.ToString());
        }

        private static string BuildDifference()
        {
            var text = new StringBuilder();

            int missing = Rows.Count(r => Load(r.Name) == null);
            if (missing > 0) text.AppendLine($"  · 에셋 {missing}개가 없습니다 — \"3. 생성/연결\" 을 실행하세요.");

            MonsterDefinition[] monsters = LoadAll<MonsterDefinition>();
            PropDefinition[] props = LoadAll<PropDefinition>();

            int monsterGap = monsters.Count(m => m.Drops == null);
            int propGap = props.Count(p => p.Drops == null);

            if (monsterGap > 0) text.AppendLine($"  · 드랍 표가 없는 몬스터 {monsterGap}/{monsters.Length}종");
            if (propGap > 0) text.AppendLine($"  · 드랍 표가 없는 오브젝트 {propGap}/{props.Length}종");

            if (text.Length == 0) text.Append("  전부 연결되어 있습니다.");
            return text.ToString();
        }

        // ── 생성 ──────────────────────────────────────────────────────────

        [MenuItem(MenuRoot + "3. 드랍 표 생성/연결", priority = 211)]
        public static void Build()
        {
            EnsureFolder();

            var tables = new Dictionary<string, DropTable>();
            int created = 0;

            foreach (Row row in Rows)
            {
                DropTable table = Load(row.Name);

                if (table == null)
                {
                    table = ScriptableObject.CreateInstance<DropTable>();
                    AssetDatabase.CreateAsset(table, $"{TargetFolder}/{row.Name}.asset");
                    created++;
                }

                Apply(table, row);
                tables[row.Name] = table;
            }

            int monsters = LinkMonsters(tables);
            int props = LinkProps(tables);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[DropTable] 표 {Rows.Length}종 (새로 {created}개) → {TargetFolder}\n" +
                $"  몬스터 {monsters}종 · 오브젝트 {props}종에 연결했습니다.\n" +
                "  이미 표가 물려 있던 정의는 건드리지 않았습니다.\n" +
                "  지금 표는 경험치만 줍니다 — 아이템 시스템이 붙으면 라벨을 아이템으로 바꿉니다.");
        }

        private static void Apply(DropTable table, Row row)
        {
            var so = new SerializedObject(table);
            SerializedProperty entries = so.FindProperty("entries");

            if (entries == null)
            {
                Debug.LogError("[DropTable] 'entries' 필드를 찾지 못했습니다. DropTable 이 바뀌었다면 이 도구도 고쳐야 합니다.");
                return;
            }

            entries.arraySize = row.Entries.Length;

            for (int i = 0; i < row.Entries.Length; i++)
            {
                SerializedProperty e = entries.GetArrayElementAtIndex(i);
                (string label, float chance, int lo, int hi) = row.Entries[i];

                e.FindPropertyRelative("label").stringValue = label;
                e.FindPropertyRelative("chance").floatValue = chance;
                e.FindPropertyRelative("minExp").intValue = lo;
                e.FindPropertyRelative("maxExp").intValue = hi;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(table);
        }

        /// <summary>등급이 표를 정한다. Normal·Elite·Boss 각각 하나씩.</summary>
        private static int LinkMonsters(IReadOnlyDictionary<string, DropTable> tables)
        {
            int linked = 0;

            foreach (MonsterDefinition monster in LoadAll<MonsterDefinition>())
            {
                if (monster.Drops != null) continue;

                string name = monster.Tier switch
                {
                    MonsterTier.Boss => "Drop_Monster_Boss",
                    MonsterTier.Elite => "Drop_Monster_Elite",
                    _ => "Drop_Monster_Normal"
                };

                if (!tables.TryGetValue(name, out DropTable table)) continue;

                Assign(monster, table);
                linked++;
            }

            return linked;
        }

        /// <summary>역할이 표를 정한다. 토템은 크게, 이름이 알려진 것들은 중간, 나머지는 조금.</summary>
        private static int LinkProps(IReadOnlyDictionary<string, DropTable> tables)
        {
            int linked = 0;

            foreach (PropDefinition prop in LoadAll<PropDefinition>())
            {
                if (prop.Drops != null) continue;

                string name =
                    prop.IsTotem ? "Drop_Prop_Totem" :
                    RichProps.Any(r => prop.PropId != null && prop.PropId.Contains(r)) ? "Drop_Prop_Rich" :
                    "Drop_Prop_Common";

                if (!tables.TryGetValue(name, out DropTable table)) continue;

                Assign(prop, table);
                linked++;
            }

            return linked;
        }

        private static void Assign(Object definition, DropTable table)
        {
            var so = new SerializedObject(definition);
            SerializedProperty property = so.FindProperty("dropTable");

            if (property == null)
            {
                Debug.LogError($"[DropTable] '{definition.name}' 에 'dropTable' 필드가 없습니다.", definition);
                return;
            }

            property.objectReferenceValue = table;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }

        // ── 에셋 다루기 ───────────────────────────────────────────────────

        private static DropTable Load(string name) =>
            AssetDatabase.LoadAssetAtPath<DropTable>($"{TargetFolder}/{name}.asset");

        private static T[] LoadAll<T>() where T : Object =>
            AssetDatabase.FindAssets($"t:{typeof(T).Name}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .Where(a => a != null)
                .ToArray();

        private static void EnsureFolder()
        {
            if (AssetDatabase.IsValidFolder(TargetFolder)) return;

            string parent = Path.GetDirectoryName(TargetFolder)!.Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parent)) AssetDatabase.CreateFolder("Assets", "Data");

            AssetDatabase.CreateFolder(parent, Path.GetFileName(TargetFolder));
        }
    }
}
