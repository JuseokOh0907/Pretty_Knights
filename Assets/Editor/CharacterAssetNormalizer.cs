using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PrettyKnights.EditorTools
{
    /// <summary>
    /// 캐릭터 아트의 임포트·클립 설정을 한 기준으로 맞춘다.
    ///
    /// 동작별로 다른 사람이(또는 다른 도구가) 만든 에셋은 설정이 조금씩 어긋난다.
    /// 어긋난 채로 프리팹과 블렌드 트리를 얹으면 나중에 원인을 찾기 어려운
    /// 떨림·오버드로우·플랫폼별 용량 차이로 돌아온다.
    /// </summary>
    public static class CharacterAssetNormalizer
    {
        private const string TargetRoot = "Assets/Art/Characters";
        private const string MenuRoot = "Pretty Knights/Sprite/";

        private const int PixelsPerUnit = 256;
        private const int MaxTextureSize = 2048;

        [MenuItem(MenuRoot + "3. 캐릭터 텍스처 설정 통일", priority = 110)]
        public static void NormalizeTextures()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { TargetRoot });
            int changed = 0;

            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;
                    if (Normalize(importer)) changed++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[Normalizer] 텍스처 {guids.Length}개 중 {changed}개 갱신했습니다.");
        }

        private static bool Normalize(TextureImporter importer)
        {
            bool dirty = false;

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                dirty = true;
            }

            // 투명 여백까지 그리지 않도록 알파에 밀착한 메시를 쓴다.
            if (importer.spriteMeshType != SpriteMeshType.Tight)
            {
                importer.spriteMeshType = SpriteMeshType.Tight;
                dirty = true;
            }

            // 픽셀아트이므로 보간하지 않는다.
            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                dirty = true;
            }

            if (!Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit))
            {
                importer.spritePixelsPerUnit = PixelsPerUnit;
                dirty = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                dirty = true;
            }

            dirty |= ApplyAstc(importer, "Android");
            dirty |= ApplyAstc(importer, "iPhone");

            if (dirty)
            {
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }

            return dirty;
        }

        private static bool ApplyAstc(TextureImporter importer, string platform)
        {
            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platform);

            bool needsChange =
                !settings.overridden ||
                settings.format != TextureImporterFormat.ASTC_4x4 ||
                settings.maxTextureSize != MaxTextureSize;

            if (!needsChange) return false;

            settings.overridden = true;
            settings.format = TextureImporterFormat.ASTC_4x4;
            settings.maxTextureSize = MaxTextureSize;
            settings.textureCompression = TextureImporterCompression.Compressed;

            importer.SetPlatformTextureSettings(settings);
            return true;
        }

        [MenuItem(MenuRoot + "4. 애니메이션 클립 Loop Time 켜기", priority = 111)]
        public static void EnableLoopOnClips()
        {
            string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { TargetRoot });
            int changed = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip == null) continue;

                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                if (settings.loopTime) continue;

                // 블렌드 트리 안의 모션은 루프여야 정규화 시간이 일관되게 흐른다.
                // 1프레임 클립도 나중에 프레임이 늘어날 것을 대비해 켜 둔다.
                settings.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                EditorUtility.SetDirty(clip);
                changed++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Normalizer] 클립 {guids.Length}개 중 {changed}개에 Loop Time 을 켰습니다.");
        }

        [MenuItem(MenuRoot + "0. 현재 설정 점검 (변경 없음)", priority = 90)]
        public static void Report()
        {
            var lines = AssetDatabase
                .FindAssets("t:Texture2D", new[] { TargetRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => (path, importer: AssetImporter.GetAtPath(path) as TextureImporter))
                .Where(t => t.importer != null)
                .Select(t =>
                {
                    TextureImporterPlatformSettings android =
                        t.importer.GetPlatformTextureSettings("Android");

                    return $"  {System.IO.Path.GetFileName(t.path),-46} " +
                           $"mesh={t.importer.spriteMeshType,-9} " +
                           $"ppu={t.importer.spritePixelsPerUnit,-6} " +
                           $"android={(android.overridden ? android.format.ToString() : "(없음)")}";
                });

            Debug.Log("[Normalizer] 캐릭터 텍스처 현황\n" + string.Join("\n", lines));
        }
    }
}
