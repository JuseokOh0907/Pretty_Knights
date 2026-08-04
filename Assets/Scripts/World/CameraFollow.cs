using PrettyKnights.Characters;
using PrettyKnights.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PrettyKnights.World
{
    /// <summary>
    /// 카메라가 플레이어를 따라간다. <b>카메라는 씬에 두고 프리팹의 자식으로 두지 않는다.</b>
    ///
    /// 자식으로 붙이면 캐릭터에 즉시 고정되어 부드러운 추적·경계 제한·층별 설정이 전부 막힌다.
    /// 대상은 <see cref="ServiceRegistry"/> 에서 찾으므로 인스펙터 연결이 필요 없다.
    /// 몸은 씬마다 새로 생기므로 매번 다시 찾는다.
    ///
    /// 맵이 44,780칸 규모라 <b>경계 제한이 없으면 가장자리에서 빈 공간이 보인다.</b>
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public sealed class CameraFollow : MonoBehaviour
    {
        [Header("추적")]
        [SerializeField, Min(0f), Tooltip("따라붙는 데 걸리는 시간. 0이면 즉시")]
        private float smoothTime = 0.15f;

        [SerializeField, Min(0f), Tooltip("이 반경 안에서는 카메라가 움직이지 않는다 (월드 유닛)")]
        private float deadZone = 0.35f;

        [SerializeField, Min(0f), Tooltip("대상이 이만큼 멀어지면 부드럽게 따라가지 않고 즉시 붙는다")]
        private float snapDistance = 8f;

        [Header("경계")]
        [SerializeField, Tooltip("이 타일맵의 범위를 카메라 경계로 쓴다. 비우면 제한 없음")]
        private Tilemap boundsSource;

        [SerializeField, Tooltip("경계를 안쪽으로 좁힌다 (월드 유닛)")]
        private float boundsPadding;

        private Camera cam;
        private Transform target;
        private Vector3 velocity;
        private bool hasBounds;
        private Bounds bounds;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            RefreshBounds();
        }

        // 구역 전환이 층마다 경계를 갈아 끼우려면 카메라를 찾을 수 있어야 한다.
        // 카메라도 몸과 마찬가지로 게임플레이 씬에 있어 씬마다 새로 생긴다.
        private void OnEnable() => ServiceRegistry.Register(this);

        private void OnDisable()
        {
            if (ServiceRegistry.TryGet(out CameraFollow current) && current == this)
                ServiceRegistry.Unregister<CameraFollow>();
        }

        /// <summary>
        /// 층이 바뀌면 경계도 바뀐다. 층 전환이 붙으면 그쪽에서 이걸 호출한다.
        /// </summary>
        public void SetBoundsSource(Tilemap source)
        {
            boundsSource = source;
            RefreshBounds();
        }

        public void ClearBounds() => hasBounds = false;

        private void RefreshBounds()
        {
            if (boundsSource == null)
            {
                hasBounds = false;
                return;
            }

            // 실제로 타일이 깔린 범위만 쓴다. localBounds 는 빈 청크까지 포함할 수 있다.
            boundsSource.CompressBounds();

            BoundsInt cells = boundsSource.cellBounds;
            Vector3 min = boundsSource.CellToWorld(cells.min);
            Vector3 max = boundsSource.CellToWorld(cells.max);

            bounds = new Bounds();
            bounds.SetMinMax(min, max);

            if (boundsPadding > 0f)
                bounds.Expand(-boundsPadding * 2f);

            hasBounds = bounds.size.x > 0f && bounds.size.y > 0f;
        }

        private void LateUpdate()
        {
            if (!AcquireTarget()) return;

            Vector3 desired = Desired(target.position);
            float distance = Vector2.Distance(transform.position, desired);

            if (distance <= deadZone) return;

            // 세이브 복원·워프처럼 멀리 튀는 경우 부드럽게 따라가면 한참 흘러간다.
            if (distance >= snapDistance || smoothTime <= 0f)
            {
                transform.position = desired;
                velocity = Vector3.zero;
                return;
            }

            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
        }

        /// <summary>대상 위치를 경계 안으로 가둔 카메라 목표 지점.</summary>
        private Vector3 Desired(Vector3 targetPosition)
        {
            float x = targetPosition.x;
            float y = targetPosition.y;

            if (hasBounds)
            {
                float halfHeight = cam.orthographicSize;
                float halfWidth = halfHeight * cam.aspect;

                // 맵이 화면보다 좁은 축은 가둘 수 없으므로 가운데에 맞춘다.
                x = bounds.size.x <= halfWidth * 2f
                    ? bounds.center.x
                    : Mathf.Clamp(x, bounds.min.x + halfWidth, bounds.max.x - halfWidth);

                y = bounds.size.y <= halfHeight * 2f
                    ? bounds.center.y
                    : Mathf.Clamp(y, bounds.min.y + halfHeight, bounds.max.y - halfHeight);
            }

            // z 는 카메라가 원래 서 있던 깊이를 유지한다 (2D 에서 보통 -10).
            return new Vector3(x, y, transform.position.z);
        }

        private bool AcquireTarget()
        {
            if (target != null) return true;

            if (!ServiceRegistry.TryGet(out PlayerController player) || player == null) return false;

            target = player.transform;
            SnapToTarget();
            return true;
        }

        /// <summary>보간 없이 즉시 대상 위로 옮긴다. 씬 진입·층 전환에 쓴다.</summary>
        public void SnapToTarget()
        {
            if (target == null) return;

            transform.position = Desired(target.position);
            velocity = Vector3.zero;
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) RefreshBounds();
            if (!hasBounds) return;

            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.6f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}
