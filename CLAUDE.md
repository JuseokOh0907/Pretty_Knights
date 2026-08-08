# Pretty_Knights — Claude 작업 가이드

> 이 파일은 Claude Code가 세션마다 자동으로 읽습니다.
> 프로젝트의 "무엇을/어떻게" 를 담고, 상세 기획은 `docs/game_design_plan_EN.md` 를 참조합니다.

**프로젝트명은 `Pretty_Knights` 로 통일되어 있습니다.**
저장소·로컬 폴더·Unity `productName` 이 모두 같은 이름을 씁니다.
과거 문서나 대화에 등장하는 `Gamble_Yuusha` 는 폐기된 이전 이름입니다.

- 저장소: `https://github.com/JuseokOh0907/Pretty_Knights`
- 로컬 경로: `C:\Git\Pretty_Knights`
- GitHub 계정: `JuseokOh0907` (팀 프로젝트는 `likelion-ugm-07-final` 조직을 쓰지만 이 프로젝트는 개인 저장소)
- `C:\Git` 은 사용자의 프로젝트 루트이며 형제 프로젝트(Diggers, Ghost_GomokuKing, HellEscape, PlanetDigger, ToMeetAlice 등)가 함께 있습니다. **이 저장소 밖의 폴더를 건드리지 않습니다.**

---

## 0. 세션 시작 시 필수 절차

**`HANDOFF.md` 파일이 저장소 루트에 존재하면:**

1. 반드시 먼저 전체를 읽는다.
2. 그 안의 내용 중 지속적으로 유지해야 할 사실·결정·컨벤션을 `CLAUDE.md` 또는 `docs/` 아래 적절한 문서에 반영한다.
3. 반영이 끝나면 **`HANDOFF.md` 를 삭제한다** (`git rm HANDOFF.md` 후 커밋).
4. 삭제했음을 사용자에게 한 줄로 보고한다.

`HANDOFF.md` 는 이전 대화 컨텍스트를 옮기기 위한 일회성 파일이며, 흡수 후 남아 있어서는 안 됩니다.

---

## 1. 프로젝트 개요

모바일 2D 헌팅 RPG. 하나의 캐릭터 성장을 축으로 **두 개의 플레이 리듬**을 연결합니다.

| 모드 | 역할 |
|---|---|
| **세로(Portrait)** | 자동 사냥, 일반 몬스터 파밍, 경험치·아이템 수급, 성장 관리. 게임 기본 진입 화면 |
| **가로(Landscape)** | 직접 조작, 타일맵 탐험, 정예·보스 전투, 희귀 보상 |

루프: 자동 사냥으로 준비 → 보스 원정 → 가로 모드 전투 → 희귀 장비·신규 지역 해금 → 새 자동 사냥터 개방.

상세: [`docs/game_design_plan_EN.md`](docs/game_design_plan_EN.md)

---

## 2. 기술 스택

- **Unity 6000.3.20f1** (Unity 6)
- **URP 17.3.0** / 2D Renderer (`Assets/Settings/Renderer2D.asset`)
- **Input System 1.19.0** — `activeInputHandler: 1` (신규 Input System 전용, 레거시 `Input.GetKey` 사용 금지)
- 2D 패키지: Animation, Aseprite, PSDImporter, SpriteShape, Tilemap, Tilemap Extras
- 타깃: iOS / Android (Android APK 빌드 예정)
- `defaultScreenOrientation: 4` (AutoRotation) — 세로/가로 전환이 전제

### 씬 구성

```
Assets/Scenes/
├── Title_Scene.unity
├── Ingame_Vertical.unity     ← 세로: 자동 사냥 / 베이스
└── Ingame_Horizontal.unity   ← 가로: 직접 조작 / 탐험 / 보스
```

---

## 3. 디렉터리 구조

