using PrettyKnights.Core;
using PrettyKnights.Data;
using PrettyKnights.Save;
using UnityEngine;

namespace PrettyKnights.World
{
    /// <summary>
    /// 바닥에 놓인 아이템·상자. <b>사용 키로 줍는다</b> (2026-08-09 확정).
    ///
    /// 포탈과 <b>같은 흐름</b>이다 — <see cref="InteractableBehaviour"/> 를 상속하므로
    /// 트리거에 들어가면 사용 버튼이 뜨고, 키와 화면 버튼이 같은 경로를 탄다.
    /// 줍는 전용 입력을 따로 만들지 않는 이유가 이것이다.
    ///
    /// <b>가방이 차면 줍지 않는다.</b> 그 자리에 그대로 남아야 나중에 다시 올 수 있다.
    /// 조용히 삼키면 "분명 주웠는데 없다" 가 된다.
    ///
    /// <b>주워간 것은 세이브에 남는다.</b> 손으로 놓은 것이라 배치 인덱스가 없어
    /// 칸 좌표로 짚는다 — 부술 수 있는 벽과 같은 방식이다.
    /// </summary>
    public sealed class ItemPickup : InteractableBehaviour
    {
        [Header("무엇을")]
        [SerializeField] private ItemDefinition item;

        [SerializeField, Min(1)] private int count = 1;

        [SerializeField, Tooltip(
            "여러 개가 든 상자. 아이템 대신 이 표를 굴린다. " +
            "둘 다 채우면 아이템을 주고 표도 굴린다")]
        private DropTable extraDrops;

        [Header("표시")]
        [SerializeField, Tooltip("비우면 아이템 이름으로 만든다")]
        private string customLabel = string.Empty;

        [Header("들키면")]
        [SerializeField, Tooltip(
            "주울 때 이 구역의 히든 방 봉인을 푼다. 상자를 열면 방이 열리는 식")]
        private NoSpawnZone revealOnTake;

        private AreaAnchor anchor;

        public override string PromptLabel
        {
            get
            {
                string configured = base.PromptLabel;
                if (!string.IsNullOrWhiteSpace(configured)) return configured;
                if (!string.IsNullOrWhiteSpace(customLabel)) return customLabel;

                if (item == null) return "줍기";

                return count > 1 ? $"{item.DisplayName} ×{count} 줍기" : $"{item.DisplayName} 줍기";
            }
        }

        protected override void Awake()
        {
            base.Awake();
            anchor = GetComponentInParent<AreaAnchor>();
        }

        private void OnEnable()
        {
            // 이미 주워간 것은 다시 놓이지 않는다. 층을 껐다 켤 때마다 되살아나면
            // 같은 아이템을 무한히 벌 수 있다.
            if (!ServiceRegistry.TryGet(out WorldProgress progress) || progress == null) return;
            if (anchor == null) return;

            if (progress.IsPickupTaken(anchor.AreaId, SaveKey)) gameObject.SetActive(false);
        }

        protected override void OnInteract()
        {
            if (!ServiceRegistry.TryGet(out Inventory bag) || bag == null)
            {
                Debug.LogError("[ItemPickup] Inventory 가 없습니다. GameRoot 가 Boot 씬에 있는지 확인하세요.");
                return;
            }

            // 가방이 차 있는지 먼저 본다. 넣다 말면 절반만 사라진 상태가 된다.
            if (item != null && count > 0)
            {
                int left = bag.Add(item, count);

                if (left > 0)
                {
                    Debug.Log($"[ItemPickup] 가방이 가득 차 '{item.DisplayName}' 을 다 넣지 못했습니다 ({left}개 남음).");

                    // 넣은 만큼은 이미 들어갔다. 남은 만큼만 다시 놓아 둔다.
                    count = left;
                    return;
                }
            }

            // 상자는 표를 굴린다. 경험치와 아이템이 함께 나오고 로그도 같은 모양이다.
            if (extraDrops != null) RewardGrant.Grant(PromptLabel, 0, extraDrops);

            if (revealOnTake != null) revealOnTake.Reveal(transform.position);

            Take();
        }

        private void Take()
        {
            if (anchor != null && ServiceRegistry.TryGet(out WorldProgress progress) && progress != null)
                progress.MarkPickupTaken(anchor.AreaId, SaveKey);

            // 파괴하지 않고 끈다. 층을 다시 켤 때 OnEnable 이 세이브를 보고 판단한다.
            gameObject.SetActive(false);
        }

        /// <summary>세이브에 적을 이름표. 손으로 놓은 것이라 위치가 곧 정체다.</summary>
        private Vector2Int SaveKey => new Vector2Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.y));

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.9f, 0.3f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}
