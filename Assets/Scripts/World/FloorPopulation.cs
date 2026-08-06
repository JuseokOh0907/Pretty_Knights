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

            if (alive.Count >= targetPopulation) return;

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
