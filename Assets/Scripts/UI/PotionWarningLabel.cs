using PrettyKnights.Core;
using TMPro;
using UnityEngine;

namespace PrettyKnights.UI
{
    /// <summary>
    /// "포션이 없습니다" 를 화면에 띄운다.
    ///
    /// <b>자동에 맡긴 만큼 실패도 말해야 한다.</b> 자동 사용을 켜 둔 사람은
    /// 화면을 보고 있지 않을 수 있는데, 조용히 넘어가면 왜 안 마셨는지 모른 채 죽는다.
    ///
    /// 간격 제한은 <see cref="AutoPotion"/> 쪽에 있다. 여기는 받은 것을 띄우기만 한다 —
    /// 언제 알릴지는 상태를 아는 쪽이 정하는 것이 맞다.
    ///
    /// <c>UIRoot</c> 의 <b>가로 전용</b> 패널에 둔다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class PotionWarningLabel : MonoBehaviour
    {
        [Header("연결 (비우면 자동으로 찾는다)")]
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text label;

        [Header("표시")]
        [SerializeField, Min(0.1f), Tooltip("또렷하게 떠 있는 시간")]
        private float holdDuration = 2f;

        [SerializeField, Min(0.1f), Tooltip("사그라지는 시간")]
        private float fadeDuration = 0.6f;

        private AutoPotion source;
        private float left;

        private void Awake()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            if (label == null) label = GetComponentInChildren<TMP_Text>(includeInactive: true);

            // 시작하자마자 떠 있으면 안 된다.
            if (group != null) group.alpha = 0f;
        }

        private void OnDisable() => Unbind();

        private void OnDestroy() => Unbind();

        private void Update()
        {
            if (source == null) Bind();

            if (left <= 0f) return;

            left -= Time.deltaTime;

            if (group == null) return;

            // 남은 시간이 페이드 구간에 들어오면 그때부터 흐려진다.
            group.alpha = fadeDuration <= 0f ? (left > 0f ? 1f : 0f) : Mathf.Clamp01(left / fadeDuration);
        }

        private void Bind()
        {
            if (!ServiceRegistry.TryGet(out AutoPotion found) || found == null) return;

            source = found;
            source.Warned += Show;
        }

        private void Unbind()
        {
            if (source == null) return;

            source.Warned -= Show;
            source = null;
        }

        /// <summary>밖에서도 띄울 수 있다. 다른 경고를 여기 얹을 때 쓴다.</summary>
        public void Show(string message)
        {
            if (label != null) label.text = message;

            left = holdDuration + fadeDuration;

            if (group != null) group.alpha = 1f;
        }
    }
}
