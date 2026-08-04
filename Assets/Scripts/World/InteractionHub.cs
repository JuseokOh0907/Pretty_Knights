using System;
using System.Collections.Generic;
using PrettyKnights.Characters;
using PrettyKnights.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PrettyKnights.World
{
    /// <summary>
    /// 지금 사용할 수 있는 대상 하나를 고르고, 사용 입력을 그쪽으로 넘긴다.
    /// <b>Boot 씬에 상주한다.</b> 게임플레이 씬이 갈아 끼워져도 유지돼야 하고,
    /// 사용 버튼이 <c>UIRoot</c> 쪽에 있기 때문이다.
    ///
    /// 판정은 <see cref="InteractableBehaviour"/> 가 각자의 트리거로 하고,
    /// 여기는 <b>여럿이 겹쳤을 때 무엇을 고를지</b>만 정한다.
    /// 포탈 두 개가 붙어 있거나 상자 위에 포탈이 겹친 자리에서 필요하다.
    ///
    /// 입력을 <c>PlayerController</c> 가 아니라 여기서 읽는 이유는
    /// 그쪽이 입력과 이동만 담당해야 하고, 상호작용이 없는 씬에서
    /// 그 코드를 들고 다니지 않게 하기 위해서다
    /// (docs/decisions/005-dungeon-and-monster-design.md §4 와 같은 이유).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractionHub : MonoBehaviour
    {
        private const string ActionMapName = "Player";
        private const string InteractActionName = "Interact";

        [Header("입력")]
        [SerializeField, Tooltip("Assets/InputSystem_Actions.inputactions")]
        private InputActionAsset inputActions;

        [Header("연타 방지")]
        [SerializeField, Min(0f), Tooltip("사용 직후 이 시간 동안은 다시 받지 않는다")]
        private float reuseDelay = 0.3f;

        private readonly List<IInteractable> candidates = new List<IInteractable>();
        private InputAction interactAction;
        private float nextAllowedTime;

        /// <summary>지금 고른 대상. 없으면 null. UI 가 이걸 보고 버튼을 켠다.</summary>
        public IInteractable Current { get; private set; }

        /// <summary>대상이 바뀔 때마다 발생한다. 없어지면 null 이 온다.</summary>
        public event Action<IInteractable> CurrentChanged;

        /// <summary>true 면 입력을 받지 않는다. 구역 전환·컷신 동안 켠다.</summary>
        public bool Locked { get; set; }

        private void Awake()
        {
            ResolveAction();
            ServiceRegistry.Register(this);
        }

        private void OnDestroy()
        {
            if (ServiceRegistry.TryGet(out InteractionHub current) && current == this)
                ServiceRegistry.Unregister<InteractionHub>();
        }

        private void OnEnable() => interactAction?.Enable();

        private void OnDisable() => interactAction?.Disable();

        private void ResolveAction()
        {
            if (inputActions == null)
            {
                Debug.LogError(
                    "[InteractionHub] InputActionAsset 이 비어 있습니다. " +
                    "인스펙터에 InputSystem_Actions 를 연결하세요. 키보드 사용 키가 동작하지 않습니다.");
                return;
            }

            InputActionMap map = inputActions.FindActionMap(ActionMapName, throwIfNotFound: false);
            interactAction = map?.FindAction(InteractActionName, throwIfNotFound: false);

            if (interactAction == null)
                Debug.LogError($"[InteractionHub] '{ActionMapName}/{InteractActionName}' 액션을 찾지 못했습니다.");
        }

        public void Add(IInteractable target)
        {
            if (target == null || candidates.Contains(target)) return;

            candidates.Add(target);
            Refresh();
        }

        public void Remove(IInteractable target)
        {
            if (target == null) return;
            if (!candidates.Remove(target)) return;

            Refresh();
        }

        private void Update()
        {
            // 후보가 파괴되거나 조건이 바뀔 수 있으므로 매 프레임 다시 고른다.
            // 후보는 보통 0~2개라 비용이 없다.
            Refresh();

            if (Locked || Current == null) return;
            if (interactAction == null || !interactAction.WasPressedThisFrame()) return;

            TryInteract();
        }

        /// <summary>
        /// 지금 고른 대상을 사용한다. 화면 버튼의 <c>onClick</c> 도 이걸 부른다.
        /// 키보드와 버튼이 같은 경로를 타야 동작이 갈리지 않는다.
        /// </summary>
        public bool TryInteract()
        {
            if (Locked || !IsAlive(Current)) return false;
            if (Time.unscaledTime < nextAllowedTime) return false;
            if (!Current.CanInteract) return false;

            nextAllowedTime = Time.unscaledTime + reuseDelay;
            Current.Interact();
            return true;
        }

        /// <summary>가장 가까운 사용 가능 대상을 고른다.</summary>
        private void Refresh()
        {
            // 파괴된 오브젝트를 걸러낸다.
            for (int i = candidates.Count - 1; i >= 0; i--)
                if (!IsAlive(candidates[i])) candidates.RemoveAt(i);

            IInteractable best = null;

            if (candidates.Count > 0 && TryGetPlayerPosition(out Vector2 from))
            {
                float bestDistance = float.MaxValue;

                foreach (IInteractable candidate in candidates)
                {
                    if (!candidate.CanInteract) continue;

                    float distance = ((Vector2)candidate.Anchor.position - from).sqrMagnitude;
                    if (distance >= bestDistance) continue;

                    bestDistance = distance;
                    best = candidate;
                }
            }

            if (ReferenceEquals(best, Current)) return;

            Current = best;
            CurrentChanged?.Invoke(best);
        }

        /// <summary>
        /// 파괴된 대상인지 확인한다.
        ///
        /// <b>인터페이스 참조에는 Unity 의 널 비교가 걸리지 않는다.</b>
        /// <c>IInteractable</c> 로 들고 있으면 <c>== null</c> 이 C# 기본 참조 비교라
        /// 이미 Destroy 된 MonoBehaviour 도 "살아 있음" 으로 나온다.
        /// <c>UnityEngine.Object</c> 로 캐스팅해야 오버로드된 비교가 적용된다.
        /// </summary>
        private static bool IsAlive(IInteractable candidate)
        {
            if (candidate == null) return false;

            // using System; 이 있어 Object 만 쓰면 System.Object 와 충돌한다. 전체 이름으로 쓴다.
            return !(candidate is UnityEngine.Object unityObject) || unityObject != null;
        }

        private static bool TryGetPlayerPosition(out Vector2 position)
        {
            if (ServiceRegistry.TryGet(out PlayerController player) && player != null)
            {
                position = player.transform.position;
                return true;
            }

            position = default;
            return false;
        }
    }
}
