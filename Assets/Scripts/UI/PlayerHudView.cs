using PrettyKnights.Core;
using PrettyKnights.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PrettyKnights.UI
{
    /// <summary>
    /// 화면 왼쪽 위의 플레이어 정보판. 아트 <c>player_hud_frame</c> 의 칸을 그대로 채운다 —
    /// 동그란 자리에 레벨, 하트 옆 홈에 체력, 아래 세 칸에 검·방패·날개(ATK·DEF·AGI).
    ///
    /// <b>매 프레임 읽지 않는다.</b> <see cref="PlayerRuntimeState.Changed"/> 가
    /// 레벨·경험치·HP 가 바뀔 때마다 알려주므로, 바뀐 순간에만 다시 그린다.
    /// <c>Update</c> 가 하는 일은 잔상을 따라오게 하는 것뿐이다.
    ///
    /// <b>상태는 Boot 에 상주하고 이 뷰도 그렇다.</b> 씬이 바뀌어도 같은 인스턴스를 계속 본다 —
    /// 세로 자동 사냥과 가로 직접 플레이가 한 캐릭터를 공유하기 때문이다 (기획서 §15-6).
    ///
    /// 몬스터 체력바(<see cref="Characters.MonsterHealthBar"/>)와 표현을 맞춘다:
    /// <b>채움은 즉시 줄고 잔상이 늦게 쫓아온다.</b> 그 사이의 띠가 "이번에 얼마나 맞았는가" 다.
    /// 다만 이쪽은 월드가 아니라 Canvas 라 스케일이 아니라 <see cref="Image.fillAmount"/> 로 줄인다 —
    /// <c>RectTransform</c> 을 늘리면 9슬라이스 테두리까지 함께 늘어난다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerHudView : MonoBehaviour
    {
        [Header("체력 (Image Type: Filled / Horizontal / Origin Left)")]
        [SerializeField, Tooltip("남은 체력 — player_health_fill")]
        private Image healthFill;

        [SerializeField, Tooltip("깎인 자리에 남는 잔상. 채움 뒤에 둔다. 비워도 된다")]
        private Image healthTrail;

        [SerializeField, Tooltip("\"120 / 200\". 비워도 된다")]
        private TMP_Text healthLabel;

        [Header("경험치 (아트가 없으므로 전부 선택)")]
        [SerializeField] private Image expFill;
        [SerializeField] private TMP_Text expLabel;

        [Header("글자")]
        [SerializeField, Tooltip("동그란 자리에 들어가는 레벨")]
        private TMP_Text levelLabel;

        [SerializeField, Tooltip("검 칸")] private TMP_Text attackLabel;
        [SerializeField, Tooltip("방패 칸")] private TMP_Text defenseLabel;
        [SerializeField, Tooltip("날개 칸")] private TMP_Text agilityLabel;

        [Header("깎이는 표현")]
        [SerializeField, Min(0f), Tooltip("맞은 뒤 잔상이 멈춰 있는 시간. 이 사이에 크기를 읽는다")]
        private float trailDelay = 0.25f;

        [SerializeField, Min(0.05f), Tooltip("잔상이 따라오는 속도 (비율/초). 1이면 꽉 찬 바를 1초에 훑는다")]
        private float trailSpeed = 0.9f;

        [Header("색")]
        [SerializeField, Tooltip("아트의 원래 색을 살리려면 흰색")]
        private Color healthColor = Color.white;

        [SerializeField, Tooltip("체력이 이 아래면 색이 바뀐다. 0이면 안 바뀐다")]
        [Range(0f, 1f)] private float lowRatio = 0.3f;

        [SerializeField] private Color lowColor = new Color(1f, 0.75f, 0.2f, 1f);

        private PlayerRuntimeState state;

        private float trailRatio = 1f;
        private float trailHold;
        private float lastRatio = 1f;

        private void OnDisable() => Unbind();

        private void OnDestroy() => Unbind();

        private void Update()
        {
            // GameRoot 보다 늦게 붙었을 수도, 이 뷰가 먼저 켜졌을 수도 있다.
            // 등록을 기다리는 쪽이 계속 찾아보는 편이 순서에 기대는 것보다 안전하다.
            if (state == null)
            {
                Bind();
                if (state == null) return;
            }

            AdvanceTrail(state.HpRatio);

            if (healthTrail != null) healthTrail.fillAmount = trailRatio;
        }

        private void Bind()
        {
            if (!ServiceRegistry.TryGet(out PlayerRuntimeState found) || found == null) return;

            state = found;
            state.Changed += Draw;

            trailRatio = state.HpRatio;
            lastRatio = trailRatio;

            Draw(state);
        }

        private void Unbind()
        {
            if (state == null) return;

            state.Changed -= Draw;
            state = null;
        }

        /// <summary>
        /// 상태가 바뀐 순간에만 불린다.
        /// <b>정의가 아직 안 붙었으면 아무것도 그리지 않는다</b> — 최대 HP 가 0 이라
        /// "0 / 0" 이 한 프레임 스쳐 지나가는 것을 막는다.
        /// </summary>
        private void Draw(PlayerRuntimeState source)
        {
            if (source == null || !source.IsBound) return;

            float ratio = source.HpRatio;

            if (healthFill != null)
            {
                healthFill.fillAmount = ratio;
                healthFill.color = lowRatio > 0f && ratio <= lowRatio ? lowColor : healthColor;
            }

            if (healthLabel != null)
                healthLabel.text = $"{Mathf.CeilToInt(source.CurrentHp)} / {Mathf.CeilToInt(source.MaxHp)}";

            if (levelLabel != null) levelLabel.text = source.Level.ToString();

            StatBlock stats = source.Stats;

            if (attackLabel != null) attackLabel.text = Mathf.RoundToInt(stats.Attack).ToString();
            if (defenseLabel != null) defenseLabel.text = Mathf.RoundToInt(stats.Defense).ToString();
            if (agilityLabel != null) agilityLabel.text = Mathf.RoundToInt(stats.Agility).ToString();

            DrawExp(source);
        }

        /// <summary>
        /// 만렙에서는 <see cref="PlayerRuntimeState.ExpToNextLevel"/> 이 0 이다.
        /// 그대로 나누면 0 으로 나누게 되므로 가득 찬 것으로 그린다.
        /// </summary>
        private void DrawExp(PlayerRuntimeState source)
        {
            int required = source.ExpToNextLevel;
            bool maxed = required <= 0;

            if (expFill != null)
                expFill.fillAmount = maxed ? 1f : Mathf.Clamp01((float)source.Exp / required);

            if (expLabel != null)
                expLabel.text = maxed ? "MAX" : $"{source.Exp} / {required}";
        }

        /// <summary>
        /// 잔상을 채움 쪽으로 따라오게 한다.
        ///
        /// <b>회복은 잔상이 기다리지 않는다.</b> 잔상은 잃은 만큼만 보여주는 것이라
        /// 늘어날 때 늦게 따라오면 없는 피해를 그리게 된다.
        /// </summary>
        private void AdvanceTrail(float ratio)
        {
            // 이번 프레임에 새로 깎였으면 잔상을 다시 멈춰 세운다.
            // 연타로 맞으면 멈춤이 갱신되어 잔상이 끝까지 남아 있는다.
            if (ratio < lastRatio) trailHold = trailDelay;
            lastRatio = ratio;

            if (trailRatio <= ratio)
            {
                trailRatio = ratio;
                trailHold = 0f;
                return;
            }

            if (trailHold > 0f)
            {
                trailHold -= Time.deltaTime;
                return;
            }

            trailRatio = Mathf.Max(ratio, trailRatio - trailSpeed * Time.deltaTime);
        }
    }
}
