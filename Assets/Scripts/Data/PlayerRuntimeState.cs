using System;
using UnityEngine;

namespace PrettyKnights.Data
{
    /// <summary>
    /// 씬에 종속되지 않는 플레이어의 살아 있는 상태.
    /// 세로 자동 사냥과 가로 직접 플레이가 <b>이 하나의 인스턴스를 공유한다</b> (기획서 §15-6).
    /// <see cref="PlayerStatsDefinition"/> 은 입력일 뿐이며 여기에 계산 결과를 담는다.
    /// </summary>
    [Serializable]
    public sealed class PlayerRuntimeState
    {
        [SerializeField] private int level = 1;
        [SerializeField] private int exp;

        /// <summary>음수면 아직 초기화되지 않은 것으로 보고 <see cref="Bind"/> 에서 최대치로 채운다.</summary>
        [SerializeField] private float currentHp = -1f;

        [NonSerialized] private PlayerStatsDefinition definition;

        /// <summary>
        /// 레벨 밖에서 더해진 스탯. 지금은 상점 강화가 유일한 출처다.
        ///
        /// <b>직렬화하지 않는다.</b> 강화 단계는 <see cref="PlayerUpgrades"/> 가 들고 있고
        /// 여기 있는 것은 그것을 합산한 결과일 뿐이다 —
        /// 둘 다 저장하면 어느 쪽이 진실인지 갈리고, 공식을 바꾸는 순간 어긋난다.
        /// </summary>
        [NonSerialized] private StatBlock bonusStats;

        /// <summary>레벨·경험치·HP 중 무엇이든 바뀌면 발생한다. UI 갱신용.</summary>
        public event Action<PlayerRuntimeState> Changed;

        /// <summary>레벨이 오른 순간에만 발생한다. 인자는 새 레벨.</summary>
        public event Action<int> LeveledUp;

        public int Level => level;
        public int Exp => exp;
        public float CurrentHp => currentHp;
        public bool IsBound => definition != null;
        public bool IsDead => IsBound && currentHp <= 0f;

        public StatBlock Stats => IsBound ? definition.StatsAtLevel(level) + bonusStats : StatBlock.Zero;
        public float MaxHp => IsBound ? definition.MaxHpFor(Stats) : 0f;
        public int ExpToNextLevel => IsBound ? definition.RequiredExpToNext(level) : 0;
        public float HpRatio => MaxHp > 0f ? Mathf.Clamp01(currentHp / MaxHp) : 0f;

        /// <summary>
        /// 정의를 연결한다. 저장 데이터를 불러온 직후에 반드시 한 번 호출해야 한다.
        /// 정의는 직렬화되지 않으므로 로드된 상태는 이 호출 전까지 계산이 불가능하다.
        /// </summary>
        public void Bind(PlayerStatsDefinition statsDefinition)
        {
            definition = statsDefinition != null
                ? statsDefinition
                : throw new ArgumentNullException(nameof(statsDefinition));

            level = Mathf.Clamp(level, 1, definition.MaxLevel);

            // 최초 생성이거나 저장값이 깨진 경우 최대치에서 시작한다.
            if (currentHp < 0f || float.IsNaN(currentHp))
                currentHp = MaxHp;
            else
                currentHp = Mathf.Min(currentHp, MaxHp);

            Changed?.Invoke(this);
        }

        /// <summary>
        /// 강화로 더해진 스탯을 통째로 갈아 끼운다. 더하지 않고 <b>덮어쓴다</b> —
        /// 부르는 쪽은 강화 단계 전부를 합산한 결과를 넘기므로,
        /// 누적시키면 다시 계산할 때마다 두 배가 된다.
        ///
        /// 최대 HP 가 함께 오르지만 <b>현재 HP 를 따라 올리지는 않는다.</b>
        /// 회복은 회복 수단이 하는 일이고, 강화가 곧 회복이면
        /// 죽기 직전에 사는 것이 가장 이득인 이상한 규칙이 생긴다.
        /// </summary>
        public void SetBonusStats(StatBlock bonus)
        {
            bonusStats = bonus;

            // 최대치가 줄어드는 방향(환불 등)에서 현재 HP 가 최대를 넘지 않게 한다.
            if (IsBound) currentHp = Mathf.Min(currentHp, MaxHp);

            Changed?.Invoke(this);
        }

        public void AddExp(int amount)
        {
            if (!IsBound || amount <= 0) return;

            exp += amount;

            while (level < definition.MaxLevel)
            {
                int required = definition.RequiredExpToNext(level);
                if (exp < required) break;

                exp -= required;
                level++;

                // 레벨업 시 최대 HP가 올라간 만큼 회복시킨다.
                currentHp = MaxHp;
                LeveledUp?.Invoke(level);
            }

            if (level >= definition.MaxLevel)
                exp = 0;

            Changed?.Invoke(this);
        }

        public void ApplyDamage(float amount)
        {
            if (!IsBound || amount <= 0f || IsDead) return;

            currentHp = Mathf.Max(0f, currentHp - amount);
            Changed?.Invoke(this);
        }

        public void Heal(float amount)
        {
            if (!IsBound || amount <= 0f) return;

            currentHp = Mathf.Min(MaxHp, currentHp + amount);
            Changed?.Invoke(this);
        }

        /// <summary>사망 후 부활 등에서 상태를 되돌린다.</summary>
        public void RestoreToFull()
        {
            if (!IsBound) return;

            currentHp = MaxHp;
            Changed?.Invoke(this);
        }
    }
}
