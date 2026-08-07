# 실행까지 가는 Hierarchy 세팅 가이드

> **목표** — 재생을 눌러서 *걷고 · 때리고 · 죽이고 · 포탈로 층을 넘는 것*까지 한 번에 본다.
> 기능별 근거는 [`player-attack-setup.md`](player-attack-setup.md) 와
> [`portal-area-setup.md`](portal-area-setup.md) 에 있고, 이 문서는 **배치 순서만** 다룬다.
>
> 표기 — `[C]` 는 컴포넌트, 들여쓰기는 부모-자식. `★` 는 **반드시 손으로 연결**해야 하는 칸.
> 표시 없는 칸은 비워도 자동으로 찾는다.

---

## 0. 현재 상태 — 2026-08-08 실측

씬·프리팹을 직접 훑어 확인한 것이다. **남은 것은 5절(맵) 하나뿐이다.**

### ✅ 끝난 것 — 다시 안 해도 된다

| 대상 | 확인된 내용 |
|---|---|
| `Assets/Data/CombatSettings.asset` | 생성됨 · `GameRoot` 에 연결됨 |
| `Assets/Data/Areas/` | `Area_Goblin_1F` · `_2F` · `_3F` 3종 |
| `Assets/Data/Monsters/` | `MonsterDefinition` 10종 |
| `Assets/Prefabs/Monsters/Monster_Temp.prefab` | `MonsterController` 있음 · 콜라이더 `Is Trigger` **꺼짐**(정상) |
| `Assets/Prefabs/Player/Player.prefab` | `PlayerAttack` 붙음 |
| `Assets/Prefabs/Portals/Blue_Portal.prefab` | `Portal` + `BoxCollider2D`(**Is Trigger 켜짐**) |
| `Boot.unity` | `AreaTransition` · `InteractionHub` · `ScreenFader` · `InteractButton` · `AttackButton` 전부 배치. `Landscape Only` 3개 등록 |
| `Ingame_Horizontal` | 9개 층 전부에 `WalkableArea` · `Main Camera` 에 `CameraFollow` |

### ⚠️ 지금 바로 고쳐야 하는 것

**① 층이 10개나 켜져 있다 — 이것부터.**

```
Goblin1F ON   Goblin2F ON   Goblin3F ON
Orc1F    ON   Orc2F    ON   Orc3F    ON
Vampire1F ON  Vampire2F ON  Vampire3F ON
Dungeon  ON   ← Base 맵 프리팹. 여기에도 WalkableArea 가 붙어 있다
```

`WalkableArea` 는 `OnEnable` 에서 `ServiceRegistry` 에 자기를 등록한다.
10개가 동시에 켜지면 **마지막 하나가 이기고 나머지는 덮어쓰기 경고만 남긴다.**
어느 것이 이길지는 순서에 달려 있어, 플레이어가 선 층과 다른 층의 판정이 쓰일 수 있다.
층들이 좌표계를 공유하므로 타일맵도 전부 겹쳐 그려진다.

> **`Goblin1F` 하나만 남기고 나머지 9개를 끈다.** `Dungeon` 도 끈다.

**② `Gold_Portal` · `Red_Portal` 의 `Is Trigger` 가 꺼져 있다.**
`Blue_Portal` 만 켜져 있다. 지금 안 쓰더라도 나중에 원인을 찾기 어려운 종류라 함께 켜 둔다.

### ❌ 남은 것 — 전부 `Ingame_Horizontal` 안

| 위치 | 할 일 | 절 |
|---|---|---|
| `Map` | `AreaRegistry` 추가 | 5-1 |
| `Goblin1F` `Goblin2F` `Goblin3F` | `AreaAnchor` 추가 + 3칸 연결 | 5-2 |
| 각 층 아래 | `Arrivals` 묶음 + `ArrivalPoint` | 5-3 |
| `Goblin1F` `Goblin2F` 아래 | `Portals` 묶음 + `Blue_Portal` 인스턴스 | 5-4 |
| `Goblin1F` 아래 | `Monster_Temp` 몇 개 | 5-5 |

**1~4절은 읽지 않아도 된다.** 이미 되어 있으므로 계층 참고용으로만 남겨 둔다.

---

## 1. 에셋 먼저  ✅ 완료 — 참고용

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

