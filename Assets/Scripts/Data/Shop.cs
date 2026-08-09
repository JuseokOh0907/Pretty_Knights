using System;
using System.Collections.Generic;
using PrettyKnights.Core;
using UnityEngine;

namespace PrettyKnights.Data
{
    /// <summary>
    /// 상점의 살아 있는 부분. <b>화면이 이걸 보고 그리고, 눌리면 이걸 부른다.</b>
    ///
    /// <see cref="ShopOffer"/> 가 목록이고 <see cref="PlayerUpgrades"/> 가 단계라면
    /// 여기는 <b>둘을 지갑과 이어 붙이는 곳</b>이다. UI 가 지갑·강화·목록 셋을 각각
    /// 뒤지지 않도록 창구를 하나로 둔다.
    ///
    /// <c>MonoBehaviour</c> 가 아니다 — 씬에 종속되지 않아야 모드가 바뀌어도 그대로 살아 있다.
    /// <c>GameRoot</c> 가 만들고 <see cref="ServiceRegistry"/> 에 등록한다.
    /// </summary>
    public sealed class Shop
    {
        private readonly List<ShopOffer> catalog = new List<ShopOffer>();

        private readonly Wallet purse;
        private readonly PlayerUpgrades upgrades;
        private readonly PlayerRuntimeState player;
        private readonly Inventory bag;

        /// <summary>무언가 팔렸다. UI 가 다시 그린다.</summary>
        public event Action<ShopOffer> Purchased;

        /// <summary>살 수 없었다. 이유를 화면에 띄우는 쪽이 듣는다.</summary>
        public event Action<ShopOffer, string> Rejected;

        public IReadOnlyList<ShopOffer> Catalog => catalog;

        public Shop(
            IEnumerable<ShopOffer> offers, Wallet wallet,
            PlayerUpgrades playerUpgrades, PlayerRuntimeState state, Inventory inventory)
        {
            purse = wallet;
            upgrades = playerUpgrades;
            player = state;
            bag = inventory;

            if (offers == null) return;

            foreach (ShopOffer offer in offers)
                if (offer != null) catalog.Add(offer);
        }

        public int LevelOf(ShopOffer offer) => upgrades == null ? 0 : upgrades.LevelOf(offer);

        /// <summary>지금 이걸 사는 데 드는 값.</summary>
        public long CostOf(ShopOffer offer) => offer == null ? 0 : offer.CostAt(LevelOf(offer));

        /// <summary>더 살 수 있는가 (상한에 걸리지 않았는가). 값은 보지 않는다.</summary>
        public bool HasRoom(ShopOffer offer) => offer != null && offer.HasRoom(LevelOf(offer));

        /// <summary>지금 살 수 있는가. 상한과 잔액을 함께 본다.</summary>
        public bool CanBuy(ShopOffer offer) =>
            HasRoom(offer) && purse != null && purse.CanAfford(CostOf(offer));

        /// <summary>
        /// 산다. <b>골드를 먼저 빼고 물건을 준다.</b>
        /// 순서가 반대면 주는 데 실패했을 때 값만 치른 상태가 남는다.
        ///
        /// 가방이 가득 차 소모품을 못 받는 경우가 그렇다 — 그래서 소모품은
        /// <b>넣을 수 있는지 먼저 물어보고</b> 값을 뺀다.
        /// </summary>
        public bool TryBuy(ShopOffer offer)
        {
            if (offer == null) return false;

            if (!HasRoom(offer))
            {
                Rejected?.Invoke(offer, "더 올릴 수 없습니다");
                return false;
            }

            long cost = CostOf(offer);

            if (purse == null || !purse.CanAfford(cost))
            {
                Rejected?.Invoke(offer, "골드가 모자랍니다");
                return false;
            }

            // 소모품은 자리부터 확인한다. 값을 치른 뒤에 못 받으면 그냥 잃는 것이 된다.
            if (offer.Kind == ShopOfferKind.Item && !CanReceiveItem(offer))
            {
                Rejected?.Invoke(offer, "가방이 가득 찼습니다");
                return false;
            }

            if (!purse.TrySpend(cost)) return false;

            Deliver(offer);

            Purchased?.Invoke(offer);
            return true;
        }

        private bool CanReceiveItem(ShopOffer offer) =>
            offer.Item != null && bag != null && bag.CanAdd(offer.Item, offer.ItemCount);

        private void Deliver(ShopOffer offer)
        {
            if (offer.Kind == ShopOfferKind.Item)
            {
                bag?.Add(offer.Item, offer.ItemCount);
                return;
            }

            upgrades?.Increase(offer.OfferId);
            ApplyUpgrades();
        }

        /// <summary>
        /// 강화 전부를 다시 합산해 플레이어에게 밀어 넣는다.
        /// <b>불러오기 직후에도 한 번 불러야 한다</b> — 안 그러면 산 강화가
        /// 세이브에는 있는데 스탯에는 없다.
        /// </summary>
        public void ApplyUpgrades()
        {
            if (player == null || upgrades == null) return;

            player.SetBonusStats(upgrades.TotalBonus(catalog));
        }
    }
}
