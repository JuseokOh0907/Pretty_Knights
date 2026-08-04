using System.Collections.Generic;
using PrettyKnights.Characters;
using PrettyKnights.Core;
using PrettyKnights.Data;
using UnityEngine;

namespace PrettyKnights.World
{
    /// <summary>
    /// <b>한 지점에 고정 스폰한다.</b> 위치가 곧 설계인 곳에 쓴다.
    ///
    /// 1층은 그 테마 몬스터의 특성을 안내하는 층이라 "이 자리에 이 몬스터"가
    /// 의도의 전부다. 3층 보스도 마찬가지로 자리가 정해져 있다.
    /// 넓은 파밍 층(2F)에는 <see cref="FloorPopulation"/> 을 쓴다.
    ///
    /// 플레이어가 <see cref="activationDistance"/> 안에 들어와야 작동한다.
    /// 층 오브젝트가 꺼지면 이 컴포넌트도 함께 꺼지므로 다른 층의 스포너는 돌지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MonsterSpawner : MonoBehaviour
    {
        [Header("무엇을")]
        [SerializeField] private MonsterController monsterPrefab;
        [SerializeField] private MonsterDefinition definition;

        [Header("얼마나")]
        [SerializeField, Min(1)] private int maxAlive = 1;
        [SerializeField, Min(0f), Tooltip("한 마리 죽은 뒤 다시 채우기까지")]
        private float respawnCooldown = 8f;

        [Header("언제")]
        [SerializeField, Min(0f), Tooltip("플레이어가 이 안에 들어오면 작동")]
        private float activationDistance = 20f;

        [SerializeField, Min(0.05f), Tooltip("검사 간격. 매 프레임 잴 이유가 없다")]
        private float checkInterval = 0.2f;

        [Header("배치")]
        [SerializeField, Min(0f), Tooltip("이 지점이 벽이면 주변에서 설 수 있는 자리를 찾는다")]
        private float placementSearchRadius = 2f;

        private readonly List<MonsterController> spawned = new List<MonsterController>();
        private float checkTimer;
        private float cooldownLeft;

        private void OnDisable()
        {
            // 층이 꺼지면 데려간다. 다시 켜질 때 새로 채운다.
            foreach (MonsterController m in spawned)
                if (m != null) m.gameObject.SetActive(false);

            spawned.Clear();
            cooldownLeft = 0f;
        }

        private void Update()
        {
            if (cooldownLeft > 0f) cooldownLeft -= Time.deltaTime;

            checkTimer -= Time.deltaTime;
            if (checkTimer > 0f) return;
            checkTimer = checkInterval;

            if (!PlayerIsNear()) return;
            if (cooldownLeft > 0f) return;

            spawned.RemoveAll(m => m == null || !m.gameObject.activeSelf);
            if (spawned.Count >= maxAlive) return;

            SpawnOne();
        }

        private bool PlayerIsNear()
        {
            if (!ServiceRegistry.TryGet(out PlayerController player) || player == null) return false;

            return Vector2.Distance(transform.position, player.transform.position) <= activationDistance;
        }

        private void SpawnOne()
        {
            if (monsterPrefab == null || definition == null) return;

            Vector2 point = transform.position;

            // 스포너를 벽 위에 찍었을 수 있다. 바닥이 있는 자리를 찾아 옮긴다.
            if (ServiceRegistry.TryGet(out WalkableArea area) && area != null)
            {
                if (!area.TryFindWalkable(point, placementSearchRadius, out point))
                {
                    Debug.LogWarning($"[MonsterSpawner] {name}: 주변에 설 수 있는 자리가 없습니다.", this);
                    return;
                }
            }

            MonsterController monster = Instantiate(monsterPrefab, point, Quaternion.identity, transform);
            monster.Spawn(definition, point);
            monster.Died += OnMonsterDied;

            spawned.Add(monster);
        }

        private void OnMonsterDied(MonsterController monster)
        {
            monster.Died -= OnMonsterDied;
            monster.gameObject.SetActive(false);

            cooldownLeft = respawnCooldown;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 1f, 0.5f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, activationDistance);

            Gizmos.color = new Color(1f, 1f, 1f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, placementSearchRadius);
        }
    }
}
