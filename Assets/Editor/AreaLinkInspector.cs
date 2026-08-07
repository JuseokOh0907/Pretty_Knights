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
    /// 포탈 링크를 점검한다. <b>씬을 바꾸지 않는다.</b>
    ///
    /// 구역은 areaId(숫자)와 arrivalId(문자열)로만 이어져 있어
    /// 오타를 컴파일러가 잡아주지 못한다. 게다가 잘못된 링크는
    /// <b>그 포탈을 실제로 밟아 보기 전까지 드러나지 않는다.</b>
    /// 층이 9개라 전부 걸어서 확인하는 것은 현실적이지 않으므로 정적으로 훑는다.
    ///
    /// 열려 있는 씬만 검사한다. Ingame_Horizontal 을 열고 실행할 것.
    /// </summary>
    public static class AreaLinkInspector
    {
        private const string MenuRoot = "Pretty Knights/Areas/";

        [MenuItem(MenuRoot + "0. 포탈 링크 점검 (변경 없음)", priority = 300)]
        public static void Report()
        {
            AreaAnchor[] anchors = Object.FindObjectsByType<AreaAnchor>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            Portal[] portals = Object.FindObjectsByType<Portal>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (anchors.Length == 0)
            {
                Debug.LogWarning(
                    "[AreaLink] 열린 씬에서 AreaAnchor 를 찾지 못했습니다. " +
                    "Ingame_Horizontal 을 열고 다시 실행하세요.");
                return;
            }

            var report = new StringBuilder();
            int problems = 0;

            report.AppendLine($"[AreaLink] 구역 {anchors.Length}개 · 포탈 {portals.Length}개");
            report.AppendLine();

            problems += InspectAnchors(anchors, report);
            problems += InspectPortals(portals, anchors, report);

            report.AppendLine();
            report.AppendLine(problems == 0
                ? "문제 없음."
                : $"문제 {problems}건. 위 목록을 확인하세요.");

            if (problems == 0) Debug.Log(report.ToString());
            else Debug.LogWarning(report.ToString());
        }

        private static int InspectAnchors(IReadOnlyList<AreaAnchor> anchors, StringBuilder report)
        {
            int problems = 0;

            report.AppendLine("── 구역 ──");

            var seenAreaIds = new Dictionary<int, AreaAnchor>();

            foreach (AreaAnchor anchor in anchors.OrderBy(a => a.AreaId))
            {
                // 자동 탐색 칸(Floor · Walkable)은 Resolve 가 채운다.
                // 이걸 먼저 부르지 않고 읽으면 "비어 있으니 없다" 고 잘못 보고한다.
                anchor.Resolve(force: true);

                if (anchor.Definition == null)
                {
                    report.AppendLine($"  ✗ '{anchor.name}' — AreaDefinition 이 비어 있음");
                    problems++;
                    continue;
                }

                int id = anchor.AreaId;

                if (seenAreaIds.TryGetValue(id, out AreaAnchor existing))
                {
                    report.AppendLine($"  ✗ areaId #{id} 중복 — '{existing.name}' 과 '{anchor.name}'");
                    problems++;
                }
                else
                {
                    seenAreaIds.Add(id, anchor);
                }

                if (anchor.Floor == null)
                {
                    report.AppendLine($"  ✗ #{id} '{anchor.name}' — 바닥 타일맵 없음 (카메라 경계가 안 잡힘)");
                    problems++;
                }

                if (anchor.Walkable == null)
                {
                    report.AppendLine($"  ✗ #{id} '{anchor.name}' — WalkableArea 없음 (도착 보정이 안 됨)");
                    problems++;
                }
                else if (anchor.Walkable.Floor == null)
                {
                    // 이게 비면 IsWalkable 이 항상 false 라 모든 도착 지점이 "바닥 밖" 으로 잡힌다.
                    report.AppendLine(
                        $"  ✗ #{id} '{anchor.name}' — WalkableArea 의 Floor 가 비어 있음 " +
                        "(설 수 있는 자리가 하나도 없다고 판정된다)");
                    problems++;
                }

                ArrivalPoint[] arrivals = anchor.AllArrivals;

                if (arrivals == null || arrivals.Length == 0)
                {
                    report.AppendLine($"  ✗ #{id} '{anchor.name}' — ArrivalPoint 가 하나도 없음");
                    problems++;
                    continue;
                }

                var seenArrivalIds = new HashSet<string>();
                foreach (ArrivalPoint arrival in arrivals)
                {
                    if (arrival == null) continue;

                    if (string.IsNullOrWhiteSpace(arrival.ArrivalId))
                    {
                        report.AppendLine($"  ✗ #{id} '{arrival.name}' — arrivalId 가 비어 있음");
                        problems++;
                    }
                    else if (!seenArrivalIds.Add(arrival.ArrivalId))
                    {
                        report.AppendLine($"  ✗ #{id} — arrivalId '{arrival.ArrivalId}' 가 구역 안에서 중복");
                        problems++;
                    }

                    if (anchor.Walkable != null && !anchor.Walkable.IsWalkable(arrival.Position))
                    {
                        report.AppendLine(
                            $"  △ #{id} 도착 지점 '{arrival.ArrivalId}' ({arrival.Position.x:0.#}, {arrival.Position.y:0.#}) " +
                            "가 바닥 타일 위가 아님 — 런타임에 주변 3유닛에서 설 수 있는 자리를 찾는다. " +
                            "못 찾으면 그 좌표에 그대로 내려놓으므로 벽에 낄 수 있다");
                    }
                }

                report.AppendLine(
                    $"  · #{id} {anchor.Definition.DisplayName} — 도착 지점 {arrivals.Length}개 " +
                    $"[{string.Join(", ", arrivals.Where(a => a != null).Select(a => a.ArrivalId))}]");
            }

            return problems;
        }

        private static int InspectPortals(
            IReadOnlyList<Portal> portals, IReadOnlyList<AreaAnchor> anchors, StringBuilder report)
        {
            int problems = 0;

            report.AppendLine();
            report.AppendLine("── 포탈 ──");

            if (portals.Count == 0)
            {
                report.AppendLine("  (포탈이 하나도 없습니다)");
                return problems;
            }

            foreach (Portal portal in portals)
            {
                string where = PathOf(portal.transform);

                if (portal.Destination == null)
                {
                    report.AppendLine($"  ✗ {where} — 목적지가 비어 있음");
                    problems++;
                    continue;
                }

                int targetId = portal.Destination.AreaId;
                AreaAnchor target = anchors.FirstOrDefault(a => a.AreaId == targetId);

                if (target == null)
                {
                    report.AppendLine(
                        $"  ✗ {where} — areaId #{targetId} 인 층이 씬에 없음 " +
                        $"({portal.Destination.name})");
                    problems++;
                    continue;
                }

                bool arrivalFound = target.AllArrivals != null &&
                                    target.AllArrivals.Any(a => a != null && a.ArrivalId == portal.DestinationArrivalId);

                if (!arrivalFound)
                {
                    report.AppendLine(
                        $"  ✗ {where} — 목적지 #{targetId} 에 도착 지점 '{portal.DestinationArrivalId}' 가 없음");
                    problems++;
                    continue;
                }

                Collider2D collider = portal.GetComponent<Collider2D>();
                if (collider == null || !collider.isTrigger)
                {
                    report.AppendLine($"  ✗ {where} — Is Trigger 가 켜진 Collider2D 가 없음");
                    problems++;
                }

                report.AppendLine(
                    $"  · {where} → #{targetId} '{portal.DestinationArrivalId}'");
            }

            return problems;
        }

        private static string PathOf(Transform transform)
        {
            var parts = new List<string>();
            for (Transform t = transform; t != null; t = t.parent) parts.Add(t.name);

            parts.Reverse();
            return string.Join("/", parts);
        }

        [MenuItem(MenuRoot + "1. AreaDefinition 번호 목록", priority = 301)]
        public static void ListDefinitions()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(AreaDefinition)}");

            if (guids.Length == 0)
            {
                Debug.LogWarning("[AreaLink] AreaDefinition 에셋이 하나도 없습니다.");
                return;
            }

            var definitions = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<AreaDefinition>)
                .Where(d => d != null)
                .OrderBy(d => d.AreaId)
                .ToList();

            var report = new StringBuilder($"[AreaLink] AreaDefinition {definitions.Count}개\n");

            var seen = new HashSet<int>();
            foreach (AreaDefinition definition in definitions)
            {
                string duplicate = seen.Add(definition.AreaId) ? "" : "  ← 번호 중복";
                string escape = definition.CanEscape
                    ? $"탈출 → #{definition.EscapeTo.AreaId}"
                    : "탈출 불가";

                report.AppendLine(
                    $"  #{definition.AreaId,-4} {definition.Theme,-8} {definition.DisplayName,-12} " +
                    $"{escape}{duplicate}");
            }

            Debug.Log(report.ToString());
        }
    }
}
