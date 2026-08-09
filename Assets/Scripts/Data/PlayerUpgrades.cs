using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrettyKnights.Data
{
    /// <summary>
    /// 상점에서 산 강화 단계. <b>세이브에 남는 것은 이것뿐이다</b> —
    /// 스탯 보너스는 여기서 매번 다시 계산한다.
    ///
    /// 계산 결과를 저장하지 않는 이유는 <b>공식을 고치는 순간 어긋나기</b> 때문이다.
    /// 단계만 남겨 두면 강화 하나가 주는 양을 나중에 바꿔도 세이브가 그대로 따라온다.
    ///
    /// <b>번호가 아니라 <see cref="ShopOffer.OfferId"/> 문자열로 센다.</b>
    /// 배열 인덱스로 두면 상점 목록의 순서를 바꾸는 순간
    /// 산 적 없는 강화가 올라가 있게 된다 — 아이템의 <c>itemId</c> 와 같은 이유다.
    /// </summary>
    [Serializable]
    public sealed class PlayerUpgrades
    {
        [Serializable]
        private struct Entry
        {
            public string offerId;
            public int level;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        /// <summary>단계가 바뀔 때마다. 스탯을 다시 합산하는 쪽이 듣는다.</summary>
        public event Action<PlayerUpgrades> Changed;

        public int LevelOf(string offerId)
        {
            if (string.IsNullOrEmpty(offerId)) return 0;

            foreach (Entry e in entries)
                if (e.offerId == offerId) return e.level;

            return 0;
        }

        public int LevelOf(ShopOffer offer) => offer == null ? 0 : LevelOf(offer.OfferId);

        /// <summary>한 단계 올린다. 올라간 뒤의 단계를 돌려준다.</summary>
        public int Increase(string offerId)
        {
            if (string.IsNullOrEmpty(offerId)) return 0;

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].offerId != offerId) continue;

                Entry bumped = entries[i];
                bumped.level++;
                entries[i] = bumped;

                Changed?.Invoke(this);
                return bumped.level;
            }

            entries.Add(new Entry { offerId = offerId, level = 1 });
            Changed?.Invoke(this);
            return 1;
        }

        /// <summary>
        /// 지금 강화가 더해 주는 스탯 전부.
        /// <paramref name="catalog"/> 는 화면에 없는 항목까지 포함해야 한다 —
        /// 목록에서 뺀 강화도 이미 산 사람에게는 계속 적용되어야 한다.
        /// </summary>
        public StatBlock TotalBonus(IReadOnlyList<ShopOffer> catalog)
        {
            StatBlock total = StatBlock.Zero;
            if (catalog == null) return total;

            foreach (ShopOffer offer in catalog)
            {
                if (offer == null) continue;

                int level = LevelOf(offer.OfferId);
                if (level > 0) total += offer.BonusAt(level);
            }

            return total;
        }

        public int Count => entries.Count;

        public override string ToString() => $"강화 {entries.Count}종";
    }
}
