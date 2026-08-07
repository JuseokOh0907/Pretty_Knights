using UnityEngine;

namespace PrettyKnights.Combat
{
    /// <summary>
    /// 맞을 수 있는 것. 몬스터와 파괴 가능 오브젝트가 같은 인터페이스를 쓴다.
    ///
    /// 스킬 판정이 대상을 수집할 때 "몬스터인가 오브젝트인가" 를 묻지 않게 하려는 것이다.
    /// 광역 폭발 한 번이 몬스터 셋과 토템 하나를 동시에 때리는 것이 자연스러워야 한다.
    /// </summary>
    public interface IDamageable
    {
        bool IsAlive { get; }

        /// <summary>데미지 공식에 들어갈 방어력.</summary>
        float Defense { get; }

        Transform Transform { get; }

        /// <summary>
        /// 이미 공식을 거친 최종 피해량을 받는다.
        /// <paramref name="sourcePosition"/> 은 넉백 방향을 정하는 데 쓴다.
        /// </summary>
        void ApplyDamage(float amount, Vector2 sourcePosition);
    }
}
