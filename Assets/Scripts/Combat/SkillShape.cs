using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrettyKnights.Combat
{
    public enum SkillShapeKind
    {
        /// <summary>전방 부채꼴. 근접 베기.</summary>
        Forward,

        /// <summary>앞으로 뻗는 직선. 관통.</summary>
        Line,

        /// <summary>네 방향 직선.</summary>
        Cross,

        /// <summary>앞쪽 한 점을 중심으로 한 원. 광역 폭발.</summary>
        Area,

        /// <summary>돌진 경로 위의 직선. <see cref="Line"/> 과 같은 계산이되 의미가 다르다.</summary>
        Dash
    }

    /// <summary>범위 하나를 정의하는 값. 스킬 정의가 이걸 들고 있는다.</summary>
    [Serializable]
    public struct SkillShapeParams
    {
        [Min(0f), Tooltip("사거리 (월드 유닛)")]
        public float range;

        [Min(0f), Tooltip("직선·돌진의 폭. 부채꼴에서는 쓰지 않는다")]
        public float width;

        [Range(0f, 360f), Tooltip("부채꼴이 벌어지는 각도")]
        public float angle;

        [Tooltip("광역 폭발의 중심을 앞으로 얼마나 밀지")]
        public float forwardOffset;

        public static SkillShapeParams Slash => new SkillShapeParams
        {
            range = 1.6f, width = 1f, angle = 100f, forwardOffset = 0f
        };
    }

    /// <summary>
    /// 범위 계산 한 벌. <b>무상태 순수 함수만 둔다.</b>
    ///
    /// 프리뷰 인디케이터 · 실제 데미지 · AI 회피 · 스킬 설명 UI 가 전부 이걸 부른다.
    /// 계산이 두 벌이 되면 "보이는 범위" 와 "맞는 범위" 가 달라진다 (CLAUDE.md §5).
    ///
    /// <b>여기에 필드를 만들지 말 것.</b> 같은 프레임에 보스 하나와 잡몹 열둘과
    /// 플레이어가 이걸 부른다. <c>currentCaster</c> 같은 상태를 두는 순간
    /// 앞선 판정이 뒤 판정에 덮여 사라진다.
    /// 결과 목록도 정적 버퍼가 아니라 <b>호출자가 넘긴 것</b>에 담는다.
    ///
    /// 무상태라는 성질 덕분에 나중에 Jobs/Burst 로 그대로 옮길 수 있다.
    /// </summary>
    public static class SkillShape
    {
        /// <summary>
        /// 범위 안의 콜라이더를 <paramref name="results"/> 에 담고 개수를 돌려준다.
        /// <paramref name="results"/> 는 호출 시 비워진다.
        /// </summary>
        public static int Evaluate(
            SkillShapeKind kind,
            SkillShapeParams param,
            Vector2 origin,
            Vector2 facing,
            ContactFilter2D filter,
            List<Collider2D> results)
        {
            if (results == null) return 0;
            results.Clear();

            if (param.range <= 0f) return 0;

            // 0 벡터가 들어오면 방향을 정할 수 없다. 기본 정면(아래)으로 둔다.
            Vector2 direction = facing.sqrMagnitude > 0.0001f ? facing.normalized : Vector2.down;

            switch (kind)
            {
                case SkillShapeKind.Forward:
                    EvaluateForward(param, origin, direction, filter, results);
                    break;

                case SkillShapeKind.Line:
                case SkillShapeKind.Dash:
                    EvaluateLine(param, origin, direction, filter, results);
                    break;

                case SkillShapeKind.Cross:
                    EvaluateCross(param, origin, direction, filter, results);
                    break;

                case SkillShapeKind.Area:
                    EvaluateArea(param, origin, direction, filter, results);
                    break;
            }

            return results.Count;
        }

        /// <summary>
        /// 부채꼴. 원으로 한 번 걷어낸 뒤 각도로 거른다.
        /// 물리 쿼리를 여러 번 하는 것보다 한 번 하고 산술로 거르는 쪽이 싸다.
        /// </summary>
        private static void EvaluateForward(
            SkillShapeParams param, Vector2 origin, Vector2 direction,
            ContactFilter2D filter, List<Collider2D> results)
        {
            Physics2D.OverlapCircle(origin, param.range, filter, results);

            if (param.angle >= 360f) return;

            float cosHalf = Mathf.Cos(param.angle * 0.5f * Mathf.Deg2Rad);

            for (int i = results.Count - 1; i >= 0; i--)
            {
                Collider2D collider = results[i];
                if (collider == null)
                {
                    results.RemoveAt(i);
                    continue;
                }

                // 콜라이더 중심이 아니라 가장 가까운 점을 쓴다.
                // 큰 몬스터가 중심만 밖에 있다고 안 맞는 것은 부자연스럽다.
                Vector2 toTarget = collider.ClosestPoint(origin) - origin;

                // 원점에 겹쳐 있으면 각도를 잴 수 없다. 붙어 있으므로 맞은 것으로 친다.
                if (toTarget.sqrMagnitude < 0.0001f) continue;

                if (Vector2.Dot(toTarget.normalized, direction) < cosHalf) results.RemoveAt(i);
            }
        }

        /// <summary>앞으로 뻗는 직사각형. 원점에서 시작해 사거리만큼 나간다.</summary>
        private static void EvaluateLine(
            SkillShapeParams param, Vector2 origin, Vector2 direction,
            ContactFilter2D filter, List<Collider2D> results)
        {
            float width = Mathf.Max(0.1f, param.width);
            Vector2 center = origin + direction * (param.range * 0.5f);
            Vector2 size = new Vector2(width, param.range);

            // OverlapBox 의 각도는 +Y 를 기준으로 잰다. size 를 세로로 잡았으므로
            // 방향 벡터의 각도에서 90도를 빼면 그 축이 direction 과 맞는다.
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

            Physics2D.OverlapBox(center, size, angle, filter, results);
        }

        /// <summary>
        /// 네 방향 직선. 같은 대상이 두 팔에 걸릴 수 있으므로 중복을 지운다.
        /// 중복을 남기면 십자 스킬만 교차점에서 두 배 데미지가 된다.
        /// </summary>
        private static void EvaluateCross(
            SkillShapeParams param, Vector2 origin, Vector2 direction,
            ContactFilter2D filter, List<Collider2D> results)
        {
            var arm = new List<Collider2D>();

            for (int i = 0; i < 4; i++)
            {
                Vector2 armDirection = Rotate(direction, 90f * i);

                arm.Clear();
                EvaluateLine(param, origin, armDirection, filter, arm);

                foreach (Collider2D collider in arm)
                    if (collider != null && !results.Contains(collider)) results.Add(collider);
            }
        }

        /// <summary>앞쪽 한 점을 중심으로 한 원.</summary>
        private static void EvaluateArea(
            SkillShapeParams param, Vector2 origin, Vector2 direction,
            ContactFilter2D filter, List<Collider2D> results)
        {
            Vector2 center = origin + direction * param.forwardOffset;
            Physics2D.OverlapCircle(center, param.range, filter, results);
        }

        /// <summary>
        /// 한 점이 범위 안인가. <b>인디케이터를 굽는 데 쓴다.</b>
        ///
        /// <see cref="Evaluate"/> 와 같은 파일·같은 파라미터를 쓰는 것이 핵심이다.
        /// 보이는 범위와 맞는 범위가 갈리지 않으려면 계산이 한 곳에 있어야 한다
        /// (CLAUDE.md §5).
        /// </summary>
        public static bool Contains(
            SkillShapeKind kind, SkillShapeParams param,
            Vector2 origin, Vector2 facing, Vector2 point)
        {
            if (param.range <= 0f) return false;

            Vector2 direction = facing.sqrMagnitude > 0.0001f ? facing.normalized : Vector2.down;
            Vector2 local = point - origin;

            switch (kind)
            {
                case SkillShapeKind.Forward:
                {
                    if (local.sqrMagnitude > param.range * param.range) return false;

                    // 원점에 겹치면 각도를 잴 수 없다. 붙어 있으므로 포함으로 친다.
                    if (local.sqrMagnitude < 0.0001f) return true;
                    if (param.angle >= 360f) return true;

                    float cosHalf = Mathf.Cos(param.angle * 0.5f * Mathf.Deg2Rad);
                    return Vector2.Dot(local.normalized, direction) >= cosHalf;
                }

                case SkillShapeKind.Line:
                case SkillShapeKind.Dash:
                    return InLine(param, local, direction);

                case SkillShapeKind.Cross:
                    for (int i = 0; i < 4; i++)
                        if (InLine(param, local, Rotate(direction, 90f * i))) return true;
                    return false;

                case SkillShapeKind.Area:
                {
                    Vector2 toCenter = local - direction * param.forwardOffset;
                    return toCenter.sqrMagnitude <= param.range * param.range;
                }
            }

            return false;
        }

        /// <summary>원점에서 <paramref name="direction"/> 으로 뻗는 직사각형 안인가.</summary>
        private static bool InLine(SkillShapeParams param, Vector2 local, Vector2 direction)
        {
            float along = Vector2.Dot(local, direction);
            if (along < 0f || along > param.range) return false;

            Vector2 side = new Vector2(-direction.y, direction.x);
            return Mathf.Abs(Vector2.Dot(local, side)) <= Mathf.Max(0.1f, param.width) * 0.5f;
        }

        /// <summary>
        /// 원점 기준 로컬 경계. 인디케이터 텍스처 크기를 정하는 데 쓴다.
        /// <b>넉넉하게 잡는다</b> — 조금 큰 것은 투명 픽셀이 늘 뿐이지만 작으면 범위가 잘린다.
        /// </summary>
        public static Rect LocalBounds(SkillShapeKind kind, SkillShapeParams param, Vector2 facing)
        {
            Vector2 direction = facing.sqrMagnitude > 0.0001f ? facing.normalized : Vector2.down;
            float half = Mathf.Max(0.1f, param.width) * 0.5f;

            switch (kind)
            {
                case SkillShapeKind.Line:
                case SkillShapeKind.Dash:
                {
                    Vector2 side = new Vector2(-direction.y, direction.x) * half;
                    Vector2 tip = direction * param.range;

                    Vector2 min = Vector2.Min(Vector2.Min(-side, side), Vector2.Min(tip - side, tip + side));
                    Vector2 max = Vector2.Max(Vector2.Max(-side, side), Vector2.Max(tip - side, tip + side));
                    return new Rect(min, max - min);
                }

                case SkillShapeKind.Cross:
                {
                    float extent = param.range + half;
                    return new Rect(-extent, -extent, extent * 2f, extent * 2f);
                }

                case SkillShapeKind.Area:
                {
                    Vector2 center = direction * param.forwardOffset;
                    return new Rect(center.x - param.range, center.y - param.range,
                        param.range * 2f, param.range * 2f);
                }

                default:
                    // 부채꼴은 반지름 원에 들어간다. 각도로 더 줄일 수 있지만
                    // 방향마다 경계가 달라지면 텍스처 크기가 8가지가 되어 캐시가 지저분해진다.
                    return new Rect(-param.range, -param.range, param.range * 2f, param.range * 2f);
            }
        }

        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);

            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        /// <summary>
        /// 기즈모로 범위를 그린다. 인디케이터가 붙기 전까지 이걸로 눈으로 확인한다.
        /// <b>판정과 같은 파라미터를 쓰므로 보이는 것과 맞는 것이 어긋나지 않는다.</b>
        /// </summary>
        public static void DrawGizmo(
            SkillShapeKind kind, SkillShapeParams param, Vector2 origin, Vector2 facing)
        {
            Vector2 direction = facing.sqrMagnitude > 0.0001f ? facing.normalized : Vector2.down;

            switch (kind)
            {
                case SkillShapeKind.Forward:
                    int steps = 12;
                    Vector2 previous = origin + Rotate(direction, -param.angle * 0.5f) * param.range;
                    Gizmos.DrawLine(origin, previous);

                    for (int i = 1; i <= steps; i++)
                    {
                        float t = -param.angle * 0.5f + param.angle * i / steps;
                        Vector2 point = origin + Rotate(direction, t) * param.range;
                        Gizmos.DrawLine(previous, point);
                        previous = point;
                    }

                    Gizmos.DrawLine(previous, origin);
                    break;

                case SkillShapeKind.Line:
                case SkillShapeKind.Dash:
                    DrawBox(origin, direction, param);
                    break;

                case SkillShapeKind.Cross:
                    for (int i = 0; i < 4; i++) DrawBox(origin, Rotate(direction, 90f * i), param);
                    break;

                case SkillShapeKind.Area:
                    Gizmos.DrawWireSphere(origin + direction * param.forwardOffset, param.range);
                    break;
            }
        }

        private static void DrawBox(Vector2 origin, Vector2 direction, SkillShapeParams param)
        {
            float half = Mathf.Max(0.1f, param.width) * 0.5f;
            Vector2 side = new Vector2(-direction.y, direction.x) * half;
            Vector2 tip = origin + direction * param.range;

            Gizmos.DrawLine(origin - side, tip - side);
            Gizmos.DrawLine(origin + side, tip + side);
            Gizmos.DrawLine(origin - side, origin + side);
            Gizmos.DrawLine(tip - side, tip + side);
        }
    }
}
