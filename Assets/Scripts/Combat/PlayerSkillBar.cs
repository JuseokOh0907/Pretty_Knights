using System.Collections.Generic;
using PrettyKnights.Characters;
using PrettyKnights.Core;
using PrettyKnights.Data;
using UnityEngine;

namespace PrettyKnights.Combat
{
    /// <summary>
    /// <see cref="ISkillBar"/> 의 구현체. <b>화면 버튼이 바라보던 창구가 여기로 닫힌다.</b>
    ///
    /// <c>Player.prefab</c> 루트에 붙는다. 몸은 씬마다 새로 생기므로
    /// 버튼이 인스펙터로 물고 있을 수 없고, <see cref="ServiceRegistry"/> 를 거친다.
    ///
    /// <b>슬롯 개수는 모드가 달라도 같다</b> (4칸, 2026-08-09 확정).
    /// 세로 하단과 가로 우측은 생김새만 다르고 <c>slot 2</c> 는 양쪽에서 같은 스킬이다 —
    /// 모드마다 개수가 다르면 슬롯 번호의 뜻이 갈려 세이브에 담을 수 없게 된다.
    ///
    /// <b>쿨타임은 시전 시각으로 든다.</b> 남은 시간을 매 프레임 빼는 방식은
    /// 버튼 4개가 각자 물어보는 구조에서 누가 언제 빼는지가 불분명해진다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerSkillBar : MonoBehaviour, ISkillBar
    {
        /// <summary>화면 버튼 개수와 같다. 비어 있는 슬롯은 잠김으로 그려진다.</summary>
        public const int SlotCapacity = 4;

        [Header("배운 스킬 — 왼쪽부터 슬롯 0, 1, 2, 3")]
        [SerializeField, Tooltip("비운 칸은 잠김으로 그려진다. 아직 안 배운 것이 사실이므로 맞는 표시다")]
        private PlayerSkillDefinition[] slots = new PlayerSkillDefinition[SlotCapacity];

        [Header("연결 (비우면 자동으로 찾는다)")]
        [SerializeField] private PlayerController player;
        [SerializeField] private DirectionalAnimatorDriver animatorDriver;

        [Header("판정 대상")]
        [SerializeField, Tooltip("이 레이어만 때린다. 실제 판정은 IDamageable 유무로 거른다")]
        private LayerMask targetLayers = ~0;

        [Header("조준")]
        [SerializeField, Tooltip("켜면 반경 안의 가장 가까운 대상 쪽으로 방향을 보정한다")]
        private bool autoAim = true;

        [SerializeField, Min(0f)] private float autoAimRadius = 6f;

        [Header("디버그")]
        [SerializeField] private bool logCasts;

        /// <summary>범위 계산 결과. 이 컴포넌트의 것이라 다른 시전과 섞이지 않는다.</summary>
        private readonly List<Collider2D> overlapped = new List<Collider2D>();

        private readonly List<IDamageable> struck = new List<IDamageable>();
        private readonly List<IAreaDamageable> struckAreas = new List<IAreaDamageable>();

        /// <summary>슬롯마다 다음에 쓸 수 있게 되는 시각.</summary>
        private readonly float[] nextReadyTime = new float[SlotCapacity];

        private ContactFilter2D filter;

        public int SlotCount => SlotCapacity;

        private void Awake()
        {
            if (player == null) player = GetComponent<PlayerController>();
            if (animatorDriver == null) animatorDriver = GetComponentInChildren<DirectionalAnimatorDriver>();

            filter = new ContactFilter2D
            {
                useTriggers = false,   // 포탈·상호작용 트리거를 때리면 안 된다
                useLayerMask = true,
                layerMask = targetLayers
            };

            // 인스펙터에서 배열 길이를 줄여 놨을 수 있다. 버튼은 4개를 물어본다.
            if (slots.Length != SlotCapacity) System.Array.Resize(ref slots, SlotCapacity);

            // 인터페이스 타입으로 등록해야 버튼이 찾는다.
            // Register(this) 로 부르면 PlayerSkillBar 타입으로 들어가 버튼이 못 본다.
            ServiceRegistry.Register<ISkillBar>(this);
        }

        private void OnDestroy()
        {
            if (ServiceRegistry.TryGet(out ISkillBar current) && ReferenceEquals(current, this))
                ServiceRegistry.Unregister<ISkillBar>();
        }

        // ── ISkillBar ─────────────────────────────────────────────────────

        /// <summary>
        /// 배웠는가. <b>에셋이 꽂혀 있고 레벨이 찼을 때만</b> true 다.
        /// 레벨을 여기서 보는 이유는 잠김 표시가 곧 "더 크면 열린다" 는 안내이기 때문이다.
        /// </summary>
        public bool IsUnlocked(int slot)
        {
            PlayerSkillDefinition skill = At(slot);
            if (skill == null) return false;

            if (!ServiceRegistry.TryGet(out PlayerRuntimeState state) || state == null || !state.IsBound)
                return skill.UnlockLevel <= 1;

            return state.Level >= skill.UnlockLevel;
        }

        public bool IsReady(int slot) =>
            IsUnlocked(slot) && Time.time >= nextReadyTime[slot];

        /// <summary>0~1. 1 이면 다 찼다. 아직 안 배운 슬롯은 0 으로 둔다.</summary>
        public float CooldownProgress(int slot)
        {
            PlayerSkillDefinition skill = At(slot);
            if (skill == null || skill.Cooldown <= 0f) return 1f;

            float left = nextReadyTime[slot] - Time.time;
            return left <= 0f ? 1f : Mathf.Clamp01(1f - left / skill.Cooldown);
        }

        public Sprite IconOf(int slot) => At(slot)?.Icon;

        /// <summary>
        /// 지금 쓴다. <b>버튼과 키가 같은 이 경로를 탄다.</b>
        ///
        /// 피격 경직·구역 전환 중에는 입력이 잠긴다. 그때는 스킬도 막는다 —
        /// 기본 공격과 다르게 두면 "경직 중에 스킬만 나간다" 는 예외가 생긴다.
        /// </summary>
        public bool TryCast(int slot)
        {
            if (!IsReady(slot)) return false;
            if (player != null && !player.InputEnabled) return false;

            PlayerSkillDefinition skill = slots[slot];

            nextReadyTime[slot] = Time.time + skill.Cooldown;

            Vector2 facing = ResolveFacing();
            Vector2 origin = (Vector2)transform.position + facing * skill.OriginForwardOffset;

            // 판정보다 먼저 띄운다. 대상이 없어도 쓴 것은 보여야 한다 —
            // 조용하면 입력이 씹혔다고 느낀다.
            Show(skill, origin, facing);

            int hits = SkillCast.Strike(
                transform, skill.Shape, skill.ShapeParams, origin, facing,
                SkillCast.PlayerAttackPower() * skill.DamageMultiplier,
                filter, overlapped, struck, struckAreas, logCasts);

            if (logCasts)
                Debug.Log($"[PlayerSkillBar] 슬롯 {slot} '{skill.DisplayName}' — {hits}대상", this);

            return true;
        }

        // ── 안쪽 ──────────────────────────────────────────────────────────

        private PlayerSkillDefinition At(int slot) =>
            slot < 0 || slot >= slots.Length ? null : slots[slot];

        private void Show(PlayerSkillDefinition skill, Vector2 origin, Vector2 facing)
        {
            if (!ServiceRegistry.TryGet(out SkillImpactPool impacts) || impacts == null) return;

            if (skill.Effect != null && skill.Effect.HasArt)
            {
                impacts.Play(skill.Effect, transform, transform.position, facing);
                return;
            }

            if (skill.ShowRangeWhenNoArt)
                impacts.PlayFriendly(skill.Shape, skill.ShapeParams, origin, facing);
        }

        /// <summary>
        /// 어디를 향해 쓸 것인가. 기본은 바라보던 방향이고,
        /// 자동 조준이 켜져 있으면 가까운 대상 쪽으로 돌린다.
        ///
        /// <b>세로 자동 사냥에서는 이게 사실상 필수다.</b> 손가락이 방향을 주지 않으므로
        /// 조준이 없으면 스킬이 엉뚱한 데로 나간다.
        /// </summary>
        private Vector2 ResolveFacing()
        {
            Vector2 facing = animatorDriver != null ? animatorDriver.FacingVector : Vector2.down;

            if (!autoAim || autoAimRadius <= 0f) return facing;

            Physics2D.OverlapCircle(transform.position, autoAimRadius, filter, overlapped);

            IDamageable best = null;
            float bestDistance = float.MaxValue;
            Vector2 self = transform.position;

            foreach (Collider2D collider in overlapped)
            {
                if (collider == null) continue;

                IDamageable candidate = collider.GetComponentInParent<IDamageable>();
                if (candidate == null || !candidate.IsAlive) continue;
                if (candidate.Transform == transform) continue;

                float distance = ((Vector2)candidate.Transform.position - self).sqrMagnitude;
                if (distance >= bestDistance) continue;

                bestDistance = distance;
                best = candidate;
            }

            if (best == null) return facing;

            Vector2 toTarget = (Vector2)best.Transform.position - self;
            return toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : facing;
        }
    }
}
