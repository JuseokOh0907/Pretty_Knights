using System;
using PrettyKnights.Core;
using UnityEngine;

namespace PrettyKnights.Save
{
    /// <summary>
    /// 마지막으로 있었던 자리. 게임을 껐다 켜도 여기서 다시 시작한다.
    ///
    /// 방·층 개념은 아직 구현되지 않았으므로 지금은 모드와 좌표만 담는다.
    /// 던전이 붙으면 <c>floorIndex</c> / <c>roomId</c> 가 여기에 추가된다
    /// (docs/decisions/005-dungeon-and-monster-design.md).
    /// </summary>
    [Serializable]
    public sealed class WorldLocation
    {
        [SerializeField] private bool hasValue;
        [SerializeField] private GameMode mode = GameMode.Vertical;
        [SerializeField] private float x;
        [SerializeField] private float y;

        // 바라보던 방향. 손을 떼도 방향이 유지되는 latch 규칙(결정 001)을
        // 재시작 후에도 이어가려면 이것까지 저장해야 한다.
        [SerializeField] private float facingX;
        [SerializeField] private float facingY = -1f;

        /// <summary>한 번이라도 저장된 적이 있는지. false 면 씬의 기본 위치를 쓴다.</summary>
        public bool HasValue => hasValue;

        public GameMode Mode => mode;
        public Vector2 Position => new Vector2(x, y);

        /// <summary>바라보던 방향. 기본값은 정면(아래).</summary>
        public Vector2 Facing => new Vector2(facingX, facingY);

        public void Set(GameMode currentMode, Vector2 position, Vector2 facing)
        {
            mode = currentMode;
            x = position.x;
            y = position.y;

            // 0 벡터가 들어가면 블렌드 트리에서 방향이 뭉개진다. 들어온 값이 유효할 때만 갱신.
            if (facing.sqrMagnitude > 0.0001f)
            {
                facingX = facing.x;
                facingY = facing.y;
            }

            hasValue = true;
        }

        public void Clear() => hasValue = false;

        public override string ToString() =>
            hasValue
                ? $"{mode} ({x:0.##}, {y:0.##}) 방향 ({facingX:0.##}, {facingY:0.##})"
                : "(없음)";
    }
}
