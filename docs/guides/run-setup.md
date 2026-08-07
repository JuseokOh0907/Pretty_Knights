# 실행까지 가는 Hierarchy 세팅 가이드

> **목표** — 재생을 눌러서 *걷고 · 때리고 · 죽이고 · 포탈로 층을 넘는 것*까지 한 번에 본다.
> 기능별 근거는 [`player-attack-setup.md`](player-attack-setup.md) 와
> [`portal-area-setup.md`](portal-area-setup.md) 에 있고, 이 문서는 **배치 순서만** 다룬다.
>
> 표기 — `[C]` 는 컴포넌트, 들여쓰기는 부모-자식. `★` 는 **반드시 손으로 연결**해야 하는 칸.
> 표시 없는 칸은 비워도 자동으로 찾는다.

---

## 0. 지금 무엇이 있고 무엇이 없는가

| 대상 | 상태 | 이 문서에서 |
|---|---|---|
| `MonsterDefinition` 10종 | ✅ `Assets/Data/Monsters/` | 그대로 쓴다 |
| `Player.prefab` | ⚠️ `PlayerAttack` 없음 | **3절** |
| 포탈 프리팹 3종 | ⚠️ 스프라이트·Animator뿐. **콜라이더·`Portal` 없음** | **4절** |
| `Boot.unity` | ⚠️ `GameRoot` · `UIRoot` 뼈대만 | **2절** |
| `Ingame_Horizontal` | ⚠️ 맵 9층만. 구역 컴포넌트 없음 | **5절** |
| 몬스터 프리팹 | ❌ **없다 — 때릴 대상이 없다** | **1-3절** |
| `CombatSettings` | ❌ 없다 | 1-1절 |
| `AreaDefinition` 3종 | ❌ 없다 | 1-2절 |

**순서대로 하지 않으면 중간에 연결할 대상이 없어 막힌다.** 에셋 → 프리팹 → Boot → 맵 순이다.

---

## 1. 에셋 먼저

### 1-1. `CombatSettings`

`Assets/Data/` 우클릭 → `Create > Pretty Knights > Combat Settings` → 이름 `CombatSettings`

| 항목 | 값 |
|---|---|
| Model | `Attenuate` |
| Defense Multiplier | 1.5 |
| Defense Multiplier Against Player | 0.25 |
| Attenuation Constant | 100 |
| **Minimum Damage** | **1** ← 0 이면 감산 모델에서 무적이 생긴다 |

> 공식은 확정된 것이 아니다. **재생 중에 `Model` 을 바꾸면 다음 타격부터 반영**되므로
> 실제로 때려보며 셋을 비교하고 고른다.

### 1-2. `AreaDefinition` 3종

`Assets/Data/` 에 `Areas` 폴더를 만들고 `Create > Pretty Knights > Area Definition` ×3

| 파일명 | Area Id | Display Name | Theme | Is Boss Floor | Escape To |
|---|---|---|---|---|---|
| `Area_Goblin_1F` | **101** | 고블린 소굴 1층 | Goblin | 끔 | 비움 |
| `Area_Goblin_2F` | **102** | 고블린 소굴 2층 | Goblin | 끔 | 비움 |
| `Area_Goblin_3F` | **103** | 고블린 소굴 3층 | Goblin | **켬** | 비움 |

### 1-3. 몬스터 프리팹 — **때릴 대상이 없으면 아무것도 확인 못 한다**

조립 절차는 [`monster-prefab-setup.md`](monster-prefab-setup.md) 에 있다. 요약하면:

```
Monster  (GameObject · 루트)
 ├─ [C] Transform              (0, 0, 0)
 ├─ [C] Rigidbody2D            Dynamic / Gravity Scale 0 / Freeze Rotation Z 켬
 ├─ [C] CapsuleCollider2D      Offset (0, 0.12) · Size (0.35, 0.25)
 │                             ★ Is Trigger **꺼짐** — 켜면 공격이 통과한다
 ├─ [C] CharacterMotor         Acceleration 0.08
 ├─ [C] MonsterController
 │         Definition       → ★ Monster_goblin_hob
 │         Knockback On Hit → 3
 │
 └─ Visual  (GameObject · 자식)
      ├─ [C] Transform                 (0, 0.375, 0)
      ├─ [C] SpriteRenderer            임시로 Knight 스프라이트에 색만 바꿔 쓴다
      ├─ [C] Animator                  비어 있어도 동작한다
      └─ [C] DirectionalAnimatorDriver
```

