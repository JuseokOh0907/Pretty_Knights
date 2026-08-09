using UnityEngine;

namespace PrettyKnights.Data
{
    /// <summary>
    /// 플레이어의 정적 정의. <b>읽기 전용으로만 쓴다.</b>
    /// 런타임 값은 <see cref="PlayerRuntimeState"/> 에 담는다
    /// (근거: docs/decisions/003-runtime-architecture.md).
    ///
    /// 속도·거리는 전부 월드 유닛 기준이다. PPU 가 확정되지 않았으므로
    /// 픽셀 단위 상수를 여기에 넣지 않는다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PlayerStatsDefinition",
        menuName = "Pretty Knights/Player Stats Definition")]
    public sealed class PlayerStatsDefinition : ScriptableObject
    {
        [Header("정체")]
        [SerializeField] private string displayName = "Knight";

        [Header("성장")]
        [SerializeField, Min(1)] private int maxLevel = 50;
        [SerializeField] private StatBlock baseStats = new StatBlock(20f, 10f, 5f, 8f, 5f);
        [SerializeField] private StatBlock growthPerLevel = new StatBlock(4f, 2f, 1f, 1f, 1f);

        [Header("경험치 커브 — required = expBase * level^expExponent")]
        [SerializeField, Min(1f)] private float expBase = 25f;
        [SerializeField, Min(1f)] private float expExponent = 1.6f;

        [Header("파생 수치 환산")]
        [SerializeField, Min(1f)] private float hpPerVitality = 5f;

        [Header("이동 (월드 유닛/초)")]
        [SerializeField, Min(0f)] private float walkSpeed = 2.5f;
        [SerializeField, Min(1f)] private float runSpeedMultiplier = 1.6f;

        public string DisplayName => displayName;
        public int MaxLevel => maxLevel;
        public StatBlock BaseStats => baseStats;
        public StatBlock GrowthPerLevel => growthPerLevel;
        public float WalkSpeed => walkSpeed;
        public float RunSpeed => walkSpeed * runSpeedMultiplier;

        /// <summary>해당 레벨에서의 최종 스탯. 레벨 1이 기초값이다.</summary>
        public StatBlock StatsAtLevel(int level)
        {
            int clamped = Mathf.Clamp(level, 1, maxLevel);
            return baseStats + growthPerLevel * (clamped - 1);
        }

        public float MaxHpAtLevel(int level) => StatsAtLevel(level).Vitality * hpPerVitality;

        /// <summary>
        /// 임의의 스탯 묶음에서 나오는 최대 HP.
        /// <b>상점 강화가 올린 생명력도 최대 HP 에 반영되어야</b> 하므로
        /// 레벨이 아니라 최종 스탯을 받는 창구가 따로 필요하다.
        /// </summary>
        public float MaxHpFor(StatBlock stats) => stats.Vitality * hpPerVitality;

        /// <summary>
        /// <paramref name="level"/> 에서 다음 레벨로 가는 데 필요한 경험치.
        /// 최대 레벨에서는 <see cref="int.MaxValue"/> 를 돌려 더 오르지 않게 한다.
        /// </summary>
        public int RequiredExpToNext(int level)
        {
            if (level >= maxLevel) return int.MaxValue;
            int clamped = Mathf.Max(1, level);
            return Mathf.RoundToInt(expBase * Mathf.Pow(clamped, expExponent));
        }
    }
}
