using UnityEngine;

namespace PrettyKnights.Combat
{
    /// <summary>
    /// 이펙트 한 프레임. <b>그림 한 장과 그것을 놓을 자리</b>다.
    ///
    /// 이 구조체가 있는 이유는 만드는 쪽이 둘이기 때문이다 —
    /// <see cref="SkillImpactRasterizer"/> 가 판정 도형에서 구워내는 것과
    /// <see cref="Data.SkillEffectDefinition"/> 이 손으로 그린 아트에서 가져오는 것.
    /// 재생하는 쪽(<see cref="SkillImpact"/>)은 둘을 구분할 필요가 없다.
    ///
    /// <see cref="Offset"/> 은 <b>도형 원점에서 이 그림의 중심까지</b>다.
    /// 구운 조각은 프레임마다 잘린 위치가 달라 이 값이 프레임마다 다르고,
    /// 손으로 그린 아트는 보통 전 프레임이 같은 값을 쓴다.
    /// </summary>
    public readonly struct SkillEffectFrame
    {
        public readonly Sprite Sprite;
        public readonly Vector2 Offset;

        public SkillEffectFrame(Sprite sprite, Vector2 offset)
        {
            Sprite = sprite;
            Offset = offset;
        }

        public bool IsEmpty => Sprite == null;
    }
}
