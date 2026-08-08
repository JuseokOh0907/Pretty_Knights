using PrettyKnights.Characters;
using PrettyKnights.Core;
using PrettyKnights.Data;
using PrettyKnights.World;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PrettyKnights.UI
{
    /// <summary>
    /// 가방 화면. <b>왼쪽은 격자, 오른쪽은 고른 것의 자세한 설명</b>이다 (2026-08-09 확정).
    ///
    /// 격자 방식이라 얼마나 남았는지가 숫자가 아니라 <b>빈 칸으로 보인다.</b>
    /// 오른쪽을 따로 둔 이유는 칸 안에 설명을 우겨넣으면 격자가 못 읽히기 때문이다 —
    /// 칸은 "무엇이 몇 개" 까지만 말하고 나머지는 옆에서 말한다.
    ///
    /// <b>열려 있는 동안 조작을 잠근다.</b> 안 잠그면 가방을 보는 사이에 맞는다.
    /// <c>UIRoot</c> 의 가로 전용 패널에 둔다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class InventoryPanel : MonoBehaviour
    {
        private const string ActionMapName = "Player";
        private const string ToggleActionName = "Inventory";

        [Header("입력")]
        [SerializeField, Tooltip("Assets/InputSystem_Actions.inputactions")]
        private InputActionAsset inputActions;

        [Header("연결 (비우면 자동으로 찾는다)")]
        [SerializeField] private CanvasGroup group;

        [SerializeField, Tooltip("칸들이 들어 있는 부모. 자식 순서가 곧 칸 번호다")]
        private Transform slotRoot;

        [Header("오른쪽 — 고른 것")]
        [SerializeField] private Image detailIcon;
        [SerializeField] private TMP_Text detailName;
        [SerializeField] private TMP_Text detailCategory;
        [SerializeField] private TMP_Text detailDescription;

        [SerializeField, Tooltip("쓸 수 없는 아이템에서는 흐려진다")]
        private Button useButton;

        [SerializeField] private Button discardButton;

        [SerializeField, Tooltip("아무것도 고르지 않았을 때 켜지는 안내")]
        private GameObject emptyHint;

        [Header("표시")]
        [SerializeField, Range(0f, 1f)] private float disabledAlpha = 0.35f;

        private InventorySlotView[] slots;
        private Inventory bag;
        private InputAction toggleAction;
        private int selected = -1;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            if (slotRoot == null) slotRoot = transform;

            // 자식 순서가 곧 칸 번호다. 인스펙터에서 번호를 손으로 넣지 않는다.
            slots = slotRoot.GetComponentsInChildren<InventorySlotView>(includeInactive: true);
            for (int i = 0; i < slots.Length; i++) slots[i].Bind(i, Select);

            if (useButton != null) useButton.onClick.AddListener(UseSelected);
            if (discardButton != null) discardButton.onClick.AddListener(DiscardSelected);

            ResolveAction();
            Apply(false);
        }

        private void OnDestroy()
        {
            if (useButton != null) useButton.onClick.RemoveListener(UseSelected);
            if (discardButton != null) discardButton.onClick.RemoveListener(DiscardSelected);

            Unbind();
        }

        private void OnEnable() => toggleAction?.Enable();

        private void OnDisable()
        {
            toggleAction?.Disable();

            // 패널이 꺼진 채로 잠금이 남으면 움직일 수 없게 된다.
            if (IsOpen) Apply(false);
        }

        private void ResolveAction()
        {
            if (inputActions == null)
            {
                Debug.LogError(
                    "[InventoryPanel] InputActionAsset 이 비어 있습니다. " +
                    "인스펙터에 InputSystem_Actions 를 연결하세요.");
                return;
            }

            InputActionMap map = inputActions.FindActionMap(ActionMapName, throwIfNotFound: false);
            toggleAction = map?.FindAction(ToggleActionName, throwIfNotFound: false);

            if (toggleAction == null)
                Debug.LogError(
                    $"[InventoryPanel] '{ActionMapName}/{ToggleActionName}' 액션을 찾지 못했습니다. " +
                    "InputSystem_Actions 의 Player 맵에 Inventory 액션(Button)을 추가하세요.");
        }

        private void Update()
        {
            if (bag == null) Bind();

            if (toggleAction != null && toggleAction.WasPressedThisFrame()) Toggle();
        }

        private void Bind()
        {
            if (!ServiceRegistry.TryGet(out Inventory found) || found == null) return;

            bag = found;
            bag.Changed += Redraw;

            Redraw();
        }

        private void Unbind()
        {
            if (bag == null) return;

            bag.Changed -= Redraw;
            bag = null;
        }

        // ── 열고 닫기 ─────────────────────────────────────────────────────

        public void Toggle() => Apply(!IsOpen);

        public void Close() => Apply(false);

        private void Apply(bool open)
        {
            IsOpen = open;

            if (group != null)
            {
                group.alpha = open ? 1f : 0f;
                group.blocksRaycasts = open;
                group.interactable = open;
            }

            // 가방을 보는 사이에 맞지 않도록 조작과 상호작용을 함께 잠근다.
            if (ServiceRegistry.TryGet(out PlayerController player) && player != null)
                player.InputEnabled = !open;

            if (ServiceRegistry.TryGet(out InteractionHub hub) && hub != null)
                hub.Locked = open;

            if (!open) return;

            // 열 때마다 처음부터 본다. 지난번에 고른 칸이 이미 비었을 수 있다.
            selected = -1;
            Redraw();
        }

        // ── 그리기 ────────────────────────────────────────────────────────

        private void Redraw()
        {
            if (bag == null) return;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;

                slots[i].Draw(bag.ItemAt(i), bag.CountAt(i), i == selected);
            }

            DrawDetail();
        }

        private void DrawDetail()
        {
            ItemDefinition item = bag != null ? bag.ItemAt(selected) : null;

            if (emptyHint != null) emptyHint.SetActive(item == null);

            if (item == null)
            {
                if (detailIcon != null) detailIcon.enabled = false;
                if (detailName != null) detailName.text = string.Empty;
                if (detailCategory != null) detailCategory.text = string.Empty;
                if (detailDescription != null) detailDescription.text = string.Empty;

                SetButton(useButton, false);
                SetButton(discardButton, false);
                return;
            }

            if (detailIcon != null)
            {
                detailIcon.enabled = item.Icon != null;
                if (item.Icon != null) detailIcon.sprite = item.Icon;
            }

            if (detailName != null) detailName.text = item.DisplayName;

            if (detailCategory != null)
            {
                int count = bag.CountAt(selected);
                detailCategory.text = count > 1
                    ? $"{NameOf(item.Category)} · {count}개"
                    : NameOf(item.Category);
            }

            if (detailDescription != null) detailDescription.text = item.Description;

            // 쓸 수 없는 아이템에서도 버튼은 자리를 지킨다 — 사라지면 화면이 들썩인다.
            SetButton(useButton, item.Usable);
            SetButton(discardButton, item.Discardable);
        }

        /// <summary>쓸 수 없으면 흐려지되 사라지지 않는다. HUD 의 다른 버튼과 같은 규칙이다.</summary>
        private void SetButton(Button button, bool usable)
        {
            if (button == null) return;

            button.interactable = usable;

            var canvasGroup = button.GetComponent<CanvasGroup>();
            if (canvasGroup != null) canvasGroup.alpha = usable ? 1f : disabledAlpha;
        }

        private static string NameOf(ItemCategory category) => category switch
        {
            ItemCategory.Consumable => "소모품",
            ItemCategory.Equipment => "장비",
            ItemCategory.Key => "열쇠",
            _ => "재료"
        };

        // ── 조작 ──────────────────────────────────────────────────────────

        private void Select(int slot)
        {
            // 같은 칸을 다시 누르면 선택이 풀린다. 빈 칸을 눌러도 풀린다.
            selected = selected == slot || bag == null || bag.IsEmptyAt(slot) ? -1 : slot;
            Redraw();
        }

        private void UseSelected()
        {
            if (bag == null || selected < 0) return;
            if (!ServiceRegistry.TryGet(out PlayerRuntimeState player) || player == null) return;

            // 다 쓰면 그 칸이 비므로 선택을 놓는다.
            if (bag.Use(selected, player) && bag.IsEmptyAt(selected)) selected = -1;

            Redraw();
        }

        private void DiscardSelected()
        {
            if (bag == null || selected < 0) return;

            if (bag.DiscardAt(selected)) selected = -1;

            Redraw();
        }
    }
}
