# 세로 모드 — 전투 화면 렌더링 구성

> `Ingame_Vertical` 은 지금 `Global Light 2D` 하나뿐인 빈 씬이다.
> 이 문서는 그중 **화면에 무엇이 어떻게 그려지는가**만 다룬다.
> 스폰·자동 사냥·하단 UI 는 이어지는 문서에서 다룬다.
>
> 표기 — `[C]` 는 컴포넌트, 들여쓰기는 부모-자식.
> `★` 는 **반드시 손으로 채워야** 하는 칸.

---

## 0. 화면을 세 띠로 나눈다 (2026-08-09 확정)

```
1080 × 1920
  ┌────────────────────┐  1.00
  │   상단 HUD          │   레벨 · 체력 · 재화        0.14  (269 px)
  ├────────────────────┤  0.86
  │                    │
  │   전투 화면         │   카메라가 그리는 유일한 띠  0.30  (576 px)
  │                    │
  ├────────────────────┤  0.56
  │                    │
  │   하단 조작판       │   스킬 · 메뉴               0.56 (1075 px)
  │                    │
  └────────────────────┘  0.00
```

세 숫자는 `StageViewport` 의 `Top Inset` 과 `Height Fraction` 두 칸이 정한다.
하단은 남은 자리이므로 따로 적지 않는다 — **한 곳에서만 정해야 어긋나지 않는다.**

### 왜 UI 로 덮지 않고 뷰포트를 줄이는가

덮으면 전투가 UI 뒤에서 계속 벌어지고, 카메라는 **보이지도 않을 픽셀을 끝까지 그린다.**
뷰포트를 줄이면 그리는 픽셀 자체가 1920 → 576 으로 줄어든다.
모바일에서 이 차이는 그대로 발열과 배터리다.

---

## 1. 카메라가 둘인 이유 — 이걸 모르면 아래 70%에 쓰레기가 남는다

**뷰포트 밖은 지워지지 않는다.** 카메라는 자기 띠 안만 지우고 그리므로,
나머지 70%에는 **직전 프레임에 있던 것이 그대로 남는다.**
하단 판이 완전 불투명하면 가려지지만, 상단 HUD 액자에는 **구멍이 뚫려 있다**
(`player_hud_frame` 의 동그란 자리 · 체력 홈 — [`hud-layout.md`](hud-layout.md) 2-1절).
그 구멍으로 지워지지 않은 화면이 비친다.

그래서 **화면 전체를 지우는 카메라를 한 대 더 둔다.** 아무것도 그리지 않고
지우기만 하므로 비용은 거의 없다.

```
Background Camera   Priority −10   전체 화면    Culling Mask: Nothing   지우기만 한다
Stage Camera        Priority   0   띠           Culling Mask: 전부      세계를 그린다
```

URP 는 **Priority 가 낮은 Base 카메라부터** 그린다. 스플릿 스크린과 같은 구조다.

---

## 2. 완성형 계층 — 렌더링에 관계된 것만

`Ingame_Vertical` 의 루트는 아래 넷이다.

```
Ingame_Vertical
 │
 ├─ Background Camera  (GameObject)          ← 새로 만든다
 │    ├─ [C] Transform          Pos (0, 0, -10)
 │    ├─ [C] Camera
 │    │        Projection        → Orthographic
 │    │        Culling Mask      → **Nothing**            ★ 아무것도 그리지 않는다
 │    │        Viewport Rect     → X 0 · Y 0 · W 1 · H 1  (건드리지 않는다)
 │    └─ [C] UniversalAdditionalCameraData
 │             Render Type       → **Base**
 │             Priority          → **−10**                ★ Stage 보다 낮아야 한다
 │             Background Type   → **Solid Color** (검정)  ★ 이게 지우는 일을 한다
 │             Post Processing   → 끔
 │
 ├─ Stage Camera  (GameObject)               ← 전투 화면
 │    ├─ [C] Transform          Pos (0, 0, -10)
 │    ├─ [C] Camera
 │    │        Projection        → Orthographic
 │    │        Size              → 5      ※ StageViewport 가 재생 시 덮어쓴다
 │    │        Culling Mask      → Everything
 │    │        Viewport Rect     → 건드리지 않는다        ★ StageViewport 가 정한다
 │    ├─ [C] UniversalAdditionalCameraData
 │    │        Render Type       → Base
 │    │        Priority          → 0
 │    │        Background Type   → Solid Color (검정)
 │    ├─ [C] StageViewport
 │    │        Top Inset                     → 0.14
 │    │        Height Fraction               → 0.30
 │    │        Compensate Orthographic Size  → 켬          ★
 │    │        Full Screen Orthographic Size → 5           ★ 가로 씬과 같은 값
 │    └─ [C] CameraFollow
 │             Target        → 비움 (ServiceRegistry 로 플레이어를 찾는다)
 │             Bounds Source → 비움 또는 아래 Arena 의 Floor 타일맵
 │
 ├─ Grid  (GameObject)                        ← 사냥터. 3절
 │    └─ [C] Grid   Cell Size (1, 1, 0)
 │
 └─ Global Light 2D  (GameObject)             (기존)
      └─ [C] Light2D   Global · Intensity 1
```

