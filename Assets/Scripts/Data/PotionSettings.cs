using System;
using UnityEngine;

namespace PrettyKnights.Data
{
    /// <summary>
    /// 포션 자동 사용 설정. <b>플레이어가 바꾸는 값이라 세이브에 들어간다.</b>
    ///
    /// 아이템 쪽(<see cref="ItemDefinition.AutoUse"/>)이 "이건 자동으로 쓸 것" 을 정하고,
    /// 여기는 "언제 쓸 것" 을 정한다. 둘을 갈라 둔 이유는 주인이 다르기 때문이다 —
    /// 앞은 기획이 정하고 <b>뒤는 플레이어가 정한다.</b>
    ///
    /// 인벤토리·아이템 정의와 따로 두는 것도 같은 이유다. 설정은 아이템이 몇 종이든
    /// 하나뿐이고, 나중에 옵션 화면이 생기면 그쪽이 이것만 만지면 된다.
    /// </summary>
    [Serializable]
    public sealed class PotionSettings
    {
        /// <summary>기본 임계값. 절반에서 마시면 대개 늦지 않다.</summary>
        public const float DefaultThreshold = 0.5f;

        [SerializeField] private bool autoUse = true;

        [SerializeField, Range(0.05f, 0.95f), Tooltip("HP 비율이 이 아래로 떨어지면 마신다")]
        private float threshold = DefaultThreshold;

        /// <summary>설정이 바뀌면 발생한다. 옵션 화면이 여럿이어도 서로 따라온다.</summary>
        public event Action Changed;

        public bool AutoUse => autoUse;

        /// <summary>0~1. 이 비율 아래로 떨어지면 마신다.</summary>
        public float Threshold => Mathf.Clamp(threshold, 0.05f, 0.95f);

        /// <summary>화면에 쓸 백분율. 슬라이더 옆에 붙인다.</summary>
        public int ThresholdPercent => Mathf.RoundToInt(Threshold * 100f);

        public void SetAutoUse(bool value)
        {
            if (autoUse == value) return;

            autoUse = value;
            Changed?.Invoke();
        }

        public void SetThreshold(float value)
        {
            float clamped = Mathf.Clamp(value, 0.05f, 0.95f);
            if (Mathf.Approximately(threshold, clamped)) return;

            threshold = clamped;
            Changed?.Invoke();
        }

        public override string ToString() =>
            autoUse ? $"자동 사용 {ThresholdPercent}% 이하" : "자동 사용 꺼짐";
    }
}
