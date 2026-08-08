using System;
using PrettyKnights.Core;
using UnityEngine;

namespace PrettyKnights.Save
{
    /// <summary>
    /// 마지막으로 있었던 자리. 게임을 껐다 켜도, 모드를 오갔다 돌아와도 여기서 다시 시작한다.
    ///
    /// <b>좌표만으로는 부족하다.</b> 층 13개가 한 씬에 겹쳐 있고 좌표계도 겹치므로,
    /// 어느 구역에 있었는지를 함께 저장하지 않으면 엉뚱한 구역의 같은 좌표에 서게 된다.
    ///
    /// <b>자리는 모드마다 따로 든다</b> (2026-08-09). 슬롯 하나에 모드까지 함께 담았더니
    /// 세로로 갔다가 돌아올 때 가로 좌표가 세로 좌표로 덮여 사라졌다.
    /// 두 씬은 좌표계가 전혀 달라 한쪽 값을 다른 쪽에 쓸 수도 없다 —
    /// 그래서 겹쳐 쓰는 것이 아니라 나란히 든다.
    /// </summary>
    [Serializable]
    public sealed class WorldLocation
    {
        /// <summary>모드 하나에서 마지막으로 있던 자리.</summary>
        [Serializable]
        private struct Spot
        {
            public bool hasValue;
            public float x;
            public float y;

            // 바라보던 방향. 손을 떼도 방향이 유지되는 latch 규칙(결정 001)을
            // 재시작·모드 전환 뒤에도 이어가려면 이것까지 저장해야 한다.
            public float facingX;
            public float facingY;

            public Vector2 Position => new Vector2(x, y);

            public Vector2 Facing =>
                facingX * facingX + facingY * facingY > 0.0001f
                    ? new Vector2(facingX, facingY)
                    : Vector2.down;

            public override string ToString() =>
                hasValue ? $"({x:0.##}, {y:0.##})" : "(없음)";
        }

        [SerializeField] private bool hasValue;

        [SerializeField, Tooltip("마지막으로 있던 모드. 시작할 때 어느 씬을 열지를 정한다")]
        private GameMode mode = GameMode.Vertical;

        [SerializeField] private Spot vertical;
        [SerializeField] private Spot horizontal;

        // 어느 구역이었는지. 0 은 미지정이며 구역 기록이 없던 옛 세이브가 이 값을 갖는다.
        // 구역은 가로 모드에만 있으므로 모드별로 나누지 않는다.
        [SerializeField] private int areaId;

        // ── 옛 세이브 호환 ────────────────────────────────────────────────
        // 슬롯이 하나였던 시절의 필드다. 읽기만 하고 새로 쓰지 않는다.
        // JsonUtility 는 파일에 없는 필드를 기본값으로 두므로 그대로 공존한다.
        [SerializeField] private float x;
        [SerializeField] private float y;
        [SerializeField] private float facingX;
        [SerializeField] private float facingY;

        /// <summary>한 번이라도 저장된 적이 있는지. false 면 씬의 기본 위치를 쓴다.</summary>
        public bool HasValue => hasValue;

        /// <summary>마지막으로 있던 모드. 시작 시 어느 씬을 열지를 정한다.</summary>
        public GameMode Mode => mode;

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

        /// <summary>
        /// <paramref name="currentMode"/> 의 자리를 기록한다.
        /// <b>다른 모드의 자리는 건드리지 않는다.</b>
        /// </summary>
        public void Set(GameMode currentMode, Vector2 position, Vector2 facing)
        {
            mode = currentMode;
            hasValue = true;

            ref Spot spot = ref SpotOf(currentMode);

            spot.hasValue = true;
            spot.x = position.x;
            spot.y = position.y;

            // 0 벡터가 들어가면 블렌드 트리에서 방향이 뭉개진다. 들어온 값이 유효할 때만 갱신.
            if (facing.sqrMagnitude <= 0.0001f) return;

            spot.facingX = facing.x;
            spot.facingY = facing.y;
        }

        /// <summary>
        /// <paramref name="targetMode"/> 에서 마지막으로 있던 자리.
        /// 그 모드에 간 적이 없으면 <c>false</c> — 씬의 기본 위치를 쓰라는 뜻이다.
        /// </summary>
        public bool TryGet(GameMode targetMode, out Vector2 position, out Vector2 facing)
        {
            Spot spot = SpotOf(targetMode);

            position = spot.Position;
            facing = spot.Facing;

            return spot.hasValue;
        }

        /// <summary>
        /// 슬롯이 하나였던 세이브를 새 구조로 옮긴다. 불러온 직후 한 번 부른다.
        ///
        /// <b>옛 파일에는 "그때 있던 모드의 자리" 하나뿐이다.</b> 반대 모드의 자리는
        /// 애초에 저장된 적이 없으므로 비워 둔다 — 그쪽은 씬 기본 위치에서 시작한다.
        /// </summary>
        public void MigrateLegacy()
        {
            if (!hasValue) return;
            if (vertical.hasValue || horizontal.hasValue) return;

            // 옛 필드가 전부 0 이면 실제로 저장된 적이 없는 것이다 (구역만 기록된 경우 등).
            bool empty = Mathf.Approximately(x, 0f) && Mathf.Approximately(y, 0f);
            if (empty) return;

            Set(mode, new Vector2(x, y), new Vector2(facingX, facingY));

            Debug.Log($"[WorldLocation] 옛 세이브의 자리를 {mode} 슬롯으로 옮겼습니다 — ({x:0.##}, {y:0.##})");
        }

        public void Clear()
        {
            hasValue = false;
            areaId = 0;
            vertical = default;
            horizontal = default;
            x = y = facingX = facingY = 0f;
        }

        /// <summary>
        /// 모드에 해당하는 슬롯. <c>ref</c> 로 돌려주므로 <see cref="Set"/> 가 제자리에서 고친다 —
        /// 구조체를 값으로 받아 고치면 필드에 반영되지 않는다.
        /// </summary>
        private ref Spot SpotOf(GameMode target)
        {
            if (target == GameMode.Vertical) return ref vertical;
            return ref horizontal;
        }

        public override string ToString() =>
            hasValue
                ? $"마지막 {mode} · 구역#{areaId} · 가로 {horizontal} · 세로 {vertical}"
                : "(없음)";
    }
}
