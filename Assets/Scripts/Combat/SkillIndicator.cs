using UnityEngine;

namespace PrettyKnights.Combat
{
    /// <summary>
    /// 예고 하나를 화면에 띄운다. <see cref="SkillIndicatorPool"/> 이 만들고 재사용한다.
    ///
    /// <b>바닥에 깔되 알파를 높게 쓴다</b> (결정 007). 지면에 깔면 자연스럽지만
    /// 몬스터가 겹쳐 서면 가려지므로 옅으면 안 보인다.
    ///
    /// 시전자를 따라다니지 않는다. 예고가 뜬 자리에 그대로 남아야
    /// <b>플레이어가 그 자리를 피하는 것</b>이 의미를 갖는다.
    /// 몬스터가 예고 중에 움직여 범위가 따라오면 회피가 불가능해진다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SkillIndicator : MonoBehaviour
    {
        private SpriteRenderer view;
        private float duration;
        private float elapsed;
        private Color baseColor;

        public bool IsBusy { get; private set; }

        private void Awake() => view = GetComponent<SpriteRenderer>();

        /// <summary>예고를 시작한다. <paramref name="origin"/> 은 도형의 원점이다.</summary>
        public void Show(Sprite sprite, Vector2 origin, float lifetime, Color color, float baseZ)
        {
            if (view == null) view = GetComponent<SpriteRenderer>();

            view.sprite = sprite;
            baseColor = color;
            duration = Mathf.Max(0.01f, lifetime);
            elapsed = 0f;
            IsBusy = true;

            transform.position = new Vector3(origin.x, origin.y, baseZ);
            gameObject.SetActive(true);

            Apply(0f);
        }

        public void Hide()
        {
            IsBusy = false;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!IsBusy) return;

            elapsed += Time.deltaTime;

            if (elapsed >= duration)
            {
                Hide();
                return;
            }

            Apply(elapsed / duration);
        }

        /// <summary>
        /// 시간이 갈수록 진해진다. <b>터지는 순간이 가장 진하다.</b>
        /// 반대로 하면 "이미 지나간 것" 처럼 읽혀 피할 타이밍을 놓친다.
        /// </summary>
        private void Apply(float t)
        {
            Color color = baseColor;
            color.a = baseColor.a * Mathf.Lerp(0.45f, 1f, t);
            view.color = color;
        }
    }
}
