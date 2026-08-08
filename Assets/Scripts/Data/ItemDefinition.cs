using UnityEngine;

namespace PrettyKnights.Data
{
    /// <summary>아이템의 갈래. 인벤토리 정렬과 설명 표시에 쓴다.</summary>
    public enum ItemCategory
    {
        /// <summary>재료. 쌓이고 쓸 수 없다.</summary>
        Material,

        /// <summary>소모품. 쓰면 없어진다.</summary>
        Consumable,

        /// <summary>장비. 장비 시스템이 생기면 여기 걸린다.</summary>
        Equipment,

        /// <summary>열쇠·증표. 버릴 수 없다.</summary>
        Key
    }

    /// <summary>
    /// 아이템 한 종류. <b>읽기 전용이다</b> — 개수는 <see cref="Inventory"/> 가 든다.
    ///
    /// <b>세이브에 들어가는 것은 <see cref="ItemId"/> 문자열뿐이다.</b>
    /// ScriptableObject 참조는 JSON 으로 저장할 수 없으므로
    /// 불러올 때 <see cref="ItemDatabase"/> 가 문자열을 다시 에셋으로 풀어 준다.
    /// 그래서 <b>한 번 정한 itemId 는 바꾸지 않는다</b> — 바꾸면 옛 세이브의 그 아이템이 사라진다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Item",
        menuName = "Pretty Knights/Item Definition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [Header("정체 — itemId 는 세이브에 기록된다. 정한 뒤 바꾸지 말 것")]
        [SerializeField] private string itemId = "material_scrap";
        [SerializeField] private string displayName = "잡동사니";

        [SerializeField, TextArea(2, 5)] private string description;

        [Header("표시")]
        [SerializeField] private Sprite icon;
        [SerializeField] private ItemCategory category = ItemCategory.Material;

        [Header("쌓기")]
        [SerializeField, Min(1), Tooltip("한 칸에 몇 개까지. 1이면 칸마다 하나씩 차지한다")]
        private int maxStack = 99;

        [Header("사용 — 지금은 회복만 된다")]
        [SerializeField, Tooltip("끄면 인벤토리에서 사용 버튼이 흐려진다")]
        private bool usable;

        [SerializeField, Min(0f), Tooltip("쓰면 회복하는 HP. 0이면 회복하지 않는다")]
        private float healAmount;

        [SerializeField, Tooltip("쓰면 한 개 없어지는지. 끄면 계속 쓸 수 있다")]
        private bool consumeOnUse = true;

        [SerializeField, Tooltip(
            "HP 가 정한 선 아래로 떨어지면 자동으로 쓴다. 포션에 켠다. " +
            "여러 종류가 켜져 있으면 낭비가 가장 적은 것부터 쓴다")]
        private bool autoUse;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public ItemCategory Category => category;
        public int MaxStack => Mathf.Max(1, maxStack);
        public bool Usable => usable;
        public float HealAmount => healAmount;
        public bool ConsumeOnUse => consumeOnUse;

        /// <summary>자동 사용 대상인가. 회복량이 0이면 자동으로 쓸 이유가 없다.</summary>
        public bool AutoUse => autoUse && usable && healAmount > 0f;

        /// <summary>열쇠는 버릴 수 없다. 버리면 그 던전을 영영 못 여는 상황이 생긴다.</summary>
        public bool Discardable => category != ItemCategory.Key;

        /// <summary>
        /// 실제로 쓴다. <b>효과가 늘면 여기만 늘어난다</b> —
        /// 인벤토리 UI 는 <see cref="Usable"/> 만 보고 버튼을 그리므로 바뀌지 않는다.
        /// </summary>
        public bool Use(PlayerRuntimeState player)
        {
            if (!usable || player == null || !player.IsBound) return false;

            if (healAmount > 0f)
            {
                // 이미 가득이면 쓰지 않는다. 안 그러면 물약이 조용히 사라진다.
                if (player.CurrentHp >= player.MaxHp) return false;

                player.Heal(healAmount);
                return true;
            }

            return false;
        }

        public override string ToString() => $"{displayName} ({itemId})";
    }
}
