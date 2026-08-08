# HUD 배치 가이드 — 스킬 버튼과 하단 조작 줄

> **Boot 씬의 `UIRoot/Canvas` 를 다시 잰다.** 스킬 버튼 4개와 탈출 버튼이 들어오면서
> 흩어져 있던 버튼들을 **위 줄(메뉴)** 과 **아래 줄(조작)** 두 덩어리로 정리한다.
>
> 표기 — `[C]` 는 컴포넌트, 들여쓰기는 부모-자식.
> `★` 는 **반드시 손으로 채워야** 하는 칸. 표시 없는 칸은 비우면 자동으로 찾는다.

---

## 0. 좌표를 읽는 법 — 이걸 모르면 숫자가 안 맞는다

`UIRoot` 의 가로 기준은 **1920 × 1080 · Match 1(높이 기준)** 이다.
Match 가 1 이므로 **높이만 1080 으로 고정되고 너비는 화면 비율을 따라간다.**

```
캔버스 너비(기준 단위) = 1080 × 화면비

  16 : 9   →  1920     이 문서의 그림
  20 : 9   →  2400     요즘 폰. 좌우가 더 넓어진다
   4 : 3   →  1440     태블릿. 좌우가 좁아진다
```

**그래서 모든 버튼을 화면 모서리에 앵커한다.** 모서리에 붙은 것은 기기가 바뀌어도
그 모서리에서 같은 거리에 있다. 가운데 정렬이나 절대 X 좌표를 쓰면 폰마다 어긋난다.

아래 표의 좌표는 전부 **자기 앵커 모서리로부터의 거리**다.

---

## 1. 무엇이 어디로 가는가

| | 지금 | 바뀐 뒤 |
|---|---|---|
| 모드 전환 | 우상단 `(-30, -30)` 홀로 | **`TopBar`** 안으로. 여백 16, 왼쪽으로 버튼이 늘어난다 |
| 조이스틱 | 좌하단 `(100, 100)` 360² | **그대로** |
| 공격 | 우측 중단 `(-220, 520)` | **조이스틱의 좌우 대칭 자리** `(-130, 130)` |
| 상호작용 | `(-220, 240)` | 공격과 **같은 높이**, 왼쪽으로 `(-460, 150)` |
| 스킬 4개 | 없음 | **공격 버튼 바로 위** `(-130, 460)` |
| 탈출 | 없음 | 상호작용 **왼쪽** `(-750, 180)` |

### 공격 버튼이 왜 `(-130, 130)` 인가

"조이스틱의 Y축 반전 위치" 를 **중심끼리** 맞춘 값이다.

```
조이스틱  Controls   앵커(0,0) 피벗(0,0)  pos(100,100)  size 360
          → 왼쪽에서 100~460, 아래에서 100~460 → 중심 (280, 280)

공격      앵커(1,0) 피벗(1,0)  size 300
          중심을 오른쪽에서 280 에 두려면  pos.x = -(280 - 150) = -130
          중심을 아래에서   280 에 두려면  pos.y =  280 - 150  =  130
```

상호작용(260²)과 탈출(200²)도 **같은 중심 높이 280** 에 맞춘다.
크기가 다르므로 `pos.y` 는 각각 150 과 180 으로 달라진다 — 피벗이 바닥이기 때문이다.

### 아래 줄이 오른쪽부터 어떻게 놓이는가

전부 오른쪽 모서리 기준. 사이 간격은 **30** 으로 통일했다.

```
화면 오른쪽 ─┤
             │◄─130─►│◄────300────►│◄30►│◄───260───►│◄30►│◄──200──►│
             │       공격          │    상호작용     │    탈출     │
             130     430           460   720         750   950
```

---

## 2. 완성형 계층

`Canvas` 의 **직속 자식은 7개**다 (지금은 5개).
`FadeOverlay` 는 **반드시 맨 아래**에 둔다 — Overlay 캔버스는 계층 순서가 곧 그리기 순서라
위로 올리면 페이드가 다른 UI 밑에 깔린다.

