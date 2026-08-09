using UnityEngine;

namespace PrettyKnights.Characters
{
    /// <summary>
    /// 몬스터 발밑의 체력바.
    ///
    /// <b>월드 스페이스 Canvas 를 쓰지 않는다.</b> 한 층에 16마리까지 나오는데
    /// 마리마다 Canvas 를 두면 그만큼 배치가 쪼개진다. <see cref="SpriteRenderer"/> 몇 장이면
    /// 다른 스프라이트와 함께 묶여 그려지고, 모바일에서 이 차이가 크다.
    ///
    /// <code>
    /// Frame   테두리        절대 늘어나지 않는다
    /// Track   빈 홈         늘어나지 않는다
    /// Trail   깎인 잔상     늦게 따라온다
    /// Fill    남은 체력     즉시 줄어든다
    /// </code>
    ///
    /// <b>아트를 넣으면 크기를 스프라이트에서 읽는다.</b> 인스펙터에 폭을 손으로 맞추면
    /// 아트를 갈아 끼울 때마다 어긋난다. 비워 두면 단색 사각형을 코드가 만든다.
    ///
    /// <b>가득이면 숨는다.</b> 꽉 찬 바 16개가 늘 떠 있으면 화면이 시끄럽고,
    /// 무엇보다 <b>맞은 놈이 어느 놈인지</b> 안 보인다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MonsterHealthBar : MonoBehaviour
    {
        [Header("연결 (비우면 단색 사각형을 코드가 만든다)")]
        [SerializeField] private MonsterController owner;

        [SerializeField, Tooltip("테두리. 늘어나지 않는다 — monster_health_frame")]
        private SpriteRenderer frame;

        [SerializeField, Tooltip("빈 홈. 늘어나지 않는다 — monster_health_track_bg")]
        private SpriteRenderer track;

        [SerializeField, Tooltip("깎인 자리에 남는 잔상 — monster_health_fill 을 밝게 물들여 쓴다")]
        private SpriteRenderer trail;

        [SerializeField, Tooltip("남은 체력 — monster_health_fill")]
        private SpriteRenderer fill;

        [Header("자리 — 발밑")]
        [SerializeField, Tooltip("루트에서 아래로 얼마나. 루트가 접지점이다")]
        private float offsetY = -0.3f;

        [Header("코드가 만들 때의 크기 (아트를 넣으면 무시된다)")]
        [SerializeField, Min(0.1f)] private float width = 0.9f;
        [SerializeField, Min(0.02f)] private float height = 0.12f;

        [Header("색")]
        [SerializeField, Tooltip("아트를 넣으면 흰색으로 두어 원래 색을 살린다")]
        private Color fillColor = Color.white;

        [SerializeField, Tooltip("체력이 이 아래면 색이 바뀐다. 0이면 안 바뀐다")]
        [Range(0f, 1f)] private float lowRatio = 0.3f;

        [SerializeField] private Color lowColor = new Color(1f, 0.75f, 0.2f, 1f);

        [SerializeField, Tooltip("코드가 만든 사각형에만 쓰인다")]
        private Color trackColor = new Color(0.08f, 0.06f, 0.06f, 0.85f);

        [Header("깎이는 표현")]
        [SerializeField, Tooltip("깎인 자리에 잠깐 남는 색. 이게 보여야 '얼마나 맞았는지' 가 읽힌다")]
        private Color trailColor = new Color(1f, 0.95f, 0.9f, 0.9f);

        [SerializeField, Min(0f), Tooltip("맞은 뒤 잔상이 멈춰 있는 시간. 이 사이에 크기를 읽는다")]
        private float trailDelay = 0.25f;

        [SerializeField, Min(0.05f), Tooltip("잔상이 따라오는 속도 (비율/초). 1이면 꽉 찬 바를 1초에 훑는다")]
        private float trailSpeed = 1.2f;

        [Header("숨기기")]
        [SerializeField, Tooltip("가득 차 있으면 숨긴다. 맞은 놈을 가려내기 위한 것")]
        private bool hideWhenFull = true;

        [SerializeField, Min(0f), Tooltip("가득 찬 뒤 이만큼 더 보여준다. 0이면 즉시 숨는다")]
        private float lingerAfterFull = 1.5f;

        [Header("정렬 — 몬스터 위")]
        [SerializeField] private string sortingLayer = "Default";

        [SerializeField, Tooltip("몸보다 크게. 정렬 일괄 지정 때 확정한다")]
        private int sortingOrder = 90;

        /// <summary>단색 사각형 하나를 모두가 나눠 쓴다. 마리마다 만들 이유가 없다.</summary>
        private static Sprite shared;

        private float lingerLeft;
        private float trailRatio = 1f;
        private float trailHold;
        private float lastRatio = 1f;

        /// <summary>비율 1일 때의 가로 스케일과 그때의 월드 폭. 아트든 코드 사각형이든 여기로 통일된다.</summary>
        private float baseScaleX = 1f;
        private float baseWidth = 1f;

        private void Awake()
        {
            if (owner == null) owner = GetComponentInParent<MonsterController>();

            if (owner == null)
            {
                Debug.LogError($"[MonsterHealthBar] '{name}' 이 MonsterController 를 찾지 못했습니다.", this);
                enabled = false;
                return;
            }

            // 뒤에서부터 홈 · 잔상 · 채움 · 테두리.
            // 잔상이 채움에 가리면 깎인 자리가 안 보이고, 테두리는 맨 위여야 홈을 덮는다.
            if (track == null) track = Create("Track", trackColor, sortingOrder);
            if (trail == null) trail = Create("Trail", trailColor, sortingOrder + 1);
            if (fill == null) fill = Create("Fill", fillColor, sortingOrder + 2);

            Layout();
        }

        private void OnEnable()
        {
            // 풀에서 다시 꺼내진 몸은 체력이 가득이다. 남아 있던 표시를 지운다.
            lingerLeft = 0f;
            trailRatio = 1f;
            trailHold = 0f;
            lastRatio = 1f;

            Draw(1f);
        }

        /// <summary>
        /// <c>LateUpdate</c> 에서 그린다. 같은 프레임에 맞은 것이 바로 반영되어야
        /// 한 대 늦게 줄어드는 것처럼 보이지 않는다.
        /// </summary>
        private void LateUpdate()
        {
            if (owner == null) return;

            if (owner.IsDead)
            {
                Show(false);
                return;
            }

            Draw(owner.HpRatio);
        }

        private void Draw(float ratio)
        {
            if (ratio >= 1f)
            {
                // 막 가득 찬 참이면 잠깐 더 보여준다. 회복이 눈에 보여야 한다.
                if (lingerLeft > 0f) lingerLeft -= Time.deltaTime;

                if (hideWhenFull && lingerLeft <= 0f)
                {
                    Show(false);
                    return;
                }
            }
            else
            {
                lingerLeft = lingerAfterFull;
            }

            Show(true);

            AdvanceTrail(ratio);

            Stretch(fill, ratio);
            Stretch(trail, trailRatio);

            if (fill != null)
                fill.color = lowRatio > 0f && ratio <= lowRatio ? lowColor : fillColor;
        }

        /// <summary>
        /// 잔상을 채움 쪽으로 따라오게 한다.
        ///
        /// <b>채움은 즉시 줄고 잔상이 늦게 쫓아온다.</b> 그 사이의 밝은 띠가 곧
        /// "이번에 얼마나 깎였는가" 다 — 부드럽게 줄이면 정확하지만 얼마나 맞았는지는 못 읽는다.
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

        /// <summary>
        /// 가운데 피벗 사각형을 <b>왼쪽 끝 고정</b>으로 늘린다.
        /// 줄어든 만큼의 절반을 왼쪽으로 밀면 왼쪽 끝이 제자리에 남는다 —
        /// 스프라이트 피벗을 왼쪽으로 임포트하지 않아도 되므로 아트에 조건이 붙지 않는다.
        /// </summary>
        private void Stretch(SpriteRenderer target, float ratio)
        {
            if (target == null) return;

            Vector3 scale = target.transform.localScale;
            scale.x = baseScaleX * ratio;
            target.transform.localScale = scale;

            Vector3 local = target.transform.localPosition;
            local.x = -baseWidth * (1f - ratio) * 0.5f;
            target.transform.localPosition = local;
        }

        private void Show(bool visible)
        {
            Enable(frame, visible);
            Enable(track, visible);
            Enable(trail, visible);
            Enable(fill, visible);
        }

        private static void Enable(SpriteRenderer target, bool visible)
        {
            if (target != null && target.enabled != visible) target.enabled = visible;
        }

        /// <summary>
        /// 자리와 기준 크기를 잡는다.
        ///
        /// <b>아트가 들어오면 폭을 인스펙터에서 읽지 않는다.</b> 스프라이트의 실제 크기를 쓰므로
        /// 아트를 갈아 끼워도 숫자를 다시 맞출 일이 없다 — 손으로 맞춘 값은 반드시 어긋난다.
        /// </summary>
        private void Layout()
        {
            transform.localPosition = new Vector3(0f, offsetY, 0f);

            bool generated = fill != null && fill.sprite == shared;

            // 코드가 만든 1 × 1 사각형은 스케일이 곧 크기다. 아트는 제 크기를 갖고 있다.
            baseScaleX = generated ? width : 1f;
            baseWidth = fill != null && fill.sprite != null
                ? fill.sprite.bounds.size.x * baseScaleX
                : width;

            Place(frame, 1f);
            Place(track, generated ? width : 1f);
            Place(trail, baseScaleX);
            Place(fill, baseScaleX);

            if (generated)
            {
                if (track != null) track.color = trackColor;
                if (trail != null) trail.color = trailColor;
            }
            else if (trail != null)
            {
                // 아트를 쓰면 채움과 같은 그림을 밝게 물들여 잔상으로 삼는다.
                if (trail.sprite == null && fill != null) trail.sprite = fill.sprite;
                trail.color = trailColor;
            }
        }

        private void Place(SpriteRenderer target, float scaleX)
        {
            if (target == null) return;

            target.transform.localPosition = Vector3.zero;

            Vector3 scale = target.transform.localScale;
            scale.x = scaleX;
            scale.y = target.sprite == shared ? height : scale.y;
            target.transform.localScale = scale;
        }

        private SpriteRenderer Create(string childName, Color color, int order)
        {
            var go = new GameObject(childName);
            go.transform.SetParent(transform, worldPositionStays: false);

            var renderer = go.AddComponent<SpriteRenderer>();

            renderer.sprite = SharedSprite();
            renderer.color = color;
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = order;

            return renderer;
        }

        /// <summary>
        /// 1 × 1 흰 사각형. 색은 <see cref="SpriteRenderer.color"/> 로 입히므로
        /// 홈과 채움이 같은 스프라이트를 쓴다.
        /// <b>PPU 를 1 로 둔다</b> — 그래야 <c>localScale</c> 이 곧 월드 크기가 된다.
        /// </summary>
        private static Sprite SharedSprite()
        {
            if (shared != null) return shared;

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "HealthBarPixel"
            };

            texture.SetPixel(0, 0, Color.white);
            texture.Apply(updateMipmaps: false);

            shared = Sprite.Create(
                texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), pixelsPerUnit: 1f,
                extrude: 0, meshType: SpriteMeshType.FullRect);

            shared.name = "HealthBarPixel";
            return shared;
        }

        /// <summary>
        /// 도메인 리로드를 끈 상태에서 옛 텍스처가 남지 않게 비운다.
        /// 남으면 파괴된 Texture2D 를 참조하는 Sprite 가 살아 있어 분홍색으로 그려진다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => shared = null;
    }
}
