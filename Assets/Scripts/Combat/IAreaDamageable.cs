using UnityEngine;

namespace PrettyKnights.Combat
{
    /// <summary>
    /// 범위를 <b>통째로</b> 받는 것. 타일맵처럼 한 GameObject 가 여러 칸을 대표할 때 쓴다.
    ///
    /// <see cref="IDamageable"/> 는 "이 대상이 맞았다" 를 표현하는데,
    /// 타일맵은 콜라이더가 하나로 합쳐져 있어 <b>어느 칸을 맞았는지 알 수 없다.</b>
    /// 그래서 최종 피해량이 아니라 <b>범위 자체</b>를 넘겨 받는 쪽이 칸을 고르게 한다.
    ///
    /// <c>CLAUDE.md</c> §5 의 "타일마다 데미지 오브젝트를 만들지 않는다.
    /// 한 번의 범위 결과에서 대상을 수집한다" 를 그대로 따르는 형태다.
    /// </summary>
    public interface IAreaDamageable
    {
        /// <summary>
        /// <paramref name="attack"/> 는 <b>공식을 거치기 전</b> 공격력이다.
        /// 칸마다 방어력이 다를 수 있으므로 받는 쪽이 계산한다 —
        /// 이 점이 <see cref="IDamageable.ApplyDamage"/> 와 다르다.
        /// </summary>
        void ApplyAreaDamage(
            SkillShapeKind kind, SkillShapeParams param,
            Vector2 origin, Vector2 facing, float attack);
    }
}
