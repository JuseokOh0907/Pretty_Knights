using System.Collections.Generic;
using PrettyKnights.Core;
using PrettyKnights.Data;
using UnityEngine;

namespace PrettyKnights.World
{
    /// <summary>
    /// areaId 번호를 씬의 층 오브젝트로 해석한다. <c>Map</c> 루트에 하나만 붙인다.
    ///
    /// <b>비활성 층까지 훑는 것이 이 클래스의 존재 이유다.</b>
    /// 각 층이 <c>OnEnable</c> 에서 스스로 등록하는 방식으로는 안 된다.
    /// 포탈이 목적지를 물을 때 그 층은 아직 꺼져 있기 때문이다.
    ///
    /// 활성 층을 바꾸는 책임도 여기 있다. 전부 알고 있는 쪽이 하는 게 맞고,
    /// 카메라 경계는 "지금 어느 층이 켜져 있는가" 에 정확히 종속되므로 함께 처리한다
    /// (docs/TODO.md "카메라 경계를 층마다 바꾼다").
    /// </summary>
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    public sealed class AreaRegistry : MonoBehaviour
    {
        [SerializeField, Tooltip("비우면 이 오브젝트의 자식 전체에서 찾는다")]
        private Transform searchRoot;

        private readonly Dictionary<int, AreaAnchor> byId = new Dictionary<int, AreaAnchor>();

        /// <summary>지금 켜져 있는 구역. 아직 아무것도 켜지지 않았으면 null.</summary>
        public AreaAnchor Active { get; private set; }

        private void Awake()
        {
            Transform root = searchRoot != null ? searchRoot : transform;

            // includeInactive: true — 이게 이 클래스의 전부다.
            AreaAnchor[] found = root.GetComponentsInChildren<AreaAnchor>(includeInactive: true);

            foreach (AreaAnchor anchor in found)
            {
                anchor.Resolve();

                int id = anchor.AreaId;
                if (id == AreaDefinition.NoArea) continue;

                if (byId.TryGetValue(id, out AreaAnchor existing))
                {
                    Debug.LogError(
                        $"[AreaRegistry] areaId #{id} 가 '{existing.name}' 과 '{anchor.name}' 에 중복됩니다. " +
                        "먼저 등록된 쪽만 쓰이므로 포탈이 엉뚱한 층으로 갑니다.");
                    continue;
                }

                byId.Add(id, anchor);

                if (anchor.gameObject.activeInHierarchy) Active = anchor;
            }

            ServiceRegistry.Register(this);

            if (found.Length == 0)
                Debug.LogError($"[AreaRegistry] '{root.name}' 아래에서 AreaAnchor 를 하나도 찾지 못했습니다.");
        }

        private void OnDestroy()
        {
            if (ServiceRegistry.TryGet(out AreaRegistry current) && current == this)
                ServiceRegistry.Unregister<AreaRegistry>();
        }

        public bool TryGet(int areaId, out AreaAnchor anchor)
        {
            if (areaId != AreaDefinition.NoArea) return byId.TryGetValue(areaId, out anchor);

            anchor = null;
            return false;
        }

        public bool TryGet(AreaDefinition definition, out AreaAnchor anchor)
        {
            if (definition != null) return TryGet(definition.AreaId, out anchor);

            anchor = null;
            return false;
        }

        /// <summary>
        /// <paramref name="anchor"/> 만 켜고 나머지 층은 끈다.
        ///
        /// <b>끄기가 켜기보다 먼저다.</b> <see cref="WalkableArea"/> 가
        /// <c>OnEnable/OnDisable</c> 로 <see cref="ServiceRegistry"/> 를 갱신하는데,
        /// 켜기를 먼저 하면 새 구역이 등록된 직후 옛 구역의 <c>OnDisable</c> 이 돌아
        /// "이미 등록되어 있어 덮어씁니다" 경고가 매 전환마다 찍힌다.
        /// 두 호출 사이에는 아무것도 실행되지 않으므로 순서만 지키면 빈틈은 없다.
        /// </summary>
        public void Activate(AreaAnchor anchor)
        {
            if (anchor == null) return;

            if (Active == anchor && anchor.gameObject.activeSelf)
            {
                ApplyCameraBounds(anchor);
                return;
            }

            foreach (KeyValuePair<int, AreaAnchor> pair in byId)
            {
                if (pair.Value == null || pair.Value == anchor) continue;
                if (pair.Value.gameObject.activeSelf) pair.Value.gameObject.SetActive(false);
            }

            anchor.gameObject.SetActive(true);
            Active = anchor;

            ApplyCameraBounds(anchor);
        }

        public bool Activate(int areaId)
        {
            if (!TryGet(areaId, out AreaAnchor anchor)) return false;

            Activate(anchor);
            return true;
        }

        private static void ApplyCameraBounds(AreaAnchor anchor)
        {
            if (anchor.Floor == null) return;
            if (!ServiceRegistry.TryGet(out CameraFollow camera) || camera == null) return;

            camera.SetBoundsSource(anchor.Floor);
        }
    }
}
