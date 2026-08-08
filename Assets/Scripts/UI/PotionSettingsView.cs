using PrettyKnights.Core;
using PrettyKnights.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PrettyKnights.UI
{
    /// <summary>
    /// 포션 자동 사용을 플레이어가 조절하는 자리. 인벤토리 화면 안에 둔다.
    ///
    /// <b>무엇을 마실지는 여기서 못 정한다.</b> 그건 아이템 정의의
    /// <c>Auto Use</c> 가 정하는 기획값이고, 여기는 <b>언제 마실지</b>만 다룬다.
    ///
    /// 설정은 <see cref="PotionSettings"/> 하나뿐이라 이 화면이 여럿 떠 있어도
    /// 서로 따라온다 — 바뀔 때마다 <c>Changed</c> 를 듣고 다시 그린다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PotionSettingsView : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField, Tooltip("자동 사용을 켜고 끈다")]
        private Toggle autoUseToggle;

        [SerializeField, Tooltip("임계값 슬라이더. Min 0.05 · Max 0.95 로 둘 것")]
        private Slider thresholdSlider;

        [SerializeField, Tooltip("\"HP 50% 이하\" 처럼 지금 값을 보여준다")]
        private TMP_Text thresholdLabel;

        [SerializeField, Tooltip("가진 포션 개수. 비워도 된다")]
        private TMP_Text stockLabel;

        private PotionSettings settings;

        /// <summary>슬라이더를 코드로 되돌릴 때 콜백이 다시 도는 것을 막는다.</summary>
        private bool applying;

        private void Awake()
        {
            if (autoUseToggle != null) autoUseToggle.onValueChanged.AddListener(OnAutoUseChanged);
            if (thresholdSlider != null) thresholdSlider.onValueChanged.AddListener(OnThresholdChanged);
        }

        private void OnDestroy()
        {
            if (autoUseToggle != null) autoUseToggle.onValueChanged.RemoveListener(OnAutoUseChanged);
            if (thresholdSlider != null) thresholdSlider.onValueChanged.RemoveListener(OnThresholdChanged);

            Unbind();
        }

        private void OnEnable() => Redraw();

        private void OnDisable() => Unbind();

        private void Update()
        {
            if (settings == null) Bind();

            // 개수는 인벤토리가 바뀔 때마다 달라진다. 화면이 떠 있을 때만 보므로 매 프레임이어도 싸다.
            if (isActiveAndEnabled) DrawStock();
        }

        private void Bind()
        {
            if (!ServiceRegistry.TryGet(out PotionSettings found) || found == null) return;

            settings = found;
            settings.Changed += Redraw;

            Redraw();
        }

        private void Unbind()
        {
            if (settings == null) return;

            settings.Changed -= Redraw;
            settings = null;
        }

        private void Redraw()
        {
            if (settings == null) return;

            applying = true;

            if (autoUseToggle != null) autoUseToggle.isOn = settings.AutoUse;

            if (thresholdSlider != null)
            {
                thresholdSlider.minValue = 0.05f;
                thresholdSlider.maxValue = 0.95f;
                thresholdSlider.value = settings.Threshold;
                thresholdSlider.interactable = settings.AutoUse;
            }

            if (thresholdLabel != null)
                thresholdLabel.text = settings.AutoUse
                    ? $"HP {settings.ThresholdPercent}% 이하에서 마신다"
                    : "자동으로 마시지 않는다";

            applying = false;
        }

        private void DrawStock()
        {
            if (stockLabel == null) return;
            if (!ServiceRegistry.TryGet(out Inventory bag) || bag == null) return;

            int stock = 0;
            for (int i = 0; i < bag.SlotCount; i++)
            {
                ItemDefinition item = bag.ItemAt(i);
                if (item != null && item.AutoUse) stock += bag.CountAt(i);
            }

            stockLabel.text = stock > 0 ? $"가진 포션 {stock}개" : "가진 포션 없음";
        }

        private void OnAutoUseChanged(bool value)
        {
            if (applying || settings == null) return;

            settings.SetAutoUse(value);
        }

        private void OnThresholdChanged(float value)
        {
            if (applying || settings == null) return;

            settings.SetThreshold(value);
        }
    }
}
