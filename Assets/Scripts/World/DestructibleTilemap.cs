using System;
using System.Collections.Generic;
using PrettyKnights.Combat;
using PrettyKnights.Core;
using PrettyKnights.Data;
using PrettyKnights.Save;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PrettyKnights.World
{
    /// <summary>
    /// 부술 수 있는 벽. 히든 방을 막는 데 쓴다.
    ///
    /// <b><c>Guide</c> 와 같은 타일을 쓰되 레이어를 가른다.</b> 겉보기가 같아야
    /// "때려보고 부술 수 있는지 알아내는" 재미가 성립하고, 레이어를 가르면
    /// <b>타일이 있는 곳이 곧 부술 수 있는 곳</b>이라 별도 목록을 들 필요가 없다.
    /// 한 타일맵에 섞으면 그 목록이 맵을 다시 그릴 때 조용히 어긋난다.
    ///
    /// 층마다 이 타일맵 하나에 그 층의 모든 히든 방 벽을 담는다.
    /// 칸별 HP 를 딕셔너리로 들기 때문에 방이 몇 개든 각 칸이 독립적으로 부서진다.
    ///
    /// <b>부서지면 <c>SetTile(cell, null)</c> 로 지운다.</b>
    /// <c>TilemapCollider2D</c> 가 알아서 갱신되므로 통행이 열리는 것을 따로 처리하지 않는다.
    ///
    /// ⚠ <b><c>Guide</c> 와 같은 칸에 겹치면 안 된다.</b> 부숴도 아래 콜라이더가 남아
    /// 못 지나가는데 겉보기엔 벽이 사라져 원인을 찾기 어렵다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Tilemap))]
    public sealed class DestructibleTilemap : MonoBehaviour, IAreaDamageable
    {
        [Header("수치")]
        [SerializeField, Min(1f), Tooltip("칸 하나의 HP. 벽 한 칸만 뚫으면 지나갈 수 있다")]
        private float hpPerCell = 60f;

        [SerializeField, Min(0f), Tooltip("데미지 공식에 들어갈 방어력")]
        private float defense;

        [Header("손상 표현")]
        [SerializeField, Tooltip("맞을수록 이 색에 가까워진다. 데미지가 들어간다는 신호다")]
        private Color damagedTint = new Color(0.45f, 0.35f, 0.35f, 1f);

        /// <summary>칸이 부서진 순간. 세이브가 듣는다.</summary>
        public event Action<DestructibleTilemap, Vector3Int> CellBroken;

        private Tilemap map;
        private AreaAnchor anchor;
        private readonly Dictionary<Vector3Int, float> damage = new Dictionary<Vector3Int, float>();

        public Tilemap Map => map != null ? map : map = GetComponent<Tilemap>();

        private void Awake()
        {
            map = GetComponent<Tilemap>();
            anchor = GetComponentInParent<AreaAnchor>();
        }

        private void OnEnable()
        {
            // 부숴둔 칸을 되돌린다. 층을 껐다 켤 때마다 벽이 되살아나면
            // 히든 방을 찾아낸 성과가 사라진다.
            if (!ServiceRegistry.TryGet(out WorldProgress progress) || progress == null) return;
            if (anchor == null) return;

            RestoreBroken(progress.BrokenWallsIn(anchor.AreaId));
        }

        // ── IAreaDamageable ───────────────────────────────────────────────

        public void ApplyAreaDamage(
            SkillShapeKind kind, SkillShapeParams param,
            Vector2 origin, Vector2 facing, float attack)
        {
            if (map == null) return;

            float perCell = CombatSettings.Damage(attack, defense);
            if (perCell <= 0f) return;

            // 범위의 경계 상자만 훑는다. 맵 전체를 도는 것보다 압도적으로 싸다.
            Rect local = SkillShape.LocalBounds(kind, param, facing);
            Vector3Int min = map.WorldToCell(new Vector3(origin.x + local.xMin, origin.y + local.yMin));
            Vector3Int max = map.WorldToCell(new Vector3(origin.x + local.xMax, origin.y + local.yMax));

            for (int y = min.y; y <= max.y; y++)
            {
                for (int x = min.x; x <= max.x; x++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    if (!map.HasTile(cell)) continue;

                    // 칸 한가운데로 판정한다. 인디케이터를 구울 때와 같은 함수다.
                    Vector3 center = map.GetCellCenterWorld(cell);
                    if (!SkillShape.Contains(kind, param, origin, facing, center)) continue;

                    DamageCell(cell, perCell);
                }
            }
        }

        // ── 파괴 ──────────────────────────────────────────────────────────

        private void DamageCell(Vector3Int cell, float amount)
        {
            damage.TryGetValue(cell, out float dealt);
            dealt += amount;

            if (dealt >= hpPerCell)
            {
                BreakCell(cell, silent: false);
                return;
            }

            damage[cell] = dealt;
            Tint(cell, dealt / hpPerCell);
        }

        /// <summary>
        /// 칸을 지운다. <paramref name="silent"/> 면 이벤트를 쏘지 않는다 —
        /// 세이브에서 복원할 때 다시 기록하지 않기 위해서다.
        /// </summary>
        public void BreakCell(Vector3Int cell, bool silent)
        {
            if (map == null || !map.HasTile(cell)) return;

            map.SetTile(cell, null);
            damage.Remove(cell);

            if (silent) return;

            if (anchor != null && ServiceRegistry.TryGet(out WorldProgress progress) && progress != null)
                progress.MarkWallBroken(anchor.AreaId, cell);

            CellBroken?.Invoke(this, cell);
        }

        /// <summary>
        /// 맞은 정도를 색으로 보여준다.
        ///
        /// <b><c>SetTileFlags</c> 를 먼저 부르지 않으면 아무 일도 일어나지 않는다.</b>
        /// <c>Tile</c> 의 기본 <c>flags</c> 가 <c>LockColor</c> 라 색 변경이 막혀 있고,
        /// 에러도 경고도 나지 않아 원인을 찾기 어렵다.
        /// </summary>
        private void Tint(Vector3Int cell, float ratio)
        {
            map.SetTileFlags(cell, TileFlags.None);
            map.SetColor(cell, Color.Lerp(Color.white, damagedTint, Mathf.Clamp01(ratio)));
        }

        /// <summary>세이브 복원용. 부서진 칸을 조용히 지운다.</summary>
        public void RestoreBroken(IEnumerable<Vector3Int> cells)
        {
            foreach (Vector3Int cell in cells) BreakCell(cell, silent: true);
        }

        /// <summary>남아 있는 칸 수. 디버그용.</summary>
        public int RemainingCells
        {
            get
            {
                if (map == null) return 0;

                int count = 0;
                map.CompressBounds();

                foreach (Vector3Int cell in map.cellBounds.allPositionsWithin)
                    if (map.HasTile(cell)) count++;

                return count;
            }
        }

        [ContextMenu("남은 칸 수")]
        private void LogRemaining() => Debug.Log($"[DestructibleTilemap] '{name}' 남은 칸 {RemainingCells}개");
    }
}