**만드는 메뉴 경로** — 두 카메라 모두 `GameObject > Camera`.
`UniversalAdditionalCameraData` 는 URP 프로젝트에서 카메라를 만들면 자동으로 붙는다
(인스펙터의 Render Type · Priority · Background Type 칸이 그것이다).

> **`Stage Camera` 의 Viewport Rect 를 인스펙터에서 손으로 맞추지 말 것.**
> `StageViewport` 가 `OnEnable` 과 `OnValidate` 에서 덮어쓴다.
> 손으로 맞춘 값은 두 곳에 같은 숫자를 두는 것이라 반드시 어긋난다.

---

## 3. 사냥터는 방 하나면 된다

세로는 **토템도 층 이동도 없다.** 몬스터가 끊임없이 나오고 그걸 잡는 것이 전부라
(2026-08-09 확정) 가로처럼 구역 13개를 잇는 구조가 필요 없다.

```
 ├─ Grid  (GameObject)
 │    ├─ [C] Grid
 │    │
 │    ├─ Floor  (GameObject)
 │    │    ├─ [C] Tilemap · TilemapRenderer
 │    │    └─ ※ 콜라이더 없음. 바닥은 막지 않는다
 │    │
 │    └─ Guide  (GameObject)                  벽
 │         ├─ [C] Tilemap · TilemapRenderer
 │         ├─ [C] TilemapCollider2D
 │         ├─ [C] Rigidbody2D   Body Type **Static**   ★
 │         └─ [C] CompositeCollider2D
```

타일은 `Art/Maps/Base/Campsite/Tiles` 를 쓴다 — 거점 성격의 배경이라
끝없이 사냥하는 화면에 맞는다.

**넓이는 카메라가 보는 것보다 조금만 크면 된다.** 위 설정에서 화면에 들어오는 것은
가로 **5.625유닛 · 세로 3유닛**(약 6 × 3칸)이다. `FloorPopulation` 이
플레이어 주변 고리에 뿌리므로 **20 × 12칸 정도**면 스폰 고리가 안에 들어온다.

> 정확한 시야는 `StageViewport` 가 계산한다. `Height Fraction` 을 바꾸면
> 세로 시야는 그대로고 **가로 시야만** 바뀐다는 점에 주의한다.

---

## 4. 왜 `orthographicSize` 를 건드려야 하는가

**띠를 만들면 그림이 작아진다.** `Size` 는 화면 높이의 절반을 **월드 유닛**으로 정하는 값이라,
뷰포트가 30%로 줄어도 5 를 그대로 두면 같은 세로 10유닛을 1/3 높이의 픽셀에 넣게 된다.

```
전체 화면  1080 × 1920 · size 5     세로 10유닛 / 1920px  = 0.0052 유닛/px
띠 (보정 없음)  1080 × 576 · size 5     세로 10유닛 /  576px  = 0.0174 유닛/px   ← 3.3배 작아짐
띠 (보정 있음)  1080 × 576 · size 1.5   세로  3유닛 /  576px  = 0.0052 유닛/px   ← 같다
```

찌그러지지는 않는다. **작아질 뿐이다** — 가로도 함께 3.3배 넓어지기 때문이다.
`Compensate Orthographic Size` 를 켜면 `size = 5 × 0.30 = 1.5` 가 되어
픽셀당 크기가 전체 화면과 같아지고, **가로로 보이는 범위(5.625유닛)까지 그대로**가 된다.
세로 모드는 위아래만 잘린 셈이 된다.

> `CameraFollow` 는 매 프레임 `orthographicSize` 와 `aspect` 를 다시 읽으므로
> 보정된 값으로도 경계 가두기가 그대로 동작한다. 손볼 것이 없다.

---

## 5. 확인 절차

1. `Ingame_Vertical` 을 열고 **Game 뷰의 해상도를 1080 × 1920** 으로 맞춘다
2. 재생하지 않은 상태에서 `Stage Camera` 의 `Viewport Rect` 를 본다 →
   `Y 0.56 · H 0.30` 으로 **이미 채워져 있어야 한다** (`ExecuteAlways`)
3. 재생 → 세계가 **화면 위쪽 30% 띠에만** 그려진다
4. 나머지 70%가 **검게 지워져 있다.** 잔상이 남으면 `Background Camera` 의
   Priority 가 Stage 보다 낮은지, Background Type 이 Solid Color 인지 본다
5. 캐릭터 크기가 **가로 모드와 같아 보인다.** 작아 보이면
   `Compensate Orthographic Size` 가 꺼져 있다
6. `Top Inset` 을 0.3 쯤으로 크게 바꿔 본다 → 띠가 **아래로 내려간다**
   (높이는 그대로). 되돌려 놓을 것

---

## 6. 이 문서 범위 밖

- **스폰과 자동 사냥** — `FloorPopulation`(토템 없이 인스펙터 값으로 채운다) ·
  `AutoBattle`. [`monster-spawn-setup.md`](monster-spawn-setup.md) 와 함께 별도 문서로
- **하단 조작판** — 스킬 4칸 · 메뉴 · 재화. `UIRoot` 의 `Portrait Only` 에 들어간다
- **상단 HUD** — `PlayerHudView` 는 두 모드에 다 뜬다 ([`hud-layout.md`](hud-layout.md) 2-1절)
- **정렬(Sorting)** — 마지막에 한 번에 ([`../TODO.md`](../TODO.md) "정렬 일괄 지정")