`Assets/Prefabs/Monsters/Monster.prefab` 으로 저장한다. **루트와 `Visual` 둘뿐이다.**

---

## 2. `Boot.unity` 완성형 계층

**루트 오브젝트는 `GameRoot` · `UIRoot` · `EventSystem` 셋뿐이다.** 새 루트를 만들지 않는다.

```
Boot.unity
│
├─ GameRoot  (GameObject)                          ← 루트 ①
│    ├─ [C] Transform
│    ├─ [C] GameRoot                                       (기존)
│    │        Player Stats     → ★ PlayerStatsDefinition
│    │        Combat Settings  → ★ CombatSettings          ← 추가
│    │        Start Mode       → Horizontal
│    │        Target Frame Rate → 60
│    │        Log Lifecycle    → 켬
│    │
│    ├─ [C] AreaTransition                                 ← 추가
│    │        Landing Search Radius → 3
│    │        Log Transitions       → 켬
│    │        Debug Destination     → 비움 (검증할 때만 채운다)
│    │        Debug Arrival Id        → from_1f  (없는 이름이면 대체 지점으로 간다)
│    │
│    └─ [C] InteractionHub                                 ← 추가
│             Input Actions → ★ InputSystem_Actions
│             Reuse Delay   → 0.3
│
├─ UIRoot  (GameObject)                            ← 루트 ②
│    ├─ [C] Transform
│    ├─ [C] UIRoot                                         (기존)
│    │        Portrait  Reference 1080×1920 · Match 0
│    │        Landscape Reference 1920×1080 · Match 1
│    │        Landscape Only → ★ 크기 3 : InteractButton · AttackButton · Controls
│    │
│    └─ Canvas  (GameObject)
│         ├─ [C] RectTransform
│         ├─ [C] Canvas            Screen Space - Overlay
│         ├─ [C] CanvasScaler      Scale With Screen Size
│         ├─ [C] GraphicRaycaster
│         │
│         ├─ ModeSwitchButton  (GameObject)                (기존)
│         │    ├─ [C] RectTransform
│         │    ├─ [C] Image
│         │    ├─ [C] Button
│         │    ├─ [C] ModeSwitchButton
│         │    └─ Text (TMP)  (GameObject)
│         │         ├─ [C] RectTransform
│         │         └─ [C] TextMeshProUGUI
│         │
│         ├─ Controls  (GameObject)                        (기존, 조이스틱)
│         │    ├─ [C] RectTransform
│         │    └─ JoystickArea  (GameObject)
│         │         ├─ [C] RectTransform
│         │         ├─ [C] Image
│         │         └─ Handle  (GameObject)
│         │              ├─ [C] RectTransform
│         │              ├─ [C] Image
│         │              └─ [C] OnScreenStick   <Gamepad>/leftStick
│         │
│         ├─ AttackButton  (GameObject)                    ← 추가
│         │    ├─ [C] RectTransform   Anchor 오른쪽아래 · Pivot (1,0)
│         │    │                      Pos (-220, 520) · Size (300, 300)
│         │    ├─ [C] CanvasGroup
│         │    ├─ [C] Image           Raycast Target 켬
│         │    ├─ [C] Button
│         │    ├─ [C] AttackButton
│         │    │        Cooldown Fill → ★ 아래 Fill 의 Image
│         │    │        Dimmed Alpha  → 0.45
│         │    └─ Fill  (GameObject)
│         │         ├─ [C] RectTransform   Stretch 전체 · 여백 0
│         │         └─ [C] Image           Type **Filled** · Radial 360
│         │                                Raycast Target **끔**
│         │
│         ├─ InteractButton  (GameObject)                  ← 추가
│         │    ├─ [C] RectTransform   Anchor 오른쪽아래 · Pivot (1,0)
│         │    │                      Pos (-220, 240) · Size (260, 260)
│         │    ├─ [C] CanvasGroup     ★ 필수
│         │    ├─ [C] Image
│         │    ├─ [C] Button
│         │    ├─ [C] InteractButton  Fallback Label → 사용
│         │    └─ Label  (GameObject)
│         │         ├─ [C] RectTransform   Stretch 전체 · 여백 12
│         │         └─ [C] TextMeshProUGUI  가운데 정렬 · Auto Size 켬
│         │
│         └─ FadeOverlay  (GameObject)                     ← 추가
│              ├─ [C] RectTransform   Stretch 전체 · 여백 0
│              ├─ [C] CanvasGroup     **Alpha 0 으로 저장할 것**
│              ├─ [C] Image           Color 검정 (0,0,0,255)
│              └─ [C] ScreenFader     Fade Out 0.25 · Fade In 0.18
│
└─ EventSystem  (GameObject)                       ← 루트 ③  (기존)
     ├─ [C] EventSystem
     └─ [C] InputSystemUIInputModule
```

