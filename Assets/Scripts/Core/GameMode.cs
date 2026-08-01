namespace PrettyKnights.Core
{
    /// <summary>
    /// 두 개의 플레이 리듬. 각각 Additive 로 적재되는 게임플레이 씬 하나에 대응한다.
    /// </summary>
    public enum GameMode
    {
        /// <summary>세로 — 자동 사냥 / 성장 관리. 기본 진입 화면.</summary>
        Vertical,

        /// <summary>가로 — 직접 조작 / 탐험 / 보스.</summary>
        Horizontal
    }

    public static class GameModeExtensions
    {
        public const string VerticalSceneName = "Ingame_Vertical";
        public const string HorizontalSceneName = "Ingame_Horizontal";

        public static string SceneName(this GameMode mode) => mode switch
        {
            GameMode.Vertical => VerticalSceneName,
            GameMode.Horizontal => HorizontalSceneName,
            _ => VerticalSceneName
        };

        /// <summary>해당 모드가 요구하는 화면 방향.</summary>
        public static UnityEngine.ScreenOrientation Orientation(this GameMode mode) => mode switch
        {
            GameMode.Vertical => UnityEngine.ScreenOrientation.Portrait,
            GameMode.Horizontal => UnityEngine.ScreenOrientation.LandscapeLeft,
            _ => UnityEngine.ScreenOrientation.Portrait
        };
    }
}
