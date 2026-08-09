using PrettyKnights.Core;
using PrettyKnights.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PrettyKnights.UI
{
    /// <summary>
    /// 사용 버튼. 쓸 수 있는 대상이 있을 때만 나타나고 그 대상의 이름을 표시한다.
    ///
    /// <b>버튼과 키보드가 같은 경로를 탄다.</b> 둘 다 <see cref="InteractionHub.TryInteract"/> 를 부른다.
    /// <c>OnScreenButton</c> 으로 가상 게임패드 입력을 흉내내지 않는 이유가 이것이다 —
    /// 그러면 눌림 판정이 입력 시스템을 한 바퀴 돌아 동작이 갈릴 수 있다.
    ///
    /// <b>숨길 때 <c>SetActive(false)</c> 를 쓰지 않는다.</b>
    /// 자기 자신을 끄면 <c>Update</c> 가 멈춰 다시 켤 주체가 사라진다.
    /// <see cref="CanvasGroup"/> 의 알파와 레이캐스트로만 감춘다.
    ///
    /// <b>쓸 대상이 없어도 사라지지 않고 흐려질 뿐이다</b> (2026-08-09 확정).
    /// 완전히 숨기면 버튼이 나타났다 사라지기를 반복해 화면이 들썩이고,
    /// 무엇보다 <b>처음 하는 사람이 그런 버튼이 있다는 것 자체를 모른다.</b>
    /// 자리를 늘 지키면 "가까이 가면 켜지는 것" 이라고 읽힌다.
    ///
    /// <c>UIRoot</c> 의 <b>가로 전용</b> 패널에 둔다. 세로는 자동 사냥이라 사용 버튼이 필요 없다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class InteractButton : MonoBehaviour
    {
        [Header("연결 (비우면 자동으로 찾는다)")]
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Button button;

        [SerializeField, Tooltip(
            "대상별 그림을 띄울 자리. 버튼 배경이 아니라 그 위의 자식 Image 여야 한다")]
        private Image icon;

        [SerializeField, Tooltip("글자도 함께 쓸 때만 연결한다. 비워도 된다")]
        private TMP_Text label;

        [Header("표시")]
        [SerializeField, Tooltip("대상이 아이콘을 안 줬을 때, 그리고 쓸 대상이 없을 때 쓸 그림")]
        private Sprite fallbackIcon;

        [SerializeField, Tooltip("라벨을 쓸 때만 의미가 있다")]
        private string fallbackLabel = "사용";

        [SerializeField, Range(0f, 1f), Tooltip(
            "쓸 대상이 없을 때의 알파. 0 으로 두면 예전처럼 완전히 사라진다")]
        private float disabledAlpha = 0.35f;

        private InteractionHub hub;

        private void Awake()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            if (button == null) button = GetComponentInChildren<Button>(includeInactive: true);
            if (label == null) label = GetComponentInChildren<TMP_Text>(includeInactive: true);

            // 자기 배경(루트의 Image)을 집으면 대상 그림이 버튼 판을 덮어쓴다.
            if (icon == null)
                foreach (Image found in GetComponentsInChildren<Image>(includeInactive: true))
                    if (found.gameObject != gameObject) { icon = found; break; }

            if (button != null) button.onClick.AddListener(OnClicked);
            else Debug.LogError($"[InteractButton] '{name}' 에서 Button 을 찾지 못했습니다. 터치로 사용할 수 없습니다.");

            Show(false);
        }

        private void OnEnable()
        {
            // 허브는 Boot 에 상주하지만 이 버튼보다 늦게 깨어날 수 있다.
            // 못 찾으면 Update 에서 계속 다시 시도한다.
            Bind();
            Apply(hub != null ? hub.Current : null);
        }

        private void OnDisable() => Unbind();

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnClicked);
            Unbind();
        }

        private void Update()
        {
            if (hub == null) Bind();
        }

        private void Bind()
        {
            if (hub != null) return;
            if (!ServiceRegistry.TryGet(out InteractionHub found) || found == null) return;

            hub = found;
            hub.CurrentChanged += Apply;
            Apply(hub.Current);
        }

        private void Unbind()
        {
            if (hub == null) return;

            hub.CurrentChanged -= Apply;
            hub = null;
        }

        private void OnClicked()
        {
            if (hub != null) hub.TryInteract();
        }

        private void Apply(IInteractable target)
        {
            bool usable = target != null;
            Show(usable);

            // 대상이 없을 때도 기본 그림으로 되돌린다. 버튼이 계속 보이므로
            // 그냥 두면 방금 떠나온 포탈의 그림이 화면에 남는다.
            if (icon != null)
            {
                Sprite sprite = usable ? target.PromptIcon : null;
                if (sprite == null) sprite = fallbackIcon;

                icon.sprite = sprite;
                icon.enabled = sprite != null;
            }

            if (label == null) return;

            string text = usable ? target.PromptLabel : null;
            label.text = string.IsNullOrWhiteSpace(text) ? fallbackLabel : text;
        }

        /// <summary>
        /// 쓸 수 있으면 또렷하게, 없으면 흐리게. <b>어느 쪽이든 자리는 지킨다.</b>
        /// 누를 수 없다는 것은 알파와 <c>blocksRaycasts</c> 둘 다로 말한다 —
        /// 흐리기만 하고 눌리면 "눌렀는데 아무 일도 없다" 가 된다.
        /// </summary>
        private void Show(bool usable)
        {
            if (group == null) return;

            group.alpha = usable ? 1f : disabledAlpha;
            group.blocksRaycasts = usable;
            group.interactable = usable;
        }
    }
}
