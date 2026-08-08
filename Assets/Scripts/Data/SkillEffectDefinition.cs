using System;
using PrettyKnights.Characters;
using PrettyKnights.Combat;
using UnityEngine;

namespace PrettyKnights.Data
{
    /// <summary>
    /// 손으로 그린 타격 이펙트. 검격 하나가 이 에셋 하나다.
    ///
    /// <b>판정 도형을 그대로 보여주면 칼이 아니라 부채가 된다</b> (2026-08-09 확정).
    /// 부채꼴 전체를 채우면 원점 근처까지 빽빽해서 "휘두른다" 로 읽히지 않는다.
    /// 그래서 <b>맞는 범위와 보이는 그림을 갈랐다</b> — 판정은 <see cref="SkillShape"/> 가,
    /// 그림은 이 에셋이 맡는다. 범위를 그대로 그리는 래스터화는
    /// <b>빨간 예고에만</b> 쓴다 (docs/decisions/008-impact-vfx.md §8).
    ///
    /// <b>8방향을 다 그리지 않아도 된다.</b> 좌우가 거울인 방향은 한쪽만 그리고
    /// 반전해서 쓴다. 검격은 손 소유가 드러나지 않는 추상적인 호라서
    /// CLAUDE.md §4 의 "반전하면 깨지는 방향" 문제가 없다.
    ///
    /// <code>
    /// 최소 3벌로 시작할 수 있다
    ///   01_front   아래         →  08_front_faces-screen-left 를 반전으로 대체
    ///   03_right   오른쪽       →  07_left 를 반전으로 대체
    ///   05_back    위           →  04·06 을 반전으로 대체
    /// </code>
    /// </summary>
    [CreateAssetMenu(
        fileName = "SkillEffect",
        menuName = "Pretty Knights/Skill Effect Definition")]
    public sealed class SkillEffectDefinition : ScriptableObject
    {
        /// <summary>한 방향의 프레임 묶음.</summary>
        [Serializable]
        public struct DirectionalClip
        {
            [Tooltip("이 묶음이 담당하는 방향. CLAUDE.md §4 의 01~08 순서와 같다")]
            public EightDirection direction;

            [Tooltip("프레임 순서대로. 4~8장 (CLAUDE.md §5)")]
            public Sprite[] frames;
        }

        [Header("그림")]
        [SerializeField, Tooltip("방향마다 하나. 빠진 방향은 좌우 반전으로 대신한다")]
        private DirectionalClip[] clips = Array.Empty<DirectionalClip>();

        [Header("속도")]
        [SerializeField, Min(1f), Tooltip("초당 프레임. 도트 이펙트는 보통 16~24")]
        private float framesPerSecond = 20f;

        [Header("놓는 자리")]
        [SerializeField, Tooltip("시전자에서 바라보는 쪽으로 이만큼 민다 (월드 유닛)")]
        private float forwardOffset = 0.6f;

        [SerializeField, Tooltip("위로 이만큼 올린다. 발밑이 아니라 몸통 높이에서 터지게")]
        private float verticalOffset = 0.4f;

        [Header("표시")]
        [SerializeField] private Color tint = Color.white;

        [SerializeField, Min(0.01f)] private float scale = 1f;

        [Header("따라가기")]
        [SerializeField, Tooltip(
            "켜면 이펙트가 시전자를 따라간다. 근접 검격은 켜는 것이 맞다 — " +
            "휘두르는 동안 움직이면 칼자국만 뒤에 남아 몸과 떨어져 보인다. " +
            "지면에 남는 장판이나 폭발 자국은 꺼야 한다")]
        private bool followCaster = true;

        public float FramesPerSecond => framesPerSecond;
        public float SecondsPerFrame => 1f / Mathf.Max(1f, framesPerSecond);
        public Color Tint => tint;
        public float Scale => scale;
        public bool FollowCaster => followCaster;

        /// <summary>시전자 기준 놓을 자리. 바라보는 쪽으로 밀고 몸통 높이로 올린다.</summary>
        public Vector2 AnchorOffset(Vector2 facing) =>
            facing * forwardOffset + Vector2.up * verticalOffset;

        /// <summary>그려진 방향이 하나라도 있는지. 없으면 이 에셋은 아직 쓸 수 없다.</summary>
        public bool HasArt
        {
            get
            {
                foreach (DirectionalClip clip in clips)
                    if (clip.frames != null && clip.frames.Length > 0) return true;

                return false;
            }
        }

        /// <summary>
        /// 그 방향의 프레임들. 없으면 <b>좌우 반전</b>으로 대신하고,
        /// 그것도 없으면 그려진 아무 방향이나 쓴다 — 안 보이는 것보다는 낫다.
        /// <paramref name="flipX"/> 가 true 면 재생하는 쪽이 좌우를 뒤집어야 한다.
        /// </summary>
        public Sprite[] Resolve(EightDirection direction, out bool flipX)
        {
            flipX = false;

            Sprite[] exact = Find(direction);
            if (exact != null) return exact;

            Sprite[] mirrored = Find(Mirror(direction));
            if (mirrored != null)
            {
                flipX = true;
                return mirrored;
            }

            foreach (DirectionalClip clip in clips)
                if (clip.frames != null && clip.frames.Length > 0) return clip.frames;

            return null;
        }

        private Sprite[] Find(EightDirection direction)
        {
            foreach (DirectionalClip clip in clips)
                if (clip.direction == direction && clip.frames != null && clip.frames.Length > 0)
                    return clip.frames;

            return null;
        }

        /// <summary>
        /// 좌우로 뒤집은 방향. 아래(01)와 위(05)는 자기 자신이다.
        /// 01~08 순번이 아래에서 시계 방향이라 <c>(8 − n) % 8</c> 이 그대로 거울이 된다.
        /// </summary>
        private static EightDirection Mirror(EightDirection direction) =>
            (EightDirection)((EightDirectionUtil.Count - (int)direction) % EightDirectionUtil.Count);
    }
}
