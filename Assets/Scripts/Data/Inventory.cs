using System;
using UnityEngine;

namespace PrettyKnights.Data
{
    /// <summary>
    /// 칸으로 나뉜 가방. <b>마인크래프트 방식이다</b> (2026-08-09 확정) —
    /// 칸 수가 고정이고, 같은 아이템은 <see cref="ItemDefinition.MaxStack"/> 까지 한 칸에 쌓인다.
    ///
    /// 무게 방식이 아니라 칸 방식인 이유는 <b>화면에 그대로 보이기 때문</b>이다.
    /// 격자를 보면 얼마나 남았는지 바로 알 수 있고, 숫자를 읽을 필요가 없다.
    ///
    /// <b>저장되는 것은 <c>itemId</c> 와 개수뿐이다.</b> 에셋 참조는 JSON 에 넣을 수 없으므로
    /// 불러올 때 <see cref="ItemDatabase"/> 가 문자열을 다시 에셋으로 푼다.
    /// 그래서 <b>이 배열이 유일한 진실</b>이고 따로 동기화할 사본을 두지 않는다 —
    /// 사본을 두면 저장 직전에 맞춰 주는 단계가 생기고, 그 단계를 빠뜨리면 조용히 어긋난다.
    /// </summary>
    [Serializable]
    public sealed class Inventory
    {
        /// <summary>한 칸. 비어 있으면 <see cref="itemId"/> 가 빈 문자열이다.</summary>
        [Serializable]
        public struct Slot
        {
            public string itemId;
            public int count;

            public bool IsEmpty => string.IsNullOrEmpty(itemId) || count <= 0;
        }

        /// <summary>기본 칸 수. 6 × 5 격자다.</summary>
        public const int DefaultSlotCount = 30;

        [SerializeField] private Slot[] slots = new Slot[DefaultSlotCount];

        [NonSerialized] private ItemDatabase database;

        /// <summary>칸이 하나라도 바뀌면 발생한다. UI 가 이걸 듣고 다시 그린다.</summary>
        public event Action Changed;

        public int SlotCount => slots != null ? slots.Length : 0;
        public bool IsBound => database != null;

        /// <summary>
        /// 표를 물린다. 세이브를 불러온 직후 한 번 부른다.
        /// 칸 수가 바뀌었으면 여기서 맞춘다 — 늘리는 것은 안전하고, 줄이면 뒤가 잘린다.
        /// </summary>
        public void Bind(ItemDatabase source, int slotCount = DefaultSlotCount)
        {
            database = source;

            if (slots == null) slots = new Slot[slotCount];
            else if (slots.Length != slotCount) Array.Resize(ref slots, slotCount);

            if (database == null)
                Debug.LogError(
                    "[Inventory] ItemDatabase 가 비어 있습니다. " +
                    "GameRoot 에 연결하지 않으면 저장된 아이템을 하나도 풀지 못합니다.");
        }

        // ── 읽기 ──────────────────────────────────────────────────────────

        public ItemDefinition ItemAt(int slot) =>
            IsValid(slot) && !slots[slot].IsEmpty && database != null
                ? database.Find(slots[slot].itemId)
                : null;

        public int CountAt(int slot) => IsValid(slot) ? Mathf.Max(0, slots[slot].count) : 0;

        public bool IsEmptyAt(int slot) => ItemAt(slot) == null;

        /// <summary>그 아이템을 전부 몇 개 갖고 있는지. 칸이 나뉘어 있어도 합친다.</summary>
        public int TotalOf(ItemDefinition item)
        {
            if (item == null) return 0;

            int total = 0;
            for (int i = 0; i < slots.Length; i++)
                if (slots[i].itemId == item.ItemId) total += slots[i].count;

            return total;
        }

        /// <summary>빈 칸 수. 주울 수 있는지 미리 보여줄 때 쓴다.</summary>
        public int FreeSlots
        {
            get
            {
                int free = 0;
                for (int i = 0; i < slots.Length; i++)
                    if (slots[i].IsEmpty) free++;

                return free;
            }
        }

