using System;
using System.Collections.Generic;
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

        /// <summary>
        /// 굴려서 나온 것 하나. <b>지금은 이름과 경험치뿐이다</b> —
        /// 아이템 시스템이 붙으면 여기에 아이템 참조가 더해지고,
        /// 이걸 받는 쪽은 바뀌지 않는다.
        /// </summary>
        public readonly struct Drop
        {
            public readonly string Label;
            public readonly int Exp;

            public Drop(string label, int exp)
            {
                Label = label;
                Exp = exp;
            }

            public override string ToString() =>
                string.IsNullOrEmpty(Label) ? $"경험치 {Exp}" : $"{Label} {Exp}";
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();

        /// <summary>
        /// 굴린다. <b>항목마다 독립으로 굴리므로 여러 개가 함께 나올 수 있다.</b>
        /// 하나만 나오게 하려면 가중 추첨으로 바꿔야 하는데,
        /// "희귀한 것과 흔한 것이 같이 나온다" 가 파밍에서는 더 자연스럽다.
        /// </summary>
        public int Roll() => Roll(null);

        /// <summary>
        /// 굴리면서 <b>무엇이 나왔는지</b>도 담는다.
        /// 합계만 돌려주면 화면에서 "표가 도는지" 를 확인할 방법이 없다 —
        /// 경험치가 들쭉날쭉 오르는 것과 구분되지 않는다.
        ///
        /// <paramref name="hits"/> 는 <b>비우지 않는다.</b> 부르는 쪽이 재사용 목록을
        /// 넘길 수 있게 하려는 것이다. 필요하면 넘기기 전에 비운다.
        /// </summary>
        public int Roll(List<Drop> hits)
        {
            int exp = 0;

            foreach (Entry entry in entries)
            {
                if (entry.chance <= 0f) continue;
                if (UnityEngine.Random.value > entry.chance) continue;

                int rolled = UnityEngine.Random.Range(entry.minExp, Mathf.Max(entry.minExp, entry.maxExp) + 1);

                exp += rolled;
                hits?.Add(new Drop(entry.label, rolled));
            }

            return exp;
        }

        public int EntryCount => entries?.Length ?? 0;
    }
}
