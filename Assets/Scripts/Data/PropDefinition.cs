using UnityEngine;

namespace PrettyKnights.Data
{
    /// <summary>맵 오브젝트가 하는 일. 아트가 아니라 역할로 가른다.</summary>
    public enum PropRole
    {
        /// <summary>부술 수 없다. 통행만 막는다.</summary>
        Decoration,

        /// <summary>부술 수 있다. 드랍만 준다.</summary>
        Destructible,

        /// <summary>부수면 그 층의 몬스터 목표 인구가 줄어든다.</summary>
        SubTotem,

        /// <summary>부수면 그 자리에 다음 층 포탈이 열리고 인구가 0이 된다.</summary>
        MainTotem
    }

    /// <summary>
    /// 맵 오브젝트 한 종의 정의. 18종에 하나씩 만든다.
    /// 실측과 역할은 <c>docs/design/map-objects.md</c>.
    ///
    /// <b>여기에는 수치만 둔다.</b> 콜라이더 크기와 <c>Visual</c> 오프셋은 프리팹에 굽는다 —
    /// 에디터에서 눈으로 맞춰야 하는 값이라 SO 로 빼면 오히려 불편하다.
    /// 몬스터가 <see cref="MonsterDefinition"/> 으로 수치만 갈아 끼우는 것과 같은 분리다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PropDefinition",
        menuName = "Pretty Knights/Prop Definition")]
    public sealed class PropDefinition : ScriptableObject
    {
        [Header("정체")]
        [SerializeField, Tooltip("자산 파일명과 맞춘다. 예: goblin_06_scrap_totem")]
        private string propId = "goblin_01_twisted_stump";

        [SerializeField] private string displayName = "뒤틀린 그루터기";

        [SerializeField, Tooltip("Goblin / Orc / Vampire. 배치 도구가 테마별로 고를 때 쓴다")]
        private string theme = "Goblin";

        [Header("역할")]
        [SerializeField] private PropRole role = PropRole.Destructible;

        [Header("수치")]
        [SerializeField, Min(1f), Tooltip("0 이 되면 부서진다")]
        private float maxHp = 40f;

        [SerializeField, Min(0f), Tooltip("데미지 공식에 들어갈 방어력. 오브젝트는 보통 0")]
        private float defense;

        [Header("보상")]
        [SerializeField, Min(0)] private int expReward;

        [SerializeField, Tooltip("확률 드랍. 비우면 경험치만 준다")]
        private DropTable dropTable;

        [Header("토템 전용")]
        [SerializeField, Min(0), Tooltip(
            "서브 토템이 담당하는 인구 지분. 부수면 층 목표 인구가 이만큼 줄어든다. " +
            "메인 토템은 이 값이 기본 점유량이 된다")]
        private int populationShare = 4;

        [Header("표현 — 프리팹이 하나라 여기서 결정된다")]
        [SerializeField, Tooltip("멀쩡할 때의 그림")]
        private Sprite sprite;

        [SerializeField, Tooltip("부서진 뒤 보여줄 스프라이트. 비우면 그냥 감춘다")]
        private Sprite brokenSprite;

        [SerializeField, Tooltip(
            "지면 충돌 영역. 가로는 실측 접지폭, 세로는 0.5칸이 기본. " +
            "겉보기 크기가 아니라 발이 닿는 넓이다 (CLAUDE.md §4)")]
        private Vector2 colliderSize = new Vector2(1.5f, 0.5f);

        [SerializeField, Tooltip(
            "Visual 자식의 Y 오프셋. 루트가 접지점이고 그림은 그만큼 위로 올라간다. " +
            "값은 docs/design/map-objects.md §2 의 실측표")]
        private float visualOffsetY = 0.85f;

        public string PropId => propId;
        public string DisplayName => displayName;
        public string Theme => theme;
        public PropRole Role => role;

        public float MaxHp => maxHp;
        public float Defense => defense;
        public int ExpReward => expReward;
        public DropTable Drops => dropTable;

        public int PopulationShare => populationShare;

        public Sprite Sprite => sprite;
        public Sprite BrokenSprite => brokenSprite;
        public Vector2 ColliderSize => colliderSize;
        public float VisualOffsetY => visualOffsetY;

        public bool IsDestructible => role != PropRole.Decoration;
        public bool IsTotem => role == PropRole.SubTotem || role == PropRole.MainTotem;

        public override string ToString() => $"{displayName} ({propId} · {role})";
    }
}
