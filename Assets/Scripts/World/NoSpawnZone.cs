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

        /// <summary>한 번의 퍼뜨리기가 덮을 수 있는 칸의 상한. 실수로 층 전체를 칠했을 때의 방어선.</summary>
        private const int MaxRegionCells = 4096;

        private AreaAnchor anchor;
        private float pollTimer;

        /// <summary>
        /// 타일맵 마스크에서 이미 들킨 칸들.
        ///
        /// <b>해제가 칸 단위인 이유</b> — 한 타일맵에 층의 히든 방을 전부 칠하면
        /// 컴포넌트 하나가 방 여럿을 덮는다. 해제를 컴포넌트 단위로 두면
        /// <b>A 방에 들어간 것만으로 B 방까지 열린다.</b>
        /// 그래서 들어간 자리에서 <b>이어진 칸만</b> 퍼뜨려 연다 —
        /// 떨어져 있는 방은 자연히 따로 남는다.
        /// </summary>
        private readonly HashSet<Vector3Int> revealedCells = new HashSet<Vector3Int>();

        /// <summary>사각형 방식에서 들켰는가. 사각형 하나는 방 하나이므로 통째로 열린다.</summary>
        public bool Revealed { get; private set; }

        public bool BlocksMonsters => blocksMonsters;

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
            if (!ServiceRegistry.TryGet(out WorldProgress progress) || progress == null || anchor == null) return;

            if (mask == null)
            {
                Revealed = progress.IsZoneRevealed(anchor.AreaId, SaveKey);
                return;
            }

            // 저장된 것은 방마다 씨앗 한 칸이다. 거기서 다시 퍼뜨려 방을 되살린다.
            revealedCells.Clear();

            foreach (Vector2Int seed in progress.RevealedZonesIn(anchor.AreaId))
                RevealRegion(new Vector3Int(seed.x, seed.y, 0), record: false);
        }

        private void OnDisable() => Active.Remove(this);

        private void Update()
        {
            if (!revealOnPlayerEnter) return;

            // 사각형은 한 번 열리면 끝이다. 타일맵은 방마다 따로 열리므로 계속 본다.
            if (mask == null && Revealed) return;

            pollTimer -= Time.deltaTime;
            if (pollTimer > 0f) return;
            pollTimer = PollInterval;

            if (!ServiceRegistry.TryGet(out PlayerController player) || player == null) return;

            Vector2 where = player.transform.position;
            if (!BlocksMonsterHere(where)) return;

            Reveal(where);
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

        /// <summary>
        /// 지금 <b>몬스터를 막는</b> 자리인가. 도형 안이면서 아직 안 들킨 곳이다.
        ///
        /// 오브젝트 쪽은 이걸 쓰지 않는다 — 들켜도 계속 막는다.
        /// 배치는 그 층의 지형이라 다시 왔을 때 없던 바위가 생기면 안 된다.
        /// </summary>
        private bool BlocksMonsterHere(Vector2 world)
        {
            if (!blocksMonsters) return false;

            if (mask == null) return !Revealed && WorldRect.Contains(world);

            Vector3Int cell = mask.WorldToCell(world);
            return mask.HasTile(cell) && !revealedCells.Contains(cell);
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
        public void Reveal() => Reveal(transform.position);

        /// <summary>
        /// <paramref name="where"/> 에 있는 방의 봉인을 푼다.
        ///
        /// <b>타일맵 마스크에서는 그 자리에서 이어진 칸만 연다.</b>
        /// 층의 히든 방을 한 타일맵에 다 칠했더라도 떨어져 있는 방은 따로 남는다.
        /// </summary>
        public void Reveal(Vector2 where)
        {
            if (mask != null)
            {
                RevealRegion(mask.WorldToCell(where), record: true);
                return;
            }

            RevealSelf();

            if (string.IsNullOrEmpty(roomId)) return;

            foreach (NoSpawnZone zone in Active)
                if (zone != null && zone != this && zone.roomId == roomId)
                    zone.RevealSelf();
        }

        /// <summary>
        /// 씨앗 칸에서 이어진 칸을 전부 연다. 상하좌우로만 퍼진다 —
        /// 대각선까지 이으면 모서리만 닿은 두 방이 하나로 묶인다.
        /// 방을 가르고 싶으면 <b>한 칸 띄워 칠한다.</b>
        /// </summary>
        private void RevealRegion(Vector3Int seed, bool record)
        {
            if (mask == null || !mask.HasTile(seed) || revealedCells.Contains(seed)) return;

            var frontier = new Queue<Vector3Int>();

            frontier.Enqueue(seed);
            revealedCells.Add(seed);

            while (frontier.Count > 0 && revealedCells.Count < MaxRegionCells)
            {
                Vector3Int cell = frontier.Dequeue();

                for (int i = 0; i < Neighbours.Length; i++)
                {
                    Vector3Int next = cell + Neighbours[i];

                    if (!mask.HasTile(next) || !revealedCells.Add(next)) continue;

                    frontier.Enqueue(next);
                }
            }

            if (revealedCells.Count >= MaxRegionCells)
                Debug.LogWarning(
                    $"[NoSpawnZone] '{name}' 의 방이 {MaxRegionCells}칸을 넘어 멈췄습니다. " +
                    "마스크 타일맵에 통로나 바닥까지 칠하지 않았는지 확인하세요.", this);

            if (!record || anchor == null) return;
            if (!ServiceRegistry.TryGet(out WorldProgress progress) || progress == null) return;

            // 방 전체가 아니라 씨앗 한 칸만 남긴다. 복원할 때 여기서 다시 퍼뜨린다.
            progress.MarkZoneRevealed(anchor.AreaId, new Vector2Int(seed.x, seed.y));
        }

        private static readonly Vector3Int[] Neighbours =
        {
            Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down
        };

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
                if (zone != null && zone.BlocksMonsterHere(world)) return true;
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
