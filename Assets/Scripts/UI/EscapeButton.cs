using PrettyKnights.Core;
using PrettyKnights.World;
using UnityEngine;
using UnityEngine.UI;

namespace PrettyKnights.UI
{
    /// <summary>
    /// 던전 탈출 버튼. 상호작용 버튼 왼쪽에 둔다.
    ///
    /// <b>포탈이 단방향이라 이것이 유일한 귀환 수단이다.</b>
    /// 목적지는 지금 있는 구역의 <c>AreaDefinition.EscapeTo</c> 가 정하므로
    /// 이 버튼은 어디로 가는지 몰라도 된다 — <see cref="AreaTransition.RequestEscape"/> 하나만 부른다.
    ///
    /// <b>탈출할 수 없는 구역에서도 사라지지 않고 흐려진다.</b>
    /// <see cref="InteractButton"/> 과 같은 이유다 — 자리를 지켜야 그런 수단이 있다는 것을 안다.
    /// 던전 입구·시작 신전처럼 <c>EscapeTo</c> 가 빈 구역이 그렇다.
    ///
    /// <c>UIRoot</c> 의 <b>가로 전용</b> 패널에 둔다. 세로는 자동 사냥이라 탈출할 던전이 없다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class EscapeButton : MonoBehaviour
    {
        [Header("연결 (비우면 자동으로 찾는다)")]
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Button button;

        [Header("표시")]
        [SerializeField, Range(0f, 1f), Tooltip("탈출할 수 없는 구역에서의 알파")]
        private float disabledAlpha = 0.35f;

        private void Awake()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            if (button == null) button = GetComponentInChildren<Button>(includeInactive: true);

            if (button != null) button.onClick.AddListener(OnClicked);
            else Debug.LogError($"[EscapeButton] '{name}' 에서 Button 을 찾지 못했습니다.");
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnClicked);
        }

        /// <summary>
        /// 구역은 전환할 때만 바뀌지만 <b>이 컴포넌트가 그 시점을 알 방법이 없다</b> —
        /// 구역 교체는 <c>AreaRegistry</c> 안에서 일어나고 이벤트가 없다.
        /// 매 프레임 참조 두 번을 따라가는 비용이라 이벤트를 새로 뚫을 만큼은 아니다.
        /// </summary>
        private void Update()
        {
            if (group == null) return;

            bool usable = CanEscapeNow();

            group.alpha = usable ? 1f : disabledAlpha;
            group.blocksRaycasts = usable;
            group.interactable = usable;
        }

        private static bool CanEscapeNow()
        {
            // 전환이 도는 동안 또 누르면 페이드가 겹친다. Request 쪽에서도 막지만
            // 여기서 흐려 두면 "눌렀는데 아무 일도 없다" 를 겪지 않는다.
            if (ServiceRegistry.TryGet(out AreaTransition transition) &&
                transition != null && transition.IsTransitioning) return false;

            if (!ServiceRegistry.TryGet(out AreaRegistry registry)) return false;
            if (registry == null || registry.Active == null) return false;

            return registry.Active.Definition != null && registry.Active.Definition.CanEscape;
        }

        private void OnClicked()
        {
            if (ServiceRegistry.TryGet(out AreaTransition transition) && transition != null)
                transition.RequestEscape();
        }
    }
}