**Canvas 자식은 다섯이고 순서가 중요하다.**
`FadeOverlay` 가 **맨 아래**여야 한다 — Overlay 캔버스는 계층 순서가 곧 그리기 순서라
위에 두면 조이스틱과 버튼이 페이드 위에 그려진다.

`AttackButton` 은 `InteractButton` **위쪽**에 놓는다. 겹치면 포탈 앞에서 공격이 안 눌린다.

### 여기서 틀리기 쉬운 것

1. **`InteractButton` 에 `CanvasGroup` 이 없으면 버튼이 한 번 사라진 뒤 다시 안 나온다.**
   자기 자신을 `SetActive(false)` 하지 않고 알파로만 숨기기 때문이다
2. **`InteractionHub` · `PlayerAttack` 의 `Input Actions` 를 비우면 키보드만 죽는다.**
   화면 버튼은 살아 있어 "PC 에서만 안 된다" 로 나타나 원인 찾기가 어렵다
3. `FadeOverlay` 의 `CanvasGroup Alpha` 를 1 로 두고 저장해도 재생 시 걷히지만,
   에디터에서 화면이 까매져 작업이 불편하다

---

## 3. `Player.prefab`

**루트와 `Visual` 둘뿐이다.** 루트에 컴포넌트 하나만 추가한다.

```
Player  (프리팹 루트)
 ├─ [C] Transform
 ├─ [C] Rigidbody2D                                (기존)
 ├─ [C] CapsuleCollider2D                          (기존)
 ├─ [C] CharacterMotor                             (기존)
 ├─ [C] PlayerController                           (기존)
 ├─ [C] PlayerHitReaction                          (기존)
 ├─ [C] PlayerAttack                               ← 추가
 │        Input Actions   → ★ InputSystem_Actions
 │        Shape           → Forward
 │        Shape Params    → Range 1.6 · Width 1 · Angle 100 · Forward Offset 0
 │        Origin Forward Offset → 0.3
 │        Cooldown        → 0.45
 │        Auto Aim        → 끔
 │        Target Layers   → Everything
 │        Draw Last Swing → 켬
 │        Log Hits        → 켬 (검증 동안만)
 │
 └─ Visual  (자식)
      ├─ [C] Transform                 (0, 0.375, 0)
      ├─ [C] SpriteRenderer            (기존)
      ├─ [C] Animator                  (기존)
      └─ [C] DirectionalAnimatorDriver (기존)
```

`Target Layers` 를 `Everything` 으로 둬도 안전하다. 실제 판정은 **`IDamageable` 유무**로 거르고
`useTriggers` 가 꺼져 있어 포탈 트리거는 애초에 안 걸린다.

---

## 4. 포탈 프리팹 마무리

지금 `Blue_Portal.prefab` 은 **루트 + 스프라이트 자식 둘뿐이고 콜라이더도 `Portal` 도 없다.**
루트에 둘을 추가한다.

```
Blue_Portal  (프리팹 루트)
 ├─ [C] Transform
 ├─ [C] BoxCollider2D                              ← 추가
 │        Size       (2, 2)      아트가 128px · PPU 64 라 정확히 2×2칸
 │        Offset     (0, 0)
 │        ★ Is Trigger **켬** — 끄면 벽이 되어 지나갈 수 없고 버튼도 안 뜬다
 │
 ├─ [C] Portal                                     ← 추가
 │        Prompt Label         → 비움 (목적지 이름이 자동으로 들어간다)
 │        Interactable         → 켬
 │        Destination          → ★ 씬에 놓은 뒤 인스턴스마다 지정
 │        Destination Arrival Id → ★ 인스턴스마다 지정
 │
 └─ portal_blue_entrance_animation_8x1_0  (자식)   (기존)
      ├─ [C] Transform
      ├─ [C] SpriteRenderer
      └─ [C] Animator
```

`Destination` 은 프리팹이 아니라 **씬에 놓인 인스턴스마다** 다르므로 프리팹에서는 비워 둔다.

