# 포탈 · 구역 전환 배치 가이드

> 이번에는 **Goblin 테마 3개 층만** 붙인다 (2026-08-05 확정).
> Orc·Vampire 는 이 흐름이 검증된 뒤 같은 절차를 복제한다.
> 아트 교체는 8/8 이후 별도 작업.

---

## 왜 필요한가

층 9개가 **한 씬 안에 좌표계를 공유하며 겹쳐 있다.** 지금은 층 오브젝트를
손으로 켜고 끄는 것 말고는 이동 수단이 없고, 카메라 경계도 하나로 박혀 있다.

포탈은 이 세 가지를 한 번에 푼다.

| 지금 | 포탈이 붙은 뒤 |
|---|---|
| 층 전환 수단이 없다 | 사용 키로 다음 층 |
| 카메라 경계가 한 층에 고정 | 전환할 때마다 그 층의 Floor 로 교체 |
| 재시작하면 옛 구역의 같은 좌표에 선다 | `areaId` 를 세이브에 기록 |

### 발동 방식 — 밟는 것만으로는 아무 일도 없다

```
BoxCollider2D (Is Trigger) 안에 들어감   →   화면에 사용 버튼이 뜬다
사용 키(E) 또는 그 버튼을 누름           →   전환
```

**이게 "도착하자마자 되돌아가는" 문제를 원천적으로 없앤다.**
도착 지점이 다른 포탈 안이어도 버튼을 안 누르면 아무 일도 일어나지 않으므로,
도착 지점을 포탈에서 억지로 떨어뜨려 놓을 필요가 없다.

### 포탈은 단방향이다

되돌아가는 짝 포탈을 두지 않는다. 던전에서 나오는 길은 **탈출 스킬**뿐이며,
그쪽은 `AreaTransition.RequestEscape()` 를 타고 `AreaDefinition.EscapeTo` 가 정한 곳으로 간다.
스킬 시스템이 아직 없으므로 지금은 배선만 해 두고 호출부는 비워 둔다.

---

## 무엇이 무엇을 아는가

**ScriptableObject 는 씬 오브젝트를 참조할 수 없다.** 이 한 줄이 구조 전체를 결정했다.
`AreaDefinition.asset` 에 `Goblin2F` 게임오브젝트나 도착 지점 `Transform` 을 넣는 것은 불가능하다.

```
AreaDefinition.asset   #102 라는 번호만 선언
        ↕  번호로만 이어진다
AreaAnchor (씬)        "나는 #102 다" + 내 Floor · WalkableArea · SpawnPoint 들
```

`AreaRegistry` 가 그 사이를 잇는다. **비활성 층까지 훑는 것이 이 컴포넌트의 존재 이유다** —
포탈이 목적지를 물어볼 때 그 층은 아직 꺼져 있기 때문에,
각 층이 `OnEnable` 에서 스스로 등록하는 방식으로는 절대 찾을 수 없다.

### areaId 번호 규칙

이름이 아니라 번호를 쓴다. 테마 이름은 그 층의 내용이 아니기 때문이다
(Goblin 2F 에만 고블린이 두 종류 배치될 예정이라, `goblin_2f` 같은 ID 는 곧 거짓말이 된다).

```
areaId = 테마번호 × 100 + 층

    0        미지정 (구역 기록이 없는 옛 세이브)
    1 ~  99  거점        1 시작 신전 · 2 숲 · 3 던전 입구
  101 ~ 199  테마 1 Goblin    101 = 1F · 102 = 2F · 103 = 3F
  201 ~ 299  테마 2 Orc
  301 ~ 399  테마 3 Vampire
```

환생마다 층이 늘어도 104, 105 로 이어지고 테마가 늘어도 401 로 이어진다.
**세이브에 그대로 기록되므로 한 번 정한 번호는 바꾸지 않는다.**

---

## 1단계 · AreaDefinition 에셋 3개

`Assets/Data/Areas/` 폴더를 만들고 `Create > Pretty Knights > Area Definition` 로 3개 만든다.

| 파일명 | Area Id | Display Name | Theme | Is Boss Floor | Escape To |
|---|---|---|---|---|---|
| `Area_Goblin_1F.asset` | **101** | `고블린 소굴 1층` | `Goblin` | 끔 | **비움** |
| `Area_Goblin_2F.asset` | **102** | `고블린 소굴 2층` | `Goblin` | 끔 | **비움** |
| `Area_Goblin_3F.asset` | **103** | `고블린 소굴 3층` | `Goblin` | **켬** | **비움** |

