using System;
using PrettyKnights.Data;
using UnityEngine;

namespace PrettyKnights.Characters
{
    /// <summary>
    /// 몬스터 한 마리의 행동. 플레이어와 <b>같은 프리팹 구조</b>를 쓴다
    /// (루트에 물리·모터, 자식 <c>Visual</c> 에 스프라이트·Animator·방향 드라이버).
    /// 차이는 입력 대신 이 스크립트가 모터를 구동한다는 것뿐이다.
    ///
    /// 수치는 전부 <see cref="MonsterDefinition"/> 에서 읽는다.
    /// 같은 프리팹에 정의만 갈아 끼우면 다른 몬스터가 된다.
    ///
    /// <b>경로 탐색은 아직 없다.</b> 지금은 대상 방향으로 곧장 향하는 단순 조향이며,
    /// 오브젝트가 많은 방에서는 벽에 걸릴 수 있다.
    /// 결정 005 에 따라 방 단위 그리드 A* 로 대체할 예정이다.
    /// </summary>
    [RequireComponent(typeof(CharacterMotor))]
    [DisallowMultipleComponent]
    public sealed class MonsterController : MonoBehaviour
    {
        public enum State
        {
            Idle,
            Wander,
            Chase,
            Attack,
            Dead
        }

        [Header("정의")]
        [SerializeField] private MonsterDefinition definition;

        [Header("연결 (비우면 자동으로 찾는다)")]
        [SerializeField] private CharacterMotor motor;
        [SerializeField] private DirectionalAnimatorDriver animatorDriver;

        [Header("배회")]
        [SerializeField, Min(0f), Tooltip("스폰 지점에서 벗어날 수 있는 반경 (월드 유닛)")]
        private float wanderRadius = 3f;
        [SerializeField, Min(0.1f)] private float wanderIntervalMin = 1.5f;
        [SerializeField, Min(0.1f)] private float wanderIntervalMax = 3.5f;

        /// <summary>죽는 순간 발생한다. 스포너가 이걸 듣고 리스폰 쿨타임을 시작한다.</summary>
        public event Action<MonsterController> Died;

        public MonsterDefinition Definition => definition;
        public State Current { get; private set; } = State.Idle;
        public float CurrentHp { get; private set; }
        public bool IsDead => Current == State.Dead;

        /// <summary>스폰된 자리. 배회 반경의 중심이다.</summary>
        private Vector2 anchor;

        private Transform target;
        private float wanderTimer;
        private Vector2 wanderDestination;
        private float attackCooldownLeft;

        private void Awake()
        {
            if (motor == null) motor = GetComponent<CharacterMotor>();
            if (animatorDriver == null) animatorDriver = GetComponentInChildren<DirectionalAnimatorDriver>();
        }

        private void OnEnable()
        {
            anchor = transform.position;
            wanderDestination = anchor;
            ResetRuntime();
        }

        /// <summary>
        /// 스포너가 풀에서 꺼내 재사용할 때 호출한다.
        /// <see cref="OnEnable"/> 만으로는 정의를 바꿔 끼울 수 없어 따로 둔다.
        /// </summary>
        public void Spawn(MonsterDefinition monsterDefinition, Vector2 position)
        {
            definition = monsterDefinition;

            motor.Warp(position);
            anchor = position;
            wanderDestination = position;

            ResetRuntime();
        }

        private void ResetRuntime()
        {
            CurrentHp = definition != null ? definition.MaxHp : 1f;
            Current = State.Idle;
            target = null;
            attackCooldownLeft = 0f;
            wanderTimer = 0f;

            motor.MoveSpeed = definition != null ? definition.MoveSpeed : 0f;
        }

        private void Update()
        {
            if (IsDead || definition == null) return;

            if (attackCooldownLeft > 0f) attackCooldownLeft -= Time.deltaTime;

            AcquireTarget();
            Think();

            if (animatorDriver != null) animatorDriver.SetVelocity(motor.Velocity);
        }

