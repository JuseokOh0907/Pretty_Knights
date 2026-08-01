using UnityEngine;

namespace PrettyKnights.Characters
{
    /// <summary>
    /// 이동 상태를 Animator 파라미터로 옮긴다.
    /// <see cref="SpriteRenderer"/> 와 <see cref="Animator"/> 가 붙은 오브젝트에 함께 둔다
    /// (클립이 경로 없이 <c>m_Sprite</c> 를 애니메이션하므로 같은 오브젝트여야 한다).
    ///
    /// <b>방향 파라미터는 0으로 돌아가지 않는다.</b>
    /// 2D Simple Directional 블렌드 트리는 입력이 (0,0) 이면 8개 모션을 평균내
    /// 방향이 뭉개진다. 그래서 마지막으로 향했던 방향을 계속 유지하고,
    /// 정지·이동 여부는 <see cref="SpeedParam"/> 하나로만 가른다.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [DisallowMultipleComponent]
    public sealed class PlayerAnimatorDriver : MonoBehaviour
    {
        public static readonly int MoveXParam = Animator.StringToHash("MoveX");
        public static readonly int MoveYParam = Animator.StringToHash("MoveY");
        public static readonly int SpeedParam = Animator.StringToHash("Speed");

        [SerializeField, Tooltip("이 속도 미만이면 정지로 본다 (월드 유닛/초)")]
        private float idleThreshold = 0.05f;

        [SerializeField, Tooltip("방향 전환이 급하게 튀지 않도록 하는 보간 시간. 0이면 즉시")]
        private float directionSmoothing = 0.06f;

        private Animator animator;

        /// <summary>마지막으로 향했던 방향. 정지해도 유지된다.</summary>
        private Vector2 facing = Vector2.down;

        private Vector2 smoothed = Vector2.down;
        private Vector2 smoothVelocity;

        public EightDirection Facing => EightDirectionUtil.FromVector(facing);

        private void Awake()
        {
            animator = GetComponent<Animator>();
            ApplyToAnimator(0f);
        }

        /// <summary>
        /// 매 프레임 이동 속도 벡터를 넘긴다. 0 벡터면 방향은 그대로 두고 정지 처리한다.
        /// </summary>
        public void SetVelocity(Vector2 velocity)
        {
            float speed = velocity.magnitude;

            if (speed >= idleThreshold)
                facing = velocity / speed;

            ApplyToAnimator(speed);
        }

        /// <summary>이동과 무관하게 방향만 강제한다 (공격 방향 고정 등).</summary>
        public void ForceFacing(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.0001f) return;

            facing = direction.normalized;
        }

        private void ApplyToAnimator(float speed)
        {
            if (directionSmoothing > 0f)
            {
                smoothed = Vector2.SmoothDamp(
                    smoothed, facing, ref smoothVelocity, directionSmoothing);

                // 보간 중 원점을 지나며 (0,0) 에 가까워지면 방향이 뭉개진다.
                if (smoothed.sqrMagnitude < 0.04f) smoothed = facing;
            }
            else
            {
                smoothed = facing;
            }

            animator.SetFloat(MoveXParam, smoothed.x);
            animator.SetFloat(MoveYParam, smoothed.y);
            animator.SetFloat(SpeedParam, speed);
        }
    }
}
