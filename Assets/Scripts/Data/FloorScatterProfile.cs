using System;
using UnityEngine;

namespace PrettyKnights.Data
{
    /// <summary>
    /// 한 층에 무엇을 몇 개 뿌릴지. <see cref="AreaDefinition"/> 이 참조한다.
    ///
    /// <b>가중치가 아니라 정확한 개수를 쓴다.</b> 통행 가능 면적이 층마다 크게 다르고
    /// (Goblin 2F 20,035칸 vs Orc 1F 1,950칸) 오브젝트가 2 × 2칸을 차지하므로,
    /// 비율로 두면 좁은 층이 순식간에 막힌다.
    ///
    /// 배치는 <b>에디터 베이크</b>다. 런타임 랜덤이 아니다 —
    /// 탈출 스킬로 나갔다 돌아왔을 때 지형이 바뀌면 길을 처음부터 다시 찾아야 한다
    /// (docs/design/map-objects.md §4).
    /// </summary>
    [CreateAssetMenu(
        fileName = "FloorScatterProfile",
        menuName = "Pretty Knights/Floor Scatter Profile")]
    public sealed class FloorScatterProfile : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public PropDefinition definition;

            [Min(0), Tooltip("정확히 이 개수를 놓는다. 자리가 없으면 그만큼만")]
            public int count;
        }

        [Header("무엇을 몇 개")]
        [SerializeField] private Entry[] entries = Array.Empty<Entry>();

        [Header("공용 프리팹")]
        [SerializeField, Tooltip(
            "모든 오브젝트가 이 프리팹 하나를 쓴다. 스프라이트·콜라이더 크기·Visual 오프셋은 " +
            "PropDefinition 이 결정하므로 배리언트를 18개 만들지 않는다")]
        private GameObject propPrefab;

        [Header("간격")]
        [SerializeField, Min(0f), Tooltip("오브젝트끼리 이만큼은 떨어뜨린다 (월드 유닛)")]
        private float minSpacing = 3f;

        [SerializeField, Min(0f), Tooltip("벽에서 이만큼 띄운다. 0이면 벽에 붙을 수 있다")]
        private float wallClearance = 1f;

        [Header("보호")]
        [SerializeField, Min(0f), Tooltip(
            "토템·포탈·도착 지점 주변 이 반경에는 아무것도 놓지 않는다. " +
            "출입구가 막히면 그 층을 클리어할 수 없다")]
        private float protectedRadius = 4f;

        [Header("메인 토템이 열 포탈")]
        [SerializeField, Tooltip(
            "메인 토템과 같은 자리에 꺼진 채로 만들어 둔다. " +
            "목적지는 AreaDefinition 의 Next Area 가 채운다")]
        private GameObject portalPrefab;

        [Header("난수")]
        [SerializeField, Tooltip("같은 시드는 같은 배치를 만든다. 마음에 드는 배치를 재현할 때 쓴다")]
        private int seed = 12345;

        public Entry[] Entries => entries;
        public GameObject PropPrefab => propPrefab;
        public float MinSpacing => minSpacing;
        public float WallClearance => wallClearance;
        public float ProtectedRadius => protectedRadius;
        public GameObject PortalPrefab => portalPrefab;
        public int Seed => seed;

        /// <summary>이 프로필이 놓을 총 개수. 밀도를 눈으로 가늠할 때 쓴다.</summary>
        public int TotalCount
        {
            get
            {
                int total = 0;
                foreach (Entry entry in entries) total += Mathf.Max(0, entry.count);
                return total;
            }
        }
    }
}