`Gold_Portal`(보상) 과 `Red_Portal`(보스) 도 같은 두 컴포넌트를 붙여 두면 나중에 그대로 쓴다.

---

## 5. `Ingame_Horizontal` 완성형 계층

이번에는 **Goblin 3개 층만** 붙인다. 검증 후 Orc·Vampire 로 복제한다.

`AreaAnchor` 와 `WalkableArea` 는 **층 루트에 직접** 붙인다. 빈 자식을 따로 만들지 않는다 —
이 오브젝트를 켜고 끄는 것이 곧 구역 교체이므로 한 단계 내리면 활성 타이밍이 어긋난다.

```
Ingame_Horizontal.unity
│
├─ Map  (GameObject)
│    ├─ [C] Transform
│    ├─ [C] Grid                                   (기존)
│    ├─ [C] AreaRegistry                           ← 추가
│    │        Search Root → 비움
│    │
│    ├─ Goblin  (GameObject)          묶음용. 컴포넌트 없음
│    │   │
│    │   ├─ Goblin1F  (GameObject)    ← 시작 시 **켜 두는 유일한 층**
│    │   │    ├─ [C] Transform
│    │   │    ├─ [C] AreaAnchor                    ← 추가
│    │   │    │        Definition     → ★ Area_Goblin_1F
│    │   │    │        Floor          → ★ 1Floor 의 Tilemap
│    │   │    │        Walkable       → 비움
│    │   │    │        Fallback Arrival → ★ from_entrance
│    │   │    ├─ [C] WalkableArea                  ← 추가
│    │   │    │        Floor → ★ 1Floor  ·  Guide → ★ 1FGuide
│    │   │    │        Max Attempts → 24
│    │   │    │
│    │   │    ├─ 1Floor  (GameObject)              (기존)
│    │   │    │    ├─ [C] Tilemap
│    │   │    │    └─ [C] TilemapRenderer
│    │   │    ├─ 1FGuide  (GameObject)             (기존)
│    │   │    │    ├─ [C] Tilemap
│    │   │    │    ├─ [C] TilemapRenderer
│    │   │    │    ├─ [C] TilemapCollider2D
│    │   │    │    ├─ [C] CompositeCollider2D
│    │   │    │    └─ [C] Rigidbody2D    Static
│    │   │    │
│    │   │    ├─ Arrivals  (GameObject)              묶음용. 컴포넌트 없음
│    │   │    │    └─ from_entrance  (GameObject)
│    │   │    │         ├─ [C] Transform      위치 = 여기 내릴 자리
│    │   │    │         └─ [C] ArrivalPoint
│    │   │    │                  Arrival Id → from_entrance
│    │   │    │                  Facing   → (0, -1)
│    │   │    │
│    │   │    ├─ Portals  (GameObject)             묶음용
│    │   │    │    └─ Portal_to_2F   Blue_Portal 인스턴스
│    │   │    │         └─ [C] Portal
│    │   │    │              Destination          → ★ Area_Goblin_2F
│    │   │    │              Destination Arrival Id → ★ from_1f   (2F 쪽 지점 이름)
│    │   │    │
│    │   │    └─ Monsters  (GameObject)            묶음용
│    │   │         └─ Monster  ×2~3   Monster.prefab 인스턴스
│    │   │              └─ [C] MonsterController  Definition → ★ Monster_goblin_hob
│    │   │
│    │   ├─ Goblin2F  (GameObject)    ← **꺼 둔다**
│    │   │    ├─ [C] AreaAnchor
│    │   │    │        Definition     → ★ Area_Goblin_2F
│    │   │    │        Floor          → ★ 2Floor
│    │   │    │        Fallback Arrival → ★ from_1f
│    │   │    ├─ [C] WalkableArea   Floor → ★ 2Floor · Guide → ★ 2FGuide
│    │   │    │
│    │   │    ├─ 2Floor  (GameObject)              (기존)
│    │   │    ├─ 2FGuide  (GameObject)             (기존)
│    │   │    │
│    │   │    ├─ Arrivals  (GameObject)              묶음용. 컴포넌트 없음
│    │   │    │    └─ from_1f  (GameObject)        1F 에서 올라온 사람이 서는 자리
│    │   │    │         ├─ [C] Transform
│    │   │    │         └─ [C] ArrivalPoint   Arrival Id → from_1f
│    │   │    │
│    │   │    └─ Portals  (GameObject)             묶음용
│    │   │         └─ Portal_to_3F  (Blue_Portal 인스턴스)
│    │   │              └─ [C] Portal
│    │   │                   Destination          → ★ Area_Goblin_3F
│    │   │                   Destination Arrival Id → ★ from_2f
│    │   │
│    │   └─ Goblin3F  (GameObject)    ← **꺼 둔다**
│    │        ├─ [C] AreaAnchor
│    │        │        Definition     → ★ Area_Goblin_3F
│    │        │        Floor          → ★ 3Floor
│    │        │        Fallback Arrival → ★ from_2f
│    │        ├─ [C] WalkableArea   Floor → ★ 3Floor · Guide → ★ 3FGuide
│    │        │
│    │        ├─ 3Floor  (GameObject)              (기존)
│    │        ├─ 3FGuide  (GameObject)             (기존)
│    │        │
│    │        └─ Arrivals  (GameObject)              묶음용. 컴포넌트 없음
│    │             └─ from_2f  (GameObject)        2F 에서 올라온 사람이 서는 자리
│    │                  ├─ [C] Transform
│    │                  └─ [C] ArrivalPoint   Arrival Id → from_2f
│    │
│    ├─ Orc  (GameObject)             ← 3개 층 전부 **꺼 둔다**
│    └─ Vampire  (GameObject)         ← 3개 층 전부 **꺼 둔다**
│
├─ Player   Player.prefab 인스턴스     Goblin1F 의 바닥 위에 놓는다
│
└─ Main Camera  (GameObject)
     ├─ [C] Camera          Projection Orthographic
     ├─ [C] CameraFollow    Bounds Source → 비움 (구역 전환이 층마다 갈아 끼운다)
     └─ [C] AudioListener
```

