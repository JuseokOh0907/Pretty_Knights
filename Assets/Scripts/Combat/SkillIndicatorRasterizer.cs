using System.Collections.Generic;
using PrettyKnights.Characters;
using UnityEngine;

namespace PrettyKnights.Combat
{
    /// <summary>
    /// 판정 도형을 <b>픽셀 격자에 찍어</b> 스프라이트로 만든다.
    /// 근거: docs/decisions/007-skill-indicator.md
    ///
    /// <b>메시로 그리지 않는 이유</b> — 폴리곤의 매끈한 가장자리가 64px 도트 위에서 튄다.
    /// 여기서는 <see cref="SkillShape.Contains"/> 로 텍셀마다 검사해 계단을 만든다.
    /// 판정과 같은 수학이므로 보이는 범위와 맞는 범위가 1픽셀 안에서 일치한다.
    ///
    /// <b>8방향으로 구워 캐시한다.</b> 픽셀 아트는 회전시키면 어긋나므로
    /// 런타임 회전을 쓰지 않고 방향마다 텍스처를 따로 갖는다.
    /// 같은 스킬을 쓰는 몬스터가 여럿이어도 텍스처는 방향당 하나다.
    ///
    /// 색은 굽지 않는다. 알파만 기록하고 <c>SpriteRenderer.color</c> 로 물들인다 —
    /// 그래야 빨강·노랑을 같은 텍스처로 쓴다.
    /// </summary>
    public static class SkillIndicatorRasterizer
    {
        /// <summary>타일과 같은 밀도. 인디케이터 픽셀이 타일 픽셀과 어긋나지 않는다.</summary>
        public const int PixelsPerUnit = PixelSpriteBaker.PixelsPerUnit;

        /// <summary>안쪽 채움의 알파. 테두리는 항상 1이라 더 진하게 보인다.</summary>
        private const byte FillAlpha = 110;

        private readonly struct Key
        {
            private readonly SkillShapeKind kind;
            private readonly float range, width, angle, forwardOffset;
            private readonly int direction;

            public Key(SkillShapeKind kind, SkillShapeParams p, EightDirection direction)
            {
                this.kind = kind;
                range = p.range; width = p.width; angle = p.angle; forwardOffset = p.forwardOffset;
                this.direction = (int)direction;
            }

            public override int GetHashCode() => System.HashCode.Combine(
                kind, range, width, angle, forwardOffset, direction);

            public override bool Equals(object obj) =>
                obj is Key k && k.kind == kind && k.direction == direction &&
                Mathf.Approximately(k.range, range) && Mathf.Approximately(k.width, width) &&
                Mathf.Approximately(k.angle, angle) && Mathf.Approximately(k.forwardOffset, forwardOffset);
        }

        private static readonly Dictionary<Key, Sprite> Cache = new Dictionary<Key, Sprite>();

        /// <summary>
        /// 도메인 리로드를 끈 상태에서도 플레이 시작 시 옛 텍스처가 남지 않게 비운다.
        /// 남으면 파괴된 Texture2D 를 참조하는 Sprite 가 살아 있어 분홍색으로 그려진다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => Cache.Clear();

        /// <summary>
        /// 방향별로 구운 스프라이트. 이미 있으면 그대로 돌려준다.
        /// 피벗은 <b>도형의 원점</b>이므로 시전자 위치에 그대로 놓으면 맞는다.
        /// </summary>
        public static Sprite Get(SkillShapeKind kind, SkillShapeParams param, EightDirection direction)
        {
            var key = new Key(kind, param, direction);
            if (Cache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            Sprite baked = Bake(kind, param, direction);
            Cache[key] = baked;
            return baked;
        }

        private static Sprite Bake(SkillShapeKind kind, SkillShapeParams param, EightDirection direction)
        {
            Vector2 facing = direction.ToVector();
            Rect bounds = SkillShape.LocalBounds(kind, param, facing);

            string what = $"SkillIndicator_{kind}_{direction}";
            int width = PixelSpriteBaker.SideOf(bounds.width, what);
            int height = PixelSpriteBaker.SideOf(bounds.height, what);

            var inside = new bool[width * height];

            // 1) 포함 검사. 텍셀 중심을 월드 좌표로 되돌려 판정과 같은 함수에 넣는다.
            for (int y = 0; y < height; y++)
            {
                float wy = PixelSpriteBaker.TexelCenter(bounds.yMin, y);

                for (int x = 0; x < width; x++)
                {
                    float wx = PixelSpriteBaker.TexelCenter(bounds.xMin, x);
                    inside[y * width + x] =
                        SkillShape.Contains(kind, param, Vector2.zero, facing, new Vector2(wx, wy));
                }
            }

            // 2) 테두리. 안쪽이면서 상하좌우 중 하나라도 바깥이면 가장자리다.
            //    도트 게임 예고선의 관용적인 모양이라 없으면 뭉개져 보인다.
            var pixels = new Color32[width * height];
            var fill = new Color32(255, 255, 255, FillAlpha);
            var edge = new Color32(255, 255, 255, 255);
            var clear = new Color32(255, 255, 255, 0);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = y * width + x;

                    if (!inside[i]) { pixels[i] = clear; continue; }

                    bool border =
                        x == 0 || x == width - 1 || y == 0 || y == height - 1 ||
                        !inside[i - 1] || !inside[i + 1] ||
                        !inside[i - width] || !inside[i + width];

                    pixels[i] = border ? edge : fill;
                }
            }

            // 피벗을 도형 원점에 맞춘다. 시전자 위치에 그대로 놓으면 맞는다.
            return PixelSpriteBaker.Create(
                what, width, height, pixels,
                PixelSpriteBaker.PivotAtOrigin(bounds, width, height));
        }

        /// <summary>구워둔 것이 몇 개인지. 디버그용.</summary>
        public static int CachedCount => Cache.Count;
    }
}
