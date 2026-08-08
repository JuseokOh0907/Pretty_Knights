using System;
using PrettyKnights.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PrettyKnights.UI
{
    /// <summary>
    /// 격자 한 칸. <see cref="InventoryPanel"/> 이 만들고 다시 그린다.
    ///
    /// <b>자기 번호를 안다.</b> 패널이 칸 목록을 순서대로 훑어 번호를 매기므로
    /// 인스펙터에서 번호를 손으로 넣지 않는다 — 서른 칸에 하나만 틀려도
    /// 엉뚱한 아이템이 선택되고 그 원인은 눈에 안 보인다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class InventorySlotView : MonoBehaviour
    {
        [Header("연결 (비우면 자동으로 찾는다)")]
        [SerializeField, Tooltip("아이템 그림. 빈 칸이면 꺼진다")]
        private Image icon;

        [SerializeField, Tooltip("개수. 1개면 숨긴다 — 모든 칸에 1이 적혀 있으면 시끄럽다")]
        private TMP_Text countLabel;

        [SerializeField, Tooltip("고른 칸에 켜지는 테두리")]
        private GameObject selectedFrame;

        private Button button;
        private int index = -1;
        private Action<int> onClicked;

        public int Index => index;

        private void Awake()
        {
            button = GetComponent<Button>();

            // 아이콘을 자동으로 찾을 때 자기 배경(루트의 Image)을 집으면 안 된다.
            if (icon == null)
                foreach (Image found in GetComponentsInChildren<Image>(includeInactive: true))
                    if (found.gameObject != gameObject) { icon = found; break; }

            if (countLabel == null) countLabel = GetComponentInChildren<TMP_Text>(includeInactive: true);

            button.onClick.AddListener(Click);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(Click);
        }

        /// <summary>패널이 시작할 때 번호와 콜백을 심는다.</summary>
        public void Bind(int slotIndex, Action<int> clicked)
        {
            index = slotIndex;
            onClicked = clicked;
        }

        /// <summary>칸의 내용을 그린다. <paramref name="item"/> 이 <c>null</c> 이면 빈 칸이다.</summary>
        public void Draw(ItemDefinition item, int count, bool selected)
        {
            if (icon != null)
            {
                icon.enabled = item != null && item.Icon != null;
                if (item != null && item.Icon != null) icon.sprite = item.Icon;
            }

            if (countLabel != null)
            {
                bool show = item != null && count > 1;

                countLabel.enabled = show;
                if (show) countLabel.text = count.ToString();
            }

            if (selectedFrame != null) selectedFrame.SetActive(selected);
        }

        private void Click() => onClicked?.Invoke(index);
    }
}