```
Assets/
├── Art/
│   ├── Characters/
│   │   ├── Animation_Knights-Running/     8방향 PNG 시트
│   │   └── Animation_Knights-Walking/     8방향 PNG + Animation_clips/ + Animator/
│   ├── Maps/
│   │   ├── Base/                          플레이어 시작 지점 주변 맵
│   │   │   ├── Campsite/Tiles             18타일
│   │   │   ├── Dungeon/{Tiles,Dungeon.prefab}
│   │   │   └── Start Points/Tiles
│   │   ├── Goblin/{Objects,Tiles,Goblin.prefab}
│   │   ├── Orc/{Objects,Tiles,Orc.prefab}
│   │   └── Vampire/{Objects,Tiles,Vampire.prefab}
│   └── Objects/Interactive/
│       ├── Items/
│       └── Portals/{Animation,Images/{Blue,Gold,Red}}
├── Scenes/
└── Settings/
```

### `Assets/Scripts/` (2026-08-05 기준)

```
Assets/Scripts/
├── PrettyKnights.asmdef   어셈블리 하나 (Unity.InputSystem 참조)
├── Core/        GameRoot · SceneFlow · ServiceRegistry · GameMode
├── Data/        StatBlock · PlayerStatsDefinition · MonsterDefinition
│                AreaDefinition · PlayerRuntimeState
├── Save/        SaveData · SaveService · WorldLocation
├── Characters/  CharacterMotor · PlayerController · PlayerHitReaction
│                DirectionalAnimatorDriver · MonsterController · EightDirection
├── World/       CameraFollow · WalkableArea · MonsterSpawner · FloorPopulation
│                AreaAnchor · ArrivalPoint · AreaRegistry · AreaTransition · Portal
│                IInteractable · InteractableBehaviour · InteractionHub
└── UI/          UIRoot · ModeSwitchButton · ScreenFader · InteractButton
```

**씬에 있는 것을 찾을 때는 `ServiceRegistry`.** 몸(`PlayerController`)·카메라·구역은
씬마다 새로 생기므로 인스펙터 연결 대신 등록/조회로 잇는다.
데이터(`PlayerRuntimeState`)는 Boot 에 상주하므로 그대로 유지된다.

구조 근거는 [`docs/decisions/003-runtime-architecture.md`](docs/decisions/003-runtime-architecture.md).

**코드는 PPU 에 의존하지 않게 작성합니다.** 거리·범위·속도는 월드 유닛으로만 다루고
픽셀 상수를 코드에 넣지 않습니다 (PPU 미확정).

### `Maps/` 하위 폴더의 의미

`Maps/` 아래 이름은 **맵 테마**이며 몬스터 종족 폴더가 아닙니다.

| 폴더 | 용도 | 구성 |
|---|---|---|
| `Base/` | **플레이어 시작 지점 주변 맵.** 몬스터 지역이 아닌 안전/거점 배경 (2026-08-01 확정) | `Campsite` / `Dungeon` / `Start Points` 3종, 각 18타일 |
| `Goblin/` `Orc/` `Vampire/` | 사냥터 테마 | 각 18타일 + 오브젝트 6종 |

몬스터 스프라이트는 이와 별개이며 아직 하나도 없습니다.

### 실측 픽셀 규격 (2026-08-01, 트림 반영)

| 자산 | 픽셀 | PPU | 월드 크기 |
|---|---|---|---|
| 맵 타일 | 64 × 64 | 64 | **1 × 1 칸** |
| 맵 오브젝트 | 128 × 128 | **64** | **2 × 2 칸** |
| 캐릭터 셀 | **184 × 232** (트림 후) | 256 | 약 0.72 × 0.91 유닛 |
| 캐릭터 시트 (walk/run) | **1472 × 232** = 184 셀 × 8프레임 | | |
| 캐릭터 Idle | 184 × 232 (1프레임) | | |

- 타일 18장 구성 = 9슬라이스 8장 + 중앙 원본 1 + 중앙 변형 5 + 문 4방향
- **오브젝트는 의도적으로 128px 로 제작해 2 × 2 칸을 차지한다** (2026-08-01 확정).
  타일과 같은 PPU 64 를 쓰기 때문에 픽셀이 두 배면 칸도 두 배가 된다.