        /// <summary>
        /// 감지 범위 안에 플레이어가 있으면 대상으로 삼는다.
        /// 몸은 씬마다 새로 생기므로 <see cref="Core.ServiceRegistry"/> 에서 매번 확인한다.
        /// </summary>
        private void AcquireTarget()
        {
            if (!Core.ServiceRegistry.TryGet(out PlayerController player) || player == null)
            {
                target = null;
                return;
            }

            float distance = Vector2.Distance(transform.position, player.transform.position);

            // 감지에 들어오면 붙잡고, 감지 범위의 1.5배를 벗어나면 놓는다.
            // 경계에서 붙잡았다 놓았다 반복하는 것을 막기 위한 이력(hysteresis)이다.
            if (target == null)
            {
                if (distance <= definition.DetectRange) target = player.transform;
            }
            else if (distance > definition.DetectRange * 1.5f)
            {
                target = null;
            }
        }

        private void Think()
        {
            if (target == null)
            {
                Wander();
                return;
            }

            float distance = Vector2.Distance(transform.position, target.position);

            if (distance <= definition.AttackRange)
            {
                Current = State.Attack;
                motor.SetMoveInput(Vector2.zero);

                // 공격 방향은 이동이 멈춰도 대상을 향해야 한다.
                animatorDriver?.ForceFacing((Vector2)(target.position - transform.position), snap: false);

                if (attackCooldownLeft <= 0f) PerformAttack();
                return;
            }

            Current = State.Chase;
            motor.MoveSpeed = definition.MoveSpeed;
            motor.SetMoveInput(((Vector2)(target.position - transform.position)).normalized);
        }

        private void Wander()
        {
            wanderTimer -= Time.deltaTime;

            if (wanderTimer <= 0f)
            {
                wanderTimer = UnityEngine.Random.Range(wanderIntervalMin, wanderIntervalMax);
                wanderDestination = anchor + UnityEngine.Random.insideUnitCircle * wanderRadius;
            }

            Vector2 toDestination = wanderDestination - (Vector2)transform.position;

            // 목적지에 닿았으면 다음 목적지를 정할 때까지 선다.
            if (toDestination.sqrMagnitude < 0.04f)
            {
                Current = State.Idle;
                motor.SetMoveInput(Vector2.zero);
                return;
            }

            Current = State.Wander;

            // 배회는 추격보다 느리게. 멀리서 봐도 경계 상태가 아님이 읽힌다.
            motor.MoveSpeed = definition.MoveSpeed * 0.5f;
            motor.SetMoveInput(toDestination.normalized);
        }

        private void PerformAttack()
        {
            attackCooldownLeft = definition.AttackCooldown;

            // 판정 시스템이 아직 없다. 스킬 판정이 붙으면 여기서 범위 계산을 호출한다
            // (CLAUDE.md §5 — 판정과 VFX 를 분리한다).
            if (Core.ServiceRegistry.TryGet(out PlayerRuntimeState playerState))
                playerState.ApplyDamage(definition.Stats.Attack);
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f) return;

            CurrentHp = Mathf.Max(0f, CurrentHp - amount);

            // 맞으면 감지 범위 밖이어도 반응한다.
            if (target == null && Core.ServiceRegistry.TryGet(out PlayerController player))
                target = player.transform;

            if (CurrentHp <= 0f) Die();
        }

        private void Die()
        {
            Current = State.Dead;
            motor.Stop();

            if (Core.ServiceRegistry.TryGet(out PlayerRuntimeState playerState))
                playerState.AddExp(definition.ExpReward);

            Died?.Invoke(this);
        }

        private void OnDrawGizmosSelected()
        {
            if (definition == null) return;

            Vector3 center = Application.isPlaying ? (Vector3)anchor : transform.position;

            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.5f);
            Gizmos.DrawWireSphere(center, wanderRadius);

            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, definition.DetectRange);

            Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, definition.AttackRange);
        }
    }
}
