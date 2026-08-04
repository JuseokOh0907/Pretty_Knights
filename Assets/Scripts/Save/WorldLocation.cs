using System;
using PrettyKnights.Core;
using UnityEngine;

namespace PrettyKnights.Save
{
    /// <summary>
    /// 마지막으로 있었던 자리. 게임을 껐다 켜도 여기서 다시 시작한다.
    ///
    /// <b>좌표만으로는 부족하다.</b> 층 9개가 한 씬에 겹쳐 있고 좌표계도 겹치므로,
    /// 어느 구역에 있었는지를 함께 저장하지 않으면 재시작 시
    /// 엉뚱한 구역의 같은 좌표에 서게 된다 (docs/TODO.md).
    /// </summary>
    [Serializable]
    public sealed class WorldLocation
    {
        [SerializeField] private bool hasValue;
        [SerializeField] private GameMode mode = GameMode.Vertical;
        [SerializeField] private float x;
        [SerializeField] private float y;

        // 어느 구역이었는지. 0 은 미지정이며 구역 기록이 없던 옛 세이브가 이 값을 갖는다.
        // 그 경우 씬에서 켜져 있는 층을 그대로 쓴다.
        [SerializeField] private int areaId;

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

        /// <summary>마지막으로 있었던 구역. 0 이면 기록이 없다.</summary>
        public int AreaId => areaId;

        /// <summary>
        /// 구역만 갱신한다. 좌표는 그대로 둔다 —
        /// 포탈이 몸을 옮긴 직후에는 <see cref="Set"/> 가 아직 불리지 않았기 때문이다.
        /// </summary>
        public void SetArea(int id)
        {
            areaId = id;
            hasValue = true;
        }

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

        public void Clear()
        {
            hasValue = false;
            areaId = 0;
        }

        public override string ToString() =>
            hasValue
                ? $"{mode} 구역#{areaId} ({x:0.##}, {y:0.##}) 방향 ({facingX:0.##}, {facingY:0.##})"
                : "(없음)";
    }
}