```
UIRoot  (GameObject)
 ├─ [C] Transform
 ├─ [C] UIRoot
 │        Landscape Only → Controls · SkillBar · AttackButton
 │                         · InteractButton · EscapeButton   (5개) ★
 │                         ※ TopBar 는 넣지 않는다. 모드 전환은 세로에서도 필요하다
 │
 ├─ Canvas  (GameObject)
 │    ├─ [C] RectTransform
 │    ├─ [C] Canvas / CanvasScaler / GraphicRaycaster        (기존)
 │    │
 │    ├─ TopBar  (GameObject)                    ← 새로 만든다
 │    │    ├─ [C] RectTransform
 │    │    │        Anchor (1,1)~(1,1) · Pivot (1,1)
 │    │    │        Pos (-16, -16)      Size 는 아래 Fitter 가 정한다
 │    │    ├─ [C] HorizontalLayoutGroup
 │    │    │        Reverse Arrangement → 켬   ★ 이게 "오른쪽부터 왼쪽으로" 를 만든다
 │    │    │        Child Alignment     → Upper Right
 │    │    │        Spacing             → 12
 │    │    │        Padding             → 전부 0
 │    │    │        Control Child Size  → Width·Height 끔
 │    │    │        Child Force Expand  → Width·Height 끔
 │    │    └─ [C] ContentSizeFitter
 │    │             Horizontal Fit → Preferred Size
 │    │             Vertical Fit   → Preferred Size
 │    │
 │    │    └─ ModeSwitchButton  (GameObject)     ← 기존 것을 여기로 옮긴다
 │    │         ├─ [C] RectTransform   Size 160 × 160
 │    │         │        ※ Pos 는 레이아웃 그룹이 덮어쓴다. 손으로 맞추지 말 것
 │    │         ├─ [C] LayoutElement   Preferred Width 160 · Preferred Height 160   ★
 │    │         ├─ [C] Image / Button / ModeSwitchButton     (기존)
 │    │         └─ Text (TMP)  (GameObject)      (기존)
 │    │              ├─ [C] RectTransform   Stretch 전체 · 여백 0
 │    │              └─ [C] TextMeshProUGUI
 │    │
 │    ├─ Controls  (GameObject)                  (기존, 손대지 않는다)
 │    │    ├─ [C] RectTransform   Anchor (0,0) · Pivot (0,0) · Pos (100,100) · Size 360 × 360
 │    │    └─ JoystickArea └ Handle  [C] OnScreenStick
 │    │
 │    ├─ SkillBar  (GameObject)                  ← 새로 만든다. 자식 4개
 │    │    ├─ [C] RectTransform
 │    │    │        Anchor (1,0)~(1,0) · Pivot (1,0)
 │    │    │        Pos (-130, 460)      Size (608, 140)
 │    │    └─ [C] HorizontalLayoutGroup
 │    │             Reverse Arrangement → 끔   ※ 왼쪽부터 슬롯 0,1,2,3
 │    │             Child Alignment     → Middle Center
 │    │             Spacing             → 16
 │    │             Control Child Size  → 끔 · Child Force Expand → 끔
 │    │
 │    │    ├─ Skill_1  (GameObject)              ← 아래 3개는 이것을 복제한다
 │    │    │    ├─ [C] RectTransform   Size 140 × 140
 │    │    │    ├─ [C] LayoutElement   Preferred Width 140 · Preferred Height 140   ★
 │    │    │    ├─ [C] CanvasGroup     ★ 필수. 잠김·쿨타임을 알파로 그린다
 │    │    │    ├─ [C] Image           버튼 테두리 · Raycast Target 켬
 │    │    │    ├─ [C] Button
 │    │    │    ├─ [C] SkillButton
 │    │    │    │        Slot Index → 0    ★ 버튼마다 0 / 1 / 2 / 3
 │    │    │    │        Icon           → 아래 Icon 의 Image    ★
 │    │    │    │        Cooldown Fill  → 아래 Fill 의 Image    ★
 │    │    │    │        Group / Button → 비워도 자동
 │    │    │    │        Locked Alpha   → 0.2
 │    │    │    │        Cooldown Alpha → 0.45
 │    │    │    │
 │    │    │    ├─ Icon  (GameObject)            스킬 그림이 들어갈 자리
 │    │    │    │    ├─ [C] RectTransform   Stretch 전체 · 여백 14
 │    │    │    │    └─ [C] Image           Raycast Target **끔**
 │    │    │    │
 │    │    │    └─ Fill  (GameObject)            쿨타임. ★ Icon 보다 아래(= 위에 그려짐)
 │    │    │         ├─ [C] RectTransform   Stretch 전체 · 여백 0
 │    │    │         └─ [C] Image
 │    │    │                  Image Type   → Filled      ★
 │    │    │                  Fill Method  → Radial 360
 │    │    │                  Fill Origin  → Top · Clockwise 켬
 │    │    │                  Color        → 검정 알파 0.55 정도
 │    │    │                  Raycast Target **끔**
 │    │    │
 │    │    ├─ Skill_2   Slot Index → 1
 │    │    ├─ Skill_3   Slot Index → 2
 │    │    └─ Skill_4   Slot Index → 3
 │    │
 │    ├─ AttackButton  (GameObject)              (기존 · RectTransform 만 고친다)
 │    │    ├─ [C] RectTransform
 │    │    │        Anchor (1,0)~(1,0) · Pivot (1,0)
 │    │    │        Pos (-130, 130)      Size (300, 300)      ← 기존 (-220, 520)
 │    │    ├─ [C] CanvasGroup / Image / Button / AttackButton  (기존)
 │    │    └─ Fill  (GameObject)                 (기존)
 │    │
 │    ├─ InteractButton  (GameObject)            (기존 · RectTransform 과 새 칸 하나)
 │    │    ├─ [C] RectTransform
 │    │    │        Anchor (1,0)~(1,0) · Pivot (1,0)
 │    │    │        Pos (-460, 150)      Size (260, 260)      ← 기존 (-220, 240)
 │    │    ├─ [C] CanvasGroup / Image / Button   (기존)
 │    │    ├─ [C] InteractButton
 │    │    │        Fallback Label  → 사용
 │    │    │        Disabled Alpha  → 0.35        ← **새 칸.** 0 으로 두면 예전처럼 사라진다
 │    │    └─ Label  (GameObject)                (기존)
 │    │
 │    ├─ EscapeButton  (GameObject)              ← 새로 만든다
 │    │    ├─ [C] RectTransform
 │    │    │        Anchor (1,0)~(1,0) · Pivot (1,0)
 │    │    │        Pos (-750, 180)      Size (200, 200)
 │    │    ├─ [C] CanvasGroup           ★ 필수
 │    │    ├─ [C] Image                 Raycast Target 켬
 │    │    ├─ [C] Button
 │    │    ├─ [C] EscapeButton
 │    │    │        Disabled Alpha → 0.35
 │    │    │        Group / Button → 비워도 자동
 │    │    └─ Label  (GameObject)
 │    │         ├─ [C] RectTransform    Stretch 전체 · 여백 12
 │    │         └─ [C] TextMeshProUGUI  "탈출" · 가운데 정렬 · Auto Size 켬
 │    │
 │    └─ FadeOverlay  (GameObject)               (기존) ★ 형제 중 맨 아래를 유지할 것
 │
 └─ EventSystem  (GameObject)                    (기존)
```

