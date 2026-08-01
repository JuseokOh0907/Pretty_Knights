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

### `Assets/Scripts/` (2026-08-01 착수)

```
Assets/Scripts/
├── PrettyKnights.asmdef        어셈블리 하나로 묶음 (Unity.InputSystem 참조)
├── Core/                       GameRoot, SceneFlow, ServiceRegistry, GameMode
├── Data/                       StatBlock, PlayerStatsDefinition, MonsterDefinition, PlayerRuntimeState
├── Save/                       SaveData, SaveService
├── Characters/                 EightDirection (이동·애니메이션은 후속)
├── World/                      (미작성)
└── UI/                         (미작성)
```

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

### 큰 작업의 진행 리듬 (2026-08-01 확정)

애니메이션 재구성, 타일맵 배치, 스킬 판정 시스템처럼 **한 번에 끝나지 않는 작업**은 다음 리듬으로 진행한다.

1. **중간 체크인** — 작업을 끝까지 밀어붙이지 않고, 되돌리기 비용이 커지기 직전 지점에서 한 번씩 사용자에게 확인받는다.
   체크인 지점 예: 블렌드 트리 파라미터 방식 선택 직후, 타일 팔레트/그리드 설정 확정 직후, 첫 스킬 판정 형태가 화면에 나온 직후.
   보여줄 것은 "무엇을 했고, 지금 무엇을 결정해야 하는가" 한 덩어리.
2. **작업 단위 종료 시 자동 커밋** — 하나의 작업 단위가 끝나면 사용자 요청 없이 바로 커밋한다.
   커밋을 미뤄 여러 작업을 한 덩어리로 만들지 않는다.
3. **push 는 자동이 아니다** — 로컬 커밋은 쌓아 두고, **큰 작업 단위가 끝나 큰 커밋이 생기는 시점에 그동안의 커밋을 묶어서 함께 push** 한다.
   그 시점이 오면 push 여부를 사용자에게 먼저 물어본다. 작은 문서 수정만으로는 push 하지 않는다.
4. Unity 에디터 안의 씬·프리팹 저장은 사용자가 직접 수행한다. 커밋 전에 저장 여부를 알린다.

---

## 7. 현재 진행 상태

**완료 / 진행 중**

- 스프라이트 셀 PNG 분리 진행 중
- Knights 걷기 8방향 — PNG + 클립 + Animator 완료
- Knights 달리기 8방향 — PNG만 존재 (클립·Animator 미작성)
- 맵 타일셋 3테마 준비 (Goblin / Orc / Vampire), 각 27파일
- 포탈·아이템 오브젝트 아트 존재
- 씬 3개 생성 (2026-08-01 실물 확인)
  - `Title_Scene`, `Ingame_Horizontal` — 기본 빈 씬
  - `Ingame_Vertical` — Main Camera + Global Light 2D + `Grid` 아래 타일맵 4레이어(`BasePoints` / `Orc` / `Goblin` / `Vampire`) 골격만 존재. **네 레이어 전부 타일 0개(빈 타일맵).**

- 런타임 기반 계층 작성 (Core / Data / Save)
- **캐릭터 스프라이트 트림 완료** — 셀 256×256 → **184×232** (34.9% 절감).
  세 동작 접지선이 전부 y=211 로 일치. `Visual` 오프셋 `0.375` 는 트림 전후 동일
- **플레이어 프리팹 + 블렌드 트리 컨트롤러 동작 확인** (2026-08-01)
  - 단일 `Knight.controller` 에 Idle / Walk / Run 세 블렌드 트리
  - 8방향 이동·애니메이션 정상, **손을 떼도 바라보던 방향 유지**(latch 검증 완료)

**미착수**

- 몬스터 프리팹·스프라이트, 스폰·풀링
- Boot 씬 + `GameRoot` 배치, `PlayerStatsDefinition` 에셋 생성, 세이브 연결
- UI(HUD), 세로/가로 전환
- 몬스터 스프라이트 없음 (`Maps/` 의 Goblin·Orc·Vampire 는 **맵 테마**이지 몬스터가 아님)
- 스킬 VFX 없음, 판정 시스템 없음
- 타일맵 실제 배치 없음 (레이어 골격만 있음). **타일맵 아트는 추가 수정 중이라 작업 일시 정지 상태** (2026-08-01) — 재개 지시 전까지 타일 배치·팔레트 작업에 착수하지 않는다
- 세로/가로 씬 스케일링 및 방향 전환 처리

### 남은 잡무 / 기술 부채

- `ProjectSettings` 의 `companyName` 이 `DefaultCompany` — 출시 전 변경 필요
- 루트에 옛 이름 잔재 `Gamble_Yuusha.slnx` 가 남아 있음 — 삭제/개명은 사용자 승인 후
- **Goblin 오브젝트 6장만 PPU 128** — Orc·Vampire 12장은 PPU 64(2×2칸)인데
  Goblin 만 절반 크기로 나온다. **오브젝트를 실제로 배치하는 시점에 64로 맞춘다**
  (2026-08-01 유예. 배치가 수작업이라 그때 함께 처리하는 편이 낫다는 판단)
- 타일 일괄 설정은 `Pretty Knights > Tiles` 메뉴 사용.
  대상은 `/Tiles/` 폴더만이며 `/Objects/` 는 PPU 가 달라 제외되어 있다
- ~~Aseprite 원본 파일 보유 여부 확인~~ → **`.aseprite` 원본은 없음** (아트를 Codex에서 생성 중). Aseprite Importer 전환은 Discord 에이전트 오케스트라 설정 이후로 보류. `docs/decisions/002-sprite-pipeline.md`
- Walking Animator Controller 8개 → 블렌드 트리 단일 컨트롤러로 통합 대기 (`.anim` 클립 8개는 그대로 재사용)

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
