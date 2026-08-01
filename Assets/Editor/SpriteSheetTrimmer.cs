using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PrettyKnights.EditorTools
{
    /// <summary>
    /// 캐릭터 스프라이트 시트의 투명 여백을 잘라낸다.
    ///
    /// 프레임마다 따로 자르면 프레임별 rect 가 달라져 Center 피벗 기준이 흔들리고
    /// 걸을 때 캐릭터가 떨린다. 그래서 이 도구는
    ///
    ///   1. 대상 전체(모든 동작 · 모든 방향 · 모든 프레임)의 불투명 영역 합집합을 구하고
    ///   2. 그 합집합을 감싸면서 <b>셀 중심에 대칭</b>인 하나의 rect 로 확장한 뒤
    ///   3. 모든 프레임을 그 동일한 rect 로 잘라낸다
    ///
    /// 중심 대칭이므로 Center 피벗이 그대로 유지되어 캐릭터 위치가 움직이지 않는다.
    /// 재슬라이싱 시 <b>스프라이트 이름을 그대로 유지</b>하므로 기존 .anim 클립의
    /// 스프라이트 참조도 끊기지 않는다.
    ///
    /// 되돌리려면: git checkout Assets/Art/Characters
    /// </summary>
    public static class SpriteSheetTrimmer
    {
        private const string TargetRoot = "Assets/Art/Characters";

        /// <summary>이 값 이하의 알파는 투명으로 본다. 안티에일리어싱 잔여 픽셀을 흡수한다.</summary>
        private const byte AlphaThreshold = 8;

        /// <summary>ASTC 4x4 블록 패딩이 생기지 않도록 크롭 크기를 4의 배수로 맞춘다.</summary>
        private const int BlockAlignment = 4;

        private const string MenuRoot = "Pretty Knights/Sprite/";

        [MenuItem(MenuRoot + "1. 트림 미리보기 (파일 변경 없음)", priority = 100)]
        public static void Preview() => Run(apply: false);

        [MenuItem(MenuRoot + "2. 트림 적용 (PNG 덮어쓰기)", priority = 101)]
        public static void Apply()
        {
            bool ok = EditorUtility.DisplayDialog(
                "스프라이트 트림 적용",
                $"{TargetRoot} 아래 PNG 원본을 덮어씁니다.\n\n" +
                "커밋되지 않은 변경이 있으면 먼저 커밋하세요.\n" +
                "되돌리기: git checkout Assets/Art/Characters",
                "적용", "취소");

            if (ok) Run(apply: true);
        }

        private static void Run(bool apply)
        {
            List<Sheet> sheets = CollectSheets();
            if (sheets.Count == 0)
            {
                Debug.LogWarning($"[Trimmer] {TargetRoot} 아래에서 대상 텍스처를 찾지 못했습니다.");
                return;
            }

            var cellSizes = sheets.Select(s => s.CellSize).Distinct().ToList();

            // 셀 크기가 섞여 있다 = 이미 트림된 세트에 원본 크기 파일이 새로 들어왔다.
            // (아트를 하나만 다시 뽑아 덮어쓴 경우) 전체를 다시 자르는 대신
            // 기존 셀 크기에 맞춰 새로 들어온 것만 자른다.
            if (cellSizes.Count > 1)
            {
                RetrimNewcomers(sheets, cellSizes, apply);
                return;
            }

            Vector2Int cell = cellSizes[0];
            RectInt union = ComputeUnion(sheets);
            if (union.width <= 0)
            {
                Debug.LogError("[Trimmer] 불투명 픽셀을 찾지 못했습니다.");
                return;
            }

            RectInt crop = ToCenterSymmetric(union, cell);
            LogPlan(sheets, cell, union, crop);

            if (!apply) return;

            ApplyCrop(sheets, cell, crop);
        }

        /// <summary>
        /// 이미 트림된 세트(작은 셀)에 원본 크기 파일이 섞여 들어온 경우,
        /// 기존 셀 크기에 맞춰 새로 들어온 것만 같은 중심 대칭 rect 로 자른다.
        /// 전체를 다시 자르면 이미 잘린 것들이 한 번 더 깎여 정렬이 무너진다.
        /// </summary>
        private static void RetrimNewcomers(List<Sheet> sheets, List<Vector2Int> cellSizes, bool apply)
        {
            // 가장 작은 셀을 이미 확정된 기준으로 본다.
            Vector2Int target = cellSizes.OrderBy(v => v.x * v.y).First();
            List<Sheet> oversized = sheets.Where(s => s.CellSize != target).ToList();

            var sb = new StringBuilder();
            sb.AppendLine("[Trimmer] 셀 크기가 섞여 있습니다. 기준 셀에 맞춰 새로 들어온 것만 자릅니다.");
            sb.AppendLine($"  기준 셀 : {target.x} x {target.y} ({sheets.Count - oversized.Count}장)");

            bool blocked = false;

            foreach (Sheet sheet in oversized)
            {
                Vector2Int cell = sheet.CellSize;
                if (cell.x < target.x || cell.y < target.y)
                {
                    sb.AppendLine($"  {System.IO.Path.GetFileName(sheet.Path)} — 기준보다 작아 처리할 수 없습니다 ({cell.x}x{cell.y})");
                    blocked = true;
                    continue;
                }

                var crop = new RectInt((cell.x - target.x) / 2, (cell.y - target.y) / 2, target.x, target.y);
                RectInt content = ComputeUnion(new List<Sheet> { sheet });

                bool fits = content.xMin >= crop.xMin && content.xMax <= crop.xMax &&
                            content.yMin >= crop.yMin && content.yMax <= crop.yMax;

                sb.AppendLine(
                    $"  {System.IO.Path.GetFileName(sheet.Path)} — {cell.x}x{cell.y} -> {target.x}x{target.y}, " +
                    (fits ? "그림이 크롭 창 안에 들어감" : "!! 그림이 크롭 창을 벗어나 잘립니다"));

                if (!fits) blocked = true;
            }

            if (blocked)
            {
                sb.AppendLine("  → 잘려나가는 그림이 있어 적용하지 않았습니다. 원본 캔버스 정렬을 확인하세요.");
                Debug.LogError(sb.ToString());
                return;
            }

            Debug.Log(sb.ToString());
            if (!apply) return;

            foreach (Sheet sheet in oversized)
            {
                Vector2Int cell = sheet.CellSize;
                var crop = new RectInt((cell.x - target.x) / 2, (cell.y - target.y) / 2, target.x, target.y);
                WriteCropped(sheet, crop);
                RewriteSlices(sheet, crop);
                Object.DestroyImmediate(sheet.Source);
            }

            AssetDatabase.Refresh();
            Debug.Log($"[Trimmer] {oversized.Count}장을 {target.x} x {target.y} 로 맞췄습니다.");
        }

        // ── 수집 ──────────────────────────────────────────────────────────

        private sealed class Sheet
        {
            public string Path;
            public TextureImporter Importer;
            public Texture2D Source;

            /// <summary>프레임 rect 를 텍스처 좌표(좌하단 원점)로 담는다. Single 이면 1개.</summary>
            public List<RectInt> FrameRects;

            /// <summary>Multiple 인 경우의 원본 슬라이스 정보. 이름·피벗을 그대로 물려주기 위해 보관.</summary>
            public SpriteMetaData[] Slices;

            public Vector2Int CellSize => new Vector2Int(FrameRects[0].width, FrameRects[0].height);
        }

        private static List<Sheet> CollectSheets()
        {
            var result = new List<Sheet>();

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TargetRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)) continue;
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;

                Texture2D source = LoadSourcePixels(path);
                if (source == null) continue;

                var sheet = new Sheet
                {
                    Path = path,
                    Importer = importer,
                    Source = source,
                    Slices = importer.spritesheet,
                    FrameRects = new List<RectInt>()
                };

                if (importer.spriteImportMode == SpriteImportMode.Multiple && sheet.Slices.Length > 0)
                {
                    // 가로로 늘어선 프레임 순서를 보장한다.
                    sheet.Slices = sheet.Slices.OrderBy(s => s.rect.x).ThenBy(s => s.rect.y).ToArray();
                    foreach (SpriteMetaData s in sheet.Slices)
                    {
                        sheet.FrameRects.Add(new RectInt(
                            Mathf.RoundToInt(s.rect.x), Mathf.RoundToInt(s.rect.y),
                            Mathf.RoundToInt(s.rect.width), Mathf.RoundToInt(s.rect.height)));
                    }
                }
                else
                {
                    sheet.FrameRects.Add(new RectInt(0, 0, source.width, source.height));
                }

                result.Add(sheet);
            }

            return result;
        }

        /// <summary>
        /// 임포트 설정(압축·리사이즈)을 거치지 않은 원본 픽셀을 읽는다.
        /// 임포트된 Texture2D 를 그대로 읽으면 ASTC 로 압축된 결과가 나와 트림이 뭉개진다.
        /// </summary>
        private static Texture2D LoadSourcePixels(string assetPath)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(assetPath);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
                if (tex.LoadImage(bytes)) return tex;

                Object.DestroyImmediate(tex);
                Debug.LogWarning($"[Trimmer] 디코딩 실패: {assetPath}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Trimmer] 읽기 실패 {assetPath}: {e.Message}");
            }

            return null;
        }

        // ── 계산 ──────────────────────────────────────────────────────────

        /// <summary>모든 시트·모든 프레임의 불투명 영역을 셀 로컬 좌표로 합집합한다.</summary>
        private static RectInt ComputeUnion(List<Sheet> sheets)
        {
            int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;

            foreach (Sheet sheet in sheets)
            {
                Color32[] pixels = sheet.Source.GetPixels32();
                int texWidth = sheet.Source.width;

                foreach (RectInt frame in sheet.FrameRects)
                {
                    for (int y = 0; y < frame.height; y++)
                    {
                        int rowStart = (frame.y + y) * texWidth + frame.x;
                        for (int x = 0; x < frame.width; x++)
                        {
                            if (pixels[rowStart + x].a <= AlphaThreshold) continue;

                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                        }
                    }
                }
            }

            return maxX < 0
                ? new RectInt(0, 0, 0, 0)
                : new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        /// <summary>
        /// 합집합을 감싸면서 셀 중심에 대칭인 rect 로 확장하고 4의 배수로 맞춘다.
        /// 중심 대칭이어야 Center 피벗이 유지되어 캐릭터가 움직이지 않는다.
        /// </summary>
        private static RectInt ToCenterSymmetric(RectInt union, Vector2Int cell)
        {
            float cx = (cell.x - 1) * 0.5f;
            float cy = (cell.y - 1) * 0.5f;

            float halfW = Mathf.Max(cx - union.xMin, union.xMax - 1 - cx);
            float halfH = Mathf.Max(cy - union.yMin, union.yMax - 1 - cy);

            int width = Mathf.CeilToInt(halfW * 2f + 1f);
            int height = Mathf.CeilToInt(halfH * 2f + 1f);

            width = AlignUp(width, BlockAlignment);
            height = AlignUp(height, BlockAlignment);

            // 셀보다 커지면 자를 이유가 없다.
            width = Mathf.Min(width, cell.x);
            height = Mathf.Min(height, cell.y);

            // 중심 대칭이 되려면 양쪽 여백이 같아야 하므로 셀과 크기의 패리티가 같아야 한다.
            if (((cell.x - width) & 1) != 0) width = Mathf.Min(width + 1, cell.x);
            if (((cell.y - height) & 1) != 0) height = Mathf.Min(height + 1, cell.y);

            return new RectInt((cell.x - width) / 2, (cell.y - height) / 2, width, height);
        }

        private static int AlignUp(int value, int alignment) =>
            ((value + alignment - 1) / alignment) * alignment;

        private static void LogPlan(List<Sheet> sheets, Vector2Int cell, RectInt union, RectInt crop)
        {
            int frames = sheets.Sum(s => s.FrameRects.Count);
            float saved = 1f - (float)(crop.width * crop.height) / (cell.x * cell.y);

            var sb = new StringBuilder();
            sb.AppendLine("[Trimmer] 트림 계획");
            sb.AppendLine($"  대상        : 시트 {sheets.Count}개 / 프레임 {frames}개");
            sb.AppendLine($"  현재 셀     : {cell.x} x {cell.y}");
            sb.AppendLine($"  불투명 합집합: x {union.xMin}..{union.xMax - 1}, y {union.yMin}..{union.yMax - 1}  ({union.width} x {union.height})");
            sb.AppendLine($"  크롭 rect   : x {crop.xMin}, y {crop.yMin}, {crop.width} x {crop.height}  (셀 중심 대칭 · {BlockAlignment}의 배수)");
            sb.AppendLine($"  면적 절감   : {saved:P1}");
            sb.AppendLine("  ※ 중심 대칭이므로 Center 피벗 기준 캐릭터 위치는 변하지 않습니다.");

            Debug.Log(sb.ToString());
        }

        // ── 적용 ──────────────────────────────────────────────────────────

        private static void ApplyCrop(List<Sheet> sheets, Vector2Int cell, RectInt crop)
        {
            if (crop.width == cell.x && crop.height == cell.y)
            {
                Debug.Log("[Trimmer] 잘라낼 여백이 없어 아무것도 하지 않았습니다.");
                return;
            }

            try
            {
                AssetDatabase.StartAssetEditing();

                for (int i = 0; i < sheets.Count; i++)
                {
                    Sheet sheet = sheets[i];
                    EditorUtility.DisplayProgressBar(
                        "스프라이트 트림", sheet.Path, (float)i / sheets.Count);

                    WriteCropped(sheet, crop);
                    RewriteSlices(sheet, crop);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            foreach (Sheet sheet in sheets)
            {
                if (sheet.Source != null) Object.DestroyImmediate(sheet.Source);
            }

            Debug.Log($"[Trimmer] 완료 — 시트 {sheets.Count}개를 {crop.width} x {crop.height} 셀로 잘랐습니다.");
        }

        private static void WriteCropped(Sheet sheet, RectInt crop)
        {
            int count = sheet.FrameRects.Count;
            var output = new Texture2D(crop.width * count, crop.height, TextureFormat.RGBA32, mipChain: false);

            Color32[] src = sheet.Source.GetPixels32();
            int srcWidth = sheet.Source.width;
            var dst = new Color32[output.width * output.height];

            for (int f = 0; f < count; f++)
            {
                RectInt frame = sheet.FrameRects[f];
                int dstOriginX = f * crop.width;

                for (int y = 0; y < crop.height; y++)
                {
                    int srcRow = (frame.y + crop.yMin + y) * srcWidth + frame.x + crop.xMin;
                    int dstRow = y * output.width + dstOriginX;

                    for (int x = 0; x < crop.width; x++)
                        dst[dstRow + x] = src[srcRow + x];
                }
            }

            output.SetPixels32(dst);
            output.Apply();
            File.WriteAllBytes(sheet.Path, output.EncodeToPNG());
            Object.DestroyImmediate(output);
        }

        /// <summary>
        /// 새 셀 크기에 맞춰 슬라이스를 다시 쓴다.
        /// <b>이름을 그대로 유지</b>하므로 Unity 가 기존 스프라이트 fileID 를 재사용하고,
        /// .anim 클립의 참조가 끊기지 않는다.
        /// </summary>
        private static void RewriteSlices(Sheet sheet, RectInt crop)
        {
            if (sheet.Importer.spriteImportMode != SpriteImportMode.Multiple) return;

            var updated = new SpriteMetaData[sheet.Slices.Length];
            for (int i = 0; i < sheet.Slices.Length; i++)
            {
                SpriteMetaData meta = sheet.Slices[i];
                meta.rect = new Rect(i * crop.width, 0f, crop.width, crop.height);
                updated[i] = meta; // name · alignment · pivot · border 는 원본 그대로
            }

            sheet.Importer.spritesheet = updated;
            EditorUtility.SetDirty(sheet.Importer);
            sheet.Importer.SaveAndReimport();
        }
    }
}
