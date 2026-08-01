using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace PrettyKnights.EditorTools
{
    /// <summary>
    /// 스프라이트 애니메이션 클립의 프레임 순서를 스프라이트 이름 순으로 되돌린다.
    ///
    /// 스프라이트를 0번이 아닌 프레임부터 끌어다 클립을 만들면 시퀀스가 회전한다
    /// (예: <c>1 2 3 4 5 6 7 0</c>). 단독 재생으로는 루프라 티가 나지 않지만,
    /// 블렌드 트리 안에서는 방향을 바꿔도 정규화 시간이 이어지기 때문에
    /// 그 방향만 위상이 어긋나 다리 동작이 한 프레임 튄다.
    ///
    /// 클립 에셋을 새로 만들지 않고 키프레임만 재배치하므로 GUID 와 참조가 유지된다.
    /// </summary>
    public static class ClipFrameOrderFixer
    {
        private const string TargetRoot = "Assets/Art/Characters";
        private const string MenuRoot = "Pretty Knights/Sprite/";

        /// <summary>스프라이트 이름 끝의 <c>_숫자</c> 를 시트 내 프레임 번호로 본다.</summary>
        private static readonly Regex FrameSuffix = new Regex(@"_(\d+)$", RegexOptions.Compiled);

        [MenuItem(MenuRoot + "5. 클립 프레임 순서 점검", priority = 120)]
        public static void Report() => Run(fix: false);

        [MenuItem(MenuRoot + "6. 클립 프레임 순서 정규화", priority = 121)]
        public static void Fix()
        {
            bool ok = EditorUtility.DisplayDialog(
                "클립 프레임 순서 정규화",
                "스프라이트 이름 순서대로 키프레임을 재배치합니다.\n" +
                "클립 에셋은 새로 만들지 않으므로 참조는 유지됩니다.",
                "적용", "취소");

            if (ok) Run(fix: true);
        }

        private static void Run(bool fix)
        {
            var log = new StringBuilder(fix ? "[ClipOrder] 정규화 결과\n" : "[ClipOrder] 점검 결과\n");
            int touched = 0;

            foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { TargetRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip == null) continue;

                foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                {
                    ObjectReferenceKeyframe[] keys =
                        AnimationUtility.GetObjectReferenceCurve(clip, binding);

                    if (keys == null || keys.Length < 2) continue;

                    List<int> order = keys.Select(k => FrameIndex(k.value)).ToList();

                    // 프레임 번호를 못 읽는 스프라이트가 섞여 있으면 건드리지 않는다.
                    if (order.Any(i => i < 0))
                    {
                        log.AppendLine($"  {clip.name,-28} 프레임 번호를 읽을 수 없어 건너뜀");
                        continue;
                    }

                    bool sorted = order.Select((v, i) => (v, i)).All(t => t.v == t.i);
                    if (sorted) continue;

                    log.AppendLine(
                        $"  {clip.name,-28} {string.Join(" ", order)}  ->  " +
                        $"{string.Join(" ", Enumerable.Range(0, order.Count))}");
                    touched++;

                    if (!fix) continue;

                    ObjectReferenceKeyframe[] reordered = keys
                        .OrderBy(k => FrameIndex(k.value))
                        .ToArray();

                    // 원래 클립의 프레임 간격을 그대로 유지한다.
                    float step = clip.frameRate > 0f ? 1f / clip.frameRate : keys[1].time - keys[0].time;
                    for (int i = 0; i < reordered.Length; i++)
                        reordered[i].time = i * step;

                    AnimationUtility.SetObjectReferenceCurve(clip, binding, reordered);
                    EditorUtility.SetDirty(clip);
                }
            }

            if (fix) AssetDatabase.SaveAssets();

            if (touched == 0) log.AppendLine("  어긋난 클립 없음");
            Debug.Log(log.ToString());
        }

        private static int FrameIndex(Object sprite)
        {
            if (sprite == null) return -1;

            Match m = FrameSuffix.Match(sprite.name);
            return m.Success ? int.Parse(m.Groups[1].Value) : -1;
        }
    }
}
