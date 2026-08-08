using System.Collections.Generic;
using PrettyKnights.Characters;
using UnityEngine;

namespace PrettyKnights.Combat
{
    /// <summary>
    /// 타격 순간을 <b>4~8프레임 도트 애니메이션</b>으로 굽는다 (CLAUDE.md §5 의 "임팩트").
    /// 예고와 같은 판단이다 — 메시로 그리면 매끈한 가장자리가 64px 도트 위에서 튄다
    /// (docs/decisions/007-skill-indicator.md §2).
    ///
    /// <b>움직임은 그림이 아니라 "진행도" 에서 나온다.</b> 텍셀마다 그 도형의
    /// 어느 시점에 속하는지를 0~1 로 재고, 프레임마다 그 값의 좁은 띠만 남긴다.
    /// 띠가 훑고 지나가는 것이 화면에서는 베기·관통·폭발로 읽힌다.
    ///
    /// <code>
    /// 부채꼴  진행도 = 한쪽 끝에서 반대쪽 끝까지의 각도   → 훑는다 (벤다)
    /// 직선    진행도 = 앞으로 나아간 거리                 → 뻗는다 (꿰뚫는다)
    /// 원      진행도 = 중심에서의 거리                    → 퍼진다 (터진다)
    /// 십자    진행도 = 원점에서의 거리                    → 퍼진다
    /// </code>
    ///
    /// <b>아트가 한 장도 필요 없다.</b> 부채꼴 각도가 스킬마다 달라도,
    /// 사거리가 몬스터마다 달라도 같은 코드가 맞는 모양을 만든다.
    /// 그리고 판정과 같은 <see cref="SkillShape.Contains"/> 를 쓰므로
    /// <b>맞은 자리에만 이펙트가 뜬다.</b>
    ///
    /// 알파는 <b>몇 단계로 끊는다.</b> 매끄러운 그라데이션은 도트가 아니라 그림자다.
    /// </summary>
    public static class SkillImpactRasterizer
    {
        /// <summary>훑고 지나가는 띠의 길이. 진행도 기준 비율.</summary>
        private const float TrailLength = 0.5f;

        /// <summary>알파 단계. 도트 이펙트는 색을 몇 개만 쓴다.</summary>
        private static readonly byte[] LevelAlpha = { 70, 140, 215 };

        /// <summary>띠의 가장자리. 칼날처럼 한 줄이 또렷해야 방향이 읽힌다.</summary>
        private const byte EdgeAlpha = 255;

        /// <summary>프레임 하나. 잘라낸 조각이라 원점에서의 오프셋을 함께 든다.</summary>
        public readonly struct Frame
        {
            public readonly Sprite Sprite;

            /// <summary>도형 원점에서 이 조각의 중심까지 (월드 유닛).</summary>
            public readonly Vector2 Offset;

            public Frame(Sprite sprite, Vector2 offset)
            {
                Sprite = sprite;
                Offset = offset;
            }

            public bool IsEmpty => Sprite == null;
        }

        private readonly struct Key
        {
            private readonly SkillShapeKind kind;
            private readonly float range, width, angle, forwardOffset;
            private readonly int direction, frames;

            public Key(SkillShapeKind kind, SkillShapeParams p, EightDirection direction, int frames)
            {
                this.kind = kind;
                range = p.range; width = p.width; angle = p.angle; forwardOffset = p.forwardOffset;
                this.direction = (int)direction;
                this.frames = frames;
            }

            public override int GetHashCode() => System.HashCode.Combine(
                kind, range, width, angle, forwardOffset, direction, frames);

            public override bool Equals(object obj) =>
                obj is Key k && k.kind == kind && k.direction == direction && k.frames == frames &&
                Mathf.Approximately(k.range, range) && Mathf.Approximately(k.width, width) &&
                Mathf.Approximately(k.angle, angle) && Mathf.Approximately(k.forwardOffset, forwardOffset);
        }

        private static readonly Dictionary<Key, Frame[]> Cache = new Dictionary<Key, Frame[]>();

        /// <summary>
        /// 도메인 리로드를 끈 상태에서도 플레이 시작 시 옛 텍스처가 남지 않게 비운다.
        /// 남으면 파괴된 Texture2D 를 참조하는 Sprite 가 살아 있어 분홍색으로 그려진다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => Cache.Clear();

        /// <summary>
        /// 방향별로 구운 프레임들. 이미 있으면 그대로 돌려준다.
        /// 처음 쓸 때만 굽고 그 뒤로는 위치와 색만 바뀐다.
        /// </summary>
        public static Frame[] Get(
            SkillShapeKind kind, SkillShapeParams param, EightDirection direction, int frameCount)
        {
            frameCount = Mathf.Clamp(frameCount, 1, 16);

            var key = new Key(kind, param, Canonical(kind, param, direction), frameCount);
            if (Cache.TryGetValue(key, out Frame[] cached) && cached != null) return cached;

            Frame[] baked = Bake(kind, param, Canonical(kind, param, direction), frameCount);
            Cache[key] = baked;
            return baked;
        }

        /// <summary>
        /// <b>방향이 결과를 바꾸지 않는 도형은 한 번만 굽는다.</b>
        /// 원은 어느 쪽을 보든 같은 그림이고(중심이 앞으로 밀리지 않는 한),
        /// 십자는 90도 돌려도 자기 자신이라 정방향 넷이 모두 같다.
        /// 8배 굽던 것이 1~2배가 되므로 메모리에서 차이가 크다.
        /// </summary>
        private static EightDirection Canonical(
            SkillShapeKind kind, SkillShapeParams param, EightDirection direction)
        {
            switch (kind)
            {
                case SkillShapeKind.Area:
                    return Mathf.Approximately(param.forwardOffset, 0f)
                        ? EightDirection.Front
                        : direction;

                case SkillShapeKind.Cross:
                    // 정방향끼리 같고 대각선끼리 같다. 둘만 구우면 된다.
                    return (int)direction % 2 == 0 ? EightDirection.Front : EightDirection.FrontRight;

                default:
                    return direction;
            }
        }

