using UnityEngine;

namespace PrettyKnights.World
{
    /// <summary>
    /// 전투 화면을 <b>화면 전체가 아니라 띠 하나</b>로 만든다 (세로 모드, 2026-08-09 확정).
    ///
    /// <code>
    /// 1080 × 1920
    ///   ┌──────────────┐  ← 위
    ///   │  상단 HUD    │     레벨 · 체력 · 재화     topInset
    ///   ├──────────────┤
    ///   │  전투 화면   │     이 컴포넌트가 정하는 띠  heightFraction
    ///   ├──────────────┤
    ///   │  하단 조작판 │     스킬 · 메뉴
    ///   └──────────────┘  ← 아래
    /// </code>
    ///
    /// <b>UI 를 카메라 위에 덮는 것과는 다르다.</b> 덮으면 전투가 UI 뒤에서 계속 벌어지고
    /// 카메라는 화면 전체를 그리느라 보이지도 않을 픽셀을 채운다.
    /// 뷰포트를 줄이면 <b>그리는 픽셀 자체가 줄어</b> 모바일에서 그 차이가 크다.
    ///
    /// <b>띠를 만들면 그림이 작아진다.</b> <c>orthographicSize</c> 는 화면 높이의 절반을
    /// <b>월드 유닛</b>으로 정하는 값이라, 뷰포트가 30%로 줄어도 그대로 두면
    /// 같은 세로 10유닛을 1/3 높이의 픽셀에 욱여넣게 된다 —
    /// 픽셀당 보이는 세계가 3배 넓어지고 <b>캐릭터가 3분의 1로 작아진다.</b>
    /// (가로도 함께 3배 넓어진다. 찌그러지지는 않는다)
    ///
    /// <see cref="compensateOrthographicSize"/> 를 켜면 띠 비율만큼 size 를 <b>줄여</b>
    /// 픽셀당 크기를 전체 화면일 때와 같게 맞춘다. 그 결과 <b>가로로 보이는 범위도
    /// 전체 화면일 때와 정확히 같아진다</b> — 세로 모드는 위아래만 잘린 셈이 된다.
    ///
    /// 세로 씬의 카메라에 붙인다. 가로 씬에는 두지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [ExecuteAlways]
    public sealed class StageViewport : MonoBehaviour
    {
        [Header("띠의 자리 (화면 높이 대비 비율)")]
        [SerializeField, Range(0f, 0.9f), Tooltip("화면 위쪽에서 얼마를 HUD 에 내주는가")]
        private float topInset = 0.14f;

        [SerializeField, Range(0.05f, 1f), Tooltip("전투 화면이 차지하는 높이. 0.30 이 기준안")]
        private float heightFraction = 0.30f;

        [Header("보이는 범위")]
        [SerializeField, Tooltip(
            "켜면 띠 비율만큼 orthographicSize 를 줄여 캐릭터 크기와 가로 시야를 " +
            "전체 화면일 때와 같게 맞춘다. 끄면 띠 안의 그림이 그만큼 작아진다")]
        private bool compensateOrthographicSize = true;

        [SerializeField, Min(0.1f), Tooltip(
            "화면 전체를 쓸 때의 orthographicSize. 보정은 이 값을 기준으로 계산한다")]
        private float fullScreenOrthographicSize = 5f;

        [SerializeField, Min(0.1f), Tooltip(
            "세로 모드에서 얼마나 당겨 볼지. 1.5 면 캐릭터·타일·장애물이 모두 1.5배로 커지고 " +
            "보이는 범위는 그만큼 좁아진다 (결정 009)")]
        private float zoom = 1.5f;

        private Camera stageCamera;

        /// <summary>띠의 아래쪽 경계 (0=화면 바닥, 1=꼭대기). 하단 조작판의 높이와 같다.</summary>
        public float BottomEdge => Mathf.Clamp01(1f - topInset - heightFraction);

        /// <summary>띠의 위쪽 경계. 상단 HUD 가 여기부터 위를 쓴다.</summary>
        public float TopEdge => Mathf.Clamp01(1f - topInset);

        private void OnEnable() => Apply();

        private void OnValidate() => Apply();

        /// <summary>
        /// 뷰포트를 다시 잰다. 인스펙터에서 값을 만지면 씬 뷰에서 바로 보이도록
        /// <c>ExecuteAlways</c> 로 두었다.
        /// </summary>
        public void Apply()
        {
            if (stageCamera == null) stageCamera = GetComponent<Camera>();
            if (stageCamera == null) return;

            float height = Mathf.Clamp(heightFraction, 0.05f, 1f);
            float bottom = Mathf.Clamp(1f - topInset - height, 0f, 1f - height);

            stageCamera.rect = new Rect(0f, bottom, 1f, height);

            if (!compensateOrthographicSize || !stageCamera.orthographic) return;

            // 픽셀당 월드 크기 = size × 2 ÷ 뷰포트 픽셀 높이.
            // 뷰포트가 h 배로 줄었으니 size 도 h 배로 줄여야 그 값이 그대로다.
            // 여기에 zoom 으로 한 번 더 나눈다 — 나눌수록 같은 픽셀에 담기는 세계가
            // 좁아지므로 그림이 커진다.
            //
            // 1080 × 1920 · size 5 · h 0.30 · zoom 1.5 → size 1.0
            //   가로 시야 5.625 ÷ 1.5 = 3.75유닛 · 캐릭터 폭 0.72유닛이 화면의 19%
            stageCamera.orthographicSize =
                fullScreenOrthographicSize * height / Mathf.Max(0.1f, zoom);
        }

        /// <summary>
        /// 화면 전체로 되돌린다. 같은 씬을 가로로 쓰게 될 때를 위한 것이다.
        /// </summary>
        public void ResetToFullScreen()
        {
            if (stageCamera == null) stageCamera = GetComponent<Camera>();
            if (stageCamera == null) return;

            stageCamera.rect = new Rect(0f, 0f, 1f, 1f);

            if (compensateOrthographicSize && stageCamera.orthographic)
                stageCamera.orthographicSize = fullScreenOrthographicSize / Mathf.Max(0.1f, zoom);
        }
    }
}
