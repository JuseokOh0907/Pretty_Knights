using PrettyKnights.Combat;
using UnityEngine;

namespace PrettyKnights.Data
{
    /// <summary>
    /// 플레이어가 배우는 스킬 하나.
    ///
    /// <b>범위 계산은 여기 없다.</b> <see cref="SkillShape"/> 가 한 벌로 들고 있고
    /// 이 에셋은 "어떤 모양을 얼마만큼" 만 적는다 (CLAUDE.md §5).
    /// 몬스터 스킬과 갈리는 것은 <b>누가 무엇을 들고 있는가</b>이지
    /// 범위를 어떻게 재는가가 아니다 — 그래서 목록만 나누고 계산은 공유한다.
    ///
    /// <b>위력을 고정값이 아니라 공격력 배수로 둔다.</b> 고정값으로 두면
    /// 레벨이 오를수록 스킬이 기본 공격보다 약해져 쓸 이유가 사라진다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PlayerSkill",
        menuName = "Pretty Knights/Player Skill Definition")]
    public sealed class PlayerSkillDefinition : ScriptableObject
    {
        [Header("정체")]
        [SerializeField, Tooltip("세이브에 들어갈 수 있으므로 한 번 정하면 바꾸지 않는다")]
        private string skillId = "slash";

        [SerializeField] private string displayName = "베기";

        [SerializeField, TextArea(2, 4)] private string description;

        [SerializeField, Tooltip("버튼에 그릴 그림. 비우면 버튼이 기본 이미지를 유지한다")]
        private Sprite icon;

        [Header("배우는 조건")]
        [SerializeField, Min(1), Tooltip("이 레벨부터 쓸 수 있다. 그 전까지 버튼은 잠김으로 그려진다")]
        private int unlockLevel = 1;

        [Header("범위 — 판정과 인디케이터가 같은 값을 쓴다")]
        [SerializeField] private SkillShapeKind shape = SkillShapeKind.Forward;

        [SerializeField] private SkillShapeParams shapeParams = SkillShapeParams.Slash;

        [SerializeField, Tooltip("원점을 앞으로 얼마나 밀지. 0이면 발밑에서 잰다")]
        private float originForwardOffset = 0.3f;

        [Header("위력")]
        [SerializeField, Min(0f), Tooltip("공격력에 곱한다. 1이면 기본 공격과 같은 위력")]
        private float damageMultiplier = 1.6f;

        [SerializeField, Min(0.05f), Tooltip("쿨타임 (초)")]
        private float cooldown = 6f;

        [Header("이펙트 (없어도 판정은 그대로 된다)")]
        [SerializeField, Tooltip("손으로 그린 타격 그림. 비우면 판정 범위를 임시로 그린다")]
        private SkillEffectDefinition effect;

        [SerializeField, Tooltip(
            "그림이 없을 때 판정 범위를 그려 보여줄지. 아트가 들어오면 끈다")]
        private bool showRangeWhenNoArt = true;

        public string SkillId => skillId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public int UnlockLevel => unlockLevel;
        public SkillShapeKind Shape => shape;
        public SkillShapeParams ShapeParams => shapeParams;
        public float OriginForwardOffset => originForwardOffset;
        public float DamageMultiplier => damageMultiplier;
        public float Cooldown => cooldown;
        public SkillEffectDefinition Effect => effect;
        public bool ShowRangeWhenNoArt => showRangeWhenNoArt;
    }
}
