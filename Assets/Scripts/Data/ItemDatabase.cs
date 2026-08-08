using System.Collections.Generic;
using UnityEngine;

namespace PrettyKnights.Data
{
    /// <summary>
    /// itemId 를 <see cref="ItemDefinition"/> 으로 되돌리는 표.
    ///
    /// <b>세이브가 문자열만 들고 있기 때문에 필요하다.</b> JSON 에는 에셋 참조를 넣을 수 없어
    /// 인벤토리는 <c>itemId</c> 와 개수만 저장하고, 불러올 때 여기서 다시 푼다.
    ///
    /// <b>목록에서 빠진 아이템은 세이브에서 사라진다.</b> 그래서 새 아이템을 만들면
    /// 반드시 여기에 넣어야 하고, 도구가 그것을 대신한다
    /// (<c>Pretty Knights > Data > 5. 아이템 목록 갱신</c>).
    /// </summary>
    [CreateAssetMenu(
        fileName = "ItemDatabase",
        menuName = "Pretty Knights/Item Database")]
    public sealed class ItemDatabase : ScriptableObject
    {
        [SerializeField, Tooltip("프로젝트의 모든 ItemDefinition. 도구가 채운다")]
        private ItemDefinition[] items = System.Array.Empty<ItemDefinition>();

        private Dictionary<string, ItemDefinition> byId;

        public IReadOnlyList<ItemDefinition> All => items;
        public int Count => items != null ? items.Length : 0;

        /// <summary>
        /// itemId 로 찾는다. 없으면 <c>null</c> — 목록에서 빠졌거나 id 가 바뀐 것이다.
        /// </summary>
        public ItemDefinition Find(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;

            Build();
            return byId.TryGetValue(itemId, out ItemDefinition found) ? found : null;
        }

        private void Build()
        {
            if (byId != null) return;

            byId = new Dictionary<string, ItemDefinition>();

            foreach (ItemDefinition item in items)
            {
                if (item == null || string.IsNullOrEmpty(item.ItemId)) continue;

                if (byId.ContainsKey(item.ItemId))
                {
                    // 같은 id 가 둘이면 세이브가 어느 쪽을 가리키는지 알 수 없다.
                    Debug.LogError(
                        $"[ItemDatabase] itemId '{item.ItemId}' 가 둘 이상입니다. " +
                        "세이브가 어느 아이템인지 가릴 수 없으므로 하나로 고쳐야 합니다.", item);
                    continue;
                }

                byId.Add(item.ItemId, item);
            }
        }

        /// <summary>에디터 도구가 목록을 갈아 끼울 때 쓴다.</summary>
        public void SetAll(ItemDefinition[] source)
        {
            items = source ?? System.Array.Empty<ItemDefinition>();
            byId = null;
        }
    }
}
