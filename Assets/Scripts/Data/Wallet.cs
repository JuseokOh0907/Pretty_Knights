using System;
using UnityEngine;

namespace PrettyKnights.Data
{
    /// <summary>
    /// 재화 지갑. <b>지금은 골드 하나뿐이다</b> (2026-08-09 확정).
    ///
    /// 종류를 하나로 시작하되 <b>필드가 아니라 이 클래스를 창구로 둔다.</b>
    /// 보석·열쇠가 늘어날 때 고칠 자리가 여기 하나여야 하기 때문이다 —
    /// <c>PlayerRuntimeState</c> 에 <c>gold</c> 를 얹었다면 재화가 둘이 되는 순간
    /// 상태·세이브·UI 세 곳을 함께 고쳐야 한다.
    ///
    /// <b>가방(<see cref="Inventory"/>)과 가르는 기준은 "칸을 차지하는가" 다.</b>
    /// 골드는 칸을 차지하지 않고 상한도 없으며 정렬·버리기 대상이 아니다.
    /// 아이템으로 만들면 30칸짜리 격자에 절대 안 없어지는 칸이 하나 생긴다.
    ///
    /// <see cref="Inventory"/> 와 같이 <c>SaveData</c> 안에 살고
    /// <c>GameRoot</c> 가 <c>ServiceRegistry</c> 에 등록한다.
    /// </summary>
    [Serializable]
    public sealed class Wallet
    {
        [SerializeField] private long gold;

        /// <summary>골드가 바뀔 때마다. UI 가 이걸 듣고 다시 그린다.</summary>
        public event Action<Wallet> Changed;

        public long Gold => gold;

        /// <summary>
        /// 번다. 음수는 무시한다 — 빼는 것은 <see cref="TrySpend"/> 를 거쳐야 한다.
        /// 여기로 음수를 통과시키면 잔액 검사 없이 마이너스가 될 수 있다.
        /// </summary>
        public void AddGold(long amount)
        {
            if (amount <= 0) return;

            // 파밍이 아주 길어져도 넘치지 않게 한 번 막는다.
            gold = amount > long.MaxValue - gold ? long.MaxValue : gold + amount;

            Changed?.Invoke(this);
        }

        /// <summary>
        /// 쓴다. <b>모자라면 아무것도 하지 않고 <c>false</c> 를 돌려준다.</b>
        /// 부르는 쪽이 잔액을 미리 재고 또 여기서 재는 것을 막으려는 것이다 —
        /// 두 번 재면 그 사이에 값이 바뀌었을 때 음수가 된다.
        /// </summary>
        public bool TrySpend(long amount)
        {
            if (amount <= 0) return true;
            if (gold < amount) return false;

            gold -= amount;
            Changed?.Invoke(this);
            return true;
        }

        public bool CanAfford(long amount) => amount <= 0 || gold >= amount;

        /// <summary>
        /// 세이브에서 막 올라온 값을 다듬는다.
        /// 파일이 손상돼 음수가 들어와도 게임이 그 상태로 굴러가지 않게 한다.
        /// </summary>
        public void Sanitize()
        {
            if (gold >= 0) return;

            gold = 0;
            Changed?.Invoke(this);
        }

        public override string ToString() => $"골드 {gold:N0}";
    }
}
