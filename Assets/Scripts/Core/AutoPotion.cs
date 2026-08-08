using PrettyKnights.Data;
using UnityEngine;

namespace PrettyKnights.Core
{
    /// <summary>
    /// HP 가 정한 선 아래로 떨어지면 포션을 대신 마신다.
    /// <b>Boot 씬의 <c>GameRoot</c> 에 붙인다</b> — 몸이 씬마다 새로 생겨도 계속 돌아야 한다.
    ///
    /// 무엇을 마실지는 <see cref="ItemDefinition.AutoUse"/> 가, 언제 마실지는
    /// <see cref="PotionSettings"/> 가 정한다. 앞은 기획이, <b>뒤는 플레이어가</b> 정한다.
    ///
    /// <b>포션이 없으면 알린다.</b> 조용히 넘어가면 자동 사용을 켜 둔 사람은
    /// 왜 안 마셨는지 모른 채 죽는다 — 자동에 맡긴 만큼 실패도 말해야 한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AutoPotion : MonoBehaviour
    {
        [SerializeField, Min(0.05f), Tooltip("HP 를 얼마나 자주 볼지. 매 프레임 볼 이유가 없다")]
        private float checkInterval = 0.2f;

        [SerializeField, Min(0f), Tooltip("한 번 마신 뒤 이만큼은 다시 마시지 않는다")]
        private float useCooldown = 1f;

        [SerializeField, Min(1f), Tooltip("포션이 없다는 경고를 이 간격보다 자주 띄우지 않는다")]
        private float warningInterval = 6f;

        /// <summary>포션이 없어 못 마셨다. UI 가 이걸 듣고 문구를 띄운다.</summary>
        public event System.Action<string> Warned;

        /// <summary>마셨다. 인자는 마신 아이템. 이펙트·사운드가 붙을 자리다.</summary>
        public event System.Action<ItemDefinition> Used;

        private float checkTimer;
        private float cooldownLeft;
        private float warningLeft;

        private void Update()
        {
            if (cooldownLeft > 0f) cooldownLeft -= Time.deltaTime;
            if (warningLeft > 0f) warningLeft -= Time.deltaTime;

            checkTimer -= Time.deltaTime;
            if (checkTimer > 0f) return;
            checkTimer = checkInterval;

            Check();
        }

        private void Check()
        {
            if (!ServiceRegistry.TryGet(out PotionSettings settings) || settings == null) return;
            if (!settings.AutoUse) return;

            if (!ServiceRegistry.TryGet(out PlayerRuntimeState player) || player == null) return;
            if (!player.IsBound || player.IsDead) return;

            // 이미 가득이면 볼 것도 없다.
            if (player.HpRatio > settings.Threshold) return;

            if (cooldownLeft > 0f) return;

            if (!ServiceRegistry.TryGet(out Inventory bag) || bag == null) return;

            int slot = PickSlot(bag, player);

            if (slot < 0)
            {
                Warn(settings);
                return;
            }

            ItemDefinition potion = bag.ItemAt(slot);

            if (!bag.Use(slot, player)) return;

            cooldownLeft = useCooldown;
            warningLeft = 0f;

            Used?.Invoke(potion);
        }

        /// <summary>
        /// 마실 칸을 고른다.
        ///
        /// <b>낭비가 가장 적은 것을 고른다</b> — 잃은 만큼을 덮는 것 중 가장 작은 포션.
        /// 그런 것이 없으면 가장 큰 것을 마신다. 큰 포션을 긁힌 상처에 쓰면
        /// 정작 필요할 때 없고, 작은 것만 홀짝이면 마시는 사이에 죽는다.
        /// </summary>
        private static int PickSlot(Inventory bag, PlayerRuntimeState player)
        {
            float missing = player.MaxHp - player.CurrentHp;

            int bestFit = -1, biggest = -1;
            float bestFitHeal = float.MaxValue, biggestHeal = 0f;

            for (int i = 0; i < bag.SlotCount; i++)
            {
                ItemDefinition item = bag.ItemAt(i);
                if (item == null || !item.AutoUse) continue;

                float heal = item.HealAmount;

                if (heal >= missing && heal < bestFitHeal)
                {
                    bestFitHeal = heal;
                    bestFit = i;
                }

                if (heal > biggestHeal)
                {
                    biggestHeal = heal;
                    biggest = i;
                }
            }

            return bestFit >= 0 ? bestFit : biggest;
        }

        /// <summary>
        /// 포션이 없다고 알린다. <b>간격을 두지 않으면 HP 가 낮은 내내 도배된다</b> —
        /// 임계값 아래에 있는 동안 검사가 계속 돌기 때문이다.
        /// </summary>
        private void Warn(PotionSettings settings)
        {
            if (warningLeft > 0f) return;
            warningLeft = warningInterval;

            Warned?.Invoke($"포션이 없습니다  (HP {settings.ThresholdPercent}% 이하 자동 사용)");
        }

        [ContextMenu("지금 상태")]
        private void LogState()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[AutoPotion] 재생 중에만 의미가 있습니다.");
                return;
            }

            ServiceRegistry.TryGet(out PotionSettings settings);
            ServiceRegistry.TryGet(out PlayerRuntimeState player);
            ServiceRegistry.TryGet(out Inventory bag);

            int stock = 0;
            if (bag != null)
                for (int i = 0; i < bag.SlotCount; i++)
                {
                    ItemDefinition item = bag.ItemAt(i);
                    if (item != null && item.AutoUse) stock += bag.CountAt(i);
                }

            Debug.Log(
                $"[AutoPotion] {settings}\n" +
                $"  HP {(player != null ? player.HpRatio : 0f) * 100f:0}% · 가진 포션 {stock}개");
        }
    }
}
