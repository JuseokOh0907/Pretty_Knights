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

        [SerializeField, Min(0f), Tooltip("넉백이 사그라드는 데 걸리는 시간")]
        private float knockbackDecay = 0.18f;

        private Rigidbody2D body;
        private Vector2 desiredDirection;
        private Vector2 currentVelocity;
        private Vector2 accelerationRef;

        private Vector2 knockbackVelocity;

        /// <summary>
        /// 이동으로 만들어진 속도. 애니메이션 구동에 쓴다.
        /// 넉백은 제외한다 — 밀려나는 중에 달리기 애니메이션이 나오면 어색하다.
        /// </summary>
        public Vector2 Velocity => currentVelocity;

        public bool IsKnockedBack => knockbackVelocity.sqrMagnitude > 0.01f;

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
            knockbackVelocity = Vector2.zero;
            body.linearVelocity = Vector2.zero;
        }

        /// <summary>
        /// 뒤로 밀어낸다.
        ///
        /// <see cref="FixedUpdate"/> 가 매번 <c>linearVelocity</c> 를 통째로 덮어쓰므로
        /// 외부에서 <c>AddForce</c> 를 걸면 다음 프레임에 지워진다.
        /// 그래서 넉백을 모터 안에서 다뤄야 한다.
        /// </summary>
        public void ApplyKnockback(Vector2 direction, float force)
        {
            if (force <= 0f || direction.sqrMagnitude < 0.0001f) return;

            // 연속으로 맞으면 덮어쓴다. 누적시키면 화면 밖으로 날아간다.
            knockbackVelocity = direction.normalized * force;
        }

        /// <summary>
        /// 즉시 자리를 옮긴다. 세이브 복원·포탈·순간이동에 쓴다.
        /// <c>transform.position</c> 을 직접 쓰면 물리가 한 프레임 늦게 따라와
        /// 벽에 낀 것으로 판정될 수 있으므로 <c>Rigidbody2D.position</c> 을 함께 옮긴다.
        ///
        /// <b>여기서는 목적지가 갈 수 있는 자리인지 검사하지 않는다.</b>
        /// 맵 밖으로 옮겨질 수 있으므로 호출부가
        /// <c>WalkableArea.IsWalkable</c> / <c>TryFindWalkable</c> 로 먼저 걸러야 한다.
        /// </summary>
        public void Warp(Vector2 position)
        {
            Stop();

            transform.position = new Vector3(position.x, position.y, transform.position.z);
            if (body != null) body.position = position;
        }

        private void FixedUpdate()
        {
            Vector2 target = desiredDirection * moveSpeed;

            currentVelocity = acceleration > 0f
                ? Vector2.SmoothDamp(currentVelocity, target, ref accelerationRef, acceleration)
                : target;

            if (knockbackVelocity.sqrMagnitude > 0f)
            {
                knockbackVelocity = knockbackDecay > 0f
                    ? Vector2.Lerp(knockbackVelocity, Vector2.zero, Time.fixedDeltaTime / knockbackDecay)
                    : Vector2.zero;

                if (knockbackVelocity.sqrMagnitude < 0.01f) knockbackVelocity = Vector2.zero;
            }

            body.linearVelocity = currentVelocity + knockbackVelocity;
        }
    }
}
