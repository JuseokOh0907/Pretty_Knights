using PrettyKnights.Core;
using PrettyKnights.Data;
using TMPro;
using UnityEngine;

namespace PrettyKnights.UI
{
    /// <summary>
    /// 재화 한 칸. <b>지금은 골드 하나뿐이다</b> (2026-08-09 확정).
    ///
    /// <b>매 프레임 읽지 않는다.</b> <see cref="Wallet.Changed"/> 가 바뀔 때만 알려주므로
    /// 그때만 다시 그린다. 파밍 중에는 초당 몇 번씩 오르지만 그래도
    /// <c>Update</c> 에서 매번 문자열을 만드는 것보다 훨씬 싸다.
    ///
    /// <b>숫자가 튀지 않게 굴려 올린다.</b> 한 번에 훅 바뀌면 얼마가 들어왔는지 못 읽는데,
    /// 굴러가는 동안은 눈이 그 변화를 쫓는다. 방치형에서 재화가 오르는 것은
    /// 화면에서 가장 자주 일어나는 사건이라 여기에 값어치가 있다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CurrencyView : MonoBehaviour
    {
        [Header("연결 (비우면 자식에서 찾는다)")]
        [SerializeField] private TMP_Text amountLabel;

        [Header("굴려 올리기")]
        [SerializeField, Tooltip("끄면 즉시 바뀐다")]
        private bool rollUp = true;

        [SerializeField, Min(0f), Tooltip(
            "따라잡는 데 걸리는 시간 (초). 길게 두면 파밍 중에 실제 값과 계속 벌어진다")]
        private float rollDuration = 0.35f;

        [SerializeField, Min(0), Tooltip(
            "차이가 이보다 작으면 굴리지 않고 바로 맞춘다. 1골드씩 오를 때 떨리는 것을 막는다")]
        private long snapThreshold = 3;

        [Header("표시")]
        [SerializeField, Tooltip("천 단위 구분. 방치형은 자릿수가 금방 커진다")]
        private bool useThousandSeparator = true;

        private Wallet purse;

        /// <summary>화면에 지금 떠 있는 값. 실제 잔액을 뒤에서 따라온다.</summary>
        private long shown;

        private long targetAmount;
        private long rollFrom;
        private float rollElapsed;
        private bool rolling;

        private void Awake()
        {
            if (amountLabel == null) amountLabel = GetComponentInChildren<TMP_Text>(includeInactive: true);
        }

        private void OnDisable() => Unbind();

        private void OnDestroy() => Unbind();

        private void Update()
        {
            // GameRoot 보다 늦게 붙었을 수도 있다. 붙을 때까지 계속 찾아본다.
            if (purse == null)
            {
                Bind();
                if (purse == null) return;
            }

            if (!rolling) return;

            rollElapsed += Time.unscaledDeltaTime;

            float t = rollDuration <= 0f ? 1f : Mathf.Clamp01(rollElapsed / rollDuration);

            shown = (long)Mathf.Round(Mathf.Lerp(rollFrom, targetAmount, t));

            if (t >= 1f)
            {
                shown = targetAmount;
                rolling = false;
            }

            Redraw();
        }

        private void Bind()
        {
            if (!ServiceRegistry.TryGet(out Wallet found) || found == null) return;

            purse = found;
            purse.Changed += OnWalletChanged;

            // 처음에는 굴리지 않는다. 시작하자마자 0에서 올라가면 방금 번 것처럼 보인다.
            shown = purse.Gold;
            targetAmount = shown;
            rolling = false;

            Redraw();
        }

        private void Unbind()
        {
            if (purse == null) return;

            purse.Changed -= OnWalletChanged;
            purse = null;
        }

        private void OnWalletChanged(Wallet wallet)
        {
            targetAmount = wallet.Gold;

            // 줄어드는 것(구매)은 굴리지 않는다. 값을 치른 것은 즉시 보여야 한다.
            bool spent = targetAmount < shown;
            bool tiny = targetAmount - shown <= snapThreshold;

            if (!rollUp || spent || tiny)
            {
                shown = targetAmount;
                rolling = false;
                Redraw();
                return;
            }

            rollFrom = shown;
            rollElapsed = 0f;
            rolling = true;
        }

        private void Redraw()
        {
            if (amountLabel == null) return;

            amountLabel.text = useThousandSeparator ? shown.ToString("N0") : shown.ToString();
        }
    }
}
