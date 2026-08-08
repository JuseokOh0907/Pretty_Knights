using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrettyKnights.Save
{
    /// <summary>
    /// 세계에 남긴 흔적. 무엇을 부쉈고 어느 테마를 몇 번 클리어했는지.
    ///
    /// <b>오브젝트는 좌표가 아니라 인덱스로 저장한다.</b> 배치가 시드로 결정되므로
    /// 같은 시드면 생성 순서가 같고, i번째는 언제나 같은 것이다.
    /// 좌표보다 짧고, 부동소수 비교를 하지 않아 정확하다.
    ///
    /// <b>벽은 칸 좌표로 저장한다.</b> 타일맵은 손으로 그린 것이라 순서라는 개념이 없다.
    /// </summary>
    [Serializable]
    public sealed class WorldProgress
    {
        [Serializable]
        public struct PropRecord
        {
            public int areaId;
            public int index;

            /// <summary>
            /// 메인 토템인지. <b>테마를 옮길 때 이것만 남긴다</b> —
            /// 되살아나면 이미 뚫어둔 포탈이 다시 닫혀 같은 일을 반복해야 한다.
            /// </summary>
            public bool mainTotem;
        }

        [Serializable]
        public struct WallRecord
        {
            public int areaId;
            public int x;
            public int y;
        }

        [Serializable]
        public struct ThemeRecord
        {
            public int theme;
            public int clearCount;
        }

        /// <summary>
        /// 들켜서 봉인이 풀린 히든 방. <b>벽과 같은 방식으로 칸 좌표를 쓴다</b> —
        /// 손으로 놓은 것이라 순서라는 개념이 없다.
        /// </summary>
        [Serializable]
        public struct ZoneRecord
        {
            public int areaId;
            public int x;
            public int y;
        }

        [SerializeField] private List<PropRecord> destroyedProps = new List<PropRecord>();
        [SerializeField] private List<WallRecord> brokenWalls = new List<WallRecord>();
        [SerializeField] private List<ThemeRecord> themes = new List<ThemeRecord>();
        [SerializeField] private List<ZoneRecord> revealedZones = new List<ZoneRecord>();

        // ── 오브젝트 ──────────────────────────────────────────────────────

        public bool IsPropDestroyed(int areaId, int index)
        {
            foreach (PropRecord record in destroyedProps)
                if (record.areaId == areaId && record.index == index) return true;

            return false;
        }

        public void MarkPropDestroyed(int areaId, int index, bool mainTotem)
        {
            if (IsPropDestroyed(areaId, index)) return;

            destroyedProps.Add(new PropRecord { areaId = areaId, index = index, mainTotem = mainTotem });
        }

        // ── 부술 수 있는 벽 ───────────────────────────────────────────────

        public bool IsWallBroken(int areaId, Vector3Int cell)
        {
            foreach (WallRecord record in brokenWalls)
                if (record.areaId == areaId && record.x == cell.x && record.y == cell.y) return true;

            return false;
        }

        public void MarkWallBroken(int areaId, Vector3Int cell)
        {
            if (IsWallBroken(areaId, cell)) return;

            brokenWalls.Add(new WallRecord { areaId = areaId, x = cell.x, y = cell.y });
        }

        /// <summary>그 구역의 부서진 칸들. 층을 켤 때 복원에 쓴다.</summary>
        public IEnumerable<Vector3Int> BrokenWallsIn(int areaId)
        {
            foreach (WallRecord record in brokenWalls)
                if (record.areaId == areaId) yield return new Vector3Int(record.x, record.y, 0);
        }

        // ── 히든 방 봉인 ──────────────────────────────────────────────────

        /// <summary>
        /// 이 방이 이미 들켰는가. 들킨 방은 몬스터 스폰이 다시 열린다 —
        /// 뚫고 들어간 뒤에도 계속 비어 있으면 층의 일부가 죽은 공간이 된다.
        /// </summary>
        public bool IsZoneRevealed(int areaId, Vector2Int key)
        {
            foreach (ZoneRecord record in revealedZones)
                if (record.areaId == areaId && record.x == key.x && record.y == key.y) return true;

            return false;
        }

        public void MarkZoneRevealed(int areaId, Vector2Int key)
        {
            if (IsZoneRevealed(areaId, key)) return;

            revealedZones.Add(new ZoneRecord { areaId = areaId, x = key.x, y = key.y });
        }

        // ── 테마 ──────────────────────────────────────────────────────────

        /// <summary>완전 클리어 횟수. 재배치 시드가 된다.</summary>
        public int ClearCountOf(int theme)
        {
            foreach (ThemeRecord record in themes)
                if (record.theme == theme) return record.clearCount;

            return 0;
        }

        /// <summary>
        /// 테마를 떠났다 (아직 클리어하지 않음). <b>메인 토템만 남기고 지운다.</b>
        ///
        /// 오브젝트와 서브 토템이 되살아나 파밍이 다시 열리지만,
        /// 메인 토템은 부서진 채라 포탈은 열려 있다.
        /// <b>위치는 그대로다</b> — 시드가 안 바뀌므로 같은 배치가 나온다.
        /// </summary>
        public void SoftResetTheme(int theme)
        {
            destroyedProps.RemoveAll(r => r.areaId / 100 == theme && !r.mainTotem);
        }

        /// <summary>
        /// 테마를 완전히 클리어했다 (보상방을 나감).
        /// <b>전부 지우고 클리어 횟수를 올린다.</b> 그 횟수가 시드에 들어가므로
        /// 다음에 들어가면 배치가 새로 뽑힌다 — 새 회차라는 명확한 경계다.
        /// </summary>
        public void CompleteTheme(int theme)
        {
            destroyedProps.RemoveAll(r => r.areaId / 100 == theme);
            brokenWalls.RemoveAll(r => r.areaId / 100 == theme);

            // 벽이 되살아나므로 방도 다시 봉인된다. 여기를 빠뜨리면
            // 벽은 막혔는데 그 안에 몬스터가 차 있는 상태가 된다.
            revealedZones.RemoveAll(r => r.areaId / 100 == theme);

            for (int i = 0; i < themes.Count; i++)
            {
                if (themes[i].theme != theme) continue;

                ThemeRecord record = themes[i];
                record.clearCount++;
                themes[i] = record;
                return;
            }

            themes.Add(new ThemeRecord { theme = theme, clearCount = 1 });
        }

        public void Clear()
        {
            destroyedProps.Clear();
            brokenWalls.Clear();
            themes.Clear();
            revealedZones.Clear();
        }

        public override string ToString() =>
            $"부순 오브젝트 {destroyedProps.Count} · 부순 벽 {brokenWalls.Count} · " +
            $"들킨 히든 방 {revealedZones.Count} · 클리어 기록 {themes.Count}";
    }
}
