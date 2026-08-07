using System;
using UnityEngine;

namespace PrettyKnights.Data
{
    /// <summary>
    /// 파괴 시 확률 드랍.
    ///
    /// <b>아이템 시스템이 아직 없다.</b> 인벤토리도 아이템 SO 도 줍기도 없으므로
    /// 1단계는 경험치만 준다. 아이템이 생기면 <see cref="Entry"/> 에 참조를 더하고
    /// <see cref="Roll"/> 이 그것도 돌려주게 하면 된다 — 이 표를 쓰는 쪽은 안 바뀐다.
    ///
    /// 지금 만들어 두는 이유는 <c>PropDefinition</c> 18종을 두 번 손대지 않기 위해서다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DropTable",
        menuName = "Pretty Knights/Drop Table")]
    public sealed class DropTable : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            [Range(0f, 1f), Tooltip("이 항목이 나올 확률. 항목마다 따로 굴린다")]
            public float chance;

            [Min(0), Tooltip("나왔을 때 주는 경험치")]
            public int minExp;

            [Min(0)] public int maxExp;

            [Tooltip("사람이 읽기 위한 이름. 나중에 아이템 참조로 대체된다")]
            public string label;
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();

        /// <summary>
        /// 굴린다. <b>항목마다 독립으로 굴리므로 여러 개가 함께 나올 수 있다.</b>
        /// 하나만 나오게 하려면 가중 추첨으로 바꿔야 하는데,
        /// "희귀한 것과 흔한 것이 같이 나온다" 가 파밍에서는 더 자연스럽다.
        /// </summary>
        public int Roll()
        {
            int exp = 0;

            foreach (Entry entry in entries)
            {
                if (entry.chance <= 0f) continue;
                if (UnityEngine.Random.value > entry.chance) continue;

                exp += UnityEngine.Random.Range(entry.minExp, Mathf.Max(entry.minExp, entry.maxExp) + 1);
            }

            return exp;
        }

        public int EntryCount => entries?.Length ?? 0;
    }
}
