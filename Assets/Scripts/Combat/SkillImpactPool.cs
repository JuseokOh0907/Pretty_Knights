using System.Collections.Generic;
using PrettyKnights.Characters;
using PrettyKnights.Core;
using UnityEngine;

namespace PrettyKnights.Combat
{
    /// <summary>
    /// 타격 이펙트를 만들고 재사용한다. <b>Boot 씬의 <c>GameRoot</c> 에 붙인다.</b>
    /// <see cref="SkillIndicatorPool"/> 과 나란히 둔다.
    ///
    /// <b>예고와 정렬이 반대다.</b> 예고는 바닥에 깔려 캐릭터 아래지만,
    /// 타격은 맞은 몸 위에 터져야 한다. 그래서 풀을 나눈다 —
    /// 정렬만 다른 것이라도 한 컴포넌트가 두 값을 들면 어느 쪽이 어느 것인지 알기 어렵다.
    ///
    /// <b>색으로 누가 때렸는지 구분한다.</b> 텍스처는 알파만 들고 있어
    /// 같은 도형을 아군 파랑과 적 빨강이 함께 쓴다.
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

        [Header("속도")]
        [SerializeField, Min(1), Tooltip("한 번의 타격을 몇 프레임으로 그릴지 (CLAUDE.md §5 는 4~8)")]
        private int frameCount = 6;

        [SerializeField, Min(0.01f), Tooltip("프레임 하나가 머무는 시간. 6프레임 × 0.03 = 0.18초")]
        private float frameDuration = 0.03f;

        [Header("기본 색")]
        [SerializeField, Tooltip("플레이어가 때렸을 때")]
        private Color friendlyColor = new Color(1f, 0.96f, 0.75f, 1f);

        [SerializeField, Tooltip("몬스터가 때렸을 때")]
        private Color hostileColor = new Color(1f, 0.42f, 0.3f, 1f);

        private readonly List<SkillImpact> pool = new List<SkillImpact>();
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

        /// <summary>플레이어가 휘두른 것. 밝은 색으로 터진다.</summary>
        public SkillImpact PlayFriendly(
            SkillShapeKind kind, SkillShapeParams param, Vector2 origin, Vector2 facing)
            => Play(kind, param, origin, facing, friendlyColor);

        /// <summary>몬스터가 휘두른 것. 예고와 같은 계열의 붉은색이다.</summary>
        public SkillImpact PlayHostile(
            SkillShapeKind kind, SkillShapeParams param, Vector2 origin, Vector2 facing)
            => Play(kind, param, origin, facing, hostileColor);

        public SkillImpact Play(
            SkillShapeKind kind, SkillShapeParams param, Vector2 origin, Vector2 facing, Color color)
        {
            EightDirection direction = EightDirectionUtil.FromVector(facing);

            SkillImpactRasterizer.Frame[] frames =
                SkillImpactRasterizer.Get(kind, param, direction, frameCount);

            if (frames == null || frames.Length == 0) return null;

            SkillImpact impact = Rent();
            impact.Play(frames, origin, frameDuration, color, depth);
            return impact;
        }

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
                      $"풀 {pool.Count}개");
    }
}
