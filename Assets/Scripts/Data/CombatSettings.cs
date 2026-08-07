using UnityEngine;

namespace PrettyKnights.Data
{
    /// <summary>데미지를 어떻게 계산할지. 세 안이 아직 확정되지 않아 골라 쓸 수 있게 둔다.</summary>
    public enum DamageModel
    {
        /// <summary>감산 — <c>ATK − DEF × 배율</c>. 시트에 적힌 안.</summary>
        Subtract,

        /// <summary>비대칭 감산 — 때리는 쪽에 따라 배율이 다르다.</summary>
        AsymmetricSubtract,

        /// <summary>감쇠율 — <c>ATK × K / (K + DEF × 배율)</c>. 절대 0 이 되지 않는다.</summary>
        Attenuate
    }

    /// <summary>
    /// 전투 공식 한 곳. <b>공식이 확정되지 않았기 때문에 존재한다.</b>
    ///
    /// 플레이어 Lv1 ATK 20 · DEF 40 을 기준으로 세 안의 결과가 크게 갈린다.
    /// 숫자만 놓고 고르는 것보다 <b>실제로 때려보고 고르는 편이 정확하므로</b>,
    /// 재생 중에 이 에셋을 고치면 즉시 반영되게 만들었다.
    ///
    /// <code>
    /// Subtract     ATK 20 vs DEF 15 →  20 − 22.5 = 0     기본공격 무적이 생긴다
    /// Attenuate    ATK 20 vs DEF 15 →  20 × 100/122.5 = 16.3   완만하게 줄어든다
    /// </code>
    ///
    /// 확정되면 이 SO 를 없애지 말고 고른 모델만 남긴다.
    /// 환생 배율·속성 저항이 나중에 여기 얹힌다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "CombatSettings",
        menuName = "Pretty Knights/Combat Settings")]
    public sealed class CombatSettings : ScriptableObject
    {
        [Header("모델")]
        [SerializeField] private DamageModel model = DamageModel.Attenuate;

        [Header("감산 / 감쇠 공통")]
        [SerializeField, Min(0f), Tooltip("방어력에 곱하는 값. 시트의 안은 1.5")]
        private float defenseMultiplier = 1.5f;

        [Header("비대칭 감산 전용")]
        [SerializeField, Min(0f), Tooltip("몬스터가 플레이어를 때릴 때의 방어력 배율")]
        private float defenseMultiplierAgainstPlayer = 0.25f;

        [Header("감쇠율 전용")]
        [SerializeField, Min(1f), Tooltip("클수록 방어력의 영향이 작아진다")]
        private float attenuationConstant = 100f;

        [Header("공통")]
        [SerializeField, Min(0f), Tooltip("어떤 모델이든 이보다 적게는 안 들어간다. 0 이면 무적이 생긴다")]
        private float minimumDamage = 1f;

        /// <summary>
        /// 지금 쓰이는 설정. <c>GameRoot</c> 가 초기화 때 물려준다.
        /// 비어 있으면 기본값으로 동작하므로 게임플레이 씬 단독 실행에서도 죽지 않는다.
        /// </summary>
        public static CombatSettings Active { get; private set; }

        public static void Bind(CombatSettings settings) => Active = settings;

        public DamageModel Model => model;

        /// <summary>
        /// <paramref name="attack"/> 가 <paramref name="defense"/> 를 때렸을 때 들어가는 피해.
        /// <paramref name="targetIsPlayer"/> 는 비대칭 모델에서만 쓰인다.
        /// </summary>
        public float Compute(float attack, float defense, bool targetIsPlayer)
        {
            float raw;

            switch (model)
            {
                case DamageModel.Subtract:
                    raw = attack - defense * defenseMultiplier;
                    break;

                case DamageModel.AsymmetricSubtract:
                    float multiplier = targetIsPlayer ? defenseMultiplierAgainstPlayer : defenseMultiplier;
                    raw = attack - defense * multiplier;
                    break;

                default:
                    // 분모가 0 이 될 수 없다 — attenuationConstant 는 Min(1).
                    raw = attack * attenuationConstant
                          / (attenuationConstant + defense * defenseMultiplier);
                    break;
            }

            return Mathf.Max(minimumDamage, raw);
        }

        /// <summary>설정이 없을 때 쓰는 기본 계산. 감쇠율에 표준값을 넣은 것과 같다.</summary>
        public static float Damage(float attack, float defense, bool targetIsPlayer = false)
        {
            if (Active != null) return Active.Compute(attack, defense, targetIsPlayer);

            return Mathf.Max(1f, attack * 100f / (100f + defense * 1.5f));
        }
    }
}