## 2. `Boot.unity` 완성형 계층  ✅ 완료 — 참고용

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

## 3. `Player.prefab`  ✅ 완료 — 참고용

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

## 4. 포탈 프리팹 마무리  ⚠️ Gold·Red 만 남음

`Blue_Portal` 은 **끝났다** — `Portal` + `BoxCollider2D`(Is Trigger 켜짐)가 붙어 있다.

**`Gold_Portal` 과 `Red_Portal` 은 `Is Trigger` 가 꺼져 있다.**
지금 쓰지 않더라도 켜 두는 편이 낫다 — 나중에 "포탈 앞인데 버튼이 안 뜬다" 로
나타나면 원인을 찾기 어려운 종류다.

아래는 세 프리팹의 완성형이다.

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

## 5. `Ingame_Horizontal` — 여기가 남은 작업 전부

이번에는 **Goblin 3개 층만** 붙인다. 검증 후 Orc·Vampire 로 복제한다.

### 5-0. 층 끄기 ← 이것부터

Hierarchy 에서 아래 9개를 **비활성**으로 바꾼다. `Goblin1F` 만 켜 둔다.

```
Map / Goblin  / Goblin2F      ← 끔
              / Goblin3F      ← 끔
Map / Orc     / Orc1F         ← 끔
              / Orc2F         ← 끔
              / Orc3F         ← 끔
Map / Vampire / Vampire1F     ← 끔
              / Vampire2F     ← 끔
              / Vampire3F     ← 끔
Map / ... / Dungeon           ← 끔 (Base 맵 프리팹, WalkableArea 가 붙어 있다)
```

**왜 먼저인가** — `WalkableArea` 는 `OnEnable` 에서 `ServiceRegistry` 에 자기를 등록한다.
10개가 동시에 켜지면 마지막 하나가 이기고 나머지는 덮어쓰기 경고만 남긴다.
어느 것이 이길지는 순서에 달려 있어 **플레이어가 선 층과 다른 층의 판정이 쓰일 수 있다.**
층들이 좌표계를 공유하므로 타일맵도 전부 겹쳐 그려진다.

`AreaRegistry` 는 **켜져 있는 층을 시작 구역으로 인식한다.**

> `Portal_to_3F` 의 목적지인 `Goblin3F` 가 꺼져 있어도 된다.
> `AreaRegistry` 가 **비활성 층까지 훑기 때문**이며, 그것이 그 컴포넌트의 존재 이유다.

### 5-1. `Map` 에 `AreaRegistry`

| 위치 | `Ingame_Horizontal` / `Map` |
|---|---|
| 메뉴 | Inspector → `Add Component` → `Area Registry` |
| 채울 칸 | `Search Root` → **비움** (자기 자식 전체를 훑는다) |

### 5-2. 3개 층에 `AreaAnchor`

`Goblin1F` · `Goblin2F` · `Goblin3F` **각각의 루트**에 붙인다.
`WalkableArea` 가 이미 같은 오브젝트에 있으므로 나란히 놓이면 된다.

| 붙이는 곳 | Definition ★ | Floor ★ | Fallback Arrival ★ |
|---|---|---|---|
| `Goblin1F` | `Area_Goblin_1F` | `1Floor` 의 Tilemap | `from_entrance` |
| `Goblin2F` | `Area_Goblin_2F` | `2Floor` 의 Tilemap | `from_1f` |
| `Goblin3F` | `Area_Goblin_3F` | `3Floor` 의 Tilemap | `from_2f` |

`Walkable` 칸은 비운다 — 같은 오브젝트에서 자동으로 찾는다.
`Fallback Arrival` 은 5-3 을 먼저 하고 돌아와 채운다.

> `Floor` 를 비우면 자동 탐색이 `1FGuide` 를 집을 수 있다. **직접 연결할 것.**
> 비면 카메라 경계가 안 잡힌다.

### 5-3. 도착 지점

각 층 아래에 빈 오브젝트 `Arrivals` 를 만들고(`Create Empty`, 컴포넌트 없음)
그 자식으로 도착 지점을 하나씩 둔다. **그 오브젝트의 위치가 곧 내려서는 좌표다.**

