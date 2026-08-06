using System.Collections.Generic;
using UnityEngine;

namespace PrettyKnights.World
{
    /// <summary>
    /// 무언가를 뿌리면 안 되는 사각 구역. 히든 방이 첫 사례다.
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
    /// NoSpawnZone  →  배치 도구      오브젝트를 안 뿌린다
    ///              →  FloorPopulation  몬스터를 안 뿌린다
    ///              →  WalkableArea     모른다 (이동·복원·경로탐색 영향 없음)
    /// </code>
    ///
    /// <b>몬스터 차단이 더 급하다.</b> 봉인된 히든 방에 몬스터가 스폰되면
    /// 몬스터는 오브젝트를 부술 수 없어 영영 갇히고, 그 상태로 층 인구 상한을
    /// 계속 차지한다. 토템 지분 모델이 그대로 샌다.
    ///
    /// 물리를 쓰지 않는다. 콜라이더를 두면 레이어 설정을 맞춰야 하고
    /// 실수로 무언가와 부딪히게 된다. 사각형 하나면 충분하다.
    /// 모양이 복잡한 방은 이 컴포넌트를 여러 개 겹쳐 놓는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NoSpawnZone : MonoBehaviour
    {
        [SerializeField, Tooltip("구역 크기 (월드 유닛). 중심은 이 오브젝트의 위치")]
        private Vector2 size = new Vector2(8f, 6f);

        [SerializeField, Tooltip("이 안에 몬스터를 스폰하지 않는다")]
        private bool blocksMonsters = true;

        [SerializeField, Tooltip("이 안에 배치 도구가 오브젝트를 뿌리지 않는다")]
        private bool blocksProps = true;

        /// <summary>
        /// 활성 구역 목록. 층이 꺼지면 그 안의 구역도 함께 빠진다.
        /// 보통 층당 몇 개뿐이라 선형 검사로 충분하다.
        /// </summary>
        private static readonly List<NoSpawnZone> Active = new List<NoSpawnZone>();

        public bool BlocksMonsters => blocksMonsters;
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

        private void OnEnable() => Active.Add(this);

        private void OnDisable() => Active.Remove(this);

        public bool Contains(Vector2 world) => WorldRect.Contains(world);

        /// <summary>몬스터를 여기 스폰해도 되는가. 활성 구역만 본다.</summary>
        public static bool BlocksMonsterAt(Vector2 world)
        {
            for (int i = 0; i < Active.Count; i++)
            {
                NoSpawnZone zone = Active[i];
                if (zone != null && zone.blocksMonsters && zone.Contains(world)) return true;
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
                if (zone != null && zone.blocksProps && zone.Contains(world)) return true;
            }

            return false;
        }

        private void OnDrawGizmos()
        {
            Rect rect = WorldRect;

            Gizmos.color = new Color(1f, 0.4f, 0.8f, 0.15f);
            Gizmos.DrawCube(rect.center, rect.size);

            Gizmos.color = new Color(1f, 0.4f, 0.8f, 0.8f);
            Gizmos.DrawWireCube(rect.center, rect.size);
        }
    }
}
