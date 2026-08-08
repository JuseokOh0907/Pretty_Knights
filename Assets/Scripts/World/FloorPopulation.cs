using System;
using System.Collections.Generic;
using PrettyKnights.Characters;
using PrettyKnights.Core;
using PrettyKnights.Data;
using UnityEngine;

namespace PrettyKnights.World
{
    /// <summary>
    /// <b>층 전체의 인구를 관리한다.</b> 스포너를 손으로 찍지 않는다.
    ///
    /// 2층은 사냥과 파밍의 층이라 넓다 (Goblin 2F 만 20,035칸).
    /// 그 면적에 스포너를 일일이 배치할 수 없으므로,
    /// <see cref="WalkableArea"/> 로 설 수 있는 자리를 물어 플레이어 주변에 뿌린다.
    ///
    /// 위치가 곧 설계인 1층 안내 몬스터와 3층 보스는 <see cref="MonsterSpawner"/> 를 쓴다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FloorPopulation : MonoBehaviour
    {
        /// <summary>어디에 뿌릴지 고르는 방식. 층마다 성격을 다르게 줄 수 있다.</summary>
        public enum Distribution
        {
            /// <summary>고리 안 아무 데나. 기본.</summary>
            Uniform,

            /// <summary>먼 쪽에 가중. 갑자기 몰려오는 느낌을 줄인다.</summary>
            FarWeighted,

            /// <summary>한 점 주변에 뭉쳐서. 무리 사냥.</summary>
            Clustered
        }

        [Serializable]
        public struct Entry
        {
            public MonsterDefinition definition;

            [Min(0f), Tooltip("뽑힐 가중치. 0이면 안 나온다")]
            public float weight;
        }

        [Header("무엇을")]
        [SerializeField] private MonsterController monsterPrefab;
        [SerializeField] private Entry[] entries = Array.Empty<Entry>();

        [Header("얼마나")]
        [SerializeField, Min(0), Tooltip("이 층에 동시에 살아 있을 수 있는 수")]
        private int targetPopulation = 12;

        [SerializeField, Min(0f), Tooltip("한 마리 채우는 간격")]
        private float spawnInterval = 2.5f;

        [Header("어디에 — 플레이어 기준 고리")]
        [SerializeField, Min(0f), Tooltip("이보다 가까이는 안 나온다. 눈앞에 튀어나오지 않게")]
        private float minSpawnDistance = 14f;

        [SerializeField, Min(0f), Tooltip("이보다 멀리는 안 나온다")]
        private float maxSpawnDistance = 26f;

        [SerializeField, Min(0f), Tooltip("이보다 멀어지면 회수한다. 0이면 회수 안 함")]
        private float despawnDistance = 45f;

        [SerializeField] private Distribution distribution = Distribution.Uniform;

        [SerializeField, Min(1), Tooltip("군집 방식에서 한 무리의 마릿수")]
        private int clusterSize = 3;

        [SerializeField, Min(0f), Tooltip("군집 방식에서 무리가 퍼지는 반경")]
        private float clusterRadius = 4f;

        private readonly List<MonsterController> alive = new List<MonsterController>();
        private float spawnTimer;
        private float totalWeight;

        private Vector2 clusterAnchor;
        private int clusterLeft;

        /// <summary>
        /// 토템이 얹은 지분의 합. 목표 인구는 이 값이다.
        ///
        /// <b>인스펙터의 <see cref="targetPopulation"/> 은 토템이 없는 층의 기본값이다.</b>
        /// 토템이 하나라도 등록되면 그쪽이 목표를 결정한다
        /// (docs/design/map-objects.md §1 — 메모리 할당 모델).
        /// </summary>
        private int totemShare;
        private bool hasTotems;

        /// <summary>
        /// 메인 토템이 부서졌는가. <b>지분을 0으로 만드는 대신 걸쇠로 둔다.</b>
        /// 0으로 밀면 그 뒤에 등록되는 서브 토템이 다시 채워 넣어 순서에 따라 답이 달라진다 —
        /// 복원 경로에서는 실제로 그 순서가 섞인다.
        /// </summary>
        private bool mainTotemBroken;

        /// <summary>
        /// 지금 목표 인구. 메인 토템이 부서졌으면 0, 토템이 있으면 지분 합,
        /// 토템이 없는 층이면 인스펙터 값.
        /// </summary>
        public int CurrentTarget =>
            mainTotemBroken ? 0 :
            hasTotems ? Mathf.Max(0, totemShare) : targetPopulation;

        /// <summary>
        /// 토템이 자기 지분을 얹거나 뺀다. <paramref name="amount"/> 가 음수면 부서진 것이다.
        ///
        /// <b>살아 있는 개체는 건드리지 않는다.</b> 목표만 줄고, 실제 수는
        /// 처치되면서 자연히 수렴한다. 아래 <see cref="Update"/> 가
        /// <c>alive.Count >= 목표</c> 일 때 채우기만 멈추므로 그대로 성립한다.
        /// </summary>
        public void AddShare(int amount)
        {
            hasTotems = true;
            totemShare += amount;
        }

        /// <summary>메인 토템이 부서졌다. 할당의 밑동이 사라져 더는 채우지 않는다.</summary>
        public void ClearTarget()
        {
            hasTotems = true;
            mainTotemBroken = true;
        }

        /// <summary>
        /// 지금 상태를 한 줄로. <b>HUD 가 없어 인구를 볼 방법이 이것뿐이다</b> —
        /// 재생 중 인스펙터에서 이 컴포넌트를 우클릭한다.
        /// </summary>
        [ContextMenu("인구 상태")]
        public void LogPopulation()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[FloorPopulation] 재생 중에만 의미가 있습니다.");
                return;
            }

