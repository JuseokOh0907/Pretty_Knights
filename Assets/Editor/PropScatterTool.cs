using System.Collections.Generic;
using System.Linq;
using System.Text;
using PrettyKnights.Data;
using PrettyKnights.World;
using UnityEditor;
using UnityEngine;

namespace PrettyKnights.EditorTools
{
    /// <summary>
    /// 배치를 <b>미리 본다.</b> 실제 배치는 런타임에 <see cref="FloorProps"/> 가 만든다.
    ///
    /// <b>씬에 굽지 않는 이유</b>는 재배치 때문이다. 테마를 완전 클리어하면
    /// 그 테마가 초기화되고 배치가 새로 뽑혀야 하는데, 씬에 구워두면 그럴 수 없다
    /// (docs/decisions/006-area-transition.md §3-1).
    ///
    /// 계산은 <see cref="PropScatterer"/> 한 곳에 있으므로 여기서 본 것과
    /// 실제로 나오는 것이 같다. 시드가 같으면 결과가 같기 때문이다.
    ///
    /// <b>손으로 놓은 것은 건드리지 않는다.</b> 미리보기는 <c>PropPreview</c> 컨테이너에만
    /// 만들고, 그 밖의 오브젝트는 회피 대상으로만 읽는다.
    /// </summary>
    public static class PropScatterTool
    {
        private const string MenuRoot = "Pretty Knights/Props/";
        internal const string PreviewName = "PropPreview";

        [MenuItem(MenuRoot + "0. 배치 개수만 계산 (변경 없음)", priority = 500)]
        public static void Count() => Run(spawn: false);

        [MenuItem(MenuRoot + "1. 미리보기 만들기", priority = 501)]
        public static void Preview() => Run(spawn: true);

        [MenuItem(MenuRoot + "2. 미리보기 지우기", priority = 502)]
        public static void ClearPreview()
        {
            AreaAnchor[] anchors = Object.FindObjectsByType<AreaAnchor>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            int removed = 0;

            foreach (AreaAnchor anchor in anchors)
            {
                Transform container = anchor.transform.Find(PreviewName);
                if (container == null) continue;

                removed += container.childCount;
                Undo.DestroyObjectImmediate(container.gameObject);
            }

            Debug.Log($"[PropScatter] 미리보기 {removed}개를 지웠습니다. 손으로 놓은 것은 그대로입니다.");
        }

        private static void Run(bool spawn)
        {
            AreaAnchor[] anchors = Object.FindObjectsByType<AreaAnchor>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (anchors.Length == 0)
            {
                Debug.LogWarning("[PropScatter] 열린 씬에서 AreaAnchor 를 찾지 못했습니다.");
                return;
            }

            var report = new StringBuilder(
                spawn ? "[PropScatter] 미리보기\n" : "[PropScatter] 개수 계산 (변경 없음)\n");

            var zones = new List<NoSpawnZone>(Object.FindObjectsByType<NoSpawnZone>(
                FindObjectsInactive.Include, FindObjectsSortMode.None));

            int total = 0;

            foreach (AreaAnchor anchor in anchors.OrderBy(a => a.AreaId))
            {
                anchor.Resolve(force: true);

                AreaDefinition definition = anchor.Definition;
                FloorScatterProfile profile = definition != null ? definition.ScatterProfile : null;

                if (profile == null) continue;

                if (anchor.Walkable == null || anchor.Walkable.Floor == null)
                {
                    report.AppendLine($"  ✗ #{anchor.AreaId} '{anchor.name}' — WalkableArea 또는 Floor 가 없습니다");
                    continue;
                }

                Transform container = anchor.transform.Find(PreviewName);
                if (container != null) Undo.DestroyObjectImmediate(container.gameObject);

                List<Vector2> avoid = PropScatterer.CollectAvoidPoints(anchor, null);
                int seed = profile.Seed + definition.AreaId;

                List<PropScatterer.Placement> plan =
                    PropScatterer.Plan(anchor.Walkable, profile, seed, zones, avoid);

                total += plan.Count;

                int wanted = profile.TotalCount;
                string shortfall = plan.Count < wanted
                    ? $"  ← {wanted}개 요청 중 {plan.Count}개만 자리를 찾았습니다"
                    : "";

                report.AppendLine($"  · #{anchor.AreaId} {definition.DisplayName} — {plan.Count}개{shortfall}");

                if (spawn) SpawnPreview(anchor, profile, plan);
            }

            report.AppendLine();
            report.Append(spawn
                ? $"  미리보기 {total}개. 실제 배치는 재생 시 FloorProps 가 같은 시드로 다시 만듭니다."
                : $"  총 {total}개가 놓입니다. \"1. 미리보기 만들기\" 로 눈으로 확인하세요.");

            Debug.Log(report.ToString());
        }

        /// <summary>
        /// 눈으로 보기 위한 것이라 <see cref="Destructible"/> 만 채운다.
        /// 토템 연동과 포탈은 런타임이 담당하므로 여기서 흉내내지 않는다 —
        /// 흉내내면 미리보기와 실제가 갈릴 여지가 생긴다.
        /// </summary>
        private static void SpawnPreview(
            AreaAnchor anchor, FloorScatterProfile profile, List<PropScatterer.Placement> plan)
        {
            if (profile.PropPrefab == null)
            {
                Debug.LogError($"[PropScatter] #{anchor.AreaId} 프로필에 Prop Prefab 이 없습니다.");
                return;
            }

            var root = new GameObject(PreviewName);
            Undo.RegisterCreatedObjectUndo(root, "배치 미리보기");
            root.transform.SetParent(anchor.transform, worldPositionStays: false);

            foreach (PropScatterer.Placement placement in plan)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(profile.PropPrefab, root.transform);
                instance.transform.position = placement.Position;
                instance.name = placement.Definition.PropId;

                var destructible = instance.GetComponent<Destructible>();
                if (destructible != null) destructible.Bind(placement.Definition);

                Undo.RegisterCreatedObjectUndo(instance, "배치 미리보기");
            }
        }
    }
}
