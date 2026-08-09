using System;
using PrettyKnights.Data;
using UnityEngine;

namespace PrettyKnights.Save
{
    /// <summary>
    /// JSON 직렬화 단위.
    ///
    /// <b>필드를 추가하는 것은 버전을 올리지 않는다.</b>
    /// <c>JsonUtility</c> 는 파일에 없는 필드를 기본값으로 두므로 옛 세이브도 그대로 읽힌다.
    /// <b>필드를 지우거나 이름을 바꿀 때만</b> <see cref="Version"/> 을 올리고
    /// <see cref="SaveService"/> 에 마이그레이션을 추가한다.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        /// <summary>현재 세이브 포맷 버전.</summary>
        public const int CurrentVersion = 1;

        [SerializeField] private int version = CurrentVersion;
        [SerializeField] private long savedAtUtcTicks;
        [SerializeField] private PlayerRuntimeState player = new PlayerRuntimeState();
        [SerializeField] private WorldLocation location = new WorldLocation();
        [SerializeField] private WorldProgress progress = new WorldProgress();
        [SerializeField] private Inventory inventory = new Inventory();
        [SerializeField] private PotionSettings potions = new PotionSettings();
        [SerializeField] private Wallet wallet = new Wallet();
        [SerializeField] private PlayerUpgrades upgrades = new PlayerUpgrades();

        public int Version => version;
        public PlayerRuntimeState Player => player;

        /// <summary>가방. 옛 세이브에는 없으므로 빈 칸으로 시작한다.</summary>
        public Inventory Inventory => inventory;

        /// <summary>재화. 옛 세이브에는 없으므로 0 골드로 시작한다.</summary>
        public Wallet Wallet => wallet;

        /// <summary>상점에서 산 강화 단계. 스탯 보너스는 이걸로 매번 다시 계산한다.</summary>
        public PlayerUpgrades Upgrades => upgrades;

        /// <summary>포션 자동 사용 설정. 플레이어가 바꾸는 값이라 함께 저장한다.</summary>
        public PotionSettings Potions => potions;

        /// <summary>마지막으로 있었던 자리. 옛 세이브에는 없으므로 <c>HasValue</c> 가 false 다.</summary>
        public WorldLocation Location => location;

        /// <summary>무엇을 부쉈고 어느 테마를 클리어했는지. 옛 세이브에는 없어 비어 있다.</summary>
        public WorldProgress Progress => progress;

        public DateTime SavedAtUtc => new DateTime(savedAtUtcTicks, DateTimeKind.Utc);

        public static SaveData CreateNew() => From(
            new PlayerRuntimeState(), new WorldLocation(), new WorldProgress(),
            new Inventory(), new PotionSettings(), new Wallet(), new PlayerUpgrades());

        /// <summary>
        /// 살아 있는 상태를 그대로 감싼다. 복사하지 않고 참조를 들기 때문에
        /// 저장 직전 값이 그대로 직렬화된다.
        /// </summary>
        public static SaveData From(
            PlayerRuntimeState state, WorldLocation where, WorldProgress world,
            Inventory bag, PotionSettings potionSettings, Wallet purse,
            PlayerUpgrades bought) => new SaveData
        {
            version = CurrentVersion,
            savedAtUtcTicks = DateTime.UtcNow.Ticks,
            player = state ?? new PlayerRuntimeState(),
            location = where ?? new WorldLocation(),
            progress = world ?? new WorldProgress(),
            inventory = bag ?? new Inventory(),
            potions = potionSettings ?? new PotionSettings(),
            wallet = purse ?? new Wallet(),
            upgrades = bought ?? new PlayerUpgrades()
        };

        public void StampSaveTime() => savedAtUtcTicks = DateTime.UtcNow.Ticks;
    }
}
