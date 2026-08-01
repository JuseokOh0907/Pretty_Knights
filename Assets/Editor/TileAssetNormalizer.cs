using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PrettyKnights.EditorTools
{
    /// <summary>
    /// 맵 타일·오브젝트 텍스처 설정을 한 기준으로 맞추고,
    /// Tile 에셋의 Collider Type 을 선택 단위로 일괄 지정한다.
    ///
    /// 타일은 캐릭터와 기준이 다르다.
    /// <b>Mesh Type 은 Full Rect 여야 한다.</b> Tight 는 메시가 알파에 밀착하면서
    /// 인접 타일 사이에 머리카락 같은 틈(seam)을 만든다.
    /// 타일은 칸을 꽉 채우는 것이 전제이므로 밀착시킬 이유가 없다.
    /// </summary>
    public static class TileAssetNormalizer
    {
        private const string TargetRoot = "Assets/Art/Maps";
        private const string MenuRoot = "Pretty Knights/Tiles/";

        // ── 점검 ──────────────────────────────────────────────────────────

        [MenuItem(MenuRoot + "0. 현재 설정 점검 (변경 없음)", priority = 200)]
        public static void Report()
        {
            var sb = new StringBuilder($"[Tiles] {TargetRoot} 현황\n");

            var byMesh = new Dictionary<SpriteMeshType, int>();
            var byFormat = new Dictionary<string, int>();
            var byPpu = new Dictionary<float, int>();

            foreach (TextureImporter importer in AllTextureImporters())
            {
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);

                Bump(byMesh, settings.spriteMeshType);
                Bump(byPpu, importer.spritePixelsPerUnit);

                TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
                Bump(byFormat, android.overridden ? android.format.ToString() : "(오버라이드 없음)");
            }

            sb.AppendLine("  Mesh Type : " + Join(byMesh));
            sb.AppendLine("  PPU       : " + Join(byPpu));
            sb.AppendLine("  Android   : " + Join(byFormat));

            var byCollider = new Dictionary<Tile.ColliderType, int>();
            int nonTile = 0;
            foreach (TileBase tileBase in AllTiles())
            {
                if (tileBase is Tile tile) Bump(byCollider, tile.colliderType);
                else nonTile++;
            }

            sb.AppendLine("  Collider  : " + Join(byCollider) + (nonTile > 0 ? $", 기타 타일 타입 {nonTile}개" : ""));
            Debug.Log(sb.ToString());
        }

        // ── 텍스처 설정 통일 ───────────────────────────────────────────────

        [MenuItem(MenuRoot + "1. 타일 텍스처 설정 통일 (Full Rect / Point / Clamp)", priority = 201)]
        public static void NormalizeTextures()
        {
            int changed = 0, total = 0;

            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (TextureImporter importer in AllTextureImporters())
                {
                    total++;
                    bool dirty = false;

                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);

                    // 타일은 칸을 꽉 채운다. Tight 는 인접 타일 사이 틈의 원인이 된다.
                    if (settings.spriteMeshType != SpriteMeshType.FullRect)
                    {
                        settings.spriteMeshType = SpriteMeshType.FullRect;
                        importer.SetTextureSettings(settings);
                        dirty = true;
                    }

                    if (importer.filterMode != FilterMode.Point)
                    {
                        importer.filterMode = FilterMode.Point;
                        dirty = true;
                    }

                    // 가장자리에서 반대편 픽셀을 물어오면 타일 경계에 선이 생긴다.
                    if (importer.wrapMode != TextureWrapMode.Clamp)
                    {
                        importer.wrapMode = TextureWrapMode.Clamp;
                        dirty = true;
                    }

                    if (importer.mipmapEnabled)
                    {
                        importer.mipmapEnabled = false;
                        dirty = true;
                    }

                    if (!dirty) continue;

                    EditorUtility.SetDirty(importer);
                    importer.SaveAndReimport();
                    changed++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[Tiles] 텍스처 {total}개 중 {changed}개 갱신했습니다.");
        }

        // ── 플랫폼 압축 ────────────────────────────────────────────────────

        [MenuItem(MenuRoot + "2. 선택한 텍스처 → ASTC 4x4", priority = 210)]
        public static void SelectionToAstc() => ApplyPlatformToSelection(astc: true);

        [MenuItem(MenuRoot + "3. 선택한 텍스처 → 플랫폼 오버라이드 해제", priority = 211)]
        public static void SelectionClearOverride() => ApplyPlatformToSelection(astc: false);

        private static void ApplyPlatformToSelection(bool astc)
        {
            TextureImporter[] targets = SelectedImporters();
            if (targets.Length == 0)
            {
                Debug.LogWarning("[Tiles] Project 창에서 텍스처를 선택한 뒤 실행하세요.");
                return;
            }

            foreach (TextureImporter importer in targets)
            {
                foreach (string platform in new[] { "Android", "iPhone" })
                {
                    TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platform);
                    settings.overridden = astc;
                    if (astc)
                    {
                        settings.format = TextureImporterFormat.ASTC_4x4;
                        settings.maxTextureSize = 2048;
                    }
                    importer.SetPlatformTextureSettings(settings);
                }

                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }

            Debug.Log($"[Tiles] {targets.Length}개 텍스처의 플랫폼 설정을 " +
                      (astc ? "ASTC 4x4 로 지정했습니다." : "해제했습니다."));
        }

        // ── Collider Type ─────────────────────────────────────────────────

        [MenuItem(MenuRoot + "4. 선택한 Tile → Collider None", priority = 220)]
        public static void SelectionColliderNone() => SetCollider(Tile.ColliderType.None);

        [MenuItem(MenuRoot + "5. 선택한 Tile → Collider Sprite", priority = 221)]
        public static void SelectionColliderSprite() => SetCollider(Tile.ColliderType.Sprite);

        [MenuItem(MenuRoot + "6. 선택한 Tile → Collider Grid", priority = 222)]
        public static void SelectionColliderGrid() => SetCollider(Tile.ColliderType.Grid);

        private static void SetCollider(Tile.ColliderType type)
        {
            Tile[] tiles = Selection.GetFiltered<Tile>(SelectionMode.DeepAssets);
            if (tiles.Length == 0)
            {
                Debug.LogWarning("[Tiles] Project 창에서 Tile 에셋을 선택한 뒤 실행하세요.");
                return;
            }

            foreach (Tile tile in tiles)
            {
                if (tile.colliderType == type) continue;

                tile.colliderType = type;
                EditorUtility.SetDirty(tile);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Tiles] Tile {tiles.Length}개의 Collider Type 을 {type} 로 지정했습니다.");
        }

        // ── 공용 ──────────────────────────────────────────────────────────

        private static IEnumerable<TextureImporter> AllTextureImporters() =>
            AssetDatabase.FindAssets("t:Texture2D", new[] { TargetRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetImporter.GetAtPath)
                .OfType<TextureImporter>();

        private static IEnumerable<TileBase> AllTiles() =>
            AssetDatabase.FindAssets("t:TileBase", new[] { TargetRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<TileBase>)
                .Where(t => t != null);

        private static TextureImporter[] SelectedImporters() =>
            Selection.GetFiltered<Texture2D>(SelectionMode.DeepAssets)
                .Select(AssetDatabase.GetAssetPath)
                .Select(AssetImporter.GetAtPath)
                .OfType<TextureImporter>()
                .ToArray();

        private static void Bump<T>(Dictionary<T, int> map, T key) =>
            map[key] = map.TryGetValue(key, out int n) ? n + 1 : 1;

        private static string Join<T>(Dictionary<T, int> map) =>
            map.Count == 0 ? "(없음)" : string.Join(", ", map.Select(kv => $"{kv.Key} {kv.Value}개"));
    }
}
