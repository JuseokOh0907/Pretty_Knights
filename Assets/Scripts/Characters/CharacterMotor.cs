using UnityEngine;

namespace PrettyKnights.Characters
{
    /// <summary>
    /// 탑다운 이동. 중력이 없고 입력 방향으로만 움직인다.
    ///
    /// 속도는 전부 월드 유닛/초 단위다. PPU 가 확정되지 않았으므로
    /// 픽셀 단위 상수를 쓰지 않는다 (docs/decisions/003-runtime-architecture.md).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [DisallowMultipleComponent]
    public sealed class CharacterMotor : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("기본 이동 속도 (월드 유닛/초)")]
        private float moveSpeed = 2.5f;

        [SerializeField, Min(0f), Tooltip("가속에 걸리는 시간. 0이면 즉시 최고 속도")]
        private float acceleration = 0.08f;

        private Rigidbody2D body;
        private Vector2 desiredDirection;
        private Vector2 currentVelocity;
        private Vector2 accelerationRef;

        /// <summary>실제로 적용 중인 속도 벡터. 애니메이션 구동에 쓴다.</summary>
        public Vector2 Velocity => currentVelocity;

        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = Mathf.Max(0f, value);
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();

            // 탑다운이므로 중력과 회전을 쓰지 않는다.
            body.gravityScale = 0f;
            body.freezeRotation = true;
        }

        /// <summary>
        /// 이동 방향을 지정한다. 길이가 1을 넘으면 정규화하므로
        /// 대각선 입력이 더 빨라지지 않는다.
        /// </summary>
        public void SetMoveInput(Vector2 direction)
        {
            desiredDirection = direction.sqrMagnitude > 1f ? direction.normalized : direction;
        }

        public void Stop()
        {
            desiredDirection = Vector2.zero;
            currentVelocity = Vector2.zero;
            accelerationRef = Vector2.zero;
            body.linearVelocity = Vector2.zero;
        }

        private void FixedUpdate()
        {
            Vector2 target = desiredDirection * moveSpeed;

            currentVelocity = acceleration > 0f
                ? Vector2.SmoothDamp(currentVelocity, target, ref accelerationRef, acceleration)
                : target;

            body.linearVelocity = currentVelocity;
        }
    }
}