        // ── 넣기 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 넣는다. <b>못 넣고 남은 개수를 돌려준다</b> — 0 이면 전부 들어갔다.
        ///
        /// <b>쌓여 있는 칸부터 채우고 그다음 빈 칸을 쓴다.</b> 반대로 하면
        /// 같은 아이템이 여러 칸에 흩어져 가방이 금방 찬다.
        /// </summary>
        public int Add(ItemDefinition item, int count)
        {
            if (item == null || count <= 0) return count;

            int left = count;
            int max = item.MaxStack;

            for (int i = 0; i < slots.Length && left > 0; i++)
            {
                if (slots[i].itemId != item.ItemId) continue;
                if (slots[i].count >= max) continue;

                int room = max - slots[i].count;
                int put = Mathf.Min(room, left);

                slots[i].count += put;
                left -= put;
            }

            for (int i = 0; i < slots.Length && left > 0; i++)
            {
                if (!slots[i].IsEmpty) continue;

                int put = Mathf.Min(max, left);

                slots[i].itemId = item.ItemId;
                slots[i].count = put;
                left -= put;
            }

            if (left != count) Changed?.Invoke();

            return left;
        }

        /// <summary>
        /// 전부 들어갈 자리가 있는가. <b>넣지는 않는다.</b>
        ///
        /// 상점처럼 <b>값을 먼저 치르는 곳</b>에 필요하다 —
        /// 넣어 보고 남으면 되돌리는 방식은 되돌리는 코드가 또 틀릴 수 있고,
        /// 그 사이에 <c>Changed</c> 가 두 번 나간다.
        /// </summary>
        public bool CanAdd(ItemDefinition item, int count)
        {
            if (item == null || count <= 0) return false;

            int room = 0;
            int max = item.MaxStack;

            foreach (Slot slot in slots)
            {
                if (slot.IsEmpty) room += max;
                else if (slot.itemId == item.ItemId) room += Mathf.Max(0, max - slot.count);

                if (room >= count) return true;
            }

            return room >= count;
        }

        // ── 빼기 ──────────────────────────────────────────────────────────

        /// <summary>그 칸에서 <paramref name="count"/> 개 뺀다. 실제로 뺀 개수를 돌려준다.</summary>
        public int RemoveAt(int slot, int count)
        {
            if (!IsValid(slot) || count <= 0 || slots[slot].IsEmpty) return 0;

            int taken = Mathf.Min(count, slots[slot].count);
            slots[slot].count -= taken;

            if (slots[slot].count <= 0) slots[slot] = default;

            Changed?.Invoke();
            return taken;
        }

        /// <summary>그 칸을 통째로 버린다. 열쇠는 버릴 수 없다.</summary>
        public bool DiscardAt(int slot)
        {
            ItemDefinition item = ItemAt(slot);
            if (item == null || !item.Discardable) return false;

            slots[slot] = default;
            Changed?.Invoke();
            return true;
        }

        // ── 쓰기 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 그 칸의 아이템을 쓴다. 효과가 실제로 걸렸을 때만 개수가 준다 —
        /// HP 가 가득인데 물약이 사라지면 억울하다.
        /// </summary>
        public bool Use(int slot, PlayerRuntimeState player)
        {
            ItemDefinition item = ItemAt(slot);
            if (item == null || !item.Usable) return false;

            if (!item.Use(player)) return false;

            if (item.ConsumeOnUse) RemoveAt(slot, 1);
            else Changed?.Invoke();

            return true;
        }

        // ── 그 밖 ─────────────────────────────────────────────────────────

        /// <summary>두 칸을 바꾼다. 격자에서 끌어 옮길 때 쓴다.</summary>
        public void Swap(int a, int b)
        {
            if (!IsValid(a) || !IsValid(b) || a == b) return;

            (slots[a], slots[b]) = (slots[b], slots[a]);
            Changed?.Invoke();
        }

        public void Clear()
        {
            for (int i = 0; i < slots.Length; i++) slots[i] = default;
            Changed?.Invoke();
        }

        private bool IsValid(int slot) => slots != null && slot >= 0 && slot < slots.Length;

        public override string ToString() => $"가방 {SlotCount - FreeSlots}/{SlotCount}칸 사용";
    }
}