- 캐릭터 접지선은 셀 바닥에서 **20px** 위 → `Visual` 오프셋 `0.375` 유닛.
  트림 전후 동일하다 (중심 대칭 크롭)

---

## 4. 에셋 컨벤션

### 8방향 명명 규칙 (기존 자산이 따르는 실제 패턴)

```
{순번}_{방향}_{동작}_{프레임레이아웃}.png

01_front                     아래
02_front_faces-screen-right  아래-오른쪽
03_faces-screen-right        오른쪽
04_back_faces-screen-right   위-오른쪽
05_back                      위
06_back_faces-screen-left    위-왼쪽
07_faces-screen-left         왼쪽
08_front_faces-screen-left   아래-왼쪽
```

- 예: `03_faces-screen-right_walk_8x1.png` = 오른쪽 걷기, 가로 8프레임 1행
- 신규 동작(공격·스킬·피격)도 **반드시 이 01~08 순번과 접미사 규칙을 그대로 따를 것.**
- **셀 규격은 동작이 달라도 256 × 256 고정, 캐릭터는 셀 중앙 정렬.**
  어긋나면 상태 전이에서 캐릭터가 튄다.
- 걷기·달리기의 **0번 프레임은 중립 포즈가 아니다** (동작 시작 프레임).
  Idle 로 재사용하지 말 것 — `docs/decisions/004-idle-state.md`

### 텍스처 임포트 설정 (2026-08-01 확정)

| 항목 | 값 |
|---|---|
| PPU | 캐릭터 256 / 타일 64 / 오브젝트 128 |
| Filter Mode | Point |
| Mesh Type | 캐릭터 Tight |
| Android·iOS 오버라이드 | **RGBA ASTC 4×4**, Max Size 2048 |

캐릭터 16장 기준 32 MB → **8 MB** 로 적용 완료.
타일은 인접 경계 이음새 위험이 있어 아직 적용하지 않았다.

> 아트는 현재 **Codex 에서 이미지 생성 방식으로 제작**되고 있으며 `.aseprite` 원본은 없습니다.
> 나중에 Aseprite Importer 파이프라인을 끼워 넣을 때 재작업이 없도록,
> Codex 산출물도 **지금부터 위 명명 규칙과 캔버스·피벗 규약을 지킵니다.**
> 파이프라인 계획: `docs/decisions/002-sprite-pipeline.md`

### 스프라이트 레이어 순서 (장비 교체 대비)

```
Base body → Hair/Head → Top/Armor → Weapon → Secondary → Foreground FX
```

- 모든 레이어는 **동일한 캔버스 크기·피벗·프레임 수**를 공유한다.
- 지면 피벗과 루트 위치는 애니메이션 프레임 전체에서 고정한다.
- 좌우 반전 시 손 소유(오른손/왼손) 및 앞뒤 가림 규칙이 깨지지 않아야 한다. 깨지는 방향은 반전 대신 전용 아트를 쓴다.
- 신규 장비 1세트 비용 = `방향 × 동작 × 프레임`. 항상 이 곱을 먼저 계산할 것.

### 원근·정렬

- 앙각 탑다운 2D 고정. 캐릭터·프롭은 **지면 위치 기준 Y-소팅**.
- 벽·나무·건물 등 높은 오브젝트는 지면 충돌 영역과 상단 가림 아트를 분리한다.
- 캐릭터는 화면 높이의 대략 10~13%를 목표로 한다.

---

## 5. 스킬 판정 원칙

