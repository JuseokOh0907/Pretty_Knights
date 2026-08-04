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
    /// 구역은 areaId(숫자)와 spawnId(문자열)로만 이어져 있어
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

                SpawnPoint[] spawns = anchor.AllSpawns;

                if (spawns == null || spawns.Length == 0)
                {
                    report.AppendLine($"  ✗ #{id} '{anchor.name}' — SpawnPoint 가 하나도 없음");
                    problems++;
                    continue;
                }

                var seenSpawnIds = new HashSet<string>();
                foreach (SpawnPoint spawn in spawns)
                {
                    if (spawn == null) continue;

                    if (string.IsNullOrWhiteSpace(spawn.SpawnId))
                    {
                        report.AppendLine($"  ✗ #{id} '{spawn.name}' — spawnId 가 비어 있음");
                        problems++;
                    }
                    else if (!seenSpawnIds.Add(spawn.SpawnId))
                    {
                        report.AppendLine($"  ✗ #{id} — spawnId '{spawn.SpawnId}' 가 구역 안에서 중복");
                        problems++;
                    }

                    if (anchor.Walkable != null && !anchor.Walkable.IsWalkable(spawn.Position))
                    {
                        report.AppendLine(
                            $"  △ #{id} 도착 지점 '{spawn.SpawnId}' 가 바닥 밖 — 런타임에 주변으로 보정됨");
                    }
                }

                report.AppendLine(
                    $"  · #{id} {anchor.Definition.DisplayName} — 도착 지점 {spawns.Length}개 " +
                    $"[{string.Join(", ", spawns.Where(s => s != null).Select(s => s.SpawnId))}]");
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

                bool spawnFound = target.AllSpawns != null &&
                                  target.AllSpawns.Any(s => s != null && s.SpawnId == portal.DestinationSpawnId);

                if (!spawnFound)
                {
                    report.AppendLine(
                        $"  ✗ {where} — 목적지 #{targetId} 에 도착 지점 '{portal.DestinationSpawnId}' 가 없음");
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
                    $"  · {where} → #{targetId} '{portal.DestinationSpawnId}'");
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
