# 검격·스킬 이펙트 — PixelLab 작업 명령서

> **판정 도형을 그대로 보여주면 칼이 아니라 부채가 된다** (2026-08-09 확정).
> 맞는 범위와 보이는 그림을 갈랐다 — 판정은 `SkillShape`, 그림은
> `SkillEffectDefinition`(SO). 근거는 [`../decisions/008-impact-vfx.md`](../decisions/008-impact-vfx.md) §8.
>
> 아트는 **Claude Code 에서 PixelLab API 로 생성한다** (2026-08-09 확정).
> 이 문서는 그대로 따라 실행할 수 있는 명령서다.

---

## 0. 무엇을 만드는가

| 순서 | 이펙트 | 에셋 이름 | 쓰는 곳 |
|---|---|---|---|
| **1** | **검격** | `Effect_Slash` | 플레이어 기본 공격 — **지금 임시 표시로 돌고 있다** |
| 2 | 전방 베기 | `Effect_ForwardSlash` | 스킬 1 |
| 3 | 관통 직선 | `Effect_PierceLine` | 스킬 2 |
| 4 | 광역 폭발 | `Effect_Burst` | 스킬 3 |

**몬스터는 만들지 않는다.** 빨간 예고 → 판정 흐름이 이미 성립하고,
예고와 임팩트가 같은 도형을 연달아 그리면 신호가 오히려 흐려진다.

**1번만 끝내도 지금 보이는 부채가 사라진다.** 거기까지가 이번 목표다.

---

## 1. 연결

```
MCP URL   https://api.pixellab.ai/mcp        (HTTP 트랜스포트)
REST      https://api.pixellab.ai/v2
인증      Authorization: Bearer <API_TOKEN>
```

토큰은 PixelLab 계정 페이지에서 받는다. **저장소에 커밋하지 않는다** —
환경 변수나 `.claude/settings.local.json` 에 둔다.

먼저 `get_balance` 로 잔액을 확인한다. 아래 작업은 **잡 10건**(생성 5 + 애니메이션 5)이다.

> 생성은 전부 비동기다. `create_*` 가 잡 ID 를 즉시 돌려주고
> `get_*` 로 폴링한다. 보통 30초~3분.

---

## 2. 규격 — 이 값을 벗어나면 API 가 거절한다

| 항목 | 값 | 근거 |
|---|---|---|
| **PPU** | **64** | 타일·예고와 같다. 캐릭터(256)로 그리면 반원 한 변이 820px 이 된다 |
| 캔버스 한 변 | **≤ 256** | `animate-with-text-v3` 의 `first_frame` 상한 |
| 캔버스 | **4의 배수** | `create-image-pixen` 제약 |
| 프레임 수 | **4~16 · 짝수** | `frame_count` 제약. CLAUDE.md §5 는 4~8 |
| 총 픽셀 | **가로 × 세로 × 프레임 ≤ 524,288** | 〃 |
| 배경 | **`no_background: true`** | 투명이어야 한다 |

### 캔버스 표

```
픽셀 = 월드 유닛 × 64
```

| 이펙트 | 캔버스 | = 월드 | 프레임 | 총 픽셀 |
|---|---|---|---|---|
| **검격** | **224 × 128** | 3.5 × 2.0 | **6** | 172,032 |
| 전방 베기 | 256 × 144 | 4.0 × 2.25 | 6 | 221,184 |
| 관통 직선 | 96 × 256 | 1.5 × 4.0 | 6 | 147,456 |
| 광역 폭발 | 256 × 256 | 4.0 × 4.0 | 8 | **524,288 ← 상한 정확히** |

> **그림이 판정 범위보다 작아도 된다.** 결정 008 §8 이후 둘은 갈라졌다.
> 관통 직선의 판정은 5유닛인데 그림은 4유닛이다 — 캔버스 상한 때문이며 문제가 아니다.

---

## 3. 방향은 5개만 만든다

좌우가 거울인 방향은 **코드가 반전해서 쓴다** (`SkillEffectDefinition.Resolve`).

