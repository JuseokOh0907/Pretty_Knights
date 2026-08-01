using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PrettyKnights.Core
{
    /// <summary>
    /// 게임플레이 씬을 Additive 로 갈아 끼운다.
    /// Boot 씬은 절대 내리지 않으므로 <c>GameRoot</c> 와 캐릭터 데이터는 전환 중에도 살아 있다.
    ///
    /// 두 씬은 Build Settings 에 등록되어 있어야 한다
    /// (Ingame_Vertical / Ingame_Horizontal).
    /// </summary>
    public sealed class SceneFlow
    {
        private GameMode? current;

        public GameMode? CurrentMode => current;
        public bool IsSwitching { get; private set; }

        /// <summary>전환이 끝난 직후 발생한다. 인자는 새 모드.</summary>
        public event Action<GameMode> ModeChanged;

        /// <summary>
        /// <paramref name="mode"/> 로 전환한다. 이미 그 모드면 아무것도 하지 않는다.
        /// <c>GameRoot</c> 가 코루틴으로 돌린다.
        /// </summary>
        public IEnumerator SwitchTo(GameMode mode)
        {
            if (IsSwitching)
            {
                Debug.LogWarning("[SceneFlow] 전환 중에 다시 전환 요청이 들어와 무시합니다.");
                yield break;
            }

            if (current == mode) yield break;

            IsSwitching = true;

            // 1) 이전 게임플레이 씬을 내린다.
            if (current.HasValue)
            {
                string previous = current.Value.SceneName();
                Scene previousScene = SceneManager.GetSceneByName(previous);
                if (previousScene.isLoaded)
                {
                    AsyncOperation unload = SceneManager.UnloadSceneAsync(previousScene);
                    while (unload != null && !unload.isDone) yield return null;
                }
            }

            // 2) 새 씬을 올린다.
            string next = mode.SceneName();
            AsyncOperation load = SceneManager.LoadSceneAsync(next, LoadSceneMode.Additive);
            if (load == null)
            {
                Debug.LogError(
                    $"[SceneFlow] '{next}' 씬을 불러오지 못했습니다. Build Settings 등록 여부를 확인하세요.");
                IsSwitching = false;
                yield break;
            }

            while (!load.isDone) yield return null;

            Scene loaded = SceneManager.GetSceneByName(next);
            if (loaded.IsValid()) SceneManager.SetActiveScene(loaded);

            // 3) 씬을 내린 직후가 해제된 에셋을 회수하기 가장 안전한 시점이다.
            //    모바일 메모리 압박을 줄이기 위해 전환마다 수행한다.
            yield return Resources.UnloadUnusedAssets();

            Screen.orientation = mode.Orientation();

            current = mode;
            IsSwitching = false;
            ModeChanged?.Invoke(mode);
        }
    }
}
