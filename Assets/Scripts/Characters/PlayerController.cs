using UnityEngine;
using UnityEngine.InputSystem;

namespace PrettyKnights.Characters
{
    /// <summary>
    /// 입력을 받아 <see cref="CharacterMotor"/> 와 <see cref="DirectionalAnimatorDriver"/> 를 구동한다.
    ///
    /// 레거시 Input 매니저를 쓰지 않고 <c>Assets/InputSystem_Actions.inputactions</c> 의
    /// Player 맵을 직접 참조한다 (CLAUDE.md §6).
    /// 세로 자동 사냥에서는 입력을 끄고 AI 가 모터를 대신 구동하면 된다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerController : MonoBehaviour
    {
        private const string ActionMapName = "Player";
        private const string MoveActionName = "Move";
        private const string SprintActionName = "Sprint";

        [Header("입력")]
        [SerializeField, Tooltip("Assets/InputSystem_Actions.inputactions")]
        private InputActionAsset inputActions;

        [Header("연결")]
        [SerializeField] private CharacterMotor motor;
        [SerializeField] private DirectionalAnimatorDriver animatorDriver;

        [Header("속도 (월드 유닛/초)")]
        [SerializeField, Min(0f)] private float walkSpeed = 2.5f;
        [SerializeField, Min(1f)] private float runMultiplier = 1.6f;

        /// <summary>false 면 입력을 무시한다. 자동 사냥·컷신에서 끈다.</summary>
        public bool InputEnabled { get; set; } = true;

        private InputAction moveAction;
        private InputAction sprintAction;

        /// <summary>AI 가 대신 넣은 이동 방향. <see cref="InputEnabled"/> 가 false 일 때만 쓰인다.</summary>
        private Vector2 externalMove;

        /// <summary>그 방향을 넣은 프레임. 오래된 값으로 계속 걷지 않게 하려는 것.</summary>
        private int externalMoveFrame = int.MinValue;

        /// <summary>이 몸의 이동을 담당하는 모터. 세이브 복원이 위치를 옮길 때 쓴다.</summary>
        public CharacterMotor Motor => motor;

        /// <summary>바라보는 방향을 들고 있는 드라이버. 세이브 복원이 방향을 되돌릴 때 쓴다.</summary>
        public DirectionalAnimatorDriver AnimatorDriver => animatorDriver;

        /// <summary>피격 반응. 몬스터가 때릴 때 여기로 넘긴다.</summary>
        public PlayerHitReaction HitReaction { get; private set; }

        private void Awake()
        {
            if (motor == null) motor = GetComponent<CharacterMotor>();
            if (animatorDriver == null) animatorDriver = GetComponentInChildren<DirectionalAnimatorDriver>();

            HitReaction = GetComponent<PlayerHitReaction>();

            ResolveActions();

            // 게임플레이 씬에 있는 "몸"을 Boot 쪽에서 찾을 수 있게 등록한다.
            // 데이터(GameRoot.PlayerState)와 달리 몸은 씬마다 새로 생긴다.
            Core.ServiceRegistry.Register(this);
        }

        private void OnDestroy()
        {
            if (Core.ServiceRegistry.TryGet(out PlayerController registered) && registered == this)
                Core.ServiceRegistry.Unregister<PlayerController>();
        }

        private void ResolveActions()
        {
            if (inputActions == null)
            {
                Debug.LogError(
                    "[PlayerController] InputActionAsset 이 비어 있습니다. " +
                    "인스펙터에 InputSystem_Actions 를 연결하세요.");
                return;
            }

            InputActionMap map = inputActions.FindActionMap(ActionMapName, throwIfNotFound: false);
            if (map == null)
            {
                Debug.LogError($"[PlayerController] '{ActionMapName}' 액션 맵을 찾지 못했습니다.");
                return;
            }

            moveAction = map.FindAction(MoveActionName, throwIfNotFound: false);
            sprintAction = map.FindAction(SprintActionName, throwIfNotFound: false);

            if (moveAction == null)
                Debug.LogError($"[PlayerController] '{MoveActionName}' 액션을 찾지 못했습니다.");
        }

        private void OnEnable()
        {
            moveAction?.Enable();
            sprintAction?.Enable();
        }

        private void OnDisable()
        {
            moveAction?.Disable();
            sprintAction?.Disable();

            if (motor != null) motor.Stop();
        }

        /// <summary>
        /// AI 가 이동을 대신 넣는다. <b>모터의 주인은 끝까지 이 컴포넌트 하나다.</b>
        ///
        /// 자동 사냥이 모터를 직접 몰면 주인이 둘이 되고, 실행 순서에 따라
        /// <see cref="Update"/> 가 그 값을 0 으로 덮어쓰는 프레임이 생겨
        /// <b>가다 서다를 반복</b>한다. 원인을 찾기 어려운 종류의 증상이다.
        ///
        /// <see cref="InputEnabled"/> 가 true 면 무시된다 — 손가락이 항상 이긴다.
        /// </summary>
        public void Drive(Vector2 direction)
        {
            externalMove = direction;
            externalMoveFrame = Time.frameCount;
        }

        private void Update()
        {
            if (motor == null) return;

            Vector2 input =
                InputEnabled ? moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero :
                // 한 프레임만 유효하다. 모는 쪽이 멈추면 그 자리에 선다.
                externalMoveFrame >= Time.frameCount - 1 ? externalMove : Vector2.zero;

            // 상한은 항상 달리기 속도다. 실제 속도는 스틱을 얼마나 미느냐로 정해진다.
            // 살짝 밀면 걷고 끝까지 밀면 달린다 — 블렌드 트리의 Speed 경계가 그대로 상태를 가른다.
            // 걷기를 페널티가 아니라 플레이어의 선택으로 만들기 위한 것이다.
            //
            // 키보드는 입력 크기가 항상 1이라 늘 달리기가 된다. 개발용 입력이므로 무방하다.
            motor.MoveSpeed = walkSpeed * runMultiplier;
            motor.SetMoveInput(input);

            // 실제 적용된 속도를 넘긴다. 가속 중에도 애니메이션이 어긋나지 않는다.
            if (animatorDriver != null) animatorDriver.SetVelocity(motor.Velocity);
        }
    }
}
