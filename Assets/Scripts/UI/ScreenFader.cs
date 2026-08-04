using System.Collections;
using PrettyKnights.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PrettyKnights.UI
{
    /// <summary>
    /// 화면 전체를 덮는 검은 막. 구역 전환이 이걸로 교체 순간을 가린다.
    ///
    /// <b><c>UIRoot</c> 의 캔버스 안, 형제 중 가장 아래에 둔다.</b>
    /// Overlay 캔버스는 계층 순서가 곧 그리기 순서라 위에 있으면 조이스틱에 가려진다.
    ///
    /// 불투명한 동안 <c>CanvasGroup.blocksRaycasts</c> 로 터치를 막는다.
    /// 안 막으면 전환 중에 조이스틱과 사용 버튼이 눌린다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ScreenFader : MonoBehaviour
    {
        [Header("연결 (비우면 자동으로 찾는다)")]
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image image;

        [Header("시간")]
        [SerializeField, Min(0f), Tooltip("어두워지는 데 걸리는 시간")]
        private float fadeOutDuration = 0.25f;

        [SerializeField, Min(0f), Tooltip("밝아지는 데 걸리는 시간. 나갈 때보다 짧아야 답답하지 않다")]
        private float fadeInDuration = 0.18f;

        public bool IsOpaque => group != null && group.alpha >= 0.999f;

        private void Awake()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            if (image == null) image = GetComponent<Image>();

            // 시작은 항상 투명이다. 에디터에서 까맣게 두고 저장해도 플레이에서 걷힌다.
            Apply(0f);

            ServiceRegistry.Register(this);
        }

        private void OnDestroy()
        {
            if (ServiceRegistry.TryGet(out ScreenFader current) && current == this)
                ServiceRegistry.Unregister<ScreenFader>();
        }

        public IEnumerator FadeOut() => FadeTo(1f, fadeOutDuration);

        public IEnumerator FadeIn() => FadeTo(0f, fadeInDuration);

        private IEnumerator FadeTo(float target, float duration)
        {
            if (group == null) yield break;

            float start = group.alpha;

            if (duration <= 0f || Mathf.Approximately(start, target))
            {
                Apply(target);
                yield break;
            }

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                Apply(Mathf.Lerp(start, target, elapsed / duration));
                yield return null;
            }

            Apply(target);
        }

        private void Apply(float alpha)
        {
            if (group == null) return;

            group.alpha = alpha;

            // 완전히 투명할 때만 터치를 통과시킨다.
            bool blocking = alpha > 0.001f;
            group.blocksRaycasts = blocking;
            group.interactable = blocking;

            if (image != null) image.raycastTarget = blocking;
        }
    }
}
