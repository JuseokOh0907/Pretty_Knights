using UnityEngine;

namespace PrettyKnights.Combat
{
    /// <summary>
    /// 타격 순간의 도트 애니메이션 하나를 재생한다. <see cref="SkillImpactPool"/> 이 만들고 재사용한다.
    ///
    /// <b>Animator 를 쓰지 않는다.</b> 프레임 스프라이트가 런타임에 구워지므로
    /// 미리 만들어 둘 클립이 없다. 4~8프레임을 고정 간격으로 넘기는 것이 전부라
    /// 상태 기계가 할 일도 없다.
    ///
    /// <b>시전자를 따라다니지 않는다.</b> 휘두른 자리에 남아야 한다 —
    /// 때린 뒤 몬스터가 밀려나거나 플레이어가 움직여도 맞은 자리는 그 자리다.
    /// <see cref="SkillIndicator"/> 가 예고를 얼려두는 것과 같은 이유다.
    ///
    /// 프레임마다 잘라낸 조각의 크기가 다르므로 <b>위치도 프레임마다 다시 잡는다.</b>
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SkillImpact : MonoBehaviour
    {
        private SpriteRenderer view;

        private SkillImpactRasterizer.Frame[] frames;
        private Vector2 origin;
        private float depth;
        private float frameDuration;
        private float elapsed;
        private int current = -1;

        public bool IsBusy { get; private set; }

        private void Awake() => view = GetComponent<SpriteRenderer>();

        /// <summary><paramref name="origin"/> 은 도형의 원점이다. 조각 위치는 여기서 계산한다.</summary>
        public void Play(
            SkillImpactRasterizer.Frame[] sequence, Vector2 shapeOrigin,
            float perFrame, Color color, float baseZ)
        {
            if (view == null) view = GetComponent<SpriteRenderer>();
            if (sequence == null || sequence.Length == 0) return;

            frames = sequence;
            origin = shapeOrigin;
            depth = baseZ;
            frameDuration = Mathf.Max(0.01f, perFrame);
            elapsed = 0f;
            current = -1;
            IsBusy = true;

            view.color = color;
            gameObject.SetActive(true);

            Apply(0);
        }

        public void Stop()
        {
            IsBusy = false;
            current = -1;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!IsBusy) return;

            elapsed += Time.deltaTime;

            int index = Mathf.FloorToInt(elapsed / frameDuration);

            if (index >= frames.Length)
            {
                Stop();
                return;
            }

            if (index != current) Apply(index);
        }

        private void Apply(int index)
        {
            current = index;

            SkillImpactRasterizer.Frame frame = frames[index];

            // 그린 것이 없는 프레임도 시간은 흘러야 한다. 건너뛰면 속도가 빨라진다.
            if (frame.IsEmpty)
            {
                view.enabled = false;
                return;
            }

            view.enabled = true;
            view.sprite = frame.Sprite;

            // 조각은 중앙 피벗이라 원점이 아니라 조각 중심으로 옮긴다.
            transform.position = new Vector3(
                origin.x + frame.Offset.x, origin.y + frame.Offset.y, depth);
        }
    }
}
