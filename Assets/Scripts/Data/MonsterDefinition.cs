using UnityEngine;

namespace PrettyKnights.Data
{
    public enum MonsterTier
    {
        Normal,
        Elite,
        Boss
    }

    /// <summary>
    /// 몬스터 종별 정의. <b>읽기 전용.</b>
    /// 스프라이트가 아직 없으므로 <see cref="frames"/> 는 비어 있을 수 있고,
    /// 그 경우 프로토타입에서는 플레이스홀더로 대체한다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "MonsterDefinition",
        menuName = "Pretty Knights/Monster Definition")]
    public sealed class MonsterDefinition : ScriptableObject
    {
        [Header("정체")]
        [SerializeField] private string monsterId = "goblin_grunt";
        [SerializeField] private string displayName = "Goblin Grunt";
        [SerializeField] private MonsterTier tier = MonsterTier.Normal;

        [Header("수치")]
        [SerializeField] private StatBlock stats = new StatBlock(10f, 4f, 1f, 5f, 0f);
        [SerializeField, Min(1f)] private float hpPerVitality = 5f;

        [Header("행동 (월드 유닛)")]
        [SerializeField, Min(0f)] private float moveSpeed = 1.8f;
        [SerializeField, Min(0f)] private float detectRange = 6f;
        [SerializeField, Min(0f)] private float attackRange = 0.9f;
        [SerializeField, Min(0.05f)] private float attackCooldown = 1.2f;

        [Header("공격 예고 — 인디케이터가 뜨고 판정까지의 시간")]
        [SerializeField, Min(0f), Tooltip(
            "보스일수록 길다. 짧게 주면 그냥 맞는 공격이 되어 회피의 의미가 사라진다. " +
            "기본값 Normal 0.30 / Elite 0.45 / Boss 0.70")]
        private float telegraphDuration = 0.3f;

        [Header("공격 범위 — 판정과 인디케이터가 같은 값을 쓴다")]
        [SerializeField] private Combat.SkillShapeKind attackShape = Combat.SkillShapeKind.Forward;

        [SerializeField, Tooltip(
            "비워 두면 attackRange 로 반원을 만든다. " +
            "각도 기본값이 180 인 것은 '내 앞은 전부' 가 예고로 읽기 쉬워서다")]
        private Combat.SkillShapeParams attackShapeParams = new Combat.SkillShapeParams
        {
            range = 0f, width = 1f, angle = 180f, forwardOffset = 0f
        };

        [Header("피격 반응 — 몬스터마다 손맛을 다르게 한다")]
        [SerializeField, Min(0f), Tooltip("맞은 대상이 밀려나는 세기 (월드 유닛/초)")]
        private float knockbackForce = 4f;

        [SerializeField, Min(0f), Tooltip("맞은 대상의 입력이 잠기는 시간. 0.3초를 넘기면 렉으로 오해한다")]
        private float hitStunDuration = 0.12f;

        [Header("보상")]
        [SerializeField, Min(0), Tooltip("잡으면 언제나 주는 경험치")]
        private int expReward = 12;

        [SerializeField, Min(0), Tooltip(
            "잡으면 언제나 주는 골드. 경험치와 따로 두는 이유는 " +
            "레벨 커브와 경제를 따로 조절해야 하기 때문이다 — " +
            "경험치에 배수를 곱해 만들면 한쪽을 고칠 때 다른 쪽이 함께 움직인다")]
        private int goldReward = 5;

        [SerializeField, Tooltip(
            "확률 드랍. 비우면 위 경험치만 준다. " +
            "오브젝트(PropDefinition)와 같은 표를 쓰므로 파밍의 결이 같아진다")]
        private DropTable dropTable;

        [Header("표현 — 방향당 프레임 시트 (01~08 순서). 비어 있으면 플레이스홀더")]
        [SerializeField] private Sprite[] frames = System.Array.Empty<Sprite>();

        public string MonsterId => monsterId;
        public string DisplayName => displayName;
        public MonsterTier Tier => tier;
        public StatBlock Stats => stats;
        public float MaxHp => stats.Vitality * hpPerVitality;
        public float MoveSpeed => moveSpeed;
        public float DetectRange => detectRange;
        public float AttackRange => attackRange;
        public float AttackCooldown => attackCooldown;
        public float TelegraphDuration => telegraphDuration;
        public Combat.SkillShapeKind AttackShape => attackShape;

        /// <summary>
        /// 공격 범위. <see cref="attackShapeParams"/> 의 사거리를 비워 두면
        /// <see cref="attackRange"/> 로 채운다 — 두 곳에 같은 숫자를 넣게 하지 않으려는 것이다.
        /// </summary>
        public Combat.SkillShapeParams AttackShapeParams
        {
            get
            {
                Combat.SkillShapeParams p = attackShapeParams;
                if (p.range <= 0f) p.range = attackRange;
                if (p.angle <= 0f) p.angle = 180f;
                if (p.width <= 0f) p.width = 1f;
                return p;
            }
        }
        public float KnockbackForce => knockbackForce;
        public float HitStunDuration => hitStunDuration;
        public int ExpReward => expReward;
        public int GoldReward => goldReward;

        /// <summary>확률 드랍. 없으면 <see cref="ExpReward"/> 만 준다.</summary>
        public DropTable Drops => dropTable;
        public Sprite[] Frames => frames;
        public bool HasArt => frames != null && frames.Length > 0;
    }
}
