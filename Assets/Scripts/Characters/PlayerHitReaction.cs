using System;
using PrettyKnights.Core;
using PrettyKnights.Data;
using UnityEngine;

namespace PrettyKnights.Characters
{
    /// <summary>
    /// 플레이어가 맞았을 때의 반응. <b>넉백 + 경직 + 무적</b>을 한곳에서 다룬다.
    ///
    /// 속도를 깎는 방식은 쓰지 않는다. 맞아서 느려지면 도망을 못 쳐 또 맞고,
    /// 실수 한 번이 회복 불가능한 연쇄로 이어진다.
    /// 대신 밀려나면서 잠깐 조작이 잠기고, 그 사이 짧은 무적으로 숨통을 준다.
    ///
    /// 넉백 세기와 경직 시간은 <b>때린 몬스터의 정의</b>에서 온다.
    /// 같은 프리팹으로 잡몹과 보스의 손맛을 다르게 하기 위해서다.
    /// </summary>
    [RequireComponent(typeof(CharacterMotor))]
    [DisallowMultipleComponent]
    public sealed class PlayerHitReaction : MonoBehaviour
    {
        [Header("무적")]
        [SerializeField, Min(0f), Tooltip("경직이 끝난 뒤에도 이만큼 더 무적. 다수에게 둘러싸였을 때 즉사를 막는다")]
        private float invulnerabilityDuration = 0.4f;

        [Header("표현 (선택)")]
        [SerializeField, Tooltip("무적 동안 깜빡일 스프라이트. 비우면 자식에서 찾는다")]
        private SpriteRenderer flashTarget;

        [SerializeField, Min(0.01f)] private float flashInterval = 0.08f;

        /// <summary>피해가 실제로 들어간 순간. UI·사운드가 듣는다.</summary>
        public event Action<float> Damaged;

        public bool IsInvulnerable => invulnerableLeft > 0f;
        public bool IsStunned => stunLeft > 0f;

        private CharacterMotor motor;
        private PlayerController controller;

        private float stunLeft;
        private float invulnerableLeft;
        private float flashLeft;

        private void Awake()
        {
            motor = GetComponent<CharacterMotor>();
            controller = GetComponent<PlayerController>();

            if (flashTarget == null) flashTarget = GetComponentInChildren<SpriteRenderer>();
        }

        /// <summary>
        /// 몬스터에게 맞았다. 무적 중이면 아무 일도 일어나지 않는다.
        /// </summary>
        /// <param name="source">때린 쪽 위치. 넉백 방향을 여기서 뽑는다.</param>
        public void TakeHit(MonsterDefinition attacker, Vector2 source)
        {
            if (attacker == null || IsInvulnerable) return;

            if (ServiceRegistry.TryGet(out PlayerRuntimeState state))
            {
                // 데미지 공식이 아직 확정되지 않아 CombatSettings 가 고른 모델로 계산한다.
                // 플레이어를 때리는 쪽이므로 비대칭 모델에서 다른 배율이 적용된다.
                float defense = state.IsBound ? state.Stats.Defense : 0f;
                float damage = CombatSettings.Damage(attacker.Stats.Attack, defense, targetIsPlayer: true);

                state.ApplyDamage(damage);
                Damaged?.Invoke(damage);
            }

            Vector2 away = (Vector2)transform.position - source;
            if (away.sqrMagnitude < 0.0001f) away = Vector2.down;

            motor.ApplyKnockback(away, attacker.KnockbackForce);

            stunLeft = attacker.HitStunDuration;
            invulnerableLeft = attacker.HitStunDuration + invulnerabilityDuration;

            if (controller != null) controller.InputEnabled = false;
        }

        private void Update()
        {
            if (stunLeft > 0f)
            {
                stunLeft -= Time.deltaTime;

                // 경직이 끝나면 조작을 돌려준다. 무적은 조금 더 남아 있다.
                if (stunLeft <= 0f && controller != null) controller.InputEnabled = true;
            }

            if (invulnerableLeft <= 0f) return;

            invulnerableLeft -= Time.deltaTime;
            Blink();

            if (invulnerableLeft <= 0f && flashTarget != null) flashTarget.enabled = true;
        }

        private void Blink()
        {
            if (flashTarget == null) return;

            flashLeft -= Time.deltaTime;
            if (flashLeft > 0f) return;

            flashLeft = flashInterval;
            flashTarget.enabled = !flashTarget.enabled;
        }
    }
}
