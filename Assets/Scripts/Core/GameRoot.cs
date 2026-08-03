using PrettyKnights.Data;
using PrettyKnights.Save;
using UnityEngine;

namespace PrettyKnights.Core
{
    /// <summary>
    /// Boot 씬에 하나만 놓는 상주 오브젝트.
    /// 세이브·플레이어 상태·씬 전환을 들고 있으며 게임이 끝날 때까지 파괴되지 않는다.
    ///
    /// 이 구조 덕분에 세로/가로 씬이 갈아 끼워져도
    /// <see cref="PlayerRuntimeState"/> 는 같은 인스턴스로 유지된다 (기획서 §15-6).
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class GameRoot : MonoBehaviour
    {
        [Header("정의 에셋")]
        [SerializeField] private PlayerStatsDefinition playerStats;

        [Header("시작 설정")]
        [SerializeField] private GameMode startMode = GameMode.Vertical;
        [SerializeField, Tooltip("모바일에서 배터리를 아끼기 위한 상한. 0이면 건드리지 않음")]
        private int targetFrameRate = 60;

        public static GameRoot Instance { get; private set; }

        public PlayerRuntimeState PlayerState { get; private set; }
        public SaveService Saves { get; private set; }
        public SceneFlow Scenes { get; private set; }

        /// <summary>이번 실행이 신규 플레이인지 (세이브 파일이 없었는지).</summary>
        public bool IsNewGame { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // 게임플레이 씬을 단독 실행했다가 Boot 로 돌아온 경우 등 중복 방지.
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (targetFrameRate > 0) Application.targetFrameRate = targetFrameRate;

            InitializeServices();
        }

        private void Start()
        {
            StartCoroutine(Scenes.SwitchTo(startMode));
        }

        private void InitializeServices()
        {
            Saves = new SaveService();
            Scenes = new SceneFlow();

            IsNewGame = !Saves.TryLoad(out SaveData data);
            PlayerState = data.Player;

            if (playerStats == null)
            {
                Debug.LogError(
                    "[GameRoot] PlayerStatsDefinition 이 비어 있습니다. " +
                    "인스펙터에서 연결해야 스탯 계산이 동작합니다.");
            }
            else
            {
                PlayerState.Bind(playerStats);
            }

            ServiceRegistry.Register(this);
            ServiceRegistry.Register(PlayerState);
            ServiceRegistry.Register(Saves);
            ServiceRegistry.Register(Scenes);
        }

        /// <summary>현재 상태를 파일에 쓴다. 저장 지점마다 호출한다.</summary>
        public void SaveNow()
        {
            if (Saves == null || PlayerState == null) return;

            Saves.TrySave(SaveData.From(PlayerState));
        }

        public void RequestMode(GameMode mode)
        {
            if (Scenes == null || !isActiveAndEnabled) return;
            StartCoroutine(Scenes.SwitchTo(mode));
        }

        // 모바일은 홈 버튼 한 번으로 앱이 회수될 수 있다.
        // 종료 콜백을 기다리지 않고 백그라운드로 내려가는 시점에 저장한다.
        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveNow();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) SaveNow();
        }

        private void OnApplicationQuit() => SaveNow();

        private void OnDestroy()
        {
            if (Instance != this) return;

            ServiceRegistry.Clear();
            Instance = null;
        }

        // ── 검증용 ────────────────────────────────────────────────────────
        // HUD 가 아직 없어 상태를 볼 방법이 없다.
        // 재생 중 인스펙터에서 GameRoot 컴포넌트 우클릭 → 아래 항목으로 확인한다.

        [ContextMenu("상태 로그")]
        private void LogState()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[GameRoot] 재생 중에만 동작합니다.");
                return;
            }

            if (PlayerState == null || !PlayerState.IsBound)
            {
                Debug.LogError("[GameRoot] PlayerState 가 정의에 연결되지 않았습니다. PlayerStatsDefinition 을 확인하세요.");
                return;
            }

            Debug.Log(
                $"[GameRoot] Lv {PlayerState.Level}  EXP {PlayerState.Exp}/{PlayerState.ExpToNextLevel}  " +
                $"HP {PlayerState.CurrentHp:0.#}/{PlayerState.MaxHp:0.#}\n" +
                $"  스탯 : {PlayerState.Stats}\n" +
                $"  신규 플레이 : {IsNewGame}\n" +
                $"  세이브 : {Saves.SavePath}  (존재 {Saves.Exists})");
        }

        [ContextMenu("경험치 +100")]
        private void DebugAddExp()
        {
            if (!Application.isPlaying) return;

            PlayerState?.AddExp(100);
            LogState();
        }

        [ContextMenu("피해 10")]
        private void DebugDamage()
        {
            if (!Application.isPlaying) return;

            PlayerState?.ApplyDamage(10f);
            LogState();
        }

        [ContextMenu("지금 저장")]
        private void DebugSave()
        {
            if (!Application.isPlaying) return;

            SaveNow();
            Debug.Log($"[GameRoot] 저장했습니다 → {Saves.SavePath}");
        }

        [ContextMenu("세이브 삭제")]
        private void DebugDeleteSave()
        {
            Saves ??= new SaveService();
            Saves.Delete();
            Debug.Log("[GameRoot] 세이브를 삭제했습니다. 다음 실행이 신규 플레이가 됩니다.");
        }
    }
}
