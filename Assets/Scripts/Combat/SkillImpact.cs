using UnityEngine;

namespace PrettyKnights.Combat
{
    /// <summary>
    /// 타격 이펙트 하나를 재생한다. <see cref="SkillImpactPool"/> 이 만들고 재사용한다.
    ///
    /// <b>Animator 를 쓰지 않는다.</b> 프레임이 런타임에 구워질 수도 있어
    /// 미리 만들어 둘 클립이 없다. 4~8프레임을 고정 간격으로 넘기는 것이 전부라
    /// 상태 기계가 할 일도 없다.
    ///
    /// <b>따라갈지 말지는 이펙트가 정한다</b> (2026-08-09).
    /// 근접 검격은 시전자를 따라가야 한다 — 휘두르는 0.2초 동안 걸어가면
    /// 칼자국만 뒤에 남아 몸과 떨어져 보인다.
    /// 반대로 지면에 남는 장판과 예고는 그 자리에 있어야 한다.
    /// <b>피하는 것이 의미를 가지려면 표식이 따라오면 안 되기 때문이다.</b>
    ///
    /// 따라갈 때도 <b>부모를 바꾸지 않는다.</b> 풀은 Boot 에 상주하는데
    /// 게임플레이 씬의 오브젝트에 붙이면 씬이 내려갈 때 함께 파괴되어
    /// 풀이 죽은 참조를 들게 된다. 매 프레임 좌표만 따라간다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SkillImpact : MonoBehaviour
    {
        private SpriteRenderer view;

        private SkillEffectFrame[] frames;
        private float frameDuration;
        private float elapsed;
        private int current = -1;
        private float depth;

        /// <summary>따라갈 대상. 비어 있으면 터진 자리에 남는다.</summary>
        private Transform follow;

        /// <summary>따라갈 때 대상에서 이만큼 떨어져 있는다. 시전 순간에 정해진다.</summary>
        private Vector2 anchor;

        /// <summary>따라가지 않을 때의 고정 자리. 대상이 파괴됐을 때의 대비이기도 하다.</summary>
        private Vector2 pinned;

        public bool IsBusy { get; private set; }

        private void Awake() => view = GetComponent<SpriteRenderer>();

        /// <summary>
        /// <paramref name="shapeOrigin"/> 은 이펙트의 기준점이다.
        /// <paramref name="followTarget"/> 이 있으면 그 대상과의 거리를 유지하며 따라간다.
        /// </summary>
        public void Play(
            SkillEffectFrame[] sequence, Vector2 shapeOrigin, Transform followTarget,
            float perFrame, Color color, float scale, bool flipX, float baseZ)
        {
            if (view == null) view = GetComponent<SpriteRenderer>();
            if (sequence == null || sequence.Length == 0) return;

            frames = sequence;
            pinned = shapeOrigin;
            follow = followTarget;
            anchor = followTarget != null ? shapeOrigin - (Vector2)followTarget.position : Vector2.zero;

            depth = baseZ;
            frameDuration = Mathf.Max(0.01f, perFrame);
            elapsed = 0f;
            current = -1;
            IsBusy = true;

            view.color = color;
            view.flipX = flipX;
            transform.localScale = new Vector3(scale, scale, 1f);

            gameObject.SetActive(true);

            Apply(0);
        }

        public void Stop()
        {
            IsBusy = false;
            current = -1;
            follow = null;
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
            else if (follow != null) Place(frames[current]);
        }

        private void Apply(int index)
        {
            current = index;

            SkillEffectFrame frame = frames[index];

            // 그린 것이 없는 프레임도 시간은 흘러야 한다. 건너뛰면 속도가 빨라진다.
            if (frame.IsEmpty)
            {
                view.enabled = false;
                return;
            }

            view.enabled = true;
            view.sprite = frame.Sprite;

            Place(frame);
        }

        private void Place(SkillEffectFrame frame)
        {
            // 대상이 사라지면(죽거나 씬이 바뀌면) 마지막 자리에 남는다.
            // Unity 의 널 비교가 걸리도록 Transform 그대로 비교한다.
            if (follow != null) pinned = (Vector2)follow.position + anchor;

            transform.position = new Vector3(
                pinned.x + frame.Offset.x, pinned.y + frame.Offset.y, depth);
        }
    }
}