        private static Frame[] Bake(
            SkillShapeKind kind, SkillShapeParams param, EightDirection direction, int frameCount)
        {
            Vector2 facing = direction.ToVector();
            Rect bounds = SkillShape.LocalBounds(kind, param, facing);

            string what = $"SkillImpact_{kind}_{direction}";
            int width = PixelSpriteBaker.SideOf(bounds.width, what);
            int height = PixelSpriteBaker.SideOf(bounds.height, what);

            // 포함 여부와 진행도는 프레임마다 같다. 한 번만 재고 돌려 쓴다 —
            // 6프레임이면 점 검사가 6분의 1로 줄어든다.
            var inside = new bool[width * height];
            var phase = new float[width * height];

            for (int y = 0; y < height; y++)
            {
                float wy = PixelSpriteBaker.TexelCenter(bounds.yMin, y);

                for (int x = 0; x < width; x++)
                {
                    int i = y * width + x;
                    var local = new Vector2(PixelSpriteBaker.TexelCenter(bounds.xMin, x), wy);

                    inside[i] = SkillShape.Contains(kind, param, Vector2.zero, facing, local);
                    if (inside[i]) phase[i] = Phase(kind, param, facing, local);
                }
            }

            var frames = new Frame[frameCount];
            var pixels = new Color32[width * height];
            var drawn = new bool[width * height];

            for (int f = 0; f < frameCount; f++)
                frames[f] = BakeFrame(
                    what, f, frameCount, bounds, width, height, inside, phase, drawn, pixels);

            return frames;
        }

        private static Frame BakeFrame(
            string what, int index, int frameCount, Rect bounds, int width, int height,
            bool[] inside, float[] phase, bool[] drawn, Color32[] pixels)
        {
            // 띠의 앞머리가 도형을 지나 조금 더 나간다. 마지막 프레임에 꼬리만 남아
            // 사그라지는 것이 "지나갔다" 로 읽힌다.
            float t = frameCount == 1 ? 1f : index / (float)(frameCount - 1);
            float head = Mathf.Lerp(TrailLength * 0.5f, 1f + TrailLength * 0.5f, t);
            float tail = head - TrailLength;

            var clear = new Color32(255, 255, 255, 0);

            for (int i = 0; i < pixels.Length; i++)
            {
                if (!inside[i] || phase[i] > head || phase[i] < tail)
                {
                    drawn[i] = false;
                    pixels[i] = clear;
                    continue;
                }

                // 앞머리 쪽이 밝다. 뒤로 갈수록 사그라진다.
                float ratio = TrailLength <= 0f ? 1f : (phase[i] - tail) / TrailLength;
                int level = Mathf.Clamp(
                    Mathf.FloorToInt(ratio * LevelAlpha.Length), 0, LevelAlpha.Length - 1);

                drawn[i] = true;
                pixels[i] = new Color32(255, 255, 255, LevelAlpha[level]);
            }

            // 가장자리 한 줄을 또렷하게. 예고 인디케이터와 같은 규칙이라 결이 맞는다.
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = y * width + x;
                    if (!drawn[i]) continue;

                    bool border =
                        x == 0 || x == width - 1 || y == 0 || y == height - 1 ||
                        !drawn[i - 1] || !drawn[i + 1] ||
                        !drawn[i - width] || !drawn[i + width];

                    if (border) pixels[i] = new Color32(255, 255, 255, EdgeAlpha);
                }
            }

            Sprite sprite = PixelSpriteBaker.CreateTrimmed(
                $"{what}_{index}", bounds, width, height, pixels, out Vector2 offset);

            return new Frame(sprite, offset);
        }

        /// <summary>
        /// 이 점이 동작의 어느 시점에 속하는가. 0 이 시작, 1 이 끝.
        ///
        /// <b>도형마다 "진행" 의 뜻이 다르다.</b> 부채꼴은 훑는 것이고 직선은 뻗는 것이며
        /// 원은 퍼지는 것이다. 여기서 그 차이를 한 번만 정한다.
        /// </summary>
        private static float Phase(
            SkillShapeKind kind, SkillShapeParams param, Vector2 facing, Vector2 local)
        {
            switch (kind)
            {
                case SkillShapeKind.Forward:
                {
                    if (param.angle <= 0f) return 0f;

                    // SignedAngle 은 반시계가 양수다. 반시계 끝에서 시계 끝으로 훑는다 —
                    // 부호를 뒤집으면 반대로 벤다. 왼손잡이 모션이 필요하면 여기만 바꾼다.
                    float signed = Vector2.SignedAngle(facing, local);
                    return Mathf.Clamp01((param.angle * 0.5f - signed) / param.angle);
                }

                case SkillShapeKind.Line:
                case SkillShapeKind.Dash:
                    return param.range <= 0f
                        ? 0f
                        : Mathf.Clamp01(Vector2.Dot(local, facing) / param.range);

                case SkillShapeKind.Area:
                    return param.range <= 0f
                        ? 0f
                        : Mathf.Clamp01((local - facing * param.forwardOffset).magnitude / param.range);

                case SkillShapeKind.Cross:
                    return param.range <= 0f ? 0f : Mathf.Clamp01(local.magnitude / param.range);
            }

            return 0f;
        }

        /// <summary>구워둔 것이 몇 벌인지. 디버그용.</summary>
        public static int CachedCount => Cache.Count;
    }
}