**만드는 메뉴 경로** — `TopBar` `SkillBar` `Skill_1` `EscapeButton` 은 `UI > Image` 로 만들고
필요 없는 `Image` 는 지운다(`TopBar` `SkillBar` 는 컨테이너라 그림이 없다).
`Label` 은 `UI > Text - TextMeshPro`. **레거시 `UI > Text` 를 쓰지 않는다** — 이 프로젝트는
TMP 로 통일되어 있고, 섞으면 폰트 에셋과 자동 크기 동작이 갈린다.

---

## 3. 왜 이렇게 나눴는가

### 위 줄은 컨테이너가 필요하고, 아래 줄은 필요 없다

`TopBar` 만 `HorizontalLayoutGroup` 을 쓴다. **버튼이 계속 늘어날 자리이기 때문이다** —
인벤토리·스탯·설정이 붙을 때마다 좌표를 다시 재고 싶지 않다.
`Reverse Arrangement` 를 켜면 **새 버튼을 자식으로 떨어뜨리는 것만으로 왼쪽에 붙는다.**

아래 줄은 반대다. 버튼 셋이 각각 **크기가 다르고 중심 높이만 같으므로**
레이아웃 그룹으로 묶으면 오히려 크기를 강제로 맞추게 된다. 좌표를 직접 준다.

`SkillBar` 는 4개로 고정이지만 그룹을 쓴다. 간격이 균등해야 하는데
140 + 16 을 네 번 손으로 더하면 하나쯤 틀린다.

### 상호작용·탈출 버튼은 사라지지 않는다

둘 다 쓸 수 없을 때 **알파만 낮추고 자리는 지킨다** (2026-08-09 확정).

