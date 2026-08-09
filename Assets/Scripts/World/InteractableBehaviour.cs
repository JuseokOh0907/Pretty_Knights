using PrettyKnights.Characters;
using PrettyKnights.Core;
using UnityEngine;

namespace PrettyKnights.World
{
    /// <summary>
    /// <see cref="IInteractable"/> 의 공통 뼈대. 트리거 출입과 후보 등록을 대신 처리한다.
    ///
    /// 상속받는 쪽은 <see cref="OnInteract"/> 만 채우면 된다.
    /// 포탈·상자·아이템이 각자 <c>OnTriggerEnter2D</c> 를 다시 쓰지 않게 하려는 것이다.
    ///
    /// <b>같은 오브젝트에 Is Trigger 가 켜진 Collider2D 가 있어야 한다.</b>
    /// 플레이어가 Rigidbody2D 를 들고 있으므로 이쪽은 콜라이더만 있으면 된다.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class InteractableBehaviour : MonoBehaviour, IInteractable
    {
        [Header("사용")]
        [SerializeField, Tooltip("사용 버튼에 표시할 말. 비우면 대상이 스스로 정한다")]
        private string promptLabel = string.Empty;

        [SerializeField, Tooltip(
            "사용 버튼에 띄울 그림. 버튼 하나를 포탈·아이템·상자가 나눠 쓰므로 " +
            "이게 무엇을 하게 되는지를 말한다. 비우면 버튼의 기본 그림")]
        private Sprite promptIcon;

        [SerializeField, Tooltip("끄면 겹쳐도 버튼이 뜨지 않는다. 보스를 잡아야 열리는 포탈 등에 쓴다")]
        private bool interactable = true;

        private bool playerInside;

        public virtual bool CanInteract => interactable && isActiveAndEnabled;
        public virtual string PromptLabel => promptLabel;
        public virtual Sprite PromptIcon => promptIcon;
        public Transform Anchor => transform;

        /// <summary>런타임에 열고 닫는다. 보스 처치 후 보상 포탈을 여는 식으로 쓴다.</summary>
        public void SetInteractable(bool value)
        {
            interactable = value;

            // 닫히는 순간 이미 떠 있던 버튼을 거둔다.
            if (!value) Withdraw();
            else if (playerInside) Offer();
        }

        protected virtual void Awake()
        {
            Collider2D[] colliders = GetComponents<Collider2D>();
            if (colliders.Length == 0)
            {
                Debug.LogError($"[{GetType().Name}] '{name}' 에 Collider2D 가 없습니다. 사용 판정이 동작하지 않습니다.");
                return;
            }

            bool anyTrigger = false;
            foreach (Collider2D collider in colliders)
                if (collider.isTrigger) anyTrigger = true;

            if (!anyTrigger)
                Debug.LogError(
                    $"[{GetType().Name}] '{name}' 의 Collider2D 에 Is Trigger 가 꺼져 있습니다. " +
                    "켜지 않으면 플레이어가 벽처럼 막히고 사용 버튼도 뜨지 않습니다.");
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayer(other)) return;

            playerInside = true;
            Offer();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayer(other)) return;

            playerInside = false;
            Withdraw();
        }

        /// <summary>
        /// 구역이 꺼지면 <c>OnTriggerExit2D</c> 는 오지 않는다.
        /// 여기서 직접 빠져나오지 않으면 사라진 포탈의 버튼이 화면에 남는다.
        /// </summary>
        protected virtual void OnDisable()
        {
            playerInside = false;
            Withdraw();
        }

        void IInteractable.Interact()
        {
            if (!CanInteract) return;
            OnInteract();
        }

        /// <summary>실제로 하는 일. 포탈은 여기서 구역 전환을 요청한다.</summary>
        protected abstract void OnInteract();

        private void Offer()
        {
            if (ServiceRegistry.TryGet(out InteractionHub hub) && hub != null) hub.Add(this);
        }

        private void Withdraw()
        {
            if (ServiceRegistry.TryGet(out InteractionHub hub) && hub != null) hub.Remove(this);
        }

        /// <summary>
        /// 콜라이더는 자식(Visual)에 있을 수도 있으므로 부모까지 거슬러 올라가 확인한다.
        /// 태그 대신 컴포넌트로 판정한다 — 태그는 오타가 나도 조용히 실패한다.
        /// </summary>
        private static bool IsPlayer(Collider2D other) =>
            other != null && other.GetComponentInParent<PlayerController>() != null;
    }
}
