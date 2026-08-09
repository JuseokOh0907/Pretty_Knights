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
  │   전투 화면         │   카메라가 그리는 유일한 띠  0.25  (480 px)
  │                    │
  ├────────────────────┤  0.61
  │                    │
  │                    │
  │   하단 — 상점       │   재화 · 상점 6칸 · 메뉴    0.61 (1171 px)
  │                    │
  │                    │
  └────────────────────┘  0.00
```

`Height Fraction 0.25` 는 **2026-08-09 확정**이다. 1080 × 1920 기준으로
전투 화면이 480px, 남는 **1440px** 을 상점 등 하단 UI 가 쓴다.

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
 │    │        Height Fraction               → 0.25
 │    │        Compensate Orthographic Size  → 켬          ★
 │    │        Full Screen Orthographic Size → 5           ★ 가로 씬과 같은 값
 │    │        Zoom                          → 1.5         ★ 세로는 당겨 본다
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
끝없이 파밍하는 화면에 맞는다.

**넓이는 카메라가 보는 것보다 조금만 크면 된다.** 위 설정(0.25 · Zoom 1.5)에서
화면에 들어오는 것은 가로 **3.75유닛 · 세로 1.67유닛**(약 4 × 2칸)이다.
`ObstacleField` 가 플레이어 주변 **2.5~7유닛** 고리에 뿌리므로
**20 × 20칸 정도**면 고리가 넉넉히 들어온다.

`Guide` 로 사방을 막아 두면 자동 이동이 맵 밖으로 나가지 않는다.

> 정확한 시야는 `StageViewport` 가 계산한다. `Height Fraction` 을 바꾸면
> 세로 시야는 그대로고 **가로 시야만** 바뀐다는 점에 주의한다.

---

## 4. 왜 `orthographicSize` 를 건드려야 하는가

**띠를 만들면 그림이 작아진다.** `Size` 는 화면 높이의 절반을 **월드 유닛**으로 정하는 값이라,
뷰포트가 25%로 줄어도 5 를 그대로 두면 같은 세로 10유닛을 1/4 높이의 픽셀에 넣게 된다.

```
전체 화면       1080 × 1920 · size 5       세로 10유닛 / 1920px  = 0.0052 유닛/px
띠 (보정 없음)   1080 ×  480 · size 5       세로 10유닛 /  480px  = 0.0208 유닛/px  ← 4배 작아짐
띠 (보정 있음)   1080 ×  480 · size 1.25    세로 2.5유닛 /  480px = 0.0052 유닛/px  ← 가로와 같다
띠 (+ Zoom 1.5)  1080 ×  480 · size 0.833   세로 1.67유닛 / 480px = 0.0035 유닛/px  ← 1.5배 커진다
```

찌그러지지는 않는다. **작아질 뿐이다** — 가로도 함께 4배 넓어지기 때문이다.
`Compensate Orthographic Size` 를 켜면 `size = 5 × 0.25 = 1.25` 가 되어
픽셀당 크기가 전체 화면과 같아지고, **가로로 보이는 범위(5.625유닛)까지 그대로**가 된다.

여기에 `Zoom 1.5` 를 나누면 `size = 0.833` 이 되어 **모든 것이 1.5배로 커진다.**
가로 시야는 5.625 ÷ 1.5 = **3.75유닛**(약 4칸)으로 좁아지고,
캐릭터 폭 0.72유닛이 화면의 29%를 차지한다 (가로에서는 13%).

> **캐릭터만 1.5배로 키우는 것은 카메라가 아니라 프리팹의 일이다.**
> 세로 전용 `Player_Vertical` 프리팹의 `Visual` 스케일로 잡는다 —
> 공용 `Player.prefab` 을 건드리면 가로까지 커진다.

> `CameraFollow` 는 매 프레임 `orthographicSize` 와 `aspect` 를 다시 읽으므로
> 보정된 값으로도 경계 가두기가 그대로 동작한다. 손볼 것이 없다.

---

## 5. 확인 절차

1. `Ingame_Vertical` 을 열고 **Game 뷰의 해상도를 1080 × 1920** 으로 맞춘다
2. 재생하지 않은 상태에서 `Stage Camera` 의 `Viewport Rect` 를 본다 →
   `Y 0.61 · H 0.25` 로 **이미 채워져 있어야 한다** (`ExecuteAlways`)
3. 재생 → 세계가 **화면 위쪽 25% 띠(480px)에만** 그려진다
4. 나머지 75%(1440px)가 **검게 지워져 있다.** 잔상이 남으면 `Background Camera` 의
   Priority 가 Stage 보다 낮은지, Background Type 이 Solid Color 인지 본다
5. 캐릭터 크기가 **가로 모드보다 1.5배 커 보인다.** 그대로면
   `Compensate Orthographic Size` 가 꺼져 있거나 `Zoom` 이 1 로 남아 있다
6. `Top Inset` 을 0.3 쯤으로 크게 바꿔 본다 → 띠가 **아래로 내려간다**
   (높이는 그대로). 되돌려 놓을 것

---

## 6. 장애물이 계속 나오게 한다

세로는 몬스터가 아니라 **부술 수 있는 장애물**을 파밍한다 (결정 009 §2).
`Grid` 와 나란히 빈 게임오브젝트를 하나 두고 아래를 붙인다.

```
 ├─ Field  (GameObject)                       ← 뽑힌 장애물이 이 아래에 쌓인다
 │    ├─ [C] Transform          (0, 0, 0)
 │    ├─ [C] WalkableArea
 │    │        Floor / Guide → 위 Grid 의 두 타일맵          ★
 │    └─ [C] ObstacleField
 │             Obstacle Prefab   → Assets/Prefabs/Prop.prefab   ★
 │             Entries           → PropDefinition + 가중치       ★
 │                                 ※ 토템(Main/Sub)은 넣지 않는다.
 │                                   세로에는 지분 모델이 돌 곳이 없다
 │             Target Count      → 8
 │             Spawn Interval    → 0.6
 │             Debris Duration   → 1.2
 │             Min / Max Spawn Distance → 2.5 / 7
 │             Despawn Distance  → 14
 │             Min Separation    → 1.6
 │             Area              → 비움 (같은 오브젝트의 WalkableArea 를 찾는다)
