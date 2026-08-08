using PrettyKnights.Characters;
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

        [SerializeField, Tooltip("데미지 공식. 비우면 감쇠율 기본값으로 동작한다")]
        private CombatSettings combatSettings;

        [Header("시작 설정")]
        [SerializeField, Tooltip("신규 플레이일 때의 시작 모드. 저장된 위치가 있으면 그쪽이 우선한다")]
        private GameMode startMode = GameMode.Horizontal;
        [SerializeField, Tooltip("모바일에서 배터리를 아끼기 위한 상한. 0이면 건드리지 않음")]
        private int targetFrameRate = 60;

        [Header("디버그")]
        [SerializeField, Tooltip("시작과 씬 전환을 콘솔에 남긴다")]
        private bool logLifecycle = true;

        public static GameRoot Instance { get; private set; }

        public PlayerRuntimeState PlayerState { get; private set; }
        public SaveService Saves { get; private set; }
        public SceneFlow Scenes { get; private set; }

        /// <summary>마지막으로 있었던 자리. 씬이 올라온 뒤 여기로 몸을 옮긴다.</summary>
        public WorldLocation Location { get; private set; }

        /// <summary>무엇을 부쉈고 어느 테마를 클리어했는지.</summary>
        public WorldProgress Progress { get; private set; }

        /// <summary>이번 실행이 신규 플레이인지 (세이브 파일이 없었는지).</summary>
        public bool IsNewGame { get; private set; }

        /// <summary>디버그로 세이브를 지운 뒤 자동 저장이 되살리는 것을 막는다.</summary>
        private bool suppressAutoSave;

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
            // 저장된 모드가 있으면 그쪽으로 복귀한다. 없으면 인스펙터의 시작 모드.
            GameMode target = Location.HasValue ? Location.Mode : startMode;
            StartCoroutine(Scenes.SwitchTo(target));
        }

        /// <summary>
        /// 씬이 올라온 직후 저장된 자리로 몸을 옮긴다.
        /// 몸(<see cref="PlayerController"/>)은 게임플레이 씬에 있고 씬마다 새로 생기므로
        /// 매 전환마다 다시 찾아야 한다.
        /// </summary>
        private void RestorePlayerPosition(GameMode mode)
        {
            // 구역부터 켠다. 저장된 좌표가 없어도 이건 해야 한다 —
            // 층 13개가 좌표계를 공유하므로 엉뚱한 층이 켜져 있으면
            // 어디에 서 있든 벽 사이에 끼거나 카메라 경계 밖으로 밀려난다.
            ActivateSavedArea();

            if (!ServiceRegistry.TryGet(out PlayerController body) || body == null || body.Motor == null)
            {
                Debug.LogWarning(
                    "[GameRoot] 씬에서 플레이어를 찾지 못해 위치를 복원하지 못했습니다. " +
                    "Player 프리팹이 씬에 있는지 확인하세요.");
                return;
            }

            // 자리는 모드마다 따로 든다. 두 씬은 좌표계가 전혀 달라
            // 한쪽 좌표를 다른 쪽에 쓰면 맵 밖으로 튄다.
            bool saved = Location.TryGet(mode, out Vector2 destination, out Vector2 facing);
            if (!saved) destination = body.transform.position;

            if (!ResolveLanding(destination, out Vector2 landing))
            {
                Debug.LogError(
                    $"[GameRoot] {mode} 에서 설 수 있는 자리를 찾지 못했습니다 (기준 {destination}). " +
                    "그 자리에 그대로 둡니다 — 벽에 끼어 보이면 이 로그가 원인입니다.");
                return;
            }

            body.Motor.Warp(landing);

            // 바라보던 방향까지 되돌린다. snap 이라 보간 없이 즉시 그 방향으로 선다.
            // 이게 없으면 돌아올 때마다 정면(기본값)을 보게 되어 latch 규칙이 끊긴다.
            if (saved) body.AnimatorDriver?.ForceFacing(facing);

            if (logLifecycle)
                Debug.Log(
                    $"[GameRoot] 위치 복원 — {mode} {landing} " +
                    $"({(saved ? "저장된 자리" : "씬 기본 위치")})");
        }

        /// <summary>
        /// 저장된 구역을 켠다. 구역은 가로 모드에만 있으므로 세로에서는 조용히 지나간다.
        /// </summary>
        private void ActivateSavedArea()
        {
            if (Location.AreaId == 0) return;
            if (!ServiceRegistry.TryGet(out World.AreaRegistry areas) || areas == null) return;

            if (!areas.Activate(Location.AreaId))
                Debug.LogWarning(
                    $"[GameRoot] 저장된 구역 #{Location.AreaId} 을 씬에서 찾지 못했습니다. " +
                    "켜져 있는 층을 그대로 씁니다.");
        }

        /// <summary>
        /// 실제로 설 수 있는 자리를 고른다.
        ///
        /// <b>못 찾았을 때 그냥 포기하면 안 된다.</b> 그러면 몸이 씬에 적힌 기본 위치에
        /// 남는데, 그 자리는 켜져 있는 층 밖일 수 있다. 카메라는 그 층 경계에 묶여 있어
        /// <b>플레이어가 화면에서 사라진 것처럼 보인다.</b>
        /// 그래서 마지막 수단으로 그 구역의 도착 지점으로 데려간다.
        /// </summary>
        private static bool ResolveLanding(Vector2 wanted, out Vector2 landing)
        {
            landing = wanted;

            ServiceRegistry.TryGet(out World.WalkableArea area);

            // 통행 판정이 없는 씬(세로 등)은 원하는 자리를 그대로 쓴다.
            if (area == null || area.Floor == null) return true;

            if (area.TryFindWalkable(wanted, 3f, out landing)) return true;

            if (!ServiceRegistry.TryGet(out World.AreaRegistry areas) || areas?.Active == null) return false;

            World.ArrivalPoint arrival = areas.Active.ResolveArrival(null);
            if (arrival == null) return false;

            Debug.LogWarning(
                $"[GameRoot] {wanted} 주변에 설 자리가 없어 도착 지점 '{arrival.ArrivalId}' 로 보냅니다. " +
                "저장된 좌표가 지금 켜진 층 밖입니다.");

            landing = area.TryFindWalkable(arrival.Position, 3f, out Vector2 corrected)
                ? corrected
                : arrival.Position;

            return true;
        }

        /// <summary>현재 몸의 좌표와 방향을 <see cref="Location"/> 에 담는다. 저장 직전에 호출한다.</summary>
        private void CaptureLocation()
        {
            if (Scenes?.CurrentMode == null) return;
            if (!ServiceRegistry.TryGet(out PlayerController body) || body == null) return;

            GameMode current = Scenes.CurrentMode.Value;

            Vector2 facing = body.AnimatorDriver != null
                ? body.AnimatorDriver.FacingVector
                : Vector2.down;

            // 지금 모드의 슬롯에만 쓴다. 반대 모드의 자리는 그대로 남는다.
            Location.Set(current, body.transform.position, facing);
        }

        private void InitializeServices()
        {
            Saves = new SaveService();
            Scenes = new SceneFlow();

            IsNewGame = !Saves.TryLoad(out SaveData data);
            PlayerState = data.Player;
            Location = data.Location;
            Progress = data.Progress;

            // 슬롯이 하나였던 시절의 세이브를 모드별 슬롯으로 옮긴다.
            Location.MigrateLegacy();

            // 드랍 로그를 라이프사이클 로그와 같은 스위치에 묶는다.
            // 검증 중에는 켜 두고, 손맛을 볼 때는 꺼야 콘솔이 조용하다.
            RewardGrant.LogDrops = logLifecycle;

            // 공식이 확정되지 않아 SO 로 갈아 끼울 수 있게 두었다.
            // 비어 있어도 감쇠율 기본값으로 동작하므로 게임이 멈추지는 않는다.
            CombatSettings.Bind(combatSettings);

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
            ServiceRegistry.Register(Progress);
            ServiceRegistry.Register(Saves);
            ServiceRegistry.Register(Scenes);

            // 씬이 올라온 뒤라야 몸이 존재한다. 전환 완료 시점에 자리를 되돌린다.
            Scenes.ModeChanged += RestorePlayerPosition;

            if (!logLifecycle) return;

            Scenes.ModeChanged += mode => Debug.Log($"[GameRoot] 씬 전환 완료 — {mode}");
            Debug.Log($"[GameRoot] 시작 — {(IsNewGame ? "신규 플레이" : "이어하기")}\n{BuildStateReport()}");
        }

        /// <summary>상태 한 덩어리. 시작 로그와 컨텍스트 메뉴가 함께 쓴다.</summary>
        private string BuildStateReport()
        {
            if (PlayerState == null || !PlayerState.IsBound)
                return "  (PlayerStatsDefinition 이 연결되지 않아 스탯을 계산할 수 없습니다)";

            return
                $"  Lv {PlayerState.Level}  EXP {PlayerState.Exp}/{PlayerState.ExpToNextLevel}  " +
                $"HP {PlayerState.CurrentHp:0.#}/{PlayerState.MaxHp:0.#}\n" +
                $"  스탯 : {PlayerState.Stats}\n" +
                $"  위치 : {Location}\n" +
                $"  진행 : {Progress}\n" +
                $"  세이브 : {Saves.SavePath}  (존재 {Saves.Exists})";
        }

        /// <summary>현재 상태를 파일에 쓴다. 저장 지점마다 호출한다.</summary>
        public void SaveNow()
        {
            if (Saves == null || PlayerState == null || suppressAutoSave) return;

            CaptureLocation();
            Saves.TrySave(SaveData.From(PlayerState, Location, Progress));
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

            Debug.Log($"[GameRoot] 현재 상태 — {(IsNewGame ? "신규 플레이" : "이어하기")}\n{BuildStateReport()}");
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

            suppressAutoSave = false;   // 명시적 저장은 억제를 푼다
            SaveNow();
            Debug.Log($"[GameRoot] 저장했습니다 → {Saves.SavePath}");
        }

        [ContextMenu("세이브 삭제")]
        private void DebugDeleteSave()
        {
            Saves ??= new SaveService();
            Saves.Delete();
            Location?.Clear();
            Progress?.Clear();

            // 이걸 안 하면 재생을 멈추는 순간 OnApplicationQuit 이 다시 저장해
            // 방금 지운 것이 되살아난다.
            suppressAutoSave = true;

            Debug.Log(
                "[GameRoot] 세이브를 삭제했습니다. 다음 실행이 신규 플레이가 됩니다.\n" +
                "  이번 세션의 자동 저장은 꺼졌습니다 (지금 저장 메뉴로 다시 켤 수 있습니다).");
        }
    }
}
