using PrettyKnights.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PrettyKnights.UI
{
    /// <summary>
    /// Boot 씬에 상주하는 UI 루트. <b>게임플레이 씬이나 플레이어 프리팹에 두지 않는다.</b>
    ///
    /// 모드 전환 버튼은 전환을 요청하는 주체인데, 전환되는 씬 안에 있으면
    /// 자기가 언로드되면서 사라진다. 플레이어 프리팹도 씬 전환 시 파괴되므로 마찬가지다.
    /// <see cref="GameRoot"/> 와 같은 상주 계층에 두는 것이 맞다.
    ///
    /// 캔버스는 <c>Screen Space - Overlay</c> 를 쓴다.
    /// Camera 모드는 씬마다 카메라가 바뀔 때마다 다시 물려줘야 하고, 놓치면 UI 가 통째로 사라진다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIRoot : MonoBehaviour
    {
        [Header("연결 (비우면 자동으로 찾는다)")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasScaler scaler;

        [Header("모드별 기준 해상도")]
        [SerializeField] private Vector2 portraitReference = new Vector2(1080f, 1920f);
        [SerializeField] private Vector2 landscapeReference = new Vector2(1920f, 1080f);

        [SerializeField, Range(0f, 1f), Tooltip("세로 모드의 Match. 0=너비 기준")]
        private float portraitMatch;

        [SerializeField, Range(0f, 1f), Tooltip("가로 모드의 Match. 1=높이 기준")]
        private float landscapeMatch = 1f;

        [Header("모드별 패널 (선택)")]
        [SerializeField, Tooltip("세로에서만 켜지는 오브젝트")]
        private GameObject[] portraitOnly = System.Array.Empty<GameObject>();

        [SerializeField, Tooltip("가로에서만 켜지는 오브젝트")]
        private GameObject[] landscapeOnly = System.Array.Empty<GameObject>();

        public static UIRoot Instance { get; private set; }

        private SceneFlow scenes;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (canvas == null) canvas = GetComponentInChildren<Canvas>();
            if (scaler == null && canvas != null) scaler = canvas.GetComponent<CanvasScaler>();

            ServiceRegistry.Register(this);
        }

        private void Start()
        {
            // GameRoot 는 DefaultExecutionOrder -1000 이라 Awake 가 먼저 끝나 있다.
            if (!ServiceRegistry.TryGet(out SceneFlow flow)) return;

            scenes = flow;
            scenes.ModeChanged += Apply;

            // 이미 전환이 끝난 뒤에 붙었을 수도 있다.
            if (scenes.CurrentMode.HasValue) Apply(scenes.CurrentMode.Value);
        }

        private void OnDestroy()
        {
            if (scenes != null) scenes.ModeChanged -= Apply;

            if (Instance != this) return;

            ServiceRegistry.Unregister<UIRoot>();
            Instance = null;
        }

        /// <summary>모드에 맞춰 스케일 기준과 패널을 바꾼다.</summary>
        private void Apply(GameMode mode)
        {
            bool portrait = mode == GameMode.Vertical;

            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = portrait ? portraitReference : landscapeReference;
                scaler.matchWidthOrHeight = portrait ? portraitMatch : landscapeMatch;
            }

            foreach (GameObject go in portraitOnly)
                if (go != null) go.SetActive(portrait);

            foreach (GameObject go in landscapeOnly)
                if (go != null) go.SetActive(!portrait);
        }
    }
}