```

플레이어 프리팹(`Player_Vertical`)에는 `AutoBattle` 이 붙어 있어야 한다.
장애물도 `IDamageable` 이므로 **대상 고르기·이동·때리기가 그대로 동작한다** —
자동 사냥 코드에 고칠 것이 없었다.

### 확인할 것

1. 재생 → 플레이어 주변에 장애물이 하나씩 늘어 **8개**에서 멈춘다
2. 자동으로 다가가 부순다 → 부서진 그림이 1.2초 남았다가 사라진다
3. 부서진 자리가 아니라 **다른 곳**에서 새것이 나온다
4. 콘솔에 `[보상] … · 골드 N` 이 찍히고 상단 재화 칸이 오른다
5. `ObstacleField` 우클릭 → **장애물 상태** 로 개수를 본다
6. 한참 두어도 `Field` 아래 자식이 8~10개를 넘지 않는다 (꺼진 몸을 다시 쓴다)

---

## 7. 세로 전용 플레이어 프리팹 — `Player_Vertical`

`Player.prefab` 을 복제해 만든다. **원본은 건드리지 않는다** — 캐릭터를 키우는 것은
카메라가 아니라 프리팹의 일이라, 공용 프리팹을 고치면 가로까지 커진다.

```
Player_Vertical  (프리팹, Player.prefab 을 복제)
   ├─ (기존 컴포넌트 전부 그대로)
   ├─ [C] AutoBattle                     ← 추가
   │        Active Mode        → Vertical
   │        Force Always On    → 끔
   │        Search Radius      → 12
   │        Engage Distance    → 1.1     ※ 장애물 접지폭(약 1.5)보다 짧게
   │        Retarget Interval  → 0.35
   │        Wander Radius      → 4
   │
   └─ Visual  (기존 자식)
         └─ [C] Transform   **Scale (1.5, 1.5, 1.5)**   ★ 이것이 "1.5배" 의 실체
