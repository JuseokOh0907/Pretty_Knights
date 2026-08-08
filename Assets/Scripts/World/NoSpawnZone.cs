using System.Collections.Generic;
using PrettyKnights.Characters;
using PrettyKnights.Core;
using PrettyKnights.Save;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PrettyKnights.World
{
    /// <summary>
    /// 무언가를 뿌리면 안 되는 구역. 히든 방이 첫 사례다.
    ///
    /// <b><see cref="WalkableArea"/> 를 건드리지 않는 것이 핵심이다.</b>
    /// 그쪽은 "여기 설 수 있는가" 라는 지형의 사실을 답하는 자리이고,
    /// 히든 방은 부수고 들어가면 실제로 설 수 있는 곳이다.
    /// 거기서 배제하면 세이브 복원·포탈 도착 보정·그리드 A* 가 전부
    /// 히든 방을 "갈 수 없는 곳" 으로 읽어 버린다.
    ///
    /// 막고 싶은 것은 지형이 아니라 <b>뿌리는 행위</b>이므로 층위를 따로 둔다.
    ///
    /// <code>
    /// NoSpawnZone  →  배치 도구        오브젝트를 안 뿌린다
    ///              →  FloorPopulation  몬스터를 안 뿌린다 (들키기 전까지)
    ///              →  WalkableArea     모른다 (이동·복원·경로탐색 영향 없음)
    /// </code>
    ///
    /// <b>몬스터 차단이 더 급하다.</b> 봉인된 히든 방에 몬스터가 스폰되면
    /// 몬스터는 오브젝트를 부술 수 없어 영영 갇히고, 그 상태로 층 인구 상한을
    /// 계속 차지한다. 토템 지분 모델이 그대로 샌다.
    ///
    /// <b>물리를 쓰지 않는다</b> (유지). 콜라이더를 두면 레이어를 맞춰야 하고
    /// 실수로 무언가와 부딪히게 된다. 플레이어가 들어왔는지는 같은 도형 판정을
    /// 낮은 빈도로 물어서 안다 — 트리거와 결과가 같고 딸려오는 것이 없다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NoSpawnZone : MonoBehaviour
    {
        /// <summary>플레이어가 들어왔는지 얼마나 자주 보는지. 방에 들어가는 일은 드물다.</summary>
        private const float PollInterval = 0.25f;

        [Header("모양 — 타일맵이 있으면 그쪽이 이긴다")]
        [SerializeField, Tooltip(
            "칠한 칸이 곧 구역이다. 직사각형이 아닌 방은 이걸 쓴다. " +
            "맵을 그리는 것과 같은 손놀림이고 판정도 칸 조회 한 번이라 더 싸다")]
        private Tilemap mask;

        [SerializeField, Tooltip("타일맵을 비웠을 때 쓸 사각형 크기 (월드 유닛). 중심은 이 오브젝트의 위치")]
        private Vector2 size = new Vector2(8f, 6f);

        [Header("무엇을 막는가")]
        [SerializeField, Tooltip("이 안에 몬스터를 스폰하지 않는다. 들키면 풀린다")]
        private bool blocksMonsters = true;

        [SerializeField, Tooltip(
            "이 안에 배치 도구가 오브젝트를 뿌리지 않는다. " +
            "이쪽은 들켜도 풀리지 않는다 — 배치는 그 층의 지형이라 다시 와도 같아야 한다")]
        private bool blocksProps = true;

        [Header("봉인 해제")]
        [SerializeField, Tooltip("플레이어가 안에 들어오면 몬스터 차단을 푼다")]
        private bool revealOnPlayerEnter = true;

        [SerializeField, Tooltip(
            "같은 방을 덮는 구역들에 같은 이름을 준다. 하나가 들키면 함께 풀린다. " +
            "비우면 이 구역 하나만 따로 논다")]
        private string roomId = string.Empty;

        /// <summary>
        /// 활성 구역 목록. 층이 꺼지면 그 안의 구역도 함께 빠진다.
        /// 보통 층당 몇 개뿐이라 선형 검사로 충분하다.
        /// </summary>
        private static readonly List<NoSpawnZone> Active = new List<NoSpawnZone>();

        private AreaAnchor anchor;
        private float pollTimer;

        /// <summary>들켰는가. 들키면 몬스터가 다시 뿌려진다.</summary>
        public bool Revealed { get; private set; }

        public bool BlocksMonsters => blocksMonsters && !Revealed;

        /// <summary>배치는 그 층의 지형이므로 들켜도 풀리지 않는다.</summary>
        public bool BlocksProps => blocksProps;

        public Rect WorldRect
        {
            get
            {
                Vector2 center = transform.position;
                Vector2 half = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y)) * 0.5f;
                return new Rect(center - half, half * 2f);
            }
        }

        private void OnEnable()
        {
            Active.Add(this);

            anchor = GetComponentInParent<AreaAnchor>();

            // 이미 들킨 방은 다시 봉인되지 않는다. 층을 껐다 켤 때마다 되살아나면
            // 뚫고 들어간 성과가 사라진다.
            if (ServiceRegistry.TryGet(out WorldProgress progress) && progress != null && anchor != null)
                Revealed = progress.IsZoneRevealed(anchor.AreaId, SaveKey);
        }

        private void OnDisable() => Active.Remove(this);

        private void Update()
        {
            if (Revealed || !revealOnPlayerEnter) return;

            pollTimer -= Time.deltaTime;
            if (pollTimer > 0f) return;
            pollTimer = PollInterval;

            if (!ServiceRegistry.TryGet(out PlayerController player) || player == null) return;
            if (!Contains(player.transform.position)) return;

            Reveal();
        }

        // ── 모양 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 이 점이 구역 안인가.
        /// <b>타일맵이 있으면 칠한 칸이 곧 구역이다</b> — 사각형으로 못 덮는 방을 위한 것이고,
        /// 딕셔너리 조회 한 번이라 사각형 여러 개를 겹치는 것보다 오히려 싸다.
        /// </summary>
        public bool Contains(Vector2 world)
        {
            if (mask != null) return mask.HasTile(mask.WorldToCell(world));

            return WorldRect.Contains(world);
        }

        // ── 봉인 해제 ─────────────────────────────────────────────────────

        /// <summary>
        /// 봉인을 푼다. 플레이어가 들어오면 자동으로 불리고,
        /// <b>히든 상자를 열었을 때처럼 밖에서 불러도 된다.</b>
        ///
        /// 같은 <c>roomId</c> 를 가진 구역이 함께 풀린다 —
        /// 사각형 여러 개로 한 방을 덮었을 때 들어간 칸만 풀리면
        /// 나머지 절반에는 계속 몬스터가 안 나온다.
        /// </summary>
        public void Reveal()
        {
            RevealSelf();

            if (string.IsNullOrEmpty(roomId)) return;

            foreach (NoSpawnZone zone in Active)
                if (zone != null && zone != this && zone.roomId == roomId)
                    zone.RevealSelf();
        }

        private void RevealSelf()
        {
            if (Revealed) return;

            Revealed = true;

            if (anchor == null || !ServiceRegistry.TryGet(out WorldProgress progress) || progress == null) return;

            progress.MarkZoneRevealed(anchor.AreaId, SaveKey);
        }

        /// <summary>
        /// 세이브에 적을 이름표. <b><c>roomId</c> 가 있으면 그것을 쓴다</b> —
        /// 위치로만 저장하면 에디터에서 구역을 조금 옮겼을 때 기록이 끊긴다.
        /// 비어 있으면 칸 좌표로 대신한다 (부술 수 있는 벽과 같은 방식).
        /// </summary>
        private Vector2Int SaveKey =>
            string.IsNullOrEmpty(roomId)
                ? new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y))
                : new Vector2Int(roomId.GetHashCode(), 0);

        // ── 조회 ──────────────────────────────────────────────────────────

        /// <summary>몬스터를 여기 스폰해도 되는가. 활성 구역만 본다.</summary>
        public static bool BlocksMonsterAt(Vector2 world)
        {
            for (int i = 0; i < Active.Count; i++)
            {
                NoSpawnZone zone = Active[i];
                if (zone != null && zone.BlocksMonsters && zone.Contains(world)) return true;
            }

            return false;
        }

        /// <summary>
        /// 배치 도구용. 꺼진 층의 구역까지 봐야 하므로 목록이 아니라 인자로 받는다.
        /// 에디터에서 <c>FindObjectsByType(FindObjectsInactive.Include, ...)</c> 로 모은 것을 넘긴다.
        /// </summary>
        public static bool BlocksPropAt(Vector2 world, IReadOnlyList<NoSpawnZone> zones)
        {
            if (zones == null) return false;

            for (int i = 0; i < zones.Count; i++)
            {
                NoSpawnZone zone = zones[i];
                if (zone != null && zone.BlocksProps && zone.Contains(world)) return true;
            }

            return false;
        }

        private void OnDrawGizmos()
        {
            // 타일맵으로 모양을 준 구역은 칠한 칸이 에디터에 그대로 보이므로
            // 여기서 또 그리지 않는다. 사각형일 때만 상자를 그린다.
            if (mask != null) return;

            Rect rect = WorldRect;
            float alpha = Revealed ? 0.05f : 0.15f;

            Gizmos.color = new Color(1f, 0.4f, 0.8f, alpha);
            Gizmos.DrawCube(rect.center, rect.size);

            Gizmos.color = new Color(1f, 0.4f, 0.8f, Revealed ? 0.3f : 0.8f);
            Gizmos.DrawWireCube(rect.center, rect.size);
        }
    }
}
