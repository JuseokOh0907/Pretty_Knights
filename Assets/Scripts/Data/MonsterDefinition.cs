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

        [Header("보상")]
        [SerializeField, Min(0)] private int expReward = 12;

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
        public int ExpReward => expReward;
        public Sprite[] Frames => frames;
        public bool HasArt => frames != null && frames.Length > 0;
    }
}