```

`ISkillBar`(`PlayerSkillBar`)는 **떼지 않는다.** 세로에 스킬 버튼이 없을 뿐,
가로로 전환하면 그대로 필요하다.

> `AutoBattle.Engage Distance` 를 장애물 접지폭보다 넉넉히 짧게 잡을 것.
> 길면 사거리 끝에서 멈췄다가 장애물이 없어지는 순간(부서짐) 한 걸음 더 다가가
> **다음 장애물과 부딪혀 밀리는 것처럼 보인다.**

---

## 8. 상점이 스킬 자리를 대신 채운다

세로 하단에는 스킬 버튼이 없다. **`ShopView` 가 그 자리를 통째로 차지한다**
(결정 009 §4) — 골드를 쓰는 곳은 세로 모드에만 있다.

### 칸 나누기 — 아래 1440px 안

```
1440
 ├─ CurrencyBar    120     골드
 ├─ (여백)          40
 ├─ ShopList      1040     카드 2열 × 3행(6칸) 스크롤 없이 다 보인다
 ├─ (여백)          40
 └─ MenuBar        200     인벤토리 · 설정 등 (이 문서 범위 밖)
```

카드 크기 계산: 좌우 여백 32 · 열 간격 24 →
`(1080 − 32×2 − 24) ÷ 2 = 496` 폭, 세로는 `(1040 − 행간격 24×2) ÷ 3 = 331`.

### 계층 — `PlayerHud` 와 마찬가지로 세로 전용 패널 안에 둔다

```
 │    ├─ VerticalBottom  (GameObject)          ← UIRoot 의 Portrait Only 에 등록 ★
 │    │    ├─ [C] RectTransform
 │    │    │        Anchor (0, 0)~(1, 0.75)     ★ 전투 화면 밑 경계(0.61)보다 낮게 잡아
 │    │    │                                      여백을 캔버스가 알아서 흡수하게 한다
 │    │    │        오프셋 0
 │    │    │
 │    │    ├─ CurrencyBar  (GameObject)
 │    │    │    ├─ [C] RectTransform   Anchor (0,1)~(1,1) · Pivot (0.5,1) · Pos (0,0) · H 120
 │    │    │    ├─ Icon  (GameObject)         [C] Image  골드 아이콘
 │    │    │    └─ Amount  (GameObject)
 │    │    │         └─ [C] CurrencyView / TextMeshProUGUI    ★
 │    │    │
 │    │    ├─ ShopList  (GameObject)
 │    │    │    ├─ [C] RectTransform   Anchor (0,1)~(1,1) · Pos (0,-160) · H 1040
 │    │    │    ├─ [C] GridLayoutGroup
 │    │    │    │        Cell Size      → (496, 331)
 │    │    │    │        Spacing        → (24, 24)
 │    │    │    │        Constraint     → Fixed Column Count = 2
 │    │    │    ├─ [C] ShopView
 │    │    │    │        Slots  → 아래 카드 6개                ★
 │    │    │    │
 │    │    │    ├─ Slot_1  (GameObject)          ← 아래 5개는 이것을 복제한다
 │    │    │    │    ├─ [C] RectTransform  (GridLayoutGroup 이 크기·위치를 정한다)
 │    │    │    │    ├─ [C] CanvasGroup     ★ 필수. 못 사면 알파로 그린다
 │    │    │    │    ├─ [C] Image           카드 배경 · Raycast Target 켬
 │    │    │    │    ├─ [C] Button
 │    │    │    │    ├─ [C] ShopSlotView
 │    │    │    │    │        Icon                → 아래 Icon 의 Image             ★
 │    │    │    │    │        Name Label          → 아래 Name 의 TextMeshProUGUI    ★
 │    │    │    │    │        Description Label   → 아래 Desc 의 TextMeshProUGUI
 │    │    │    │    │        Level Label         → 아래 Level 의 TextMeshProUGUI
 │    │    │    │    │        Cost Label          → 아래 Cost 의 TextMeshProUGUI     ★
 │    │    │    │    │        Group / Buy Button  → 비워도 자동
 │    │    │    │    │        Unaffordable Alpha  → 0.45
 │    │    │    │    │
 │    │    │    │    ├─ Icon    (GameObject)   [C] Image   Raycast Target 끔
 │    │    │    │    ├─ Name    (GameObject)   [C] TextMeshProUGUI
 │    │    │    │    ├─ Desc    (GameObject)   [C] TextMeshProUGUI  작은 글자
 │    │    │    │    ├─ Level   (GameObject)   [C] TextMeshProUGUI  "Lv.2"
 │    │    │    │    └─ Cost    (GameObject)   [C] TextMeshProUGUI  골드 아이콘 + 숫자
 │    │    │    │
 │    │    │    ├─ Slot_2 ~ Slot_6
 │    │    │
 │    │    └─ MenuBar  (GameObject)             이 문서 범위 밖