### `ArrivalPoint` 는 **플레이어 도착 지점**이다 — 몬스터와 무관하다

이름이 `MonsterSpawner` 와 비슷해 헷갈리지만 하는 일이 다르다.
몬스터는 층 오브젝트의 자식이라 그 층이 꺼지면 **같이 꺼진 채 제자리에 남는다.**

한 줄에 두 가지가 나오는데 서로 다른 것이다.

| | 정체 | 누가 쓰나 |
|---|---|---|
| `from_1f` (오브젝트 이름) | Hierarchy 에서 사람이 찾으려고 붙인 이름 | **사람만** |
| `Arrival Id = "from_1f"` | 컴포넌트의 문자열 필드 | **코드** — 포탈이 이걸로 찾는다 |

둘이 달라도 동작한다. 코드는 `Arrival Id` 만 본다. 같게 두는 것은 찾기 편해서다.

**이름은 "어디서 왔는가" 를 가리킨다.**

```
Goblin1F ──[Portal_to_2F]──▶ Goblin2F 의 "from_1f"    1F 에서 올라온 사람이 서는 자리
Goblin2F ──[Portal_to_3F]──▶ Goblin3F 의 "from_2f"    2F 에서 올라온 사람이 서는 자리
```

한 구역에 들어오는 길이 여럿일 수 있기 때문이다. 나중에 던전 입구에서 2F 로 바로 오는 길이
생기면 `Goblin2F` 에 `from_dungeon_entrance` 를 하나 더 두고 `from_1f` 는 그대로 둔다.
**입구마다 내리는 자리가 다르므로** 지점이 여러 개 필요하다.

**활성 층은 `Goblin1F` 하나뿐이다.** `AreaRegistry` 가 켜져 있는 층을 시작 구역으로 인식한다.
나머지 8개 층은 전부 꺼 둔다.

> `Portal_to_3F` 의 목적지 `Goblin3F` 는 꺼져 있어도 된다.
> `AreaRegistry` 가 **비활성 층까지 훑기 때문**이며, 그것이 그 컴포넌트의 존재 이유다.

---

## 6. 배치 후 점검 — 재생 전에

씬을 저장하고 메뉴에서 실행한다. **둘 다 씬을 바꾸지 않는다.**

```
Pretty Knights > Areas > 0. 포탈 링크 점검 (변경 없음)
Pretty Knights > Data  > 0. 몬스터 정의 점검 (변경 없음)
```

포탈 점검이 잡아주는 것 — `AreaDefinition` 누락 · areaId 중복 · 바닥 타일맵/`WalkableArea` 누락
· `ArrivalPoint` 없음 · arrivalId 중복 · **목적지 arrivalId 오타** · `Is Trigger` 꺼진 포탈.

**전부 통과한 뒤에 재생한다.** 링크 오타는 그 포탈을 실제로 밟기 전까지 드러나지 않는다.

