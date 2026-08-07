using System.Collections.Generic;
using PrettyKnights.Characters;
using PrettyKnights.Core;
using UnityEngine;

namespace PrettyKnights.Combat
{
    /// <summary>
    /// 예고 인디케이터를 만들고 재사용한다. <b>Boot 씬에 상주한다.</b>
    ///
    /// 예고는 몬스터가 공격할 때마다 뜨고 사라진다. 매번 <c>Instantiate</c> 하면
    /// 전투 중 계속 할당이 일어나므로 풀에 담아 돌려 쓴다.
    ///
    /// 정렬 설정을 여기 모아 둔다 — 인디케이터는 코드로 만들어지므로
    /// 인스펙터가 없다. 바닥 타일맵 위 · 캐릭터 아래에 오도록 맞춘다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SkillIndicatorPool : MonoBehaviour
    {
        [Header("정렬 — 바닥 위, 캐릭터 아래")]
        [SerializeField, Tooltip("타일맵과 캐릭터가 쓰는 Sorting Layer 이름")]
        private string sortingLayer = "Default";

        [SerializeField, Tooltip("바닥 타일맵보다 크고 캐릭터보다 작게")]
        private int sortingOrder = 50;

        [SerializeField, Tooltip("인디케이터를 놓을 z. 캐릭터와 겹치지 않게 살짝 뒤로")]
        private float depth = 0.01f;

        [Header("기본 색")]
        [SerializeField, Tooltip("몬스터 예고. 알파는 여기서 정하고 시간에 따라 진해진다")]
        private Color hostileColor = new Color(1f, 0.18f, 0.16f, 0.85f);

        [Header("풀")]
        [SerializeField, Min(0)] private int prewarm = 8;

        private readonly List<SkillIndicator> pool = new List<SkillIndicator>();
        private Transform root;

        public Color HostileColor => hostileColor;

        private void Awake()
        {
            root = new GameObject("SkillIndicators").transform;
            root.SetParent(transform, worldPositionStays: false);

            for (int i = 0; i < prewarm; i++) Create().Hide();

            ServiceRegistry.Register(this);
        }

        private void OnDestroy()
        {
            if (ServiceRegistry.TryGet(out SkillIndicatorPool current) && current == this)
                ServiceRegistry.Unregister<SkillIndicatorPool>();
        }

        /// <summary>
        /// 예고를 띄운다. 도형은 방향별로 구운 것을 재사용하므로
        /// 같은 스킬이 반복돼도 텍스처는 다시 만들지 않는다.
        /// </summary>
        public SkillIndicator Show(
            SkillShapeKind kind, SkillShapeParams param,
            Vector2 origin, Vector2 facing, float lifetime)
            => Show(kind, param, origin, facing, lifetime, hostileColor);

        public SkillIndicator Show(
            SkillShapeKind kind, SkillShapeParams param,
            Vector2 origin, Vector2 facing, float lifetime, Color color)
        {
            EightDirection direction = EightDirectionUtil.FromVector(facing);
            Sprite sprite = SkillIndicatorRasterizer.Get(kind, param, direction);

            if (sprite == null) return null;

            SkillIndicator indicator = Rent();
            indicator.Show(sprite, origin, lifetime, color, depth);
            return indicator;
        }

        private SkillIndicator Rent()
        {
            foreach (SkillIndicator candidate in pool)
                if (candidate != null && !candidate.IsBusy) return candidate;

            return Create();
        }

        private SkillIndicator Create()
        {
            var go = new GameObject("SkillIndicator");
            go.transform.SetParent(root, worldPositionStays: false);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = sortingOrder;

            var indicator = go.AddComponent<SkillIndicator>();
            pool.Add(indicator);
            return indicator;
        }

        [ContextMenu("구워둔 인디케이터 수")]
        private void LogCached() =>
            Debug.Log($"[SkillIndicatorPool] 구워둔 텍스처 {SkillIndicatorRasterizer.CachedCount}개 · " +
                      $"풀 {pool.Count}개");
    }
}
