using System.Collections.Generic;
using PrettyKnights.Combat;
using PrettyKnights.Core;
using UnityEngine;

namespace PrettyKnights.Characters
{
    /// <summary>
    /// 세로 모드의 자동 사냥. <b>이동과 기본 공격만 대신한다</b> (2026-08-09 확정).
    /// 스킬은 사람이 누른다 — 그래야 하단 스킬 버튼이 존재할 이유가 있고,
    /// 방치해도 파밍은 계속 돈다.
    ///
    /// <c>Player.prefab</c> 루트에 붙는다. <b>모드에 따라 스스로 켜고 끈다</b> —
    /// 세로 씬에만 두면 몸이 씬마다 새로 생기는 구조와 어긋나고,
    /// 가로에서 켜지면 손가락 조작과 싸운다.
    ///
    /// <b>모터를 직접 몰지 않는다.</b> <see cref="PlayerController.Drive"/> 로 넘겨
    /// 모터의 주인을 하나로 둔다 — 둘이 몰면 실행 순서에 따라 가다 서다를 반복한다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    public sealed class AutoBattle : MonoBehaviour
    {
        [Header("언제 켜지는가")]
        [SerializeField, Tooltip("이 모드에서만 자동으로 싸운다")]
        private GameMode activeMode = GameMode.Vertical;

        [SerializeField, Tooltip(
            "켜면 모드와 상관없이 항상 자동이다. 가로에서 검증할 때만 쓴다")]
        private bool forceAlwaysOn;

        [Header("연결 (비우면 자동으로 찾는다)")]
        [SerializeField] private PlayerController player;
        [SerializeField] private PlayerAttack attack;
        [SerializeField] private DirectionalAnimatorDriver animatorDriver;

        [Header("찾기 (월드 유닛)")]
        [SerializeField, Min(0f), Tooltip("이 반경 안에서 대상을 찾는다")]
        private float searchRadius = 12f;

        [SerializeField, Min(0f), Tooltip(
            "이보다 가까워지면 멈추고 때린다. 공격 사거리보다 조금 짧게 잡을 것 — " +
            "사거리 끝에서 멈추면 대상이 한 걸음만 물러나도 헛스윙이 된다")]
        private float engageDistance = 1.1f;

        [SerializeField, Min(0f), Tooltip(
            "대상을 다시 고르는 간격 (초). 매 프레임 고르면 두 대상 사이에서 떨린다")]
        private float retargetInterval = 0.35f;

        [Header("대상이 없을 때")]
        [SerializeField, Tooltip("아무도 없으면 이 반경 안을 어슬렁거린다. 0이면 가만히 선다")]
        private float wanderRadius = 4f;

        [SerializeField, Min(0.1f)] private float wanderInterval = 2.5f;

        [Header("판정 대상")]
        [SerializeField, Tooltip("이 레이어에서 찾는다. 실제 선별은 IDamageable 유무로 한다")]
        private LayerMask targetLayers = ~0;

        [Header("디버그")]
        [SerializeField] private bool drawGizmos = true;

        /// <summary>탐색 결과 버퍼. 이 컴포넌트의 것이다.</summary>
        private readonly List<Collider2D> found = new List<Collider2D>();

        private ContactFilter2D filter;
        private SceneFlow scenes;

        private Transform target;
        private float retargetTimer;

        private Vector2 wanderPoint;
        private float wanderTimer;
        private bool hasWanderPoint;

        /// <summary>지금 자동으로 싸우는 중인가. 하단 UI 가 이걸 표시할 수 있다.</summary>
        public bool IsActive { get; private set; }

        private void Awake()
        {
            if (player == null) player = GetComponent<PlayerController>();
            if (attack == null) attack = GetComponent<PlayerAttack>();
            if (animatorDriver == null) animatorDriver = GetComponentInChildren<DirectionalAnimatorDriver>();

            filter = new ContactFilter2D
            {
                useTriggers = false,
                useLayerMask = true,
                layerMask = targetLayers
            };
        }

        private void OnDisable() => Deactivate();

        private void Update()
        {
            if (!ResolveActive())
            {
                Deactivate();
                return;
            }

            Activate();

            retargetTimer -= Time.deltaTime;

            // 잡거나 놓친 대상은 즉시 버린다. 죽은 몸을 계속 쫓으면 그 자리에 멈춰 선다.
            if (!IsValid(target)) target = null;

            if (target == null || retargetTimer <= 0f)
            {
                retargetTimer = retargetInterval;
                target = FindNearest();
            }

            if (target == null)
            {
                Wander();
                return;
            }

            Engage();
        }

        /// <summary>
        /// 지금 이 모드인가. <see cref="SceneFlow"/> 는 Boot 에 있고 몸은 씬에 있으므로
        /// 캐시하지 않고 필요할 때 묻는다 — 몸이 먼저 켜지는 프레임이 있다.
        /// </summary>
        private bool ResolveActive()
        {
            if (forceAlwaysOn) return true;

            if (scenes == null && !ServiceRegistry.TryGet(out scenes)) return false;

            return scenes.CurrentMode.HasValue && scenes.CurrentMode.Value == activeMode;
        }

        private void Activate()
        {
            if (IsActive) return;

            IsActive = true;

            // 손가락 입력을 끈다. 이걸 안 끄면 조이스틱의 0 입력이 매 프레임 이겨
            // 자동 이동이 전혀 먹지 않는다.
            if (player != null) player.InputEnabled = false;
        }

        private void Deactivate()
        {
            if (!IsActive) return;

            IsActive = false;
            target = null;
            hasWanderPoint = false;

            // 돌려주지 않으면 가로로 갔을 때 몸이 아예 안 움직인다.
            if (player != null) player.InputEnabled = true;
        }

        /// <summary>가장 가까운 살아 있는 대상. 부서지는 오브젝트도 대상이 된다.</summary>
        private Transform FindNearest()
        {
            if (searchRadius <= 0f) return null;

            Physics2D.OverlapCircle(transform.position, searchRadius, filter, found);

            Transform best = null;
            float bestDistance = float.MaxValue;
            Vector2 self = transform.position;

            foreach (Collider2D collider in found)
            {
                if (collider == null) continue;

                IDamageable candidate = collider.GetComponentInParent<IDamageable>();
                if (candidate == null || !candidate.IsAlive) continue;
                if (candidate.Transform == transform) continue;

                float distance = ((Vector2)candidate.Transform.position - self).sqrMagnitude;
                if (distance >= bestDistance) continue;

                bestDistance = distance;
                best = candidate.Transform;
            }

            return best;
        }

        /// <summary>
        /// 인터페이스로 들고 있으면 유니티의 널 비교가 안 걸린다 (docs/pitfalls.md).
        /// 여기서는 <see cref="Transform"/> 으로 들고 있으므로 평범한 비교로 충분하다.
        /// </summary>
        private static bool IsValid(Transform candidate)
        {
            if (candidate == null || !candidate.gameObject.activeInHierarchy) return false;

            IDamageable damageable = candidate.GetComponentInParent<IDamageable>();
            return damageable != null && damageable.IsAlive;
        }

        private void Engage()
        {
            Vector2 self = transform.position;
            Vector2 toTarget = (Vector2)target.position - self;
            float distance = toTarget.magnitude;

            if (distance > engageDistance)
            {
                player.Drive(toTarget / Mathf.Max(0.0001f, distance));
                return;
            }

            // 사거리 안이면 멈춰서 때린다. 붙은 채로 계속 밀면 몸이 대상을 밀고 다닌다.
            player.Drive(Vector2.zero);

            // 멈추면 이동 방향이 갱신되지 않아 바라보는 쪽이 굳는다. 직접 돌려세운다.
            if (animatorDriver != null && toTarget.sqrMagnitude > 0.0001f)
                animatorDriver.ForceFacing(toTarget.normalized);

            if (attack != null) attack.TryAttack();
        }

        /// <summary>
        /// 아무도 없을 때. <b>가만히 서 있지 않는 이유는 스폰이 플레이어 기준이기 때문이다</b> —
        /// <see cref="World.FloorPopulation"/> 이 플레이어 주변 고리에 뿌리므로
        /// 조금씩 움직여야 새로 나온 것이 시야에 들어온다.
        /// </summary>
        private void Wander()
        {
            if (wanderRadius <= 0f)
            {
                player.Drive(Vector2.zero);
                return;
            }

            wanderTimer -= Time.deltaTime;

            if (!hasWanderPoint || wanderTimer <= 0f)
            {
                wanderTimer = wanderInterval;
                wanderPoint = (Vector2)transform.position + Random.insideUnitCircle * wanderRadius;
                hasWanderPoint = true;
            }

            Vector2 toPoint = wanderPoint - (Vector2)transform.position;

            // 도착했으면 다음 목적지를 앞당겨 고른다.
            if (toPoint.sqrMagnitude < 0.09f)
            {
                wanderTimer = 0f;
                player.Drive(Vector2.zero);
                return;
            }

            player.Drive(toPoint.normalized);
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos) return;

            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, searchRadius);

            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.7f);
            Gizmos.DrawWireSphere(transform.position, engageDistance);

            if (!Application.isPlaying || target == null) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }
}
