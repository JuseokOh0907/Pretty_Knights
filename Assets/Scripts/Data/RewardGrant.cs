using System.Collections.Generic;
using System.Text;
using PrettyKnights.Core;
using UnityEngine;

namespace PrettyKnights.Data
{
    /// <summary>
    /// 잡거나 부순 것의 보상을 준다. <b>몬스터와 오브젝트가 같은 경로를 탄다.</b>
    ///
    /// 두 곳에 같은 계산을 두면 파밍의 결이 갈리고, 아이템 시스템이 붙을 때
    /// 고칠 자리가 둘이 된다. 계산은 한 줄뿐이지만 <b>한 줄이라서 갈라지기 쉽다.</b>
    ///
    /// <code>
    /// 확정 경험치  +  표를 굴린 값   →  AddExp
    /// </code>
    ///
    /// <b>무엇이 나왔는지 로그로 남긴다.</b> 아이템이 아직 없어 드랍은 경험치로만
    /// 나타나는데, 그러면 화면에서 "표가 도는지" 를 확인할 방법이 없다 —
    /// 경험치가 그냥 들쭉날쭉 오르는 것과 구분되지 않는다.
    /// </summary>
    public static class RewardGrant
    {
        /// <summary>드랍 로그를 남길지. <c>GameRoot</c> 의 Log Lifecycle 이 정한다.</summary>
        public static bool LogDrops = true;

        /// <summary>굴린 결과를 담는 재사용 목록. 메인 스레드 단일이고 중첩 호출이 없다.</summary>
        private static readonly List<DropTable.Drop> Hits = new List<DropTable.Drop>();

        private static readonly StringBuilder Line = new StringBuilder();

        /// <summary>
        /// <paramref name="who"/> 를 잡아 얻은 것을 준다. 준 경험치 총합을 돌려준다.
        ///
        /// <paramref name="gold"/> 는 <b>표를 굴리지 않는 확정 지급</b>이다.
        /// 잡을 때마다 0 이 나오면 파밍이 도는지 화면에서 읽히지 않는다.
        /// </summary>
        public static int Grant(string who, int baseExp, DropTable table, int gold = 0)
        {
            Hits.Clear();

            int rolled = table != null ? table.Roll(Hits) : 0;
            int total = baseExp + rolled;

            // 로그를 먼저 만든다. AddExp 가 Changed 이벤트를 쏘므로
            // 그 안에서 무언가가 또 보상을 주면 Hits 가 갈린다.
            string message = LogDrops ? Describe(who, baseExp, total, gold) : null;

            GrantItems();
            GrantGold(gold);

            if (total > 0 && ServiceRegistry.TryGet(out PlayerRuntimeState state) && state != null)
                state.AddExp(total);

            if (message != null) Debug.Log(message);

            return total;
        }

        /// <summary>
        /// 골드를 넣는다. <b>지갑이 없어도 조용히 넘어간다</b> —
        /// 게임플레이 씬을 Boot 없이 단독 실행한 경우가 그렇고,
        /// 그때 보상 하나 때문에 예외가 나면 검증이 막힌다.
        /// </summary>
        private static void GrantGold(int gold)
        {
            if (gold <= 0) return;
            if (!ServiceRegistry.TryGet(out Wallet purse) || purse == null) return;

            purse.AddGold(gold);
        }

        /// <summary>
        /// 나온 아이템을 가방에 넣는다.
        ///
        /// <b>가방이 차면 그 자리에 떨어뜨리지 않고 알린다.</b> 바닥에 떨어뜨리려면
        /// 줍는 것과 사라지는 규칙이 필요한데 그게 아직 없다 —
        /// 조용히 삼키면 "분명 나왔는데 없다" 가 된다.
        /// </summary>
        private static void GrantItems()
        {
            if (!ServiceRegistry.TryGet(out Inventory bag) || bag == null) return;

            foreach (DropTable.Drop drop in Hits)
            {
                if (drop.Item == null || drop.Count <= 0) continue;

                int left = bag.Add(drop.Item, drop.Count);
                if (left <= 0) continue;

                Debug.LogWarning(
                    $"[보상] 가방이 가득 차 '{drop.Item.DisplayName}' {left}개를 받지 못했습니다.");
            }
        }

        private static string Describe(string who, int baseExp, int total, int gold)
        {
            Line.Clear();
            Line.Append("[보상] ").Append(who).Append(" — 경험치 ").Append(baseExp);

            if (Hits.Count == 0)
            {
                Line.Append(" (드랍 없음)");
                return AppendGold(gold);
            }

            Line.Append(" + ");

            for (int i = 0; i < Hits.Count; i++)
            {
                if (i > 0) Line.Append(", ");
                Line.Append(Hits[i]);
            }

            Line.Append(" = ").Append(total);
            return AppendGold(gold);
        }

        private static string AppendGold(int gold)
        {
            if (gold > 0) Line.Append(" · 골드 ").Append(gold);

            return Line.ToString();
        }
    }
}