| 부모 | 오브젝트 이름 | `[C] ArrivalPoint` 의 Arrival Id | Facing |
|---|---|---|---|
| `Goblin1F/Arrivals` | `from_entrance` | `from_entrance` | (0, -1) |
| `Goblin2F/Arrivals` | `from_1f` | `from_1f` | (0, -1) |
| `Goblin3F/Arrivals` | `from_2f` | `from_2f` | (0, -1) |

**오브젝트 이름과 `Arrival Id` 는 다른 것이다.** 코드는 `Arrival Id` 만 보고,
오브젝트 이름은 Hierarchy 에서 사람이 찾으라고 있는 것이다. 같게 두면 찾기 편할 뿐이다.

이름의 `from_` 은 **어디서 왔는가**를 가리킨다. `Goblin2F` 의 `from_1f` 는
*1층에서 올라온 사람이 서는 자리*이며, 좌표는 2F 맵 안에서 직접 잡는다.

### 5-4. 포탈 두 개

각 층 아래에 빈 오브젝트 `Portals` 를 만들고 `Blue_Portal.prefab` 을 끌어다 놓는다.

| 부모 | 이름 | Destination ★ | Destination Arrival Id ★ |
|---|---|---|---|
| `Goblin1F/Portals` | `Portal_to_2F` | `Area_Goblin_2F` | `from_1f` |
| `Goblin2F/Portals` | `Portal_to_3F` | `Area_Goblin_3F` | `from_2f` |

`Prompt Label` 은 비운다 — 목적지 이름이 자동으로 들어간다.
`Goblin3F` 에는 포탈을 두지 않는다. 보스 처치 시 보상 포탈이 열릴 자리다.

> **포탈을 도착 지점 위에 놓아도 된다.** 사용 키를 눌러야 발동하므로 무한 왕복이 없다.
> 다만 눈으로 구분되게 조금 떨어뜨리는 편이 낫다.

### 5-5. 몬스터 몇 마리

`Goblin1F` 아래에 빈 오브젝트 `Monsters` 를 만들고
`Monster_Temp.prefab` 을 2~3개 끌어다 **바닥 위에** 놓는다.

| 칸 | 값 |
|---|---|
| `MonsterController` → `Definition` ★ | `Monster_goblin_hob` |
| `Knockback On Hit` | 3 |

벽 안에 놓으면 밀려나며 떤다. `1Floor` 타일이 깔린 자리인지 확인할 것.

---

### 완성형 계층 (참고)

