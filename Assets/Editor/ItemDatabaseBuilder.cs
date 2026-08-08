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
    /// 프로젝트의 <see cref="ItemDefinition"/> 을 전부 모아 <see cref="ItemDatabase"/> 에 넣는다.
    ///
    /// <b>손으로 관리하면 반드시 빠뜨린다.</b> 그리고 빠뜨린 아이템은
    /// <b>세이브에서 조용히 사라진다</b> — 인벤토리는 itemId 문자열만 저장하므로
    /// 표에 없으면 불러올 때 풀리지 않는다. 증상이 "가끔 아이템이 없어진다" 라
    /// 원인을 찾기 어렵다.
    ///
    /// itemId 중복도 여기서 잡는다. 둘이면 세이브가 어느 쪽인지 가릴 수 없다.
    /// </summary>
    public static class ItemDatabaseBuilder
    {
        private const string MenuRoot = "Pretty Knights/Data/";
        private const string DatabasePath = "Assets/Data/ItemDatabase.asset";

        [MenuItem(MenuRoot + "4. 아이템 목록 점검 (변경 없음)", priority = 220)]
        public static void Report()
        {
            ItemDefinition[] found = LoadAll();
            var database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(DatabasePath);

            var report = new StringBuilder($"[ItemDatabase] 프로젝트의 아이템 {found.Length}종\n");

            foreach (ItemDefinition item in found.OrderBy(i => i.Category).ThenBy(i => i.ItemId))
                report.AppendLine(
                    $"  {item.ItemId,-28} {item.Category,-11} 최대 {item.MaxStack,3}개" +
                    $"{(item.Usable ? " · 사용 가능" : "")}{(item.Icon == null ? "  ← 아이콘 없음" : "")}");

            report.AppendLine();
            report.Append(Describe(found, database));

            Debug.Log(report.ToString());
        }

        private static string Describe(ItemDefinition[] found, ItemDatabase database)
        {
            var text = new StringBuilder();

            var seen = new HashSet<string>();
            foreach (ItemDefinition item in found)
            {
                if (string.IsNullOrEmpty(item.ItemId))
                    text.AppendLine($"  ✗ '{item.name}' 의 itemId 가 비어 있습니다.");
                else if (!seen.Add(item.ItemId))
                    text.AppendLine($"  ✗ itemId '{item.ItemId}' 가 둘 이상입니다.");
            }

            if (database == null)
            {
                text.AppendLine($"  · {DatabasePath} 가 없습니다 — \"5. 아이템 목록 갱신\" 을 실행하세요.");
                return text.ToString();
            }

            int missing = found.Count(i => !database.All.Contains(i));
            if (missing > 0)
                text.AppendLine($"  · 표에 없는 아이템 {missing}종 — 갱신하지 않으면 세이브에서 사라집니다.");

            int stale = database.All.Count(i => i == null);
            if (stale > 0) text.AppendLine($"  · 표에 빈 칸 {stale}개 (지워진 에셋)");

            if (text.Length == 0) text.Append($"  표가 최신입니다 ({database.Count}종).");
            return text.ToString();
        }

        [MenuItem(MenuRoot + "5. 아이템 목록 갱신", priority = 221)]
        public static void Build()
        {
            EnsureFolder();

            var database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(DatabasePath);
            bool created = false;

            if (database == null)
            {
                database = ScriptableObject.CreateInstance<ItemDatabase>();
                AssetDatabase.CreateAsset(database, DatabasePath);
                created = true;
            }

            ItemDefinition[] found = LoadAll()
                .OrderBy(i => i.Category)
                .ThenBy(i => i.ItemId)
                .ToArray();

            database.SetAll(found);

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[ItemDatabase] 아이템 {found.Length}종을 담았습니다{(created ? " (표를 새로 만듦)" : "")} → {DatabasePath}\n" +
                "  GameRoot 의 Item Database 에 이 에셋이 연결되어 있어야 합니다.\n" +
                "  아이템을 새로 만들 때마다 이걸 다시 실행하세요 — 빠지면 세이브에서 사라집니다.",
                database);
        }

        private static ItemDefinition[] LoadAll() =>
            AssetDatabase.FindAssets($"t:{nameof(ItemDefinition)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ItemDefinition>)
                .Where(i => i != null)
                .ToArray();

        private static void EnsureFolder()
        {
            string folder = Path.GetDirectoryName(DatabasePath)!.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(folder)) return;

            AssetDatabase.CreateFolder("Assets", "Data");
        }
    }
}