- **판정과 VFX를 분리한다.** 하나의 범위 계산 결과가 프리뷰 인디케이터·실제 데미지·AI 회피·스킬 설명 UI를 전부 구동해야 한다.
- 공용 범위 패턴: `Forward` / `Line` / `Cross` / `Area` / `Dash`
- 최초 구현 스킬 3종: **전방 베기, 관통 직선, 광역 폭발**
- 타일마다 데미지 오브젝트를 만들지 않는다. 한 번의 범위 결과에서 대상을 수집한다.
- 다단히트는 매 프레임이 아니라 고정 간격으로 해결한다.
- VFX 3요소: 예고(지면 인디케이터) → 임팩트(4~8프레임) → 반응(플래시·넉백·데미지 숫자·사운드·햅틱)
- **인디케이터는 메시가 아니라 런타임 래스터화로 그린다** (2026-08-08, `docs/decisions/007-skill-indicator.md`).
  폴리곤의 매끈한 가장자리가 64px 도트 위에서 튄다. 판정과 같은 수학으로 64 px/unit 텍스처에 찍고
  Point 필터로 표시하며, 픽셀 아트는 회전시키면 어긋나므로 **8방향으로 구워 캐시**한다.
- 몬스터 공격은 **예고 → 판정 → 경직** 3단계다. 예고 길이는 `MonsterDefinition.telegraphDuration`.

---

## 6. 작업 규칙

- **파일 삭제가 필요한 작업은 반드시 사용자 승인을 먼저 받는다** (2026-08-01 확정). 유일한 예외는 §0 의 `HANDOFF.md` 제거 절차.
- **`.meta` 파일을 임의로 삭제·생성하지 않는다.** 에셋 이동은 Unity 에디터에서 하거나, 파일과 `.meta` 를 항상 쌍으로 옮긴다.
- `.unity` 씬 파일과 `.prefab` 을 텍스트로 직접 편집하지 않는다. 필요하면 에디터 스크립트를 통한다.
- **커밋 범위는 전체 포함** — `Assets/Art` 전부를 저장소에 넣는다 (2026-08-01 확정). `Library/`, `Temp/`, `Logs/`, `UserSettings/` 만 제외한다.
- `.gitignore` 는 **GitHub 공식 Unity 템플릿**을 채택했고 에디터·OS 규칙만 덧붙였다. 임의로 재작성하지 않는다.
- 레거시 Input 매니저 API를 쓰지 않는다. Input System 액션 애셋(`Assets/InputSystem_Actions.inputactions`)을 사용한다.
- **방향·상태 애니메이션은 블렌드 트리 방식으로 간다** (2026-08-01 확정, `docs/decisions/001-animator-blend-tree.md`).
  현재 Walking은 방향마다 Animator Controller가 따로 있어 `동작 × 8` 로 증가한다. 컨트롤러를 늘리지 말 것.
- 결정이 내려질 때마다 `docs/decisions/` 에 기록한다. 다음 세션이 같은 질문을 반복하지 않도록.
- **증상만 봐서는 원인을 알 수 없던 문제를 겪으면 [`docs/pitfalls.md`](docs/pitfalls.md) 에 한 항목 추가한다.**
  조용히 무시되는 API, Unity 특유의 널 처리처럼 두 번째로 밟으면 또 같은 시간이 드는 것들.

### 가이드 작성 규칙 (2026-08-05 확정)

Unity 에디터 안에서 조립하는 작업은 자동 생성하지 않고 **`docs/guides/` 에 절차서**를 쓴다.
그때 **Hierarchy 의 부모-자식 계층과 각 오브젝트의 컴포넌트를 빠짐없이 명시한다.**

- **`[C]` 로 컴포넌트를 표시하고 들여쓰기로 부모-자식을 나타낸다.**
  컴포넌트와 자식 게임오브젝트가 섞여 보이면 안 된다
- **게임오브젝트가 몇 개인지 셀 수 있어야 한다.** "루트는 A 와 B 둘뿐" 처럼 명시한다
- **`Transform` 인지 `RectTransform` 인지 적는다.** 캔버스 안에서는 이게 갈리고,
  잘못 만들면 자식 UI 의 앵커 계산이 어긋난다
- 인스펙터에서 **채워야 할 필드와 비워도 되는 필드**(자동 탐색)를 구분한다
- 만드는 **메뉴 경로**를 적는다 (`Create Empty` / `UI > Image` / `UI > Button - TextMeshPro`).
  레거시 메뉴를 쓰면 안 되는 경우 그 이유도 함께

