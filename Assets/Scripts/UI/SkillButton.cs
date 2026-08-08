using PrettyKnights.Combat;
using PrettyKnights.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PrettyKnights.UI
{
    /// <summary>
    /// 스킬 버튼 하나. 공격 버튼 위에 4개를 나란히 둔다.
    ///
    /// <b>세 가지 상태를 그린다</b> — 잠김 · 쿨타임 · 준비됨.
    /// 셋을 알파 하나로 구분하는 이유는 눌리지 않는 이유가 화면에 없으면
    /// 플레이어가 입력이 씹혔다고 느끼기 때문이다 (<see cref="AttackButton"/> 과 같은 판단).
    ///
    /// <b><see cref="ISkillBar"/> 구현체가 아직 없다.</b> 그때까지 네 버튼은 전부
    /// 잠김으로 그려진다 — 배우지 않았다는 것이 지금의 사실이다.
    ///
    /// <c>UIRoot</c> 의 <b>가로 전용</b> 패널에 둔다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class SkillButton : MonoBehaviour
    {
        [Header("슬롯")]
        [SerializeField, Min(0), Tooltip("왼쪽부터 0, 1, 2, 3. 버튼마다 다르게 줄 것")]
        private int slotIndex;

        [Header("연결 (비우면 자동으로 찾는다)")]
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Button button;

        [SerializeField, Tooltip("스킬 아이콘. 스킬이 아이콘을 주면 여기 덮어쓴다")]
        private Image icon;

        [SerializeField, Tooltip("쿨타임을 그릴 이미지. Image Type 을 Filled 로 둘 것")]
        private Image cooldownFill;

        [Header("표시")]
        [SerializeField, Range(0f, 1f), Tooltip("아직 배우지 않은 슬롯의 알파")]
        private float lockedAlpha = 0.2f;

        [SerializeField, Range(0f, 1f), Tooltip("쿨타임 도는 중의 알파")]
        private float cooldownAlpha = 0.45f;

        /// <summary>지금 물고 있는 아이콘. 같은 스프라이트를 매 프레임 다시 넣지 않으려는 것.</summary>
        private Sprite appliedIcon;

        private void Awake()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            if (button == null) button = GetComponentInChildren<Button>(includeInactive: true);

            if (button != null) button.onClick.AddListener(OnClicked);
            else Debug.LogError($"[SkillButton] '{name}' 에서 Button 을 찾지 못했습니다.");

            // 스킬 창구가 붙기 전에는 잠김이다. 첫 프레임부터 그렇게 보여야 한다.
            Draw(unlocked: false, ready: false, progress: 0f);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnClicked);
        }

        private void Update()
        {
            // 몸은 씬마다 새로 생기므로 캐시하지 않고 매번 묻는다.
            if (!ServiceRegistry.TryGet(out ISkillBar bar) || bar == null || slotIndex >= bar.SlotCount)
            {
                Draw(unlocked: false, ready: false, progress: 0f);
                return;
            }

            bool unlocked = bar.IsUnlocked(slotIndex);

            Draw(unlocked, unlocked && bar.IsReady(slotIndex), bar.CooldownProgress(slotIndex));
            ApplyIcon(bar.IconOf(slotIndex));
        }

        private void Draw(bool unlocked, bool ready, float progress)
        {
            if (cooldownFill != null) cooldownFill.fillAmount = unlocked ? progress : 0f;

            if (group == null) return;

            group.alpha = !unlocked ? lockedAlpha : ready ? 1f : cooldownAlpha;

            // 잠긴 것도 자리는 지키되 눌리지 않는다. 쿨타임 중에는 눌러도 되게 둔다 —
            // 막으면 연타로 "다음 순간 나가게" 하는 조작을 할 수 없다.
            group.blocksRaycasts = unlocked;
            group.interactable = unlocked;
        }

        private void ApplyIcon(Sprite sprite)
        {
            if (icon == null || sprite == null || sprite == appliedIcon) return;

            icon.sprite = sprite;
            appliedIcon = sprite;
        }

        private void OnClicked()
        {
            if (ServiceRegistry.TryGet(out ISkillBar bar) && bar != null) bar.TryCast(slotIndex);
        }
    }
}
