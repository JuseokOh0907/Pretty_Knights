using UnityEngine;

namespace PrettyKnights.Data
{
    /// <summary>상점 한 칸이 파는 것의 종류.</summary>
    public enum ShopOfferKind
    {
        /// <summary>사면 스탯이 영구히 오른다. 여러 번 살 수 있고 값이 오른다.</summary>
        StatUpgrade,

        /// <summary>사면 가방에 들어간다. 포션 같은 소모품.</summary>
        Item
    }

    /// <summary>강화가 올리는 수치. <see cref="StatBlock"/> 의 어느 칸인가.</summary>
    public enum UpgradeStat
    {
        Vitality,
        Attack,
        Defense,
        Agility,
        Focus
    }

    /// <summary>
    /// 상점 한 칸. <b>골드를 쓰는 곳은 세로 모드에만 있다</b> (결정 009 §4).
    ///
    /// 강화와 소모품을 한 에셋으로 다루는 이유는 <b>화면에서 같은 칸으로 보이기</b> 때문이다.
    /// 이름 · 그림 · 값 · 누르면 산다 — 여기까지가 같고, 다른 것은 산 뒤에 무슨 일이
    /// 일어나는가뿐이다. 둘로 가르면 UI 도 둘이 된다.
    ///
    /// <b>수치는 비워 둔 채로 만든다.</b> 값은 밸런싱에서 채운다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ShopOffer",
        menuName = "Pretty Knights/Shop Offer")]
    public sealed class ShopOffer : ScriptableObject
    {
        [Header("정체")]
        [SerializeField, Tooltip(
            "강화 단계가 이 id 로 세이브에 들어간다. **한 번 정하면 바꾸지 않는다** — " +
            "바꾸면 그 강화를 산 세이브에서 단계가 사라진다")]
        private string offerId = "upgrade_attack";

        [SerializeField] private string displayName = "공격력 강화";

        [SerializeField, TextArea(2, 4)] private string description;

        [SerializeField] private Sprite icon;

        [Header("무엇을 파는가")]
        [SerializeField] private ShopOfferKind kind = ShopOfferKind.StatUpgrade;

        [Header("강화 (Stat Upgrade 일 때)")]
        [SerializeField] private UpgradeStat stat = UpgradeStat.Attack;

        [SerializeField, Tooltip("한 단계마다 이만큼 오른다")]
        private float amountPerLevel = 1f;

        [SerializeField, Min(0), Tooltip("최대 단계. 0이면 상한 없음")]
        private int maxLevel;

        [Header("소모품 (Item 일 때)")]
        [SerializeField] private ItemDefinition item;

        [SerializeField, Min(1)] private int itemCount = 1;

        [Header("값")]
        [SerializeField, Min(0), Tooltip("첫 구매 가격")]
        private long baseCost = 100;

        [SerializeField, Min(1f), Tooltip(
            "한 단계 오를 때마다 값에 곱한다. 1이면 값이 안 오른다. " +
            "소모품은 이 값을 쓰지 않는다 — 살 때마다 비싸지면 쟁여 둘 수 없다")]
        private float costGrowth = 1.15f;

        public string OfferId => offerId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public ShopOfferKind Kind => kind;

        public UpgradeStat Stat => stat;
        public float AmountPerLevel => amountPerLevel;
        public int MaxLevel => maxLevel;

        public ItemDefinition Item => item;
        public int ItemCount => itemCount;

        public long BaseCost => baseCost;

        /// <summary>강화만 단계를 센다. 소모품은 몇 번을 사도 값이 같다.</summary>
        public bool IsRepeatableUpgrade => kind == ShopOfferKind.StatUpgrade;

        /// <summary>이미 <paramref name="level"/> 단계일 때 다음 하나의 값.</summary>
        public long CostAt(int level)
        {
            if (!IsRepeatableUpgrade || level <= 0) return baseCost;

            double scaled = baseCost * System.Math.Pow(Mathf.Max(1f, costGrowth), level);

            return scaled >= long.MaxValue ? long.MaxValue : (long)scaled;
        }

        /// <summary>더 살 수 있는가. 상한이 0이면 언제나 살 수 있다.</summary>
        public bool HasRoom(int level) =>
            !IsRepeatableUpgrade || maxLevel <= 0 || level < maxLevel;

        /// <summary><paramref name="level"/> 단계에서 이 항목이 더해 주는 스탯.</summary>
        public StatBlock BonusAt(int level)
        {
            if (kind != ShopOfferKind.StatUpgrade || level <= 0) return StatBlock.Zero;

            float total = amountPerLevel * level;

            return stat switch
            {
                UpgradeStat.Vitality => new StatBlock(total, 0f, 0f, 0f, 0f),
                UpgradeStat.Attack => new StatBlock(0f, total, 0f, 0f, 0f),
                UpgradeStat.Defense => new StatBlock(0f, 0f, total, 0f, 0f),
                UpgradeStat.Agility => new StatBlock(0f, 0f, 0f, total, 0f),
                UpgradeStat.Focus => new StatBlock(0f, 0f, 0f, 0f, total),
                _ => StatBlock.Zero
            };
        }
    }
}
