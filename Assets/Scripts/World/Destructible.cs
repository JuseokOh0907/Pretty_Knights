using System;
using PrettyKnights.Combat;
using PrettyKnights.Core;
using PrettyKnights.Data;
using UnityEngine;

namespace PrettyKnights.World
{
    /// <summary>
    /// 부술 수 있는 맵 오브젝트. 몬스터와 <see cref="IDamageable"/> 을 공유하므로
    /// 광역 폭발 한 번이 몬스터 셋과 토템 하나를 함께 때린다.
    ///
    /// <b>부서져도 오브젝트를 지우지 않는다.</b> 콜라이더를 끄고 스프라이트를 바꾼다.
    /// 완전히 지우면 세이브의 "부순 것" 목록을 복원할 대상이 사라져
    /// 던전을 나갔다 오면 되살아난다 (docs/design/map-objects.md §4).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Destructible : MonoBehaviour, IDamageable
    {
        [Header("정의")]
        [SerializeField] private PropDefinition definition;

        [Header("연결 (비우면 자동으로 찾는다)")]
        [SerializeField, Tooltip("부서질 때 스프라이트를 바꿀 대상")]
        private SpriteRenderer view;

        [SerializeField, Tooltip("부서질 때 끌 콜라이더들. 비우면 이 오브젝트와 자식 전부")]
        private Collider2D[] colliders;

        /// <summary>부서진 순간. <see cref="SpawnTotem"/> 이 듣는다.</summary>
        public event Action<Destructible> Broken;

        public PropDefinition Definition => definition;
        public float CurrentHp { get; private set; }
        public bool IsBroken { get; private set; }

        private void Awake()
        {
            if (view == null) view = GetComponentInChildren<SpriteRenderer>();
            if (colliders == null || colliders.Length == 0)
                colliders = GetComponentsInChildren<Collider2D>(includeInactive: true);

            // 런타임 생성이면 Instantiate 직후 Bind 가 따로 불린다.
            // 그때는 여기서 아직 비어 있는 것이 정상이므로 조용히 넘어간다.
            if (definition != null) Bind(definition);
        }

        private void Start()
        {
            if (definition == null)
                Debug.LogError($"[Destructible] '{name}' 에 PropDefinition 이 끝내 연결되지 않았습니다.");
        }

        /// <summary>
        /// 정의를 적용한다. <b>프리팹이 하나이므로 겉모습도 여기서 결정된다</b> —
        /// 스프라이트·콜라이더 크기·<c>Visual</c> 오프셋이 전부 정의에서 온다.
        ///
        /// 배리언트 18개를 만들지 않는 대신 그 값들이 데이터로 옮겨왔다.
        /// 아트가 교체돼도 에셋만 고치면 되고 프리팹을 하나씩 열 필요가 없다.
        /// </summary>
        public void Bind(PropDefinition source)
        {
            definition = source;

            if (definition == null)
            {
                CurrentHp = 1f;
                return;
            }

            CurrentHp = definition.MaxHp;
            IsBroken = false;

            if (view != null)
            {
                if (definition.Sprite != null) view.sprite = definition.Sprite;
                view.enabled = true;

                // 루트가 접지점이고 그림은 그만큼 위로 올라간다.
                // 오브젝트마다 하단 여백이 달라 이 값이 제각각이다 (실측표 §2).
                Vector3 local = view.transform.localPosition;
                view.transform.localPosition = new Vector3(local.x, definition.VisualOffsetY, local.z);
            }

            ApplyColliderSize();
        }

        /// <summary>
        /// 지면 충돌 영역을 정의대로 맞춘다.
        /// <b>겉보기 크기가 아니라 발이 닿는 넓이다</b> — 탑다운이라 지면 충돌과
        /// 상단 가림 아트를 분리해야 한다 (CLAUDE.md §4).
        /// </summary>
        private void ApplyColliderSize()
        {
            Vector2 size = definition.ColliderSize;
            if (size.x <= 0f || size.y <= 0f) return;

            foreach (Collider2D collider in colliders)
            {
                switch (collider)
                {
                    case CapsuleCollider2D capsule:
                        capsule.size = size;
                        break;
                    case BoxCollider2D box:
                        box.size = size;
                        break;
                }
            }
        }

        // ── IDamageable ───────────────────────────────────────────────────

        bool IDamageable.IsAlive => !IsBroken && definition != null && definition.IsDestructible;

        float IDamageable.Defense => definition != null ? definition.Defense : 0f;

        Transform IDamageable.Transform => transform;

        void IDamageable.ApplyDamage(float amount, Vector2 sourcePosition)
        {
            if (IsBroken || amount <= 0f) return;
            if (definition == null || !definition.IsDestructible) return;

            CurrentHp = Mathf.Max(0f, CurrentHp - amount);
            if (CurrentHp <= 0f) Break(grantRewards: true);
        }

        // ── 파괴 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 부순다. <paramref name="grantRewards"/> 가 false 면 조용히 부서진 모습만 만든다 —
        /// 세이브에서 복원할 때 쓴다. 그때 또 경험치를 주면 무한히 벌 수 있다.
        /// </summary>
        public void Break(bool grantRewards)
        {
            if (IsBroken) return;

            IsBroken = true;
            CurrentHp = 0f;

            // 통행을 막던 것이 사라진다. 이게 파괴의 실질적인 효과다.
            foreach (Collider2D collider in colliders)
                if (collider != null) collider.enabled = false;

            if (view != null)
            {
                Sprite broken = definition != null ? definition.BrokenSprite : null;

                // 부서진 그림이 없으면 감춘다. 원래 모습이 남아 있으면
                // 통과할 수 있는데도 막힌 것처럼 보인다.
                if (broken != null) view.sprite = broken;
                else view.enabled = false;
            }

            if (grantRewards) GrantRewards();

            Broken?.Invoke(this);
        }

        private void GrantRewards()
        {
            if (definition == null) return;

            RewardGrant.Grant(definition.DisplayName, definition.ExpReward, definition.Drops);
        }

        [ContextMenu("부수기 (검증용)")]
        private void DebugBreak()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[Destructible] 재생 중에만 동작합니다.");
                return;
            }

            Break(grantRewards: true);
        }
    }
}