```
쓸 수 있음    alpha 1     누를 수 있음
쓸 수 없음    alpha 0.35  누를 수 없음 (blocksRaycasts 끔)
```

이유가 둘이다. 나타났다 사라지기를 반복하면 화면이 들썩이고,
무엇보다 **처음 하는 사람이 그런 버튼이 있다는 것 자체를 모른다.**
자리를 늘 지키면 "가까이 가면 켜지는 것" 이라고 읽힌다.

**흐리게만 하고 누를 수 있게 두면 안 된다.** 그러면 "눌렀는데 아무 일도 없다" 가 된다.
알파와 `blocksRaycasts` 를 함께 끄는 이유다.

### 스킬 버튼은 지금 전부 잠겨 있다

**스킬 3종(전방 베기 · 관통 직선 · 광역 폭발)이 아직 없다.**
`SkillButton` 은 `ISkillBar` 라는 창구를 바라보는데 그것을 등록하는 쪽이 아직 없어서
네 버튼 모두 `Locked Alpha` 로 그려진다. **그게 지금의 사실이므로 맞는 표시다.**

스킬이 붙을 때 고칠 것은 `Player.prefab` 쪽뿐이고 이 HUD 는 그대로 간다.
`ISkillBar` 를 구현해 `ServiceRegistry.Register<ISkillBar>(this)` 하면
버튼이 알아서 아이콘·쿨타임·잠김을 그린다.

---

## 4. 확인 절차

1. Boot 씬 재생 → 가로 모드로 전환
2. **상호작용 버튼이 처음부터 흐리게 보인다.** 라벨은 "사용"
3. 포탈 위로 걸어간다 → 또렷해지고 라벨이 목적지 이름으로 바뀐다
4. 포탈에서 벗어난다 → **사라지지 않고** 다시 흐려지며 라벨이 "사용" 으로 되돌아간다
5. **탈출 버튼** — 던전 층에서는 또렷, 던전 입구(#3)에서는 흐림
   (입구는 `Escape To` 가 비어 있다)
6. 탈출 버튼을 누른다 → 페이드 후 던전 입구 `from_escape` 지점
7. 스킬 버튼 4개는 **전부 흐리게** 보이고 눌리지 않는다
8. 세로 모드로 전환 → 조이스틱·공격·상호작용·탈출·스킬이 전부 사라지고
   **모드 전환 버튼만 남는다**

> 8번이 안 되면 `UIRoot` 의 `Landscape Only` 배열을 확인한다.
> `SkillBar` 와 `EscapeButton` 을 새로 넣어야 하고, **`TopBar` 는 넣으면 안 된다** —
> 넣으면 세로에서 모드 전환 버튼이 사라져 가로로 돌아올 방법이 없어진다.

---

## 5. 실기에서 확인할 것

에디터의 Free Aspect 로는 드러나지 않는다.

- **`TopBar` 의 여백 16 이 노치·둥근 모서리에 걸리는가.**
  가로에서는 노치가 좌우 짧은 변에 오므로 우상단 버튼이 위험하다.
  잘리면 24 로 올린다 — 세이프 에리어 대응을 넣기 전까지의 임시 조치다
- **4 : 3 태블릿에서 탈출 버튼과 조이스틱 사이가 30 밖에 안 남는다.**
  캔버스 너비가 1440 으로 줄어 탈출 버튼 왼쪽 끝이 490, 조이스틱 오른쪽 끝이 460 이다.
  폰(20:9, 너비 2400)에서는 990 이 남으므로 태블릿에서만 드러난다
- 스킬 버튼 140 이 손가락으로 구분되는가 (1080 기준 13%, 대략 9 mm)
- 아래 줄 네 덩어리가 엄지 하나로 다 닿는가

---

## 6. 이 문서 범위 밖

- **스킬 아이콘 아트** — `Icon` 의 `Image` 는 비워 둔 채로 둔다
- **스킬 구현** — `ISkillBar` 구현체. `docs/TODO.md` 5절
- **키보드 스킬 키** — 버튼과 같은 `ISkillBar.TryCast` 를 타야 한다.
  `InputSystem_Actions` 에 액션을 추가할 때 함께 잡는다
- **세이프 에리어** — 지금은 고정 여백이다. `Screen.safeArea` 대응은 별도 작업
- **정렬(Sorting)** — UI 는 Overlay 캔버스라 월드 정렬과 무관하다
