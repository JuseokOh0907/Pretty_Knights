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
    /// <see cref="MonsterDefinition"/> 에셋 10종을 만든다.
    ///
    /// 손으로 넣으면 10종 × 15필드 = 150개 값이라 오타가 반드시 난다.
    /// 그리고 오타난 값은 <b>게임을 해봐도 티가 안 난다</b> — 그냥 조금 이상할 뿐이다.
    /// 그래서 표를 코드에 두고 생성한다.
    ///
    /// 값의 출처는 <c>docs/design/monster-definitions.xlsx</c> 다.
    /// <b>이 표를 고치면 시트도 함께 고칠 것.</b> 둘이 어긋나면 어느 쪽이 사실인지 알 수 없어진다.
    ///
    /// 데미지 공식이 아직 확정되지 않았으므로 이 값들은 임시다.
    /// 확정 후에는 "1. 갱신" 으로 덮어쓴다 — 에셋을 지우지 않으므로 참조가 끊기지 않는다.
    /// </summary>
    public static class MonsterDefinitionBuilder
    {
        private const string MenuRoot = "Pretty Knights/Data/";
        private const string TargetFolder = "Assets/Data/Monsters";

        /// <summary>시트 한 줄. 컬럼 순서는 시트와 같다.</summary>
        private readonly struct Row
        {
            public readonly string Id;
            public readonly string Name;
            public readonly MonsterTier Tier;
            public readonly float Vit, Atk, Def, Agi, Foc;
            public readonly float HpPerVit;
            public readonly float MoveSpeed, DetectRange, AttackRange, AttackCooldown;
            public readonly float Knockback, HitStun;
            public readonly int Exp;

            public Row(string id, string name, MonsterTier tier,
                float vit, float atk, float def, float agi, float foc, float hpPerVit,
                float moveSpeed, float detectRange, float attackRange, float attackCooldown,
                float knockback, float hitStun, int exp)
            {
                Id = id; Name = name; Tier = tier;
                Vit = vit; Atk = atk; Def = def; Agi = agi; Foc = foc;
                HpPerVit = hpPerVit;
                MoveSpeed = moveSpeed; DetectRange = detectRange;
                AttackRange = attackRange; AttackCooldown = attackCooldown;
                Knockback = knockback; HitStun = hitStun; Exp = exp;
            }

            public float MaxHp => Vit * HpPerVit;
        }

        // docs/design/monster-definitions.xlsx · 시트 "몬스터 정의" 6~15행
        //          id              표시명            등급               VIT  ATK  DEF  AGI  FOC HP/VIT  이동  감지  공격  쿨   넉백  경직  경험치
        private static readonly Row[] Rows =
        {
            new Row("goblin_hob",     "Hobgoblin",      MonsterTier.Normal,  10,  10,   8,   6,   0,  10,  2.0f,  5f, 1.2f, 1.4f,  3f, 0.10f,  15),
            new Row("goblin_shaman",  "Goblin Shaman",  MonsterTier.Elite,   15,  20,   5,   6,   0,   8,  1.5f, 15f, 3.0f, 1.2f,  3f, 0.18f,  35),
            new Row("goblin_rider",   "Goblin Rider",   MonsterTier.Elite,   15,  15,  10,   6,   0,  12,  2.5f, 10f, 1.5f, 1.2f,  5f, 0.10f,  35),
            new Row("goblin_king",    "Goblin King",    MonsterTier.Boss,    80,  25,  15,   6,   0,  15,  1.8f, 20f, 2.5f, 1.6f, 10f, 0.20f, 200),
            new Row("orc_brute",      "Orc Brute",      MonsterTier.Normal,  20,  15,  10,   4,   0,  12,  1.5f,  6f, 1.8f, 1.8f,  6f, 0.15f,  30),
            new Row("orc_warrior",    "Orc Warrior",    MonsterTier.Elite,   35,  30,  15,   4,   0,  15,  1.8f,  8f, 2.5f, 1.8f,  7f, 0.18f,  60),
            new Row("orc_warlord",    "Orc Warlord",    MonsterTier.Boss,   180,  50,  30,   4,   0,  20,  2.0f, 22f, 6.0f, 2.2f, 12f, 0.25f, 500),
            new Row("vampire_bat",    "Vampire Bat",    MonsterTier.Normal,  10,  10,   8,  12,   2,  10,  3.0f,  6f, 2.0f, 2.5f,  3f, 0.10f,  20),
            new Row("vampire_noble",  "Vampire Noble",  MonsterTier.Elite,   15,  15,  15,   8,   8,  18,  2.0f, 10f, 8.0f, 2.5f,  4f, 0.15f,  70),
            new Row("vampire_lord",   "Vampire Lord",   MonsterTier.Boss,   100,  25,  20,  10,  20,  20,  1.5f, 25f,10.0f, 2.5f,  5f, 0.25f, 500)
        };

        // ── 점검 ──────────────────────────────────────────────────────────

        [MenuItem(MenuRoot + "0. 몬스터 정의 점검 (변경 없음)", priority = 400)]
        public static void Report()
        {
            var report = new StringBuilder($"[MonsterDefinition] 표 {Rows.Length}종\n");
            report.AppendLine("  ID              등급    VIT  ATK  DEF   최대HP  이동  감지  공격범위  쿨   경험치");

            foreach (Row row in Rows)
            {
                report.AppendLine(
                    $"  {row.Id,-15} {row.Tier,-6} {row.Vit,4:0} {row.Atk,4:0} {row.Def,4:0} " +
                    $"{row.MaxHp,7:0} {row.MoveSpeed,5:0.0} {row.DetectRange,5:0} " +
                    $"{row.AttackRange,8:0.0} {row.AttackCooldown,4:0.0} {row.Exp,7}");
            }

            report.AppendLine();
            report.Append(BuildDifference());

            Debug.Log(report.ToString());
        }

        /// <summary>이미 있는 에셋과 표를 대조한다. 어긋난 것만 적는다.</summary>
        private static string BuildDifference()
        {
            var found = LoadExisting();

            if (found.Count == 0) return "  에셋이 아직 없습니다. \"1. 생성/갱신\" 을 실행하세요.";

            var report = new StringBuilder($"  기존 에셋 {found.Count}개와 대조\n");
            int mismatches = 0;

            foreach (Row row in Rows)
            {
                if (!found.TryGetValue(row.Id, out MonsterDefinition asset))
                {
                    report.AppendLine($"    ✗ {row.Id} — 에셋 없음");
                    mismatches++;
                    continue;
                }

                if (!Mathf.Approximately(asset.MaxHp, row.MaxHp))
                {
                    report.AppendLine($"    △ {row.Id} — 최대HP {asset.MaxHp:0} (표는 {row.MaxHp:0})");
                    mismatches++;
                }

                if (!Mathf.Approximately(asset.Stats.Attack, row.Atk))
                {
                    report.AppendLine($"    △ {row.Id} — ATK {asset.Stats.Attack:0} (표는 {row.Atk:0})");
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

        // ── 생성 ──────────────────────────────────────────────────────────

        [MenuItem(MenuRoot + "1. MonsterDefinition 생성/갱신", priority = 401)]
        public static void Build()
        {
            EnsureFolder();

            var existing = LoadExisting();
            int created = 0;
            int updated = 0;

            foreach (Row row in Rows)
            {
                string path = $"{TargetFolder}/Monster_{row.Id}.asset";

                // 이미 있으면 지우지 않고 값만 덮어쓴다.
                // 지웠다 다시 만들면 프리팹·스포너가 물고 있던 참조가 전부 끊긴다.
                MonsterDefinition asset = existing.TryGetValue(row.Id, out MonsterDefinition found)
                    ? found
                    : AssetDatabase.LoadAssetAtPath<MonsterDefinition>(path);

                bool isNew = asset == null;

                if (isNew)
                {
                    asset = ScriptableObject.CreateInstance<MonsterDefinition>();
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
                $"[MonsterDefinition] 생성 {created}개 · 갱신 {updated}개 → {TargetFolder}\n" +
                "  스프라이트(frames)는 건드리지 않았습니다. 몬스터 아트가 나오면 인스펙터에서 채우세요.");
        }

        /// <summary>
        /// 필드가 전부 private [SerializeField] 이므로 <see cref="SerializedObject"/> 로 쓴다.
        /// 리플렉션보다 안전하다 — 이름이 바뀌면 여기서 바로 걸린다.
        /// </summary>
        private static void Apply(MonsterDefinition asset, Row row)
        {
            var so = new SerializedObject(asset);

            Set(so, "monsterId", p => p.stringValue = row.Id);
            Set(so, "displayName", p => p.stringValue = row.Name);
            Set(so, "tier", p => p.enumValueIndex = (int)row.Tier);

            Set(so, "stats.Vitality", p => p.floatValue = row.Vit);
            Set(so, "stats.Attack", p => p.floatValue = row.Atk);
            Set(so, "stats.Defense", p => p.floatValue = row.Def);
            Set(so, "stats.Agility", p => p.floatValue = row.Agi);
            Set(so, "stats.Focus", p => p.floatValue = row.Foc);

            Set(so, "hpPerVitality", p => p.floatValue = row.HpPerVit);
            Set(so, "moveSpeed", p => p.floatValue = row.MoveSpeed);
            Set(so, "detectRange", p => p.floatValue = row.DetectRange);
            Set(so, "attackRange", p => p.floatValue = row.AttackRange);
            Set(so, "attackCooldown", p => p.floatValue = row.AttackCooldown);
            Set(so, "knockbackForce", p => p.floatValue = row.Knockback);
            Set(so, "hitStunDuration", p => p.floatValue = row.HitStun);
            Set(so, "expReward", p => p.intValue = row.Exp);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void Set(SerializedObject so, string path, System.Action<SerializedProperty> write)
        {
            SerializedProperty property = so.FindProperty(path);

            if (property == null)
            {
                Debug.LogError(
                    $"[MonsterDefinition] '{path}' 필드를 찾지 못했습니다. " +
                    "MonsterDefinition 의 필드 이름이 바뀌었다면 이 도구도 함께 고쳐야 합니다.");
                return;
            }

            write(property);
        }

        private static Dictionary<string, MonsterDefinition> LoadExisting()
        {
            var map = new Dictionary<string, MonsterDefinition>();

            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(MonsterDefinition)}"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<MonsterDefinition>(path);

                if (asset == null || string.IsNullOrEmpty(asset.MonsterId)) continue;
                if (!map.ContainsKey(asset.MonsterId)) map.Add(asset.MonsterId, asset);
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