| 만든다 | 순번 | PixelLab `direction` | 반전으로 대신하는 것 |
|---|---|---|---|
| 아래 | `01_front` | `south` | — |
| 아래-오른쪽 | `02_front_faces-screen-right` | `south-east` | `08_front_faces-screen-left` |
| 오른쪽 | `03_faces-screen-right` | `east` | `07_faces-screen-left` |
| 위-오른쪽 | `04_back_faces-screen-right` | `north-east` | `06_back_faces-screen-left` |
| 위 | `05_back` | `north` | — |

검격은 **손 소유가 드러나지 않는 추상적인 호**라 반전해도 깨지지 않는다.
`CLAUDE.md` §4 의 "반전하면 손이 바뀌는 방향은 전용 아트" 규칙은 캐릭터 몸에 대한 것이다.

---

## 4. 실행 — 검격 기준

### 4-1. 방향마다 첫 프레임을 만든다 (5회)

도구 `create_image_pixen` · 엔드포인트 `POST /create-image-pixen`

```json
{
  "description": "<아래 프롬프트>",
  "image_size": { "width": 224, "height": 128 },
  "no_background": true,
  "outline": "single color black outline",
  "detail": "low detail"
}
```

**프롬프트 (방향만 바꾼다)**

```
01_front   a crescent-shaped white sword slash arc sweeping downward toward the
           viewer, thin hollow crescent, bright white core with pale yellow edge,
           empty in the middle, no character, no weapon, no background

02_front-right   ... sweeping down and to the right ...
03_right         ... sweeping to the right, horizontal crescent ...
04_back-right    ... sweeping up and to the right ...
05_back          ... sweeping upward away from the viewer ...
```

> **"empty in the middle" 와 "hollow" 를 반드시 넣는다.** 이게 부채와 칼자국을 가르는
> 유일한 지시다. 안쪽이 채워져 나오면 그게 지금 문제 삼고 있는 그 그림이다.
>
> **캐릭터·무기를 그리지 말라고 명시한다.** 그냥 "sword slash" 라고만 하면
> 칼을 든 사람이 나온다.

`get_image` 로 폴링해 결과를 받는다. **안쪽이 채워졌으면 다시 뽑는다.**
프롬프트를 고치기 전에 두세 번 재생성해 보는 편이 빠르다.

### 4-2. 방향마다 애니메이션을 만든다 (5회)

도구 `animate_image` · 엔드포인트 `POST /animate-with-text-v3`

```json
{
  "first_frame": "<4-1 의 PNG base64>",
  "action": "a sword slash arc appearing thin, widening and brightening, then fading to a thin afterimage",
  "frame_count": 6
}
```

**프레임이 이렇게 나와야 한다.**

```
  0번        1번        2번          3번        4번        5번
들어간다   벌어진다   가장 길고 밝다  빠진다    잔상       사라진다
  얇게       중간        최대         중간      흐리게      거의 투명
```

2번이 타격 순간이다. **끝 프레임이 첫 프레임보다 밝으면 뒤집힌 것이다** —
그러면 `action` 에 `then fading` 을 강조해 다시 뽑는다.

### 4-3. 받아서 넣는다

```
Assets/Art/Effects/Slash/
  01_front_slash_0.png … 01_front_slash_5.png
  02_front_faces-screen-right_slash_0.png … _5.png
  03_faces-screen-right_slash_0.png … _5.png
  04_back_faces-screen-right_slash_0.png … _5.png
  05_back_slash_0.png … _5.png
```

**프레임을 한 장씩 둔다.** 캐릭터는 `8x1` 시트를 쓰지만 이펙트는 API 가
프레임 단위로 주고 `SkillEffectDefinition` 이 `Sprite[]` 를 받으므로
합칠 이유가 없다. 합치는 단계가 늘면 어긋날 자리만 늘어난다.

> 다운로드 링크는 **인증이 필요 없다.** UUID 자체가 열쇠다.

### 4-4. 임포트 설정

