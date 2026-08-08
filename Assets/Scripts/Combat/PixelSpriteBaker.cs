using UnityEngine;

namespace PrettyKnights.Combat
{
    /// <summary>
    /// 픽셀 격자에 찍은 결과를 스프라이트로 만든다. 예고와 임팩트가 함께 쓴다.
    /// 근거: docs/decisions/007-skill-indicator.md
    ///
    /// <b>도트를 지키는 규칙이 여기 한 곳에만 있다.</b> 64 px/unit · Point 필터 ·
    /// Clamp. 두 곳에 흩어지면 한쪽만 Bilinear 로 바뀌어도 알아채기 어렵다.
    ///
    /// 색은 굽지 않는다. 알파만 기록하고 <c>SpriteRenderer.color</c> 로 물들인다 —
    /// 그래야 아군 파랑과 적 빨강이 같은 텍스처를 쓴다.
    /// </summary>
    internal static class PixelSpriteBaker
    {
        /// <summary>타일과 같은 밀도. 이펙트 픽셀이 타일 픽셀과 어긋나지 않는다.</summary>
        public const int PixelsPerUnit = 64;

        /// <summary>한 변의 상한. 사거리가 터무니없이 크면 메모리를 지킨다.</summary>
        public const int MaxSide = 512;

        /// <summary>
        /// 월드 크기를 텍셀 수로 바꾼다. <b>상한에 걸리면 알린다</b> —
        /// 조용히 자르면 범위가 잘린 채로 그려져 "판정은 맞는데 안 보인다" 가 된다.
        /// </summary>
        public static int SideOf(float worldSize, string what)
        {
            int exact = Mathf.CeilToInt(worldSize * PixelsPerUnit);
            if (exact <= MaxSide) return Mathf.Max(1, exact);

            Debug.LogWarning(
                $"[PixelSpriteBaker] '{what}' 의 한 변이 {exact}px 이라 {MaxSide}px 로 잘립니다. " +
                $"사거리가 {MaxSide / (float)PixelsPerUnit:0.#} 유닛을 넘으면 이펙트가 잘려 보입니다.");

            return MaxSide;
        }

        /// <summary>
        /// 텍셀 <paramref name="x"/> 의 중심이 놓인 로컬 좌표.
        /// 굽는 쪽과 피벗을 재는 쪽이 <b>같은 식</b>을 써야 그림과 판정이 어긋나지 않는다.
        /// </summary>
        public static float TexelCenter(float boundsMin, int index) =>
            boundsMin + (index + 0.5f) / PixelsPerUnit;

        /// <summary>
        /// 원점이 텍스처 안에서 차지하는 비율. <b>bounds 가 아니라 실제 텍셀 수로 나눈다</b> —
        /// 상한에 걸려 잘렸을 때 bounds 로 나누면 피벗이 어긋나 이펙트가 통째로 밀린다.
        /// </summary>
        public static Vector2 PivotAtOrigin(Rect bounds, int width, int height) => new Vector2(
            -bounds.xMin * PixelsPerUnit / width,
            -bounds.yMin * PixelsPerUnit / height);

        /// <summary>
        /// 텍스처와 스프라이트를 만든다. <paramref name="pivot"/> 은 정규화 좌표다.
        /// </summary>
        public static Sprite Create(string name, int width, int height, Color32[] pixels, Vector2 pivot)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false)
            {
                // 도트를 지키는 두 줄이다. Bilinear 면 계단이 뭉개진다.
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = name
            };

            texture.SetPixels32(pixels);
            texture.Apply(updateMipmaps: false);

            Sprite sprite = Sprite.Create(
                texture, new Rect(0, 0, width, height), pivot, PixelsPerUnit,
                extrude: 0, meshType: SpriteMeshType.FullRect);

            sprite.name = name;
            return sprite;
        }

        /// <summary>
        /// 알파가 0 이 아닌 부분만 잘라 스프라이트로 만든다.
        /// 잘라낸 조각의 <b>중심이 원점에서 얼마나 떨어져 있는지</b>를 함께 돌려준다.
        ///
        /// <b>피벗을 원점에 두지 않는 이유</b>는 프레임마다 그려지는 자리가 크게 다르기 때문이다.
        /// 베기 첫 프레임은 부채꼴 한쪽 끝에만 있어 원점을 포함하지 않는데,
        /// 그런 조각에 원점 피벗을 주려면 피벗이 0~1 밖으로 나가야 한다.
        /// 대신 <b>중앙 피벗 + 오프셋</b>으로 두면 어떤 조각이든 같은 방식으로 놓인다.
        ///
        /// 전부 비어 있으면 <c>null</c> 을 돌려준다 — 그린 것이 없는 프레임이다.
        /// </summary>
        public static Sprite CreateTrimmed(
            string name, Rect bounds, int width, int height, Color32[] pixels, out Vector2 offset)
        {
            offset = Vector2.zero;

            int minX = width, minY = height, maxX = -1, maxY = -1;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (pixels[y * width + x].a == 0) continue;

                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            if (maxX < minX) return null;

            int trimmedWidth = maxX - minX + 1;
            int trimmedHeight = maxY - minY + 1;
            var trimmed = new Color32[trimmedWidth * trimmedHeight];

            for (int y = 0; y < trimmedHeight; y++)
                for (int x = 0; x < trimmedWidth; x++)
                    trimmed[y * trimmedWidth + x] = pixels[(minY + y) * width + (minX + x)];

            // 조각의 중심을 로컬 좌표로 되돌린다. 굽는 쪽과 같은 TexelCenter 를 쓴다.
            offset = new Vector2(
                (TexelCenter(bounds.xMin, minX) + TexelCenter(bounds.xMin, maxX)) * 0.5f,
                (TexelCenter(bounds.yMin, minY) + TexelCenter(bounds.yMin, maxY)) * 0.5f);

            return Create(name, trimmedWidth, trimmedHeight, trimmed, new Vector2(0.5f, 0.5f));
        }
    }
}
