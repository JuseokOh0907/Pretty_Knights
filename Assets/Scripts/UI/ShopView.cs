using PrettyKnights.Core;
using PrettyKnights.Data;
using UnityEngine;

namespace PrettyKnights.UI
{
    /// <summary>
    /// 세로 하단의 상점 목록. <b>스킬 자리를 대신 차지한다</b> (결정 009 §4) —
    /// 세로에는 스킬이 없고, 골드를 쓰는 곳은 세로에만 있다.
    ///
    /// <b>카드를 씬에 미리 박아 둔다.</b> 상점 목록이 6개 안팎으로 고정이라
    /// 스킬 버튼 4개를 그렇게 둔 것과 같은 이유다 — 화면 크기가 정해져 있고
    /// 스크롤은 그 이상일 때만 의미가 있다.
    ///
    /// <c>UIRoot</c> 의 <b>세로 전용</b> 패널에 둔다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShopView : MonoBehaviour
    {
        [Header("연결 — 씬에 미리 박아 둔 카드들")]
        [SerializeField] private ShopSlotView[] slots = System.Array.Empty<ShopSlotView>();

        private Shop shop;
        private Wallet purse;

        private void OnEnable() => Bind();

        private void OnDisable() => Unbind();

        private void Update()
        {
            // GameRoot 보다 늦게 켜졌을 수도 있다. 붙을 때까지 계속 찾아본다.
            if (shop == null) Bind();
        }

        private void Bind()
        {
            if (shop == null && ServiceRegistry.TryGet(out Shop found) && found != null)
            {
                shop = found;
                shop.Purchased += OnPurchased;
                shop.Rejected += OnRejected;

                BindSlots();
            }

            if (purse == null && ServiceRegistry.TryGet(out Wallet foundPurse) && foundPurse != null)
            {
                purse = foundPurse;
                // 골드가 바뀌면 "살 수 있는가" 가 전부 바뀐다. 소비만 듣던 이유가 없다 —
                // 다른 화면(오브젝트 파괴)에서 벌어도 여기 카드들이 즉시 반응해야 한다.
                purse.Changed += OnWalletChanged;
            }
        }

        private void Unbind()
        {
            if (shop != null)
            {
                shop.Purchased -= OnPurchased;
                shop.Rejected -= OnRejected;
                shop = null;
            }

            if (purse != null)
            {
                purse.Changed -= OnWalletChanged;
                purse = null;
            }
        }

        /// <summary>목록의 항목을 카드에 순서대로 물린다. 남는 카드는 숨긴다.</summary>
        private void BindSlots()
        {
            var catalog = shop.Catalog;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;

                ShopOffer offer = i < catalog.Count ? catalog[i] : null;
                slots[i].Bind(shop, offer);
            }

            if (catalog.Count > slots.Length)
                Debug.LogWarning(
                    $"[ShopView] 상점 목록이 {catalog.Count}개인데 카드가 {slots.Length}개뿐입니다. " +
                    "일부가 화면에 뜨지 않습니다.", this);
        }

        private void OnPurchased(ShopOffer offer) => RefreshAll();

        private void OnRejected(ShopOffer offer, string reason)
        {
            // 못 산 이유는 지금은 콘솔로만 남긴다. 전용 토스트가 필요하면
            // PotionWarningLabel 과 같은 패턴으로 나중에 붙인다.
            Debug.Log($"[ShopView] '{offer?.DisplayName}' 구매 실패 — {reason}");
        }

        private void OnWalletChanged(Wallet wallet) => RefreshAll();

        private void RefreshAll()
        {
            foreach (ShopSlotView slot in slots)
                if (slot != null && slot.Offer != null) slot.Refresh();
        }
    }
}