예시는 `docs/guides/boot-scene-setup.md` 의 "Boot 씬 완성형 계층" 절.

### 큰 작업의 진행 리듬 (2026-08-01 확정)

애니메이션 재구성, 타일맵 배치, 스킬 판정 시스템처럼 **한 번에 끝나지 않는 작업**은 다음 리듬으로 진행한다.

1. **중간 체크인** — 작업을 끝까지 밀어붙이지 않고, 되돌리기 비용이 커지기 직전 지점에서 한 번씩 사용자에게 확인받는다.
   체크인 지점 예: 블렌드 트리 파라미터 방식 선택 직후, 타일 팔레트/그리드 설정 확정 직후, 첫 스킬 판정 형태가 화면에 나온 직후.
   보여줄 것은 "무엇을 했고, 지금 무엇을 결정해야 하는가" 한 덩어리.
2. **작업 단위 종료 시 자동 커밋** — 하나의 작업 단위가 끝나면 사용자 요청 없이 바로 커밋한다.
   커밋을 미뤄 여러 작업을 한 덩어리로 만들지 않는다.
3. **push 는 자동이 아니다** — 로컬 커밋은 쌓아 두고, **큰 작업 단위가 끝나 큰 커밋이 생기는 시점에 그동안의 커밋을 묶어서 함께 push** 한다.
   그 시점이 오면 push 여부를 사용자에게 먼저 물어본다. 작은 문서 수정만으로는 push 하지 않는다.
   - **씬 파일(`.unity`)은 기본적으로 함께 올린다.**
     미푸시 커밋이 **5개를 넘으면** 커밋 안 된 씬 변경분도 묶어서 push 한다 (2026-08-01 확정).
     사용자가 "이 씬은 비동기로 남겨둬" 라고 지정한 경우에만 제외한다.
4. Unity 에디터 안의 씬·프리팹 저장은 사용자가 직접 수행한다. 커밋 전에 저장 여부를 알린다.

---

## 7. 현재 진행 상태

> 마지막 갱신 2026-08-08. 이 절이 실제와 어긋나면 먼저 고치고 작업할 것.
> 할 일 목록은 [`docs/TODO.md`](docs/TODO.md) 를 본다.

**동작하는 것** — 실행해서 확인된 것만 적는다

- **플레이어** — `Player.prefab` (루트: Rigidbody2D · CapsuleCollider2D · CharacterMotor ·
  PlayerController · PlayerHitReaction · **PlayerAttack** / 자식 `Visual`).
  단일 `Knight.controller` 에 Idle/Walk/Run 블렌드 트리. 8방향 이동, 손을 떼도 방향 유지
- **Boot 씬** — `GameRoot`(+ AreaTransition · InteractionHub · SkillIndicatorPool) 와
  `UIRoot`(Canvas · EventSystem · ModeSwitchButton · Controls · InteractButton ·
  AttackButton · FadeOverlay). 두 루트 모두 DontDestroyOnLoad
- **세이브** — 레벨·경험치·HP · 마지막 위치와 방향 · **areaId** ·
  **부순 오브젝트/벽 · 테마 클리어 횟수**(`WorldProgress`). 원자적 쓰기
- **구역 전환** — 포탈은 **트리거 안 + 사용 키**로 발동하는 **단방향**.
  페이드 · 구역 교체 · 카메라 경계 · 도착 보정 · 저장을 `AreaTransition` 이 순서대로 한다.
  **Goblin 3개 층 + 보상방 왕복 확인 완료**
- **전투** — `SkillShape`(무상태 범위 계산) · `PlayerAttack` · `IDamageable` ·
  `IAreaDamageable`. 몬스터는 **예고 → 판정 → 경직** 3단계
- **인디케이터** — 판정 도형을 픽셀 격자에 찍어 8방향으로 구워 캐시
- **모드 전환 · 조작 · 피격 반응 · 카메라** — 이전과 같음

**만들어졌지만 아직 씬에 다 붙지 않은 것**

