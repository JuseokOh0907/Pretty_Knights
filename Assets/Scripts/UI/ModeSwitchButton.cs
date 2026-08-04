using PrettyKnights.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PrettyKnights.UI
{
    /// <summary>
    /// 세로 ↔ 가로를 오가는 버튼. 지금 모드의 반대쪽으로 전환한다.
    ///
    /// 전환 중에는 스스로 비활성화된다. 연타로 <see cref="SceneFlow.SwitchTo"/> 가
    /// 중첩 호출되면 씬이 꼬인다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    [DisallowMultipleComponent]
    public sealed class ModeSwitchButton : MonoBehaviour
    {
        [Header("표시 (선택)")]
        [SerializeField, Tooltip("가로로 전환 가능할 때 켜지는 오브젝트")]
        private GameObject toLandscapeIcon;

        [SerializeField, Tooltip("세로로 전환 가능할 때 켜지는 오브젝트")]
        private GameObject toPortraitIcon;

        private Button button;
        private SceneFlow scenes;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(Switch);
        }

        private void OnDestroy() => button.onClick.RemoveListener(Switch);

        private void Update()
        {
            if (scenes == null && !ServiceRegistry.TryGet(out scenes)) return;

            // 전환 중에는 누를 수 없다.
            button.interactable = !scenes.IsSwitching && scenes.CurrentMode.HasValue;

            if (!scenes.CurrentMode.HasValue) return;

            bool goingToLandscape = scenes.CurrentMode.Value == GameMode.Vertical;

            if (toLandscapeIcon != null) toLandscapeIcon.SetActive(goingToLandscape);
            if (toPortraitIcon != null) toPortraitIcon.SetActive(!goingToLandscape);
        }

        private void Switch()
        {
            if (scenes == null || scenes.IsSwitching || !scenes.CurrentMode.HasValue) return;
            if (!ServiceRegistry.TryGet(out GameRoot root) || root == null) return;

            GameMode next = scenes.CurrentMode.Value == GameMode.Vertical
                ? GameMode.Horizontal
                : GameMode.Vertical;

            // 전환 전에 현재 자리를 저장해 둔다. 돌아왔을 때 이어지도록.
            root.SaveNow();
            root.RequestMode(next);
        }
    }
}
