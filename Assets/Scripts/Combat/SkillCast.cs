using System.Collections.Generic;
using PrettyKnights.Data;
using UnityEngine;

namespace PrettyKnights.Combat
{
    /// <summary>
    /// 범위를 재고 그 안의 것을 때리는 한 벌. <b>기본 공격과 스킬이 같은 이 경로를 탄다.</b>
    ///
    /// <see cref="SkillShape"/> 가 "어디가 맞는가" 를 답한다면 여기는 "맞으면 어떻게 되는가" 를 답한다.
    /// 둘을 가른 이유는 범위 계산은 프리뷰·AI·설명 UI 도 부르지만
    /// <b>때리는 것은 실제 시전만</b> 하기 때문이다.
    ///
    /// <b>여기에도 필드를 두지 않는다</b> (CLAUDE.md §5). 버퍼는 전부 호출자가 넘긴다 —
    /// 정적 목록 하나를 재사용하면 같은 프레임에 스킬 둘이 나갈 때 결과가 섞인다.
    /// 관통 직선이 같은 몬스터를 두 번 때리지 않게 하는 목록도 시전마다 따로여야 한다.
    /// </summary>
    public static class SkillCast
    {
        /// <summary>
        /// 한 번 휘두른다. 실제로 맞은 <see cref="IDamageable"/> 수를 돌려준다.
        ///
        /// <paramref name="caster"/> 는 자기 자신을 때리지 않기 위한 것이다.
        /// 세 버퍼는 <b>호출자가 소유</b>하며 여기서 비워진다.
        /// </summary>
        public static int Strike(
            Transform caster,
            SkillShapeKind kind,
            SkillShapeParams param,
            Vector2 origin,
            Vector2 facing,
            float attackPower,
            ContactFilter2D filter,
            List<Collider2D> overlapped,
            List<IDamageable> struck,
            List<IAreaDamageable> struckAreas,
            bool logHits = false)
        {
            if (overlapped == null || struck == null || struckAreas == null) return 0;

            SkillShape.Evaluate(kind, param, origin, facing, filter, overlapped);

            struck.Clear();
            struckAreas.Clear();

            foreach (Collider2D collider in overlapped)
            {
                if (collider == null) continue;

                // 타일맵처럼 한 콜라이더가 여러 칸을 대표하는 것은 범위를 통째로 넘긴다.
                // 어느 칸을 맞았는지는 받는 쪽이 같은 범위 계산으로 고른다.
                IAreaDamageable area = collider.GetComponentInParent<IAreaDamageable>();
                if (area != null && !struckAreas.Contains(area))
                {
                    struckAreas.Add(area);
                    area.ApplyAreaDamage(kind, param, origin, facing, attackPower);
                }

                // 콜라이더는 자식(Visual)에 있을 수도 있으므로 부모까지 올라가 찾는다.
                IDamageable target = collider.GetComponentInParent<IDamageable>();

                if (target == null || !target.IsAlive) continue;
                if (caster != null && target.Transform == caster) continue;   // 자기 자신
                if (struck.Contains(target)) continue;                        // 같은 몸의 콜라이더 둘

                struck.Add(target);

                float damage = CombatSettings.Damage(attackPower, target.Defense, targetIsPlayer: false);
                target.ApplyDamage(damage, origin);

                if (logHits)
                    Debug.Log($"[SkillCast] {target.Transform.name} 에 {damage:0.#} " +
                              $"(ATK {attackPower:0.#} vs DEF {target.Defense:0.#})");
            }

            return struck.Count;
        }

        /// <summary>
        /// 플레이어의 지금 공격력. 스킬로 오르는 값이 여기 반영된다.
        /// 상태가 없으면(게임플레이 씬 단독 실행) 검증이 막히지 않게 기본값을 준다.
        /// </summary>
        public static float PlayerAttackPower(float fallback = 20f)
        {
            if (Core.ServiceRegistry.TryGet(out PlayerRuntimeState state) && state != null && state.IsBound)
                return state.Stats.Attack;

            return fallback;
        }
    }
}
