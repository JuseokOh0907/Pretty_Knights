using UnityEngine;

namespace PrettyKnights.Data
{
    /// <summary>
    /// 구역 하나의 정적 정의. 시작 신전·숲·던전 각 층이 전부 이것 하나씩을 갖는다.
    ///
    /// <b>여기에 씬 오브젝트를 넣을 수 없다.</b>
    /// ScriptableObject 는 씬에 있는 게임오브젝트나 Transform 을 직렬화하지 못한다.
    /// 그래서 이 에셋은 <see cref="AreaId"/> 만 선언하고,
    /// 실제 층 오브젝트·바닥 타일맵·도착 지점은 씬 쪽 <c>AreaAnchor</c> 가 들고 있다.
    /// 둘을 잇는 것은 오직 이 번호다.
    ///
    /// <b>번호를 쓰는 이유</b> — 테마 이름은 구역의 정체가 아니다.
    /// Goblin 2F 에만 고블린이 두 종류 배치되듯 "테마 이름 = 그 층의 내용" 이 성립하지 않는다.
    /// 이름으로 된 ID 는 배치가 바뀔 때마다 거짓말이 되므로 번호로 고정한다.
    ///
    /// <code>
    /// areaId = 테마번호 × 100 + 층
    ///
    ///     0        미지정 (세이브에 구역 기록이 없는 옛 파일)
    ///     1 ~  99  거점        1 시작 신전 · 2 숲 · 3 던전 입구
    ///   101 ~ 199  테마 1      101 = 1F · 102 = 2F · 103 = 3F
    ///   201 ~ 299  테마 2
    ///   301 ~ 399  테마 3
    /// </code>
    ///
    /// 환생마다 층이 추가되어도 104, 105 로 이어지고 테마가 늘어도 401 로 이어진다
    /// (docs/decisions/005-dungeon-and-monster-design.md §6-3 "층 수를 상수로 두지 않는다").
    ///
    /// 세이브에 이 번호가 그대로 들어가므로 <b>한 번 정한 뒤에는 바꾸지 않는다.</b>
    /// </summary>
    [CreateAssetMenu(
        fileName = "AreaDefinition",
        menuName = "Pretty Knights/Area Definition")]
    public sealed class AreaDefinition : ScriptableObject
    {
        /// <summary>구역이 지정되지 않은 상태. 옛 세이브가 이 값을 갖는다.</summary>
        public const int NoArea = 0;

        [Header("정체 — areaId 는 세이브에 기록된다. 정한 뒤 바꾸지 말 것")]
        [SerializeField, Min(1), Tooltip("테마번호 × 100 + 층. 예: Goblin 2F = 102")]
        private int areaId = 101;

        [SerializeField] private string displayName = "1층";

        [Header("표시용 — 로직에 쓰지 않는다")]
        [SerializeField, Tooltip("Maps/ 폴더 이름과 맞춘다. 어디까지나 사람이 읽기 위한 것")]
        private string theme = "Goblin";

        [SerializeField, Tooltip("보스가 있는 층인지. 보상 포탈과 클리어 기록이 여기에 걸린다")]
        private bool isBossFloor;

        [Header("다음 층 — 메인 토템을 부수면 열릴 포탈의 목적지")]
        [SerializeField, Tooltip(
            "메인 토템이 어디에 랜덤 배치되든 그 자리에 열리는 포탈은 여기로 간다. " +
            "SO 는 씬 오브젝트를 참조할 수 없지만 다른 SO 는 참조할 수 있다")]
        private AreaDefinition nextArea;

        [SerializeField, Tooltip("다음 층의 도착 지점 arrivalId. 비우면 그 구역의 대체 지점")]
        private string nextArrivalId = string.Empty;

        [Header("배치 — 오브젝트 자동 배치 도구가 읽는다")]
        [SerializeField, Tooltip("이 층에 무엇을 몇 개 뿌릴지. 비우면 도구가 이 층을 건너뛴다")]
        private FloorScatterProfile scatterProfile;

        [Header("탈출 — 던전 탈출 스킬이 데려다 놓을 곳")]
        [SerializeField, Tooltip("이 구역에서 탈출하면 도착할 구역. 보통 그 던전의 입구")]
        private AreaDefinition escapeTo;

        [SerializeField, Tooltip("탈출 도착 지점의 arrivalId. 비우면 그 구역의 대체 지점")]
        private string escapeArrivalId = string.Empty;

        public int AreaId => areaId;
        public string DisplayName => displayName;
        public string Theme => theme;
        public bool IsBossFloor => isBossFloor;

        /// <summary>테마번호. 101~103 은 전부 1 을 돌려준다.</summary>
        public int ThemeNumber => areaId / 100;

        /// <summary>층 번호. 거점(1~99)은 0 이다.</summary>
        public int Floor => areaId < 100 ? 0 : areaId % 100;

        /// <summary>메인 토템을 부수면 열릴 포탈의 목적지. 없으면 그 층에 메인 토템이 없다.</summary>
        public AreaDefinition NextArea => nextArea;
        public string NextArrivalId => nextArrivalId;
        public bool HasNextArea => nextArea != null;

        /// <summary>배치 도구가 읽는 프로필. 비면 그 층은 자동 배치 대상이 아니다.</summary>
        public FloorScatterProfile ScatterProfile => scatterProfile;

        /// <summary>던전 탈출 목적지. 없으면 탈출할 수 없는 구역이다 (거점 등).</summary>
        public AreaDefinition EscapeTo => escapeTo;
        public string EscapeArrivalId => escapeArrivalId;
        public bool CanEscape => escapeTo != null;

        public override string ToString() => $"{theme} {displayName} (#{areaId})";
    }
}
