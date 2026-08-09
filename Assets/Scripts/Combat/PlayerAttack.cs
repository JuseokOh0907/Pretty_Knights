using System.Collections.Generic;
using PrettyKnights.Characters;
using PrettyKnights.Core;
using PrettyKnights.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PrettyKnights.Combat
{
    /// <summary>
    /// 플레이어 기본 공격. <b>스킬 시스템의 첫 조각이다.</b>
    ///
    /// 범위는 <see cref="SkillShape"/> 가 계산한다 — 나중에 붙을 스킬 3종과
    /// 같은 계산을 쓰므로 여기서 검증한 것이 그대로 스킬로 이어진다.
    /// 데미지는 <see cref="CombatSettings"/> 가 계산한다. 공식이 확정되지 않았으므로
    /// 재생 중에 그 에셋을 고쳐 세 안을 직접 비교할 수 있다.
    ///
    /// <b>조준은 바라보는 방향을 그대로 쓴다.</b> 스틱 기울기가 곧 속도인 조작에서
    /// 방향까지 같은 곳에서 나오는 것이 일관된다. 자동 조준은 옵션으로 둔다.
    ///
    /// 이 컴포넌트는 <c>Player.prefab</c> 루트에 붙인다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerAttack : MonoBehaviour
    {
        private const string ActionMapName = "Player";
        private const string AttackActionName = "Attack";

        [Header("입력")]
        [SerializeField, Tooltip("Assets/InputSystem_Actions.inputactions")]
        private InputActionAsset inputActions;

        [Header("연결 (비우면 자동으로 찾는다)")]
        [SerializeField] private PlayerController player;
        [SerializeField] private DirectionalAnimatorDriver animatorDriver;

        [Header("범위")]
        [SerializeField] private SkillShapeKind shape = SkillShapeKind.Forward;
        [SerializeField] private SkillShapeParams shapeParams = SkillShapeParams.Slash;

        [Header("이펙트")]
        [SerializeField, Tooltip(
            "검격 그림. 비우면 판정 범위를 그대로 그리는 임시 표시로 대신한다")]
        private SkillEffectDefinition attackEffect;

        [SerializeField, Tooltip(
            "검격 그림이 없을 때 판정 범위를 그려 보여줄지. " +
            "아트가 들어오면 끈다 — 범위를 그대로 그리면 칼이 아니라 부채로 보인다")]
        private bool showRangeWhenNoArt = true;

        [SerializeField, Tooltip("원점을 앞으로 얼마나 밀지. 0이면 발밑에서 잰다")]
        private float originForwardOffset = 0.3f;

        [Header("속도")]
        [SerializeField, Min(0.05f), Tooltip("공격 간격 (초)")]
        private float cooldown = 0.45f;

        [Header("조준")]
        [SerializeField, Tooltip("켜면 사거리 안의 가장 가까운 대상 쪽으로 방향을 보정한다")]
        private bool autoAim;

        [SerializeField, Min(0f), Tooltip("자동 조준이 찾는 반경")]
        private float autoAimRadius = 3f;

        [Header("판정 대상")]
        [SerializeField, Tooltip("이 레이어만 때린다. 기본은 전부 — 실제 판정은 IDamageable 유무로 거른다")]
        private LayerMask targetLayers = ~0;

        [Header("디버그")]
        [SerializeField, Tooltip("마지막 판정 범위를 기즈모로 남긴다")]
        private bool drawLastSwing = true;

        [SerializeField] private bool logHits;

        /// <summary>범위 계산 결과를 담는다. 이 컴포넌트의 것이므로 다른 시전과 섞이지 않는다.</summary>
        private readonly List<Collider2D> overlapped = new List<Collider2D>();

        /// <summary>한 번의 휘두름에서 이미 맞은 대상. 같은 몸에 콜라이더가 여럿일 때 중복을 막는다.</summary>
        private readonly List<IDamageable> struck = new List<IDamageable>();

        /// <summary>범위를 통째로 받는 대상(부술 수 있는 벽 등). 한 번만 넘긴다.</summary>
        private readonly List<IAreaDamageable> struckAreas = new List<IAreaDamageable>();

        private InputAction attackAction;
        private ContactFilter2D filter;
        private float nextAllowedTime;

        private Vector2 lastOrigin;
        private Vector2 lastFacing;
        private bool hasSwung;

        public bool IsReady => Time.time >= nextAllowedTime;

        /// <summary>남은 쿨타임 비율 0~1. 버튼이 이걸로 채워진 정도를 그린다.</summary>
        public float CooldownProgress =>
            cooldown <= 0f ? 1f : Mathf.Clamp01(1f - (nextAllowedTime - Time.time) / cooldown);

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

            ResolveAction();
            ServiceRegistry.Register(this);
        }

        private void OnDestroy()
        {
            if (ServiceRegistry.TryGet(out PlayerAttack current) && current == this)
                ServiceRegistry.Unregister<PlayerAttack>();
        }

        private void OnEnable() => attackAction?.Enable();

        private void OnDisable() => attackAction?.Disable();

        private void ResolveAction()
        {
            if (inputActions == null)
            {
                Debug.LogError(
                    "[PlayerAttack] InputActionAsset 이 비어 있습니다. " +
                    "인스펙터에 InputSystem_Actions 를 연결하세요. 키보드·마우스 공격이 동작하지 않습니다.");
                return;
            }

            InputActionMap map = inputActions.FindActionMap(ActionMapName, throwIfNotFound: false);
            attackAction = map?.FindAction(AttackActionName, throwIfNotFound: false);

            if (attackAction == null)
                Debug.LogError($"[PlayerAttack] '{ActionMapName}/{AttackActionName}' 액션을 찾지 못했습니다.");
        }

        private void Update()
        {
            if (attackAction == null || !attackAction.WasPressedThisFrame()) return;

            TryAttack();
        }

        /// <summary>
        /// 지금 공격한다. 화면 버튼의 <c>onClick</c> 도 이걸 부른다 —
        /// 키와 버튼이 같은 경로를 타야 동작이 갈리지 않는다.
        /// </summary>
        public bool TryAttack()
        {
            if (!IsReady) return false;

            // 피격 경직·구역 전환 중에는 입력이 잠긴다. 그때는 공격도 막는다.
            if (player != null && !player.InputEnabled) return false;

            nextAllowedTime = Time.time + cooldown;

            Vector2 facing = ResolveFacing();
            Vector2 origin = (Vector2)transform.position + facing * originForwardOffset;

            lastOrigin = origin;
            lastFacing = facing;
            hasSwung = true;

            // 판정보다 먼저 띄운다. 대상이 없어도 휘두른 것은 보여야 한다 —
            // 헛스윙이 조용하면 입력이 씹혔다고 느낀다.
            ShowSwing(origin, facing);

            // 때리는 일은 SkillCast 한 곳에 있다. 스킬도 같은 경로를 타므로
            // 기본 공격에서 검증한 것이 스킬에 그대로 이어진다.
            SkillCast.Strike(
                transform, shape, shapeParams, origin, facing, ResolveAttackPower(),
                filter, overlapped, struck, struckAreas, logHits);

            return true;
        }

        /// <summary>
        /// 휘두른 것을 화면에 보여준다.
        ///
        /// <b>검격 그림이 있으면 그것을 쓴다.</b> 판정 도형을 그대로 그리면
        /// 부채꼴 전체가 채워져 칼이 아니라 부채를 휘두르는 것처럼 보인다 (결정 008 §8).
        /// 그림이 아직 없으면 임시로 범위를 그린다 — 아무것도 안 뜨는 것보다는 낫다.
        ///
        /// 검격은 <b>몸을 따라간다.</b> 휘두르는 0.2초 사이에 걸어가면
        /// 칼자국만 뒤에 남아 몸과 떨어져 보인다. 따라갈지는 이펙트 정의가 정한다.
        /// </summary>
        private void ShowSwing(Vector2 origin, Vector2 facing)
        {
            if (!ServiceRegistry.TryGet(out SkillImpactPool impacts) || impacts == null) return;

            if (attackEffect != null && attackEffect.HasArt)
            {
                impacts.Play(attackEffect, transform, transform.position, facing);
                return;
            }

            if (showRangeWhenNoArt) impacts.PlayFriendly(shape, shapeParams, origin, facing);
        }

        /// <summary>공격력은 런타임 상태에서 읽는다. 스킬로 오르는 값이 여기 반영된다.</summary>
        private static float ResolveAttackPower() => SkillCast.PlayerAttackPower();

        private Vector2 ResolveFacing()
        {
            Vector2 facing = animatorDriver != null ? animatorDriver.FacingVector : Vector2.down;

            if (!autoAim) return facing;

            // 가까운 대상이 있으면 그쪽으로 돌린다. 없으면 바라보던 방향 그대로.
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

        private void OnDrawGizmosSelected()
        {
            Vector2 facing;
            Vector2 origin;

            if (Application.isPlaying && drawLastSwing && hasSwung)
            {
                facing = lastFacing;
                origin = lastOrigin;
                Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.9f);
            }
            else
            {
                facing = animatorDriver != null ? animatorDriver.FacingVector : Vector2.down;
                if (facing.sqrMagnitude < 0.0001f) facing = Vector2.down;

                origin = (Vector2)transform.position + facing.normalized * originForwardOffset;
                Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.5f);
            }

            SkillShape.DrawGizmo(shape, shapeParams, origin, facing);
        }
    }
}
