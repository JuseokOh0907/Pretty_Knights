using PrettyKnights.Data;
using UnityEngine;

namespace PrettyKnights.World
{
    /// <summary>
    /// 토템. 부서지면 그 층의 무언가가 바뀐다.
    ///
    /// <code>
    /// 서브 토템  →  층 목표 인구가 자기 지분만큼 줄어든다
    /// 메인 토템  →  같은 자리의 포탈이 열리고 목표 인구가 0이 된다
    /// </code>
    ///
    /// <b>이미 나와 있는 몬스터는 사라지지 않는다.</b> 줄어드는 것은 목표치뿐이고
    /// 실제 수는 처치되면서 자연히 수렴한다. 부수는 순간 증발하면 그 행동이 공짜가 되어
    /// "지금 부술까, 정리하고 부술까" 라는 판단이 사라진다
    /// (docs/design/map-objects.md §1).
    ///
    /// 결정 005 §4 의 "보주 파괴 시 리스폰도 함께 멈춘다" 가 예고한 구조가 이것이다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Destructible))]
    public sealed class SpawnTotem : MonoBehaviour
    {
        [Header("연결 (비우면 자동으로 찾는다)")]
        [SerializeField] private Destructible destructible;

        [SerializeField, Tooltip("이 층의 인구 관리. 비우면 부모에서 찾는다")]
        private FloorPopulation population;

        [Header("메인 토템 전용")]
        [SerializeField, Tooltip(
            "부서지면 켤 포탈. 배치 도구가 토템과 같은 자리에 꺼진 채로 만들어 둔다")]
        private GameObject portalToOpen;

        private void Awake()
        {
            if (destructible == null) destructible = GetComponent<Destructible>();
            if (population == null) population = GetComponentInParent<FloorPopulation>();
        }

        /// <summary>배치가 뽑힐 때 만들어진 포탈을 물린다. 런타임 생성 경로에서 쓴다.</summary>
        public void SetPortal(GameObject portal) => portalToOpen = portal;

        private void OnEnable()
        {
            if (destructible != null) destructible.Broken += OnBroken;
        }

        private void OnDisable()
        {
            if (destructible != null) destructible.Broken -= OnBroken;
        }

        private void Start()
        {
            // 검사를 Start 로 미룬다. 런타임 생성이면 Awake 시점엔 정의가 아직 비어 있다.
            PropDefinition definition = destructible != null ? destructible.Definition : null;

            if (definition == null || !definition.IsTotem)
            {
                Debug.LogError(
                    $"[SpawnTotem] '{name}' 의 PropDefinition 이 토템이 아닙니다. " +
                    "Role 을 SubTotem 또는 MainTotem 으로 두세요.");
                return;
            }

            if (definition.Role == PropRole.MainTotem && portalToOpen == null)
                Debug.LogError(
                    $"[SpawnTotem] 메인 토템 '{name}' 에 열 포탈이 연결되지 않았습니다. " +
                    "부숴도 다음 층으로 갈 길이 생기지 않습니다.");

            // 층 인구는 살아 있는 토템의 합이다. 시작 시 자기 지분을 얹는다.
            //
            // ⚠ 이미 부서진 채로 복원된 토템은 얹지 않는다. 세이브에서 되돌릴 때
            // FloorProps 가 OnEnable 에서 Break 를 부르는데 그건 이 Start 보다 먼저다.
            // 조건 없이 얹으면 부숴 둔 토템이 지분을 되살려, 층을 나갔다 오면
            // 메인 토템이 부서졌는데도 몬스터가 다시 차오른다.
            if (population != null && destructible != null && !destructible.IsBroken)
                population.AddShare(definition.PopulationShare);
        }

        private void OnBroken(Destructible source)
        {
            PropDefinition definition = source != null ? source.Definition : null;
            if (definition == null) return;

            if (population != null)
            {
                // 메인은 할당의 밑동이라 부서지면 그 위의 지분도 함께 풀린다.
                if (definition.Role == PropRole.MainTotem) population.ClearTarget();
                else population.AddShare(-definition.PopulationShare);
            }

            if (definition.Role != PropRole.MainTotem) return;

            if (portalToOpen == null)
            {
                Debug.LogError($"[SpawnTotem] '{name}' 을 부쉈지만 열 포탈이 없습니다.");
                return;
            }

            portalToOpen.SetActive(true);
        }
    }
}
