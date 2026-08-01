using System;
using UnityEngine;

namespace PrettyKnights.Data
{
    /// <summary>
    /// 캐릭터 수치 한 묶음.
    /// 스탯 목록은 미결정 #3 이 확정되기 전까지의 임시안이다
    /// (docs/decisions/OPEN_DECISIONS.md).
    /// </summary>
    [Serializable]
    public struct StatBlock
    {
        [Tooltip("최대 HP 로 환산되는 생명력")]
        public float Vitality;

        [Tooltip("공격력")]
        public float Attack;

        [Tooltip("피해 감소에 쓰이는 방어력")]
        public float Defense;

        [Tooltip("이동·공격 속도에 관여하는 민첩")]
        public float Agility;

        [Tooltip("치명타·스킬 위력에 관여하는 집중")]
        public float Focus;

        public static readonly StatBlock Zero = default;

        public StatBlock(float vitality, float attack, float defense, float agility, float focus)
        {
            Vitality = vitality;
            Attack = attack;
            Defense = defense;
            Agility = agility;
            Focus = focus;
        }

        public static StatBlock operator +(StatBlock a, StatBlock b) => new StatBlock(
            a.Vitality + b.Vitality,
            a.Attack + b.Attack,
            a.Defense + b.Defense,
            a.Agility + b.Agility,
            a.Focus + b.Focus);

        public static StatBlock operator *(StatBlock a, float scalar) => new StatBlock(
            a.Vitality * scalar,
            a.Attack * scalar,
            a.Defense * scalar,
            a.Agility * scalar,
            a.Focus * scalar);

        public override string ToString() =>
            $"VIT {Vitality:0.#} / ATK {Attack:0.#} / DEF {Defense:0.#} / AGI {Agility:0.#} / FOC {Focus:0.#}";
    }
}