| 항목 | 값 |
|---|---|
| Texture Type | Sprite (2D and UI) |
| Sprite Mode | **Single** |
| Pixels Per Unit | **64** |
| Pivot | **Center** |
| Filter Mode | **Point** |
| Compression | Android·iOS 오버라이드 **RGBA ASTC 4×4** |
| Alpha Is Transparency | 켬 |

---

## 5. 에셋과 배선

`Create > Pretty Knights > Skill Effect Definition`
→ `Assets/Data/Effects/Effect_Slash.asset`

| 칸 | 값 |
|---|---|
| Clips | **5개.** Direction 을 `Front` `FrontRight` `Right` `BackRight` `Back` 로 두고 Frames 에 0~5 순서대로 |
| Frames Per Second | **20** (6프레임 = 0.3초) |
| Forward Offset | **0.6** |
| Vertical Offset | **0.4** |
| Tint | 흰색 |
| Scale | 1 |
| **Follow Caster** | **켬** |

그다음 `Player.prefab` 의 `PlayerAttack` 에서

- `Attack Effect` → `Effect_Slash` ★
- `Show Range When No Art` → **끈다**

---

## 6. 검격은 몸을 따라간다

`Follow Caster` 를 켜는 이유는 휘두르는 0.3초 사이에 걸어가면
칼자국만 뒤에 남아 몸과 떨어져 보이기 때문이다.

**예고는 반대로 절대 따라가면 안 된다.** 같은 질문에 답이 정반대인 것은
그림이 약속하는 것이 다르기 때문이다.

| 그림 | 약속 | 따라가나 |
|---|---|---|
| 예고 (빨강) | "**여기가** 위험해진다" — 앞으로의 약속 | **아니오.** 따라오면 피할 수 없다 |
| 검격 | "**방금** 휘둘렀다" — 몸짓의 잔상 | **예.** 몸에서 떨어지면 거짓말이다 |
| 지면 장판 · 폭발 자국 | "이 자리가 탔다" | 아니오 |

> 따라갈 때도 **부모를 바꾸지 않는다.** 풀은 Boot 상주인데 게임플레이 씬
> 오브젝트에 붙이면 씬이 내려갈 때 함께 파괴되어 풀이 죽은 참조를 든다.

---

## 7. 확인

1. Boot 에서 재생 → 가로 모드 → 공격
2. **부채꼴 채움이 사라지고 초승달 호가 뜬다**
3. 공격하면서 조이스틱을 민다 → **호가 몸을 따라온다**
4. 8방향 전부 휘둘러 본다 → 왼쪽 계열 3방향이 **반전되어** 나온다
5. 몬스터에게 맞아 본다 → **빨간 예고만** 뜨고 임팩트는 없다 (의도된 것)

### 안 되면

| 증상 | 원인 |
|---|---|
| 여전히 부채꼴이 뜬다 | `Attack Effect` 가 비었거나 `Show Range When No Art` 가 켜져 있다 |
| 아무것도 안 뜬다 | 둘 다 비었다. 또는 Boot 에 `SkillImpactPool` 이 없다 |
| 호가 흐릿하다 | Filter Mode 가 Point 가 아니다 |
| 호가 너무 크다/작다 | PPU 가 64 가 아니다. `Scale` 로 때우지 말 것 — 도트가 깨진다 |
| 왼쪽 방향이 안 나온다 | `Clips` 의 `Direction` 을 잘못 지정했다. 반전은 5개가 다 있어야 돈다 |
| 호가 발밑에서 터진다 | `Vertical Offset` 이 0 이다 |

---

## 8. 남은 것

- 스킬 3종(2~4번) — 스킬 시스템(`ISkillBar` 구현체)이 붙을 때 같은 절차로
- 몬스터 전용 검격 — 필요해지면. 지금은 예고로 충분하다
- **반응** — 데미지 숫자 · 히트 플래시 · 사운드 · 햅틱 (`../TODO.md` 5절)

---

## 참고

- [PixelLab API 문서](https://www.pixellab.ai/docs)
- [OpenAPI 스펙](https://api.pixellab.ai/v2/openapi.json) — 파라미터 제약의 출처
- [MCP 문서](https://api.pixellab.ai/mcp/docs)