            alive.RemoveAll(m => m == null || !m.gameObject.activeSelf);

            string source =
                mainTotemBroken ? "메인 토템이 부서져 0" :
                hasTotems ? $"토템 지분 합 {totemShare}" :
                $"인스펙터 값 {targetPopulation} (토템 없음)";

            Debug.Log(
                $"[FloorPopulation] '{name}' — 살아 있음 {alive.Count} / 목표 {CurrentTarget}\n" +
                $"  목표의 출처 : {source}\n" +
                $"  종류 {entries.Length}가지 · 가중치 합 {totalWeight:0.#}", this);
        }

        private void OnEnable()
        {
            totalWeight = 0f;
            foreach (Entry e in entries)
                if (e.definition != null) totalWeight += Mathf.Max(0f, e.weight);
        }

        private void OnDisable()
        {
            // 층이 꺼지면 전부 데려간다.
            foreach (MonsterController m in alive)
                if (m != null) m.gameObject.SetActive(false);

            alive.Clear();
            clusterLeft = 0;
        }

        private void Update()
        {
            if (!ServiceRegistry.TryGet(out PlayerController player) || player == null) return;

            Vector2 playerPos = player.transform.position;

            Recycle(playerPos);

            spawnTimer -= Time.deltaTime;
            if (spawnTimer > 0f) return;
            spawnTimer = spawnInterval;

            if (alive.Count >= CurrentTarget) return;

            SpawnOne(playerPos);
        }

        /// <summary>죽었거나 너무 멀어진 개체를 회수한다.</summary>
        private void Recycle(Vector2 playerPos)
        {
            for (int i = alive.Count - 1; i >= 0; i--)
            {
                MonsterController m = alive[i];

                if (m == null || !m.gameObject.activeSelf)
                {
                    alive.RemoveAt(i);
                    continue;
                }

                if (despawnDistance <= 0f) continue;
                if (Vector2.Distance(m.transform.position, playerPos) <= despawnDistance) continue;

                m.gameObject.SetActive(false);
                alive.RemoveAt(i);
            }
        }

        private void SpawnOne(Vector2 playerPos)
        {
            if (monsterPrefab == null || totalWeight <= 0f) return;
            if (!ServiceRegistry.TryGet(out WalkableArea area) || area == null) return;

            Vector2 wanted = PickPoint(playerPos);

            // 고른 자리가 벽이나 맵 밖일 수 있다. 주변에서 설 수 있는 곳을 찾는다.
            if (!area.TryFindWalkable(wanted, 4f, out Vector2 point)) return;

            // 히든 방처럼 봉인된 구역에는 뿌리지 않는다.
            // 들어가면 몬스터가 오브젝트를 부술 수 없어 영영 갇히고,
            // 그 상태로 층 인구 상한을 계속 차지해 토템 지분 모델이 샌다.
            if (NoSpawnZone.BlocksMonsterAt(point)) return;

            MonsterDefinition definition = PickDefinition();
            if (definition == null) return;

            MonsterController monster = Instantiate(monsterPrefab, point, Quaternion.identity, transform);
            monster.Spawn(definition, point);

            alive.Add(monster);
        }

        /// <summary>분포 방식에 따라 후보 지점을 고른다.</summary>
        private Vector2 PickPoint(Vector2 playerPos)
        {
            if (distribution == Distribution.Clustered)
            {
                // 무리가 남아 있으면 같은 자리 주변에 계속 붙인다.
                if (clusterLeft > 0)
                {
                    clusterLeft--;
                    return clusterAnchor + UnityEngine.Random.insideUnitCircle * clusterRadius;
                }

                clusterAnchor = RingPoint(playerPos, UnityEngine.Random.value);
                clusterLeft = clusterSize - 1;
                return clusterAnchor;
            }

            // FarWeighted 는 제곱해서 바깥쪽 확률을 높인다.
            float t = UnityEngine.Random.value;
            if (distribution == Distribution.FarWeighted) t = Mathf.Sqrt(t);

            return RingPoint(playerPos, t);
        }

        /// <summary>플레이어를 중심으로 한 고리 위의 한 점. <paramref name="t"/> 0=안쪽 1=바깥쪽.</summary>
        private Vector2 RingPoint(Vector2 center, float t)
        {
            float angle = UnityEngine.Random.value * Mathf.PI * 2f;
            float radius = Mathf.Lerp(minSpawnDistance, maxSpawnDistance, t);

            return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        private MonsterDefinition PickDefinition()
        {
            float roll = UnityEngine.Random.value * totalWeight;

            foreach (Entry e in entries)
            {
                if (e.definition == null) continue;

                roll -= Mathf.Max(0f, e.weight);
                if (roll <= 0f) return e.definition;
            }

            return null;
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;
            if (!ServiceRegistry.TryGet(out PlayerController player) || player == null) return;

            Vector3 p = player.transform.position;

            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(p, minSpawnDistance);
            Gizmos.DrawWireSphere(p, maxSpawnDistance);

            if (despawnDistance <= 0f) return;

            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
            Gizmos.DrawWireSphere(p, despawnDistance);
        }
    }
}
