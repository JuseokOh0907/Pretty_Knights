using UnityEngine;

namespace PrettyKnights.World
{
    /// <summary>
    /// 구역 안의 도착 지점. 포탈이 목적지를 지정할 때 <see cref="SpawnId"/> 로 가리킨다.
    ///
    /// 좌표만이 아니라 <b>도착 후 바라볼 방향까지</b> 들고 있다.
    /// 방향을 안 정해두면 도착할 때마다 정면(아래)을 보게 되어,
    /// 왔던 포탈을 등지고 서야 할 자리에서 포탈을 마주보고 서는 일이 생긴다.
    ///
    /// 위치는 이 오브젝트의 <c>transform.position</c> 이다.
    /// 실제 도착 직전에 <see cref="WalkableArea"/> 로 한 번 더 걸러지므로
    /// 살짝 벽에 걸쳐 놓아도 근처의 설 수 있는 칸으로 보정된다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpawnPoint : MonoBehaviour
    {
        [SerializeField, Tooltip("같은 구역 안에서 유일해야 한다. 포탈이 이 문자열로 찾는다")]
        private string spawnId = "default";

        [SerializeField, Tooltip("도착한 뒤 바라볼 방향. 8방향 중 가까운 쪽으로 맞춰진다")]
        private Vector2 facing = Vector2.down;

        public string SpawnId => spawnId;
        public Vector2 Position => transform.position;

        /// <summary>0 벡터가 들어가면 블렌드 트리에서 방향이 뭉개지므로 기본값으로 되돌린다.</summary>
        public Vector2 Facing => facing.sqrMagnitude > 0.0001f ? facing : Vector2.down;

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.4f, 1f, 0.6f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, 0.4f);
            Gizmos.DrawRay(transform.position, (Vector3)Facing.normalized * 0.9f);
        }
    }
}
