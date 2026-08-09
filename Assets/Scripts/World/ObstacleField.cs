using System;
using System.Collections.Generic;
using PrettyKnights.Core;
using PrettyKnights.Characters;
using PrettyKnights.Data;
using UnityEngine;

namespace PrettyKnights.World
{
    /// <summary>
    /// 세로 모드의 파밍 대상을 계속 채운다. <b>몬스터가 아니라 장애물이다</b> (결정 009).
    ///
    /// <see cref="FloorPopulation"/> 과 짝을 이루는 구조다 — 저쪽이 몬스터를,
    /// 이쪽이 부술 것을 플레이어 주변 고리에 뿌리고 목표 수를 유지한다.
    /// <b>가로의 <see cref="FloorProps"/> 와는 목적이 정반대다.</b>
    /// 저쪽은 층의 지형을 만드는 것이라 "다시 와도 그대로" 여야 하고,
    /// 이쪽은 파밍 대상이라 <b>부수는 족족 새로 나와야</b> 한다.
    ///
    /// <b>토템은 다루지 않는다.</b> 세로에는 층도 포탈도 없으므로
    /// <see cref="PropRole.MainTotem"/> · <see cref="PropRole.SubTotem"/> 을 넣어도
    /// 부수면 그냥 보상만 준다. 넣지 않는 것을 권한다 — 지분 모델이 돌 곳이 없다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ObstacleField : MonoBehaviour
    {
        [Serializable]
        public struct Entry
        {
            public PropDefinition definition;

            [Min(0f), Tooltip("뽑힐 가중치. 0이면 안 나온다")]
            public float weight;
        }

        [Header("무엇을")]
        [SerializeField, Tooltip("Prop.prefab. 겉모습은 정의가 정하므로 하나면 된다")]
        private Destructible obstaclePrefab;

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();

        [Header("얼마나")]
        [SerializeField, Min(0), Tooltip("동시에 서 있을 수 있는 수")]
        private int targetCount = 8;

        [SerializeField, Min(0f), Tooltip("하나 채우는 간격 (초). 0이면 한 프레임에 다 채운다")]
        private float spawnInterval = 0.6f;

        [SerializeField, Min(0f), Tooltip(
            "부서진 뒤 자리를 비워 두는 시간. 0이면 부서지자마자 치운다. " +
            "부서진 그림을 잠깐 남기면 무엇을 부쉈는지 읽힌다")]
        private float debrisDuration = 1.2f;

        [Header("어디에 — 플레이어 기준 고리")]
        [SerializeField, Min(0f), Tooltip("이보다 가까이는 안 나온다. 눈앞에 튀어나오지 않게")]
        private float minSpawnDistance = 2.5f;

        [SerializeField, Min(0f), Tooltip("이보다 멀리는 안 나온다")]
        private float maxSpawnDistance = 7f;

        [SerializeField, Min(0f), Tooltip("이보다 멀어지면 회수한다. 0이면 회수 안 함")]
        private float despawnDistance = 14f;

        [SerializeField, Min(0f), Tooltip(
            "장애물끼리 이만큼은 떨어뜨린다. 겹쳐 놓으면 하나를 때릴 때 " +
            "뒤엣것이 가려 무엇을 부수는지 안 보인다")]
        private float minSeparation = 1.6f;

        [SerializeField, Min(1), Tooltip("자리를 못 찾으면 이만큼 다시 뽑아본다")]
        private int placementAttempts = 8;

        [Header("연결 (비우면 ServiceRegistry 에서 찾는다)")]
        [SerializeField, Tooltip("설 수 있는 자리를 물어볼 대상")]
        private WalkableArea area;

        [Header("디버그")]
        [SerializeField] private bool drawGizmos = true;

        /// <summary>서 있는 것과 부서져 치우기를 기다리는 것 전부.</summary>
        private readonly List<Standing> alive = new List<Standing>();

        private float spawnTimer;
        private float totalWeight;

        /// <summary>하나의 장애물과 그것이 부서진 시각.</summary>
        private struct Standing
        {
            public Destructible Body;

            /// <summary>부서진 시각. 0 이면 아직 멀쩡하다.</summary>
            public float BrokenAt;
        }

        private void OnEnable()
        {
            totalWeight = 0f;
            foreach (Entry e in entries)
                if (e.definition != null) totalWeight += Mathf.Max(0f, e.weight);

            if (totalWeight <= 0f)
                Debug.LogWarning($"[ObstacleField] '{name}' 에 뽑을 수 있는 항목이 없습니다.", this);
        }

        private void OnDisable()
        {
            foreach (Standing s in alive)
                if (s.Body != null) s.Body.gameObject.SetActive(false);

            alive.Clear();
        }

        private void Update()
        {
            if (!ServiceRegistry.TryGet(out PlayerController player) || player == null) return;

            Vector2 playerPos = player.transform.position;

            Sweep(playerPos);

            spawnTimer -= Time.deltaTime;
            if (spawnTimer > 0f) return;

            spawnTimer = spawnInterval;

            if (alive.Count >= targetCount) return;

            SpawnOne(playerPos);
        }

        /// <summary>
        /// 부서진 것을 시간이 지나면 치우고, 너무 멀어진 것을 회수한다.
        ///
        /// <b>부서진 것도 목록에 남겨 둔다.</b> 즉시 빼면 그 자리에 새것이 바로 뽑혀
        /// 부서지는 순간 같은 자리에서 다시 솟는 것처럼 보인다.
        /// </summary>
        private void Sweep(Vector2 playerPos)
        {
            for (int i = alive.Count - 1; i >= 0; i--)
            {
                Standing s = alive[i];

                if (s.Body == null)
                {
                    alive.RemoveAt(i);
                    continue;
                }

                if (s.Body.IsBroken && s.BrokenAt <= 0f)
                {
                    s.BrokenAt = Time.time;
                    alive[i] = s;
                }

                bool debrisExpired = s.BrokenAt > 0f && Time.time - s.BrokenAt >= debrisDuration;

                bool tooFar = despawnDistance > 0f &&
                              Vector2.Distance(s.Body.transform.position, playerPos) > despawnDistance;

                if (!debrisExpired && !tooFar) continue;

                s.Body.gameObject.SetActive(false);
                alive.RemoveAt(i);
            }
        }

        private void SpawnOne(Vector2 playerPos)
        {
            if (obstaclePrefab == null || totalWeight <= 0f) return;

            WalkableArea ground = ResolveArea();
            if (ground == null) return;

            if (!TryFindSpot(playerPos, ground, out Vector2 point)) return;

            PropDefinition definition = PickDefinition();
            if (definition == null) return;

            Destructible body = Rent();
            if (body == null) return;

            body.transform.position = point;

            // Bind 가 HP·그림·콜라이더를 되돌린다. 부서졌던 몸도 이걸로 되살아난다.
            body.Bind(definition);
            body.gameObject.SetActive(true);

            alive.Add(new Standing { Body = body, BrokenAt = 0f });
        }

        /// <summary>
        /// 설 자리를 찾는다. 고리 위에서 몇 번 뽑아보고 <b>다 실패하면 이번 판은 건너뛴다</b> —
        /// 억지로 놓으면 벽 안이나 다른 장애물 위에 겹친다.
        /// </summary>
        private bool TryFindSpot(Vector2 playerPos, WalkableArea ground, out Vector2 point)
        {
            for (int attempt = 0; attempt < placementAttempts; attempt++)
            {
                float angle = UnityEngine.Random.value * Mathf.PI * 2f;
                float radius = Mathf.Lerp(minSpawnDistance, maxSpawnDistance, UnityEngine.Random.value);

                Vector2 wanted = playerPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

                if (!ground.TryFindWalkable(wanted, 2f, out Vector2 found)) continue;
                if (NoSpawnZone.BlocksMonsterAt(found)) continue;
                if (TooClose(found, playerPos)) continue;

                point = found;
                return true;
            }

            point = default;
            return false;
        }

        /// <summary>
        /// 이미 서 있는 것이나 플레이어와 너무 가까운가.
        /// <b>플레이어와의 거리도 본다</b> — 발밑에 솟으면 몸이 밀려난다.
        /// </summary>
        private bool TooClose(Vector2 candidate, Vector2 playerPos)
        {
            if (Vector2.Distance(candidate, playerPos) < minSpawnDistance) return true;

            foreach (Standing s in alive)
            {
                if (s.Body == null) continue;
                if (Vector2.Distance(candidate, s.Body.transform.position) < minSeparation) return true;
            }

            return false;
        }

        private WalkableArea ResolveArea()
        {
            if (area != null) return area;

            ServiceRegistry.TryGet(out WalkableArea found);
            return found;
        }

        /// <summary>
        /// 꺼져 있는 몸을 다시 쓴다. <b>매번 새로 만들면 파밍 한 시간에 수천 개가 쌓인다</b> —
        /// 몬스터에서 이미 한 번 밟은 문제다 (docs/pitfalls.md).
        /// </summary>
        private Destructible Rent()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (child.activeSelf) continue;

                Destructible reused = child.GetComponent<Destructible>();
                if (reused != null) return reused;
            }

            return Instantiate(obstaclePrefab, transform);
        }

        private PropDefinition PickDefinition()
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

        /// <summary>지금 몇 개가 서 있는지. 검증용.</summary>
        [ContextMenu("장애물 상태")]
        public void LogState()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[ObstacleField] 재생 중에만 의미가 있습니다.");
                return;
            }

            int standing = 0;
            foreach (Standing s in alive)
                if (s.Body != null && !s.Body.IsBroken) standing++;

            Debug.Log(
                $"[ObstacleField] '{name}' — 멀쩡함 {standing} / 목록 {alive.Count} / 목표 {targetCount}\n" +
                $"  종류 {entries.Length}가지 · 가중치 합 {totalWeight:0.#}", this);
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos || !Application.isPlaying) return;
            if (!ServiceRegistry.TryGet(out PlayerController player) || player == null) return;

            Vector3 p = player.transform.position;

            Gizmos.color = new Color(0.4f, 1f, 0.5f, 0.6f);
            Gizmos.DrawWireSphere(p, minSpawnDistance);
            Gizmos.DrawWireSphere(p, maxSpawnDistance);

            if (despawnDistance <= 0f) return;

            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.4f);
            Gizmos.DrawWireSphere(p, despawnDistance);
        }
    }
}