```
Ingame_Horizontal.unity
│
├─ Map  (GameObject)
│    ├─ [C] Transform
│    ├─ [C] Grid                                   (기존)
│    ├─ [C] AreaRegistry                           ← 5-1
│    │        Search Root → 비움
│    │
│    ├─ Goblin  (GameObject)          묶음용. 컴포넌트 없음
│    │   │
│    │   ├─ Goblin1F  (GameObject)    ← 켜 두는 유일한 층
│    │   │    ├─ [C] Transform
│    │   │    ├─ [C] AreaAnchor                    ← 5-2
│    │   │    │        Definition       → ★ Area_Goblin_1F
│    │   │    │        Floor            → ★ 1Floor 의 Tilemap
│    │   │    │        Walkable         → 비움
│    │   │    │        Fallback Arrival → ★ from_entrance
│    │   │    ├─ [C] WalkableArea                  (기존)
│    │   │    │        Floor → 1Floor  ·  Guide → 1FGuide
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
│    │   │    ├─ Arrivals  (GameObject)            ← 5-3. 묶음용
│    │   │    │    └─ from_entrance  (GameObject)
│    │   │    │         ├─ [C] Transform    위치 = 내려서는 자리
│    │   │    │         └─ [C] ArrivalPoint
│    │   │    │                  Arrival Id → from_entrance
│    │   │    │                  Facing     → (0, -1)
│    │   │    │
│    │   │    ├─ Portals  (GameObject)             ← 5-4. 묶음용
│    │   │    │    └─ Portal_to_2F  (Blue_Portal 인스턴스)
│    │   │    │         ├─ [C] BoxCollider2D   Size (2,2) · Is Trigger 켬
│    │   │    │         ├─ [C] Portal
│    │   │    │         │        Destination            → ★ Area_Goblin_2F
│    │   │    │         │        Destination Arrival Id → ★ from_1f
│    │   │    │         └─ portal_blue_..._0  (자식)
│    │   │    │              ├─ [C] SpriteRenderer
│    │   │    │              └─ [C] Animator
│    │   │    │
│    │   │    └─ Monsters  (GameObject)            ← 5-5. 묶음용
│    │   │         └─ Monster_Temp  ×2~3
│    │   │              └─ [C] MonsterController
│    │   │                       Definition → ★ Monster_goblin_hob
│    │   │
│    │   ├─ Goblin2F  (GameObject)    ← 끔
│    │   │    ├─ [C] AreaAnchor
│    │   │    │        Definition       → ★ Area_Goblin_2F
│    │   │    │        Floor            → ★ 2Floor
│    │   │    │        Fallback Arrival → ★ from_1f
│    │   │    ├─ [C] WalkableArea                  (기존)
│    │   │    ├─ 2Floor  (GameObject)              (기존)
│    │   │    ├─ 2FGuide  (GameObject)             (기존)
│    │   │    ├─ Arrivals  (GameObject)
│    │   │    │    └─ from_1f  (GameObject)
│    │   │    │         ├─ [C] Transform
│    │   │    │         └─ [C] ArrivalPoint   Arrival Id → from_1f
│    │   │    └─ Portals  (GameObject)
│    │   │         └─ Portal_to_3F  (Blue_Portal 인스턴스)
│    │   │              └─ [C] Portal
│    │   │                   Destination            → ★ Area_Goblin_3F
│    │   │                   Destination Arrival Id → ★ from_2f
│    │   │
│    │   └─ Goblin3F  (GameObject)    ← 끔
│    │        ├─ [C] AreaAnchor
│    │        │        Definition       → ★ Area_Goblin_3F
│    │        │        Floor            → ★ 3Floor
│    │        │        Fallback Arrival → ★ from_2f
│    │        ├─ [C] WalkableArea                  (기존)
│    │        ├─ 3Floor  (GameObject)              (기존)
│    │        ├─ 3FGuide  (GameObject)             (기존)
│    │        └─ Arrivals  (GameObject)
│    │             └─ from_2f  (GameObject)
│    │                  ├─ [C] Transform
│    │                  └─ [C] ArrivalPoint   Arrival Id → from_2f
│    │
│    ├─ Orc  (GameObject)             ← 3개 층 전부 끔
│    ├─ Vampire  (GameObject)         ← 3개 층 전부 끔
│    └─ Dungeon  등 Base 맵           ← 끔 (WalkableArea 가 붙어 있다)
│
├─ Player   Player.prefab 인스턴스     Goblin1F 의 바닥 위
│
└─ Main Camera  (GameObject)
     ├─ [C] Camera          Orthographic
     ├─ [C] CameraFollow    Bounds Source → 비움 (구역 전환이 층마다 갈아 끼운다)
     └─ [C] AudioListener
```

### `ArrivalPoint` 는 **플레이어 도착 지점**이다 — 몬스터와 무관하다

예전 이름이 `SpawnPoint` 라 `MonsterSpawner` 와 헷갈렸다.
몬스터는 층 오브젝트의 자식이라 **그 층이 꺼지면 같이 꺼진 채 제자리에 남는다.**
1층 몬스터가 2층으로 옮겨지는 일은 없다.

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
| 콘솔에 `이미 등록되어 있어 덮어씁니다` 가 쌓인다 | 층이 둘 이상 켜져 있다. **5-0** 을 안 했다 |
| 타일맵이 서로 겹쳐 보인다 | 같은 원인. 9개 층이 좌표계를 공유한다 |
| 몬스터가 벽에 붙어 떤다 | 알려진 문제. 경로 탐색 미구현 (`docs/TODO.md` 4번) |

---

## 9. 이번 실행으로 답이 나오는 것

- **데미지 공식** — `CombatSettings.Model` 셋을 바꿔가며 때려보고 고른다.
  고른 것이 미결 #3(스탯 공식)의 답이 된다
- **넉백 4 / 경직 0.12 체감** — `docs/TODO.md` "실기 확인이 필요한 것"
- **조이스틱 `Movement Range`** — 걷기/달리기가 손가락으로 구분되는지
- **공격 쿨타임 0.45** — 연타감이 맞는지