> `Escape To` 는 던전 입구 구역(#3)이 만들어진 뒤에 채운다.
> 지금 비워 두면 "탈출할 수 없는 구역" 으로 동작하며, 그게 현재 사실이다.

---

## 2단계 · Boot 씬에 서비스 3개 추가

**루트 오브젝트는 여전히 `GameRoot` 와 `UIRoot` 둘뿐이다.** 새 루트를 만들지 않는다.

```
Boot.unity
│
├─ GameRoot  (GameObject)
│    ├─ [C] Transform
│    ├─ [C] GameRoot                          (기존)
│    │
│    ├─ [C] AreaTransition                    ← 추가
│    │        Landing Search Radius → 3
│    │        Log Transitions       → 켬
│    │        Debug Destination     → 비움 (재생 중 검증할 때만 채운다)
│    │        Debug Spawn Id        → default
│    │
│    └─ [C] InteractionHub                    ← 추가
│             Input Actions  → InputSystem_Actions   ★ 반드시 연결
│             Reuse Delay    → 0.3
│
└─ UIRoot  (GameObject)
     ├─ [C] Transform
     ├─ [C] UIRoot                            (기존)
     │        Landscape Only → 배열에 아래 InteractButton 을 추가한다
     │
     ├─ Canvas  (GameObject)
     │    ├─ [C] RectTransform
     │    ├─ [C] Canvas / CanvasScaler / GraphicRaycaster   (기존)
     │    │
     │    ├─ ModeSwitchButton  (GameObject)   (기존, 그대로 둠)
     │    ├─ Controls          (GameObject)   (기존, 조이스틱)
     │    │
     │    ├─ InteractButton  (GameObject)     ← 추가. Canvas 의 자식
     │    │    ├─ [C] RectTransform
     │    │    │        Anchor  오른쪽 아래 · Pivot (1, 0)
     │    │    │        Pos    (-220, 240)   Size (260, 260)
     │    │    ├─ [C] CanvasGroup             ← 필수. 이걸로 보였다 숨는다
     │    │    ├─ [C] Image                   버튼 배경 · Raycast Target 켬
     │    │    ├─ [C] Button
     │    │    ├─ [C] InteractButton
     │    │    │        Group / Button / Label → 비워도 자동 탐색
     │    │    │        Fallback Label → 사용
     │    │    │
     │    │    └─ Label  (GameObject)
     │    │         ├─ [C] RectTransform      Stretch 전체 · 여백 12
     │    │         └─ [C] TextMeshProUGUI    Alignment 가운데 · Auto Size 켬
     │    │
     │    └─ FadeOverlay  (GameObject)        ← 추가. **Canvas 자식 중 맨 아래**
     │         ├─ [C] RectTransform           Anchor Stretch 전체 · 여백 0
     │         ├─ [C] CanvasGroup             Alpha 0 으로 저장할 것
     │         ├─ [C] Image                   Color 검정 (0,0,0,255)
     │         └─ [C] ScreenFader
     │                  Fade Out Duration → 0.25
     │                  Fade In  Duration → 0.18
     │
     └─ EventSystem  (GameObject)             (기존)
```

### 여기서 틀리기 쉬운 것 넷

1. **`FadeOverlay` 는 형제 중 가장 아래**여야 한다. Overlay 캔버스는 계층 순서가 곧 그리기 순서라,
   위에 두면 조이스틱과 버튼이 페이드 위에 그려진다.
2. **`InteractButton` 오브젝트에 `CanvasGroup` 이 반드시 있어야 한다.**
   이 컴포넌트는 자기 자신을 `SetActive(false)` 로 끄지 않는다 —
   끄면 `Update` 가 멈춰 다시 켤 주체가 사라진다. 알파로만 숨는다.
3. **`InteractionHub` 의 `Input Actions` 를 비우면 키보드 E 가 죽는다.** 화면 버튼은 살아 있어
   증상이 "PC 에서만 안 된다" 로 나타나 원인을 찾기 어렵다.
4. `InteractButton` 은 `UIRoot` 의 **Landscape Only** 배열에 넣는다. 세로는 자동 사냥이라 필요 없다.

---

## 3단계 · Ingame_Horizontal 의 Map 계층

`AreaAnchor` 와 `WalkableArea` 는 **층 루트에 직접 붙인다.** 빈 자식 오브젝트를 따로 만들지 않는다.
이 오브젝트를 켜고 끄는 것이 곧 구역 교체이므로, 한 단계 내리면 활성 타이밍만 어긋난다.

```
Ingame_Horizontal.unity
│
└─ Map  (GameObject)
     ├─ [C] Transform
     ├─ [C] Grid                              (기존)
     ├─ [C] AreaRegistry                      ← 추가
     │        Search Root → 비움 (자기 자식 전체를 훑는다)
     │
     └─ Goblin  (GameObject)                  묶음용. 컴포넌트 없음
          │
          ├─ Goblin1F  (GameObject)           ← 이 단위로 켜고 끈다
          │    ├─ [C] Transform
          │    ├─ [C] AreaAnchor              ← 추가
          │    │        Definition      → Area_Goblin_1F  ★
          │    │        Floor           → 1Floor 의 Tilemap  ★ 직접 연결할 것
          │    │        Walkable        → 비움 (같은 오브젝트에서 자동)
          │    │        Fallback Spawn  → from_entrance
          │    ├─ [C] WalkableArea            ← 추가
          │    │        Floor  → 1Floor 의 Tilemap
          │    │        Guide  → 1FGuide 의 Tilemap
          │    │        Max Attempts → 24
          │    │
          │    ├─ 1Floor  (GameObject)        (기존)
          │    │    ├─ [C] Tilemap
          │    │    └─ [C] TilemapRenderer
          │    │
          │    ├─ 1FGuide  (GameObject)       (기존)
          │    │    ├─ [C] Tilemap
          │    │    ├─ [C] TilemapRenderer
          │    │    ├─ [C] TilemapCollider2D
          │    │    ├─ [C] CompositeCollider2D
          │    │    └─ [C] Rigidbody2D        Body Type: Static
          │    │
          │    ├─ Spawns  (GameObject)        묶음용. 컴포넌트 없음
          │    │    └─ from_entrance  (GameObject)
          │    │         ├─ [C] Transform
          │    │         └─ [C] SpawnPoint    Spawn Id → default
          │    │                              Facing   → (0, -1)
          │    │
          │    └─ Portals  (GameObject)       묶음용. 컴포넌트 없음
          │         └─ Portal_to_2F  (GameObject)
          │              ├─ [C] Transform     Scale 1
          │              ├─ [C] SpriteRenderer   portal_blue_entrance 프레임 1
          │              ├─ [C] Animator         포탈 8프레임 루프
          │              ├─ [C] BoxCollider2D    ★ Is Trigger 켬 · Size (2, 2)
          │              └─ [C] Portal
          │                       Prompt Label   → 비움 (목적지 이름이 자동으로 들어간다)
          │                       Interactable   → 켬
          │                       Destination    → Area_Goblin_2F  ★
          │                       Destination Spawn Id → from_1f   ★
          │
          ├─ Goblin2F  (GameObject)           위와 같은 구성
          │    ├─ [C] AreaAnchor    Definition → Area_Goblin_2F
          │    ├─ [C] WalkableArea  Floor → 2Floor · Guide → 2FGuide
          │    ├─ 2Floor / 2FGuide  (기존)
          │    ├─ Spawns
          │    │    └─ from_1f      [C] SpawnPoint  Spawn Id → from_1f
          │    └─ Portals
          │         └─ Portal_to_3F  [C] Portal  → Area_Goblin_3F / from_2f
          │
          └─ Goblin3F  (GameObject)
               ├─ [C] AreaAnchor    Definition → Area_Goblin_3F
               ├─ [C] WalkableArea  Floor → 3Floor · Guide → 3FGuide
               ├─ 3Floor / 3FGuide  (기존)
               ├─ Spawns
               │    └─ from_2f      [C] SpawnPoint  Spawn Id → from_2f
               └─ Portals
                    └─ Portal_reward  [C] Portal
                             Interactable → **끔**    보스를 잡아야 열린다
                             Destination  → 비움      보상 구역이 아직 없다
```

**시작 시 켜 둘 층은 `Goblin1F` 하나다.** 나머지 8개 층 오브젝트는 전부 꺼 둔다.
`AreaRegistry` 는 켜져 있는 층을 시작 구역으로 인식한다.

### 포탈 오브젝트 규격

| 항목 | 값 | 근거 |
|---|---|---|
| 아트 | `portal_blue_entrance_animation_8x1.png` | 1024 × 128 = 128px 8프레임 |
| PPU | **64** | 타일과 같아야 128px 이 정확히 2 × 2칸 |
| BoxCollider2D Size | **(2, 2)** | 스프라이트와 같은 크기 |
| Is Trigger | **켬** | 끄면 벽이 되어 지나갈 수 없고 버튼도 안 뜬다 |

사용 키 방식이라 트리거를 스프라이트 전체로 잡아도 오발동이 없다.

---

## 4단계 · 점검

씬을 저장한 뒤 메뉴에서 실행한다. **씬을 바꾸지 않는다.**

```
Pretty Knights > Areas > 0. 포탈 링크 점검 (변경 없음)
Pretty Knights > Areas > 1. AreaDefinition 번호 목록
```

잡아주는 것:

- `AreaDefinition` 이 비어 있는 층
- areaId 번호 중복
- 바닥 타일맵 · `WalkableArea` 누락
- `SpawnPoint` 가 하나도 없는 층 · spawnId 중복 · spawnId 공백
- 포탈의 목적지가 비었거나 그 번호의 층이 씬에 없음
- 포탈의 목적지 spawnId 가 그 층에 없음
- `Is Trigger` 가 꺼진 포탈
- 도착 지점이 바닥 밖 (경고만 — 런타임에 주변으로 보정된다)

**전부 통과한 뒤에 재생한다.** 링크 오타는 그 포탈을 실제로 밟기 전까지 드러나지 않아,
층 9개를 전부 걸어서 확인하는 것은 현실적이지 않다.

---

## 5단계 · 확인 절차

1. Boot 씬에서 재생 → `Goblin1F` 만 켜진 상태로 시작
2. `Portal_to_2F` 위로 걸어간다 → **화면 오른쪽 아래에 "고블린 소굴 2층(으)로 이동" 버튼**
3. 포탈에서 벗어난다 → **버튼이 사라진다**
4. 다시 올라가 버튼(또는 키보드 E)을 누른다
   - 화면이 어두워진다 (0.25초)
   - `Goblin1F` 가 꺼지고 `Goblin2F` 가 켜진다
   - 플레이어가 `from_1f` 에 서고 지정한 방향을 본다
   - 카메라가 2F 범위로 즉시 붙는다
   - 화면이 밝아진다 (0.18초)
5. 페이드가 도는 동안 조이스틱을 밀어본다 → **움직이지 않는다**
6. 콘솔에 `[AreaTransition] 이동 완료 — Goblin 고블린 소굴 2층 (#102) / 지점 'from_1f'`
7. **재생을 멈추고 다시 재생** → 2F 에서 시작한다 (세이브에 `#102` 가 들어갔다)

### 되돌아오려면

포탈이 단방향이라 걸어서 돌아올 수 없다. 배치를 고쳐가며 반복 확인할 때는
재생 중 인스펙터에서 `GameRoot` 의 **`AreaTransition` 컴포넌트를 우클릭 → 디버그 이동**.
보낼 구역은 그 컴포넌트의 `Debug Destination` 에 지정한다.

세이브를 처음 상태로 되돌리려면 `GameRoot` 우클릭 → **세이브 삭제**.

---

## 아직 안 한 것

- **몬스터** — `WalkableArea` 는 붙었지만 `MonsterSpawner` / `FloorPopulation` 은 이번 범위 밖.
  `MonsterDefinition` 에셋이 아직 없어 스폰해도 아무것도 안 나온다 (`docs/TODO.md`)
- **탈출 스킬** — `AreaTransition.RequestEscape()` 는 있지만 부르는 쪽이 없다.
  스킬 시스템이 붙을 때 연결한다
- **보상 포탈** — `Portal_reward` 는 자리만 잡고 꺼 둔다.
  보스 처치 판정이 생기면 `SetInteractable(true)` 로 연다
- **Orc · Vampire** — 이 흐름이 검증된 뒤 201~203 / 301~303 으로 같은 절차를 복제
