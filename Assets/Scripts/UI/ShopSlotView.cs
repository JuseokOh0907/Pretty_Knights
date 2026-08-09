using PrettyKnights.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PrettyKnights.UI
{
    /// <summary>
    /// 상점 카드 하나. <b>세로 하단의 스킬 자리를 이것들이 차지한다</b> (결정 009 §4).
    ///
    /// <b>스스로 갱신하지 않는다.</b> <see cref="ShopView"/> 가 살 수 있는 상태가 바뀔 때
    /// 한 번에 <see cref="Refresh"/> 를 돌린다 — 카드마다 <c>Update</c> 를 돌면
    /// 여섯 칸이 매 프레임 문자열을 만든다.
    ///
    /// <b>못 사는 이유를 눌러 보기 전에 보여준다.</b> 값이 모자라면 흐려지고,
    /// 상한에 닿았으면 값 대신 <c>MAX</c> 가 뜬다. 눌러야 알 수 있으면
    /// "눌렀는데 아무 일도 없다" 가 된다 (<see cref="SkillButton"/> 과 같은 판단).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShopSlotView : MonoBehaviour
    {
        [Header("연결 (비우면 자동으로 찾는다)")]
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Button buyButton;

        [Header("글자와 그림")]
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text descriptionLabel;

        [SerializeField, Tooltip("현재 단계. 강화가 아니면 비워진다")]
        private TMP_Text levelLabel;

        [SerializeField, Tooltip("값. 상한에 닿으면 MAX 로 바뀐다")]
        private TMP_Text costLabel;

        [Header("표시")]
        [SerializeField, Range(0f, 1f), Tooltip("살 수 없을 때의 알파")]
        private float unaffordableAlpha = 0.45f;

        [SerializeField, Tooltip("상한에 닿았을 때 값 대신 띄울 글자")]
        private string maxedText = "MAX";

        [SerializeField, Tooltip("단계 표시 형식. {0} 이 단계")]
        private string levelFormat = "Lv.{0}";

        private Shop shop;
        private ShopOffer offer;

        public ShopOffer Offer => offer;

        private void Awake()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            if (buyButton == null) buyButton = GetComponentInChildren<Button>(includeInactive: true);

            if (buyButton != null) buyButton.onClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            if (buyButton != null) buyButton.onClick.RemoveListener(OnClicked);
        }

        /// <summary>
        /// 무엇을 파는 칸인지 정한다. <b>바뀌지 않는 것만 여기서 넣는다</b> —
        /// 이름과 그림은 다시 그릴 이유가 없다.
        /// </summary>
        public void Bind(Shop source, ShopOffer target)
        {
            shop = source;
            offer = target;

            if (offer == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            if (nameLabel != null) nameLabel.text = offer.DisplayName;
            if (descriptionLabel != null) descriptionLabel.text = offer.Description;

            if (icon != null)
            {
                // 그림이 없으면 칸의 기본 이미지를 그대로 둔다. 비우면 흰 사각형이 된다.
                if (offer.Icon != null) icon.sprite = offer.Icon;
                icon.enabled = icon.sprite != null;
            }

            Refresh();
        }

        /// <summary>값·단계·살 수 있는지를 다시 그린다.</summary>
        public void Refresh()
        {
            if (shop == null || offer == null) return;

            int level = shop.LevelOf(offer);
            bool hasRoom = shop.HasRoom(offer);
            bool canBuy = shop.CanBuy(offer);

            if (levelLabel != null)
            {
                // 소모품은 단계가 없다. 0 을 띄우면 안 산 강화처럼 보인다.
                bool showsLevel = offer.IsRepeatableUpgrade;

                levelLabel.gameObject.SetActive(showsLevel);
                if (showsLevel) levelLabel.text = string.Format(levelFormat, level);
            }

            if (costLabel != null)
                costLabel.text = hasRoom ? shop.CostOf(offer).ToString("N0") : maxedText;

            if (group != null) group.alpha = canBuy ? 1f : unaffordableAlpha;

            // 자리는 지키되 눌리지 않는다. 흐리기만 하고 눌리게 두면
            // 눌렀을 때 아무 일도 안 일어나는 버튼이 된다.
            if (buyButton != null) buyButton.interactable = canBuy;
        }

        private void OnClicked()
        {
            if (shop == null || offer == null) return;

            shop.TryBuy(offer);
        }
    }
}
