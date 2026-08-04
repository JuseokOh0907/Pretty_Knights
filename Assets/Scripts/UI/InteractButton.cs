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
    /// <c>UIRoot</c> 의 <b>가로 전용</b> 패널에 둔다. 세로는 자동 사냥이라 사용 버튼이 필요 없다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class InteractButton : MonoBehaviour
    {
        [Header("연결 (비우면 자동으로 찾는다)")]
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;

        [Header("표시")]
        [SerializeField, Tooltip("대상이 라벨을 비워뒀을 때 쓸 기본 문구")]
        private string fallbackLabel = "사용";

        private InteractionHub hub;

        private void Awake()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            if (button == null) button = GetComponentInChildren<Button>(includeInactive: true);
            if (label == null) label = GetComponentInChildren<TMP_Text>(includeInactive: true);

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
            bool show = target != null;
            Show(show);

            if (!show || label == null) return;

            string text = target.PromptLabel;
            label.text = string.IsNullOrWhiteSpace(text) ? fallbackLabel : text;
        }

        private void Show(bool visible)
        {
            if (group == null) return;

            group.alpha = visible ? 1f : 0f;
            group.blocksRaycasts = visible;
            group.interactable = visible;
        }
    }
}
