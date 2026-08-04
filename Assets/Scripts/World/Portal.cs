using PrettyKnights.Core;
using PrettyKnights.Data;
using UnityEngine;

namespace PrettyKnights.World
{
    /// <summary>
    /// 다음 구역으로 보내는 포탈. <b>단방향이다.</b>
    ///
    /// 짝이 되는 되돌아가는 포탈을 두지 않는다.
    /// 던전에서 나오는 길은 탈출 스킬뿐이며 그쪽은 <c>AreaTransition.RequestEscape()</c> 를 탄다.
    /// 그래서 이 컴포넌트에는 "왔던 곳" 개념이 없고 목적지 하나만 있다.
    ///
    /// 겹쳐 있는 것만으로는 발동하지 않는다. 사용 키를 눌러야 한다
    /// (<see cref="InteractableBehaviour"/>).
    /// 그래서 도착 지점이 다른 포탈 위여도 무한 왕복이 생기지 않는다.
    ///
    /// <b>같은 오브젝트에 Is Trigger 를 켠 BoxCollider2D 가 필요하다.</b>
    /// 아트가 128px(= 2 × 2칸)이므로 콜라이더도 2 × 2 로 맞춘다.
    /// </summary>
    public sealed class Portal : InteractableBehaviour
    {
        [Header("목적지")]
        [SerializeField, Tooltip("이 포탈이 보낼 구역")]
        private AreaDefinition destination;

        [SerializeField, Tooltip("목적지 구역 안의 도착 지점 spawnId")]
        private string destinationSpawnId = "default";

        public AreaDefinition Destination => destination;
        public string DestinationSpawnId => destinationSpawnId;

        public override bool CanInteract => base.CanInteract && destination != null;

        /// <summary>버튼 라벨을 비워두면 목적지 이름으로 자동 생성한다.</summary>
        public override string PromptLabel
        {
            get
            {
                string configured = base.PromptLabel;
                if (!string.IsNullOrWhiteSpace(configured)) return configured;

                return destination != null ? $"{destination.DisplayName}(으)로 이동" : "이동";
            }
        }

        protected override void Awake()
        {
            base.Awake();

            if (destination == null)
                Debug.LogError($"[Portal] '{name}' 의 목적지가 비어 있습니다. 이 포탈은 아무 데도 가지 않습니다.");
        }

        protected override void OnInteract()
        {
            if (!ServiceRegistry.TryGet(out AreaTransition transition) || transition == null)
            {
                Debug.LogError(
                    "[Portal] AreaTransition 이 없습니다. Boot 씬의 GameRoot 에 붙였는지 확인하세요.");
                return;
            }

            transition.Request(destination, destinationSpawnId);
        }

        private void OnDrawGizmosSelected()
        {
            if (destination == null) return;

            // 목적지가 같은 씬에 있으면 선으로 이어 그린다. 배치할 때 링크를 눈으로 확인하려는 것.
            foreach (AreaAnchor anchor in FindObjectsByType<AreaAnchor>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (anchor.AreaId != destination.AreaId) continue;

                SpawnPoint spawn = anchor.ResolveSpawn(destinationSpawnId);
                if (spawn == null) continue;

                Gizmos.color = new Color(0.4f, 0.7f, 1f, 0.8f);
                Gizmos.DrawLine(transform.position, spawn.transform.position);
                return;
            }
        }
    }
}