씬에는 구역이 **13개**(Goblin 3층+보상방 · Orc 4 · Vampire 4 · 던전 입구) 있고
`AreaAnchor` · `WalkableArea` 는 13개 전부에 붙어 있다. **아래는 Goblin·던전 입구만 된 상태다.**

| | 되어 있는 곳 | 남은 곳 |
|---|---|---|
| `AreaAnchor.definition` | 5 (Goblin 4 · 던전 입구) | **8 (Orc 4 · Vampire 4)** — 비면 `AreaRegistry` 가 등록조차 안 한다 |
| `FloorProps` | 3 (Goblin 1~3F) | 6 |
| `FloorScatterProfile` | 3 (Goblin) | 6 |
| `DestructibleTilemap` | 2 | 나머지 히든 방 |
| 던전 입구 → 각 테마 포탈 | 0 | **3** — 순환이 닫히지 않은 지점 |

- `MonsterSpawner` · `FloorPopulation` — 스폰. 씬에 **하나도 안 붙었다**.
  `MonsterDefinition` 10종은 생성되어 있고 **몬스터 아트가 없다** (임시 프리팹 `Monster_Temp`)
- **오브젝트 자동 배치 일습** — `PropDefinition`(18종 생성 완료) · `DropTable` ·
  `FloorScatterProfile` · `PropScatterer`(계산) · `FloorProps`(런타임 생성) ·
  `Destructible` · `SpawnTotem` · `NoSpawnZone` · `Prop.prefab`.
  절차는 [`docs/guides/prop-scatter-setup.md`](docs/guides/prop-scatter-setup.md)
- **부술 수 있는 벽** — `DestructibleTilemap`. 층마다 붙이고
  `WalkableArea.Breakable` 에 연결해야 한다

**전 구역 배선 절차는 [`docs/guides/all-maps-setup.md`](docs/guides/all-maps-setup.md) 하나로 묶여 있다.**

**에디터 도구** — `Assets/Editor/` 9종. 전부 "점검(변경 없음)" 메뉴가 따로 있다

```
Pretty Knights > Areas > 0. 포탈 링크 점검 · 1. AreaDefinition 번호 목록
                          2. 구역 정의 점검 · 3. AreaDefinition·배치 프로필 생성/갱신
                > Data  > 몬스터 정의 점검 · MonsterDefinition 생성/갱신
                > Props > 개수 계산 · 미리보기 · 미리보기 지우기
                          연결성 검사 · 막는 것 치우기
                          오브젝트 정의 점검 · PropDefinition 생성/갱신
                > Tiles · Characters (기존)
```

`Areas > 3` 은 **씬을 열어 둔 채로** 실행한다. 배치 개수를 표에 박지 않고
바닥 칸 수로 계산하기 때문이다 — 층마다 넓이가 1,950~20,035칸으로 10배 넘게 차이 난다.
밀도는 일반 층 **145칸/개**, 보스 층 **287칸/개** (검증된 Goblin 3층의 개수를 재현하는 값).

**에셋**

- **아트** — Knights 걷기·달리기·Idle 8방향, 클립 24개. ASTC 4×4, 셀 184×232
- **타일** — 6테마 × 18타일 + 연결 타일 24장. `physicsShape` 전량 생성
- **맵** — 3테마 × (3층 + 보상방) + 던전 입구. `Ingame_Horizontal` 한 씬에 전부.
  1F·2F 에 히든 방 벽 타일맵(`HiddenRewards`)이 있다
- **오브젝트** — 18종 전부 PPU 64 · Pivot Center · 2×2칸
- **데이터(SO)** — `Assets/Data/` 아래 `Monsters` 10 · `Props` 18 ·
  `Areas` 5(101·102·103·190·3) · `Scatter` 3(Goblin) · `CombatSettings` · `PlayerStatsDefinition`
- **프리팹** — `Player` · `Prop` · `Monster_Temp` · 포탈 3색(`Blue`/`Red`/`Gold`)

### 남은 잡무 / 기술 부채