```

**만드는 메뉴 경로** — `CurrencyBar` `ShopList` `Slot_1` 은 `UI > Image` 로 만들고
`Label` 류는 `UI > Text - TextMeshPro`.

> **`ShopView.Slots` 배열은 6개 고정이다.** `GameRoot.Shop Offers` 의 목록이
> 6개를 넘으면 콘솔에 경고가 뜨고 초과분은 화면에 뜨지 않는다.
> 지금은 스크롤을 만들지 않았다 — 6칸이면 강화 5종 + 포션 1종 정도로 충분하다는 가정이다.
> 늘어나면 `ShopList` 에 `ScrollRect` 를 씌우는 작업이 별도로 필요하다.

### 못 사는 이유는 카드가 미리 보여준다

`ShopSlotView` 는 버튼을 누르기 전에 이미 흐려져 있다 — 값이 모자라거나
강화 상한에 닿으면 알파가 낮아지고 버튼이 눌리지 않는다.
`Cost Label` 은 상한에 닿으면 숫자 대신 `MAX` 를 띄운다.

### 확인할 것

1. `GameRoot` 인스펙터의 `Shop Offers` 에 `ShopOffer` 에셋을 몇 개 넣고 재생한다
2. 하단에 카드가 진열된다. 순서는 배열 순서와 같다
3. 장애물을 부숴 골드를 모은다 → 카드가 **살 수 있게 되는 순간 알파가 올라간다**
4. 강화 카드를 누른다 → 골드가 빠지고 `Lv.` 숫자가 오른다.
   상단 `PlayerHudView` 의 스탯 숫자도 함께 오른다
   (`Shop.ApplyUpgrades()` 가 `PlayerRuntimeState.SetBonusStats` 를 부른다)
5. 상한까지 사면 값 칸이 `MAX` 로 바뀌고 카드가 눌리지 않는다
6. 소모품 카드를 누른다 → 인벤토리에 들어간다. 가방이 가득 차면
   콘솔에 `가방이 가득 찼습니다` 가 뜨고 **골드는 빠지지 않는다**
7. 재생을 멈췄다 다시 시작한다 → 강화 단계가 유지된다 (세이브에 `PlayerUpgrades` 로 남는다)

---

## 9. 이 문서 범위 밖

- **`ShopOffer` 에셋의 실제 값** — 사용자가 직접 채운다
- **인벤토리 · 설정 메뉴** — 하단의 `MenuBar`
- **상단 HUD** — `PlayerHudView` 는 두 모드에 다 뜬다 ([`hud-layout.md`](hud-layout.md) 2-1절)
- **정렬(Sorting)** — 마지막에 한 번에 ([`../TODO.md`](../TODO.md) "정렬 일괄 지정")
