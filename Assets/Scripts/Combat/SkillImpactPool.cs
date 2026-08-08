using System.Collections.Generic;
using PrettyKnights.Characters;
using PrettyKnights.Core;
using PrettyKnights.Data;
using UnityEngine;

namespace PrettyKnights.Combat
{
    /// <summary>
    /// 타격 이펙트를 만들고 재사용한다. <b>Boot 씬의 <c>GameRoot</c> 에 붙인다.</b>
    /// <see cref="SkillIndicatorPool"/> 과 나란히 둔다.
    ///
    /// <b>그림의 출처가 둘이다.</b>
    ///
    /// <code>
    /// SkillEffectDefinition  손으로 그린 검격·스킬 이펙트   ← 기본
    /// SkillImpactRasterizer  판정 도형을 그대로 구운 것     ← 아트가 없을 때의 임시
    /// </code>
    ///
    /// 아트가 원칙이다. 판정 도형을 그대로 보여주면 부채꼴 전체가 채워져
    /// 칼이 아니라 부채를 휘두르는 것처럼 보인다 (결정 008 §8).
    /// 래스터화는 <b>빨간 예고</b>가 주 용도이며 여기서는 아트가 아직 없을 때만 쓴다.
    ///
    /// <b>예고와 정렬이 반대다.</b> 예고는 바닥에 깔려 캐릭터 아래지만
    /// 타격은 맞은 몸 위에 터져야 한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SkillImpactPool : MonoBehaviour
    {
        [Header("정렬 — 캐릭터 위")]
        [SerializeField, Tooltip("타일맵과 캐릭터가 쓰는 Sorting Layer 이름")]
        private string sortingLayer = "Default";

        [SerializeField, Tooltip("캐릭터보다 크게. 맞은 몸을 덮어야 타격감이 산다")]
        private int sortingOrder = 100;

        [SerializeField, Tooltip("이펙트를 놓을 z. 캐릭터보다 살짝 앞")]
        private float depth = -0.01f;

        [Header("아트가 없을 때의 임시 표시")]
        [SerializeField, Tooltip("한 번의 타격을 몇 프레임으로 그릴지 (CLAUDE.md §5 는 4~8)")]
        [Min(1)] private int frameCount = 6;

        [SerializeField, Min(0.01f), Tooltip("프레임 하나가 머무는 시간")]
        private float frameDuration = 0.03f;

        [SerializeField, Tooltip("플레이어가 때렸을 때")]
        private Color friendlyColor = new Color(1f, 0.96f, 0.75f, 1f);

        [SerializeField, Tooltip("몬스터가 때렸을 때")]
        private Color hostileColor = new Color(1f, 0.42f, 0.3f, 1f);

        private readonly List<SkillImpact> pool = new List<SkillImpact>();

        /// <summary>아트 이펙트의 프레임 배열. 시전마다 새로 만들지 않으려고 들고 있는다.</summary>
        private readonly Dictionary<(SkillEffectDefinition, EightDirection), SkillEffectFrame[]> artFrames =
            new Dictionary<(SkillEffectDefinition, EightDirection), SkillEffectFrame[]>();

        private readonly Dictionary<(SkillEffectDefinition, EightDirection), bool> artFlip =
            new Dictionary<(SkillEffectDefinition, EightDirection), bool>();

        private Transform root;

        public Color FriendlyColor => friendlyColor;
        public Color HostileColor => hostileColor;

        private void Awake()
        {
            root = new GameObject("SkillImpacts").transform;
            root.SetParent(transform, worldPositionStays: false);

            ServiceRegistry.Register(this);
        }

        private void OnDestroy()
        {
            if (ServiceRegistry.TryGet(out SkillImpactPool current) && current == this)
                ServiceRegistry.Unregister<SkillImpactPool>();
        }

        // ── 아트 이펙트 (기본) ────────────────────────────────────────────

        /// <summary>
        /// 손으로 그린 이펙트를 재생한다.
        /// <paramref name="caster"/> 를 넘기면 정의의 <c>Follow Caster</c> 에 따라 따라간다.
        /// </summary>
        public SkillImpact Play(
            SkillEffectDefinition definition, Transform caster, Vector2 casterPosition, Vector2 facing)
        {
            if (definition == null || !definition.HasArt) return null;

            EightDirection direction = EightDirectionUtil.FromVector(facing);
            SkillEffectFrame[] frames = ResolveArt(definition, direction, out bool flipX);

            if (frames == null || frames.Length == 0) return null;

            Vector2 origin = casterPosition + definition.AnchorOffset(facing);

            SkillImpact impact = Rent();
            impact.Play(
                frames, origin,
                definition.FollowCaster ? caster : null,
                definition.SecondsPerFrame, definition.Tint, definition.Scale, flipX, depth);

            return impact;
        }

        /// <summary>
        /// 방향별 프레임 배열. <b>스프라이트 배열을 매번 감싸지 않으려고 캐시한다</b> —
        /// 초당 두어 번 공격해도 배열이 계속 쌓이면 GC 가 돈다.
        /// </summary>
        private SkillEffectFrame[] ResolveArt(
            SkillEffectDefinition definition, EightDirection direction, out bool flipX)
        {
            var key = (definition, direction);

            if (artFrames.TryGetValue(key, out SkillEffectFrame[] cached))
            {
                flipX = artFlip[key];
                return cached;
            }

            Sprite[] sprites = definition.Resolve(direction, out flipX);

            if (sprites == null)
            {
                artFrames[key] = null;
                artFlip[key] = false;
                return null;
            }

            var frames = new SkillEffectFrame[sprites.Length];
            for (int i = 0; i < sprites.Length; i++) frames[i] = new SkillEffectFrame(sprites[i], Vector2.zero);

            artFrames[key] = frames;
            artFlip[key] = flipX;
            return frames;
        }

        // ── 래스터화 (아트가 없을 때의 임시) ──────────────────────────────

        /// <summary>플레이어가 휘두른 것. 아트가 없을 때만 쓴다.</summary>
        public SkillImpact PlayFriendly(
            SkillShapeKind kind, SkillShapeParams param, Vector2 origin, Vector2 facing)
            => PlayRange(kind, param, origin, facing, friendlyColor);

        /// <summary>몬스터가 휘두른 것. 아트가 없을 때만 쓴다.</summary>
        public SkillImpact PlayHostile(
            SkillShapeKind kind, SkillShapeParams param, Vector2 origin, Vector2 facing)
            => PlayRange(kind, param, origin, facing, hostileColor);

        public SkillImpact PlayRange(
            SkillShapeKind kind, SkillShapeParams param, Vector2 origin, Vector2 facing, Color color)
        {
            EightDirection direction = EightDirectionUtil.FromVector(facing);

            SkillEffectFrame[] frames =
                SkillImpactRasterizer.Get(kind, param, direction, frameCount);

            if (frames == null || frames.Length == 0) return null;

            SkillImpact impact = Rent();

            // 구운 조각은 도형 원점 기준이라 따라가게 하면 판정과 그림이 갈린다.
            // 임시 표시이므로 그 자리에 남긴다.
            impact.Play(frames, origin, null, frameDuration, color, 1f, false, depth);
            return impact;
        }

        // ── 풀 ────────────────────────────────────────────────────────────

        private SkillImpact Rent()
        {
            foreach (SkillImpact candidate in pool)
                if (candidate != null && !candidate.IsBusy) return candidate;

            return Create();
        }

        private SkillImpact Create()
        {
            var go = new GameObject("SkillImpact");
            go.transform.SetParent(root, worldPositionStays: false);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = sortingOrder;

            var impact = go.AddComponent<SkillImpact>();
            impact.Stop();

            pool.Add(impact);
            return impact;
        }

        [ContextMenu("구워둔 임팩트 수")]
        private void LogCached() =>
            Debug.Log($"[SkillImpactPool] 구워둔 프레임 묶음 {SkillImpactRasterizer.CachedCount}벌 · " +
                      $"아트 캐시 {artFrames.Count}벌 · 풀 {pool.Count}개");
    }
}