- **정렬(Sorting)을 하나도 안 잡았다** — 바닥·벽·인디케이터·캐릭터가 전부 Default/0 이고
  Y-소팅도 꺼져 있다. **마지막에 한 번에 잡기로 했고** 현재 상태와 임시값을
  `docs/TODO.md` "정렬 일괄 지정" 에 전부 모아 두었다
- **오브젝트 접지선이 테마마다 다르다** — Goblin·Vampire 는 캔버스 중앙 정렬,
  Orc 만 바닥 정렬(하단 여백 7px). `PropDefinition.visualOffsetY` 로 흡수하고 있으며
  8/8 아트 교체 때 하단 여백 7px 로 통일하면 사라진다
- `ProjectSettings` 의 `companyName` 이 `DefaultCompany` — 출시 전 변경 필요
- Unity Cloud 의 `projectName` 이 `Gamble_Yuusha` 로 되돌아온다. 대시보드에서 바꿔야 한다
- 루트에 옛 이름 잔재 `Gamble_Yuusha.slnx` — 추적되지 않는 생성 파일. 삭제는 승인 후
- 히든 방 벽 타일맵 이름이 갈려 있다 — `1FHIddenRewards`(Goblin·Orc) vs
  `1FHiddenRewards`(Vampire). "Rewards" 가 보상방과 헷갈리므로 `Breakable` 계열 권장
- 문 타일 4종(`10~13`)은 사용하지 않기로 하여 `physicsShape` 미생성
- 방향별 Animator Controller 24개 — 블렌드 트리로 대체됐으므로 제거 가능. 삭제는 승인 후
- `Ingame_Vertical` 은 옛 테스트 맵이 남아 있다. 세로 모드 설계 시 갈아엎어도 무방
- `Assets/Art/Reference/` 에 참고용 이미지가 들어 있다. 빌드 제외이나 정리 대상

---

## 8. 다음 작업 순서 (기획서 §15)

1. 플레이어블 캐릭터 확정 및 8방향 코어 에셋 세트 완성
2. 캔버스·피벗·프레임·장비 레이어 컨벤션 고정
3. 전방 베기 / 관통 직선 / 광역 폭발 판정 + VFX 구현
4. 추격형·원거리형·정예 몬스터 스프라이트 제작
5. 타일맵 1지역 + 보스 공간 그레이박스 연결
6. 세로 자동 사냥과 가로 직접 플레이가 **동일한 캐릭터 데이터**를 공유하도록 구성
   → 아키텍처 제약: 캐릭터 스탯·스킬·장비·인벤토리는 **씬에 종속되지 않는 계층**에 두어야 한다.
7. 경험치·드랍·보스 보상·세이브 데이터 연결
8. 반응형 UI와 세로/가로 전환

---

## 9. 미결정 사항

`docs/decisions/OPEN_DECISIONS.md` 참조. 총 9건이며, 결정 시 해당 문서를 갱신하고 이 절의 링크를 유지합니다.

그중 **코드 작성을 실제로 막고 있는 3건**은 다음과 같습니다. 첫 구현 착수 전에 사용자와 먼저 정리하십시오.

| # | 항목 | 무엇이 막히는가 |
|---|---|---|
| 2 | 카메라·타일 투영·충돌·가림 규칙 | 타일맵 그리드 설정과 Y-소팅 구현 |
| 8 | 세로/가로 기준 해상도 및 전환 동작 | 씬·캔버스 구조 |
| 3 | 스탯 목록과 공식 | 캐릭터 데이터 모델 |

---

## 10. 범위 밖 (현재 MVP에서 제외)

멀티플레이, 카드/덱빌딩/포커 핸드, 대규모 오픈월드, 복잡한 제작 경제, 다중 플레이어블 캐릭터, 무거운 실시간 라이팅·대량 파티클.

과거에 탐색했던 홀덤 덱빌딩 RPG, 협동 탈출 플랫포머 등은 `docs/game_design_plan_EN.md` §16에 보관되어 있으며 **현재 MVP에 다시 섞지 않습니다.**
