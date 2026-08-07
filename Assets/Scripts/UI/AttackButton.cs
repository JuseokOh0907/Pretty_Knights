using PrettyKnights.Combat;
using PrettyKnights.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PrettyKnights.UI
{
    /// <summary>
    /// 공격 버튼. 조이스틱 반대쪽(오른쪽 아래)에 둔다.
    ///
    /// <b>버튼과 키가 같은 경로를 탄다</b> — 둘 다 <see cref="PlayerAttack.TryAttack"/> 을 부른다.
    /// <see cref="InteractButton"/> 과 같은 이유다.
    ///
    /// 쿨타임은 채워지는 이미지로 보여준다. 눌리지 않는 이유가 화면에 없으면
    /// 플레이어는 입력이 씹혔다고 느낀다.
    ///
    /// <c>UIRoot</c> 의 <b>가로 전용</b> 패널에 둔다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AttackButton : MonoBehaviour
    {
        [Header("연결 (비우면 자동으로 찾는다)")]
        [SerializeField] private Button button;

        [SerializeField, Tooltip("쿨타임을 그릴 이미지. Image Type 을 Filled 로 둘 것")]
        private Image cooldownFill;

        [Header("표시")]
        [SerializeField, Tooltip("쿨타임 중 버튼이 흐려지는 정도")]
        [Range(0f, 1f)] private float dimmedAlpha = 0.45f;

        [SerializeField] private CanvasGroup group;

        private PlayerAttack attack;

        private void Awake()
        {
            if (button == null) button = GetComponentInChildren<Button>(includeInactive: true);
            if (group == null) group = GetComponent<CanvasGroup>();

            if (button != null) button.onClick.AddListener(OnClicked);
            else Debug.LogError($"[AttackButton] '{name}' 에서 Button 을 찾지 못했습니다.");
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnClicked);
        }

        private void Update()
        {
            // 몸은 씬마다 새로 생기므로 놓쳤으면 계속 다시 찾는다.
            if (attack == null && !ServiceRegistry.TryGet(out attack)) return;
            if (attack == null) return;

            float progress = attack.CooldownProgress;

            if (cooldownFill != null) cooldownFill.fillAmount = progress;
            if (group != null) group.alpha = attack.IsReady ? 1f : dimmedAlpha;
        }

        private void OnClicked()
        {
            if (attack != null) attack.TryAttack();
        }
    }
}