---

## 7. 실행 확인

`Boot.unity` 를 열고 재생한다. **게임플레이 씬을 직접 재생하면 `GameRoot` 가 없어 아무것도 안 된다.**

| 순서 | 확인할 것 | 기대 |
|---|---|---|
| 1 | 시작 | `Goblin1F` 만 켜진 채 시작. 콘솔에 `[GameRoot] 시작 — 신규 플레이` |
| 2 | 이동 | 조이스틱을 살짝 밀면 걷고 끝까지 밀면 달린다. 손을 떼도 방향 유지 |
| 3 | 카메라 | 맵 가장자리에서 빈 공간이 안 보인다 |
| 4 | **공격** | 버튼을 누르면 즉시 판정. 0.45초 흐려지고 Fill 이 채워진다 |
| 5 | **판정 범위** | 씬 뷰에서 Player 선택 → 마지막 휘두름이 빨간 부채꼴로 남는다 |
| 6 | **데미지** | 콘솔에 `[PlayerAttack] Hobgoblin 에 16.3 (ATK 20 vs DEF 8)` |
| 7 | 넉백 | 맞은 몬스터가 뒤로 밀린다 |
| 8 | 처치 | `GameRoot` 우클릭 → 상태 로그로 경험치 증가 확인 |
| 9 | 피격 | 몬스터에게 맞으면 밀려나고 잠깐 조작이 잠긴 뒤 깜빡인다 |
| 10 | **포탈** | 트리거에 들어가면 `고블린 소굴 2층(으)로 이동` 버튼. **밟기만 해선 안 움직인다** |
| 11 | 전환 | 눌러야 이동. 페이드 도는 동안 조이스틱·공격이 전부 잠긴다 |
| 12 | 세이브 | 재생 정지 후 다시 재생 → **2F 에서 시작한다** |

### 되돌아오려면

포탈은 단방향이라 걸어서 못 돌아온다. 재생 중 `GameRoot` 의 **`AreaTransition` 우클릭 → 디버그 이동**
(보낼 곳은 그 컴포넌트의 `Debug Destination`).
세이브를 처음으로 되돌리려면 `GameRoot` 우클릭 → **세이브 삭제**.

---

## 8. 안 되면 여기부터

| 증상 | 원인 |
|---|---|
| 아무것도 안 움직인다 | `Ingame_Horizontal` 을 직접 재생했다. **`Boot.unity` 에서 재생할 것** |
| 공격이 몬스터를 통과한다 | 몬스터 `CapsuleCollider2D` 의 **`Is Trigger` 가 켜져 있다** |
| 공격 버튼은 되는데 키가 안 먹는다 | `PlayerAttack` 의 `Input Actions` 가 비어 있다 |
| 사용 버튼이 한 번 뜨고 다시 안 뜬다 | `InteractButton` 오브젝트에 `CanvasGroup` 이 없다 |
| 포탈에서 버튼이 안 뜬다 | `BoxCollider2D` 의 `Is Trigger` 가 꺼졌거나 `Destination` 이 비었다 |
| 포탈을 눌러도 안 간다 | `GameRoot` 에 `AreaTransition` 이 없거나 `Map` 에 `AreaRegistry` 가 없다 |
| 전환은 되는데 화면이 안 어두워진다 | `FadeOverlay` 가 없다. 없어도 전환 자체는 동작한다 |
| 전환 후 카메라가 엉뚱한 데 갇힌다 | `AreaAnchor` 의 `Floor` 가 비었다 |
| 페이드가 조이스틱에 가려진다 | `FadeOverlay` 가 Canvas 자식 중 맨 아래가 아니다 |
| 데미지가 전부 1 | `Model` 이 `Subtract` 이고 DEF 가 높다. `Minimum Damage` 에 걸린 것 |
| 몬스터가 벽에 붙어 떤다 | 알려진 문제. 경로 탐색 미구현 (`docs/TODO.md` 4번) |

---

## 9. 이번 실행으로 답이 나오는 것

- **데미지 공식** — `CombatSettings.Model` 셋을 바꿔가며 때려보고 고른다.
  고른 것이 미결 #3(스탯 공식)의 답이 된다
- **넉백 4 / 경직 0.12 체감** — `docs/TODO.md` "실기 확인이 필요한 것"
- **조이스틱 `Movement Range`** — 걷기/달리기가 손가락으로 구분되는지
- **공격 쿨타임 0.45** — 연타감이 맞는지
