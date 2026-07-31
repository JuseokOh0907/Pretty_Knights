# Gamble_Yuusha — Claude 작업 가이드

> 이 파일은 Claude Code가 세션마다 자동으로 읽습니다.
> 프로젝트의 "무엇을/어떻게" 를 담고, 상세 기획은 `docs/game_design_plan_EN.md` 를 참조합니다.

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
│   │   ├── Base/                          (비어 있음)
│   │   ├── Goblin/{Objects,Tiles}
│   │   ├── Orc/{Objects,Tiles}
│   │   └── Vampire/{Objects,TIles}        ※ 오타: TIles → Tiles
│   └── Objects/Interactive/
│       ├── Items/
│       └── Portals/{Animation,Images/{Blue,Gold,Red}}
├── Scenes/
└── Settings/
```

**`Assets/Scripts/` 는 아직 존재하지 않습니다.** 코드는 전부 이제부터 작성됩니다.

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

- **`.meta` 파일을 임의로 삭제·생성하지 않는다.** 에셋 이동은 Unity 에디터에서 하거나, 파일과 `.meta` 를 항상 쌍으로 옮긴다.
- `.unity` 씬 파일과 `.prefab` 을 텍스트로 직접 편집하지 않는다. 필요하면 에디터 스크립트를 통한다.
- `Library/`, `Temp/`, `Logs/`, `UserSettings/` 는 커밋 대상이 아니다 (`.gitignore` 참조).
- 레거시 Input 매니저 API를 쓰지 않는다. Input System 액션 애셋(`Assets/InputSystem_Actions.inputactions`)을 사용한다.
- 방향·상태 애니메이션은 방향별 Animator Controller를 늘리는 대신 **파라미터/블렌드 트리 방식**을 우선 검토한다 (현재 Walking은 방향마다 컨트롤러가 따로 있어 확장이 어렵다).
- 결정이 내려질 때마다 `docs/decisions/` 에 기록한다. 다음 세션이 같은 질문을 반복하지 않도록.

---

## 7. 현재 진행 상태

**완료 / 진행 중**

- 스프라이트 셀 PNG 분리 진행 중
- Knights 걷기 8방향 — PNG + 클립 + Animator 완료
- Knights 달리기 8방향 — PNG만 존재 (클립·Animator 미작성)
- 맵 타일셋 3테마 준비 (Goblin / Orc / Vampire), 각 27파일
- 포탈·아이템 오브젝트 아트 존재
- 씬 3개 생성 (내용은 미확인 — 대부분 비어 있을 가능성)

**미착수**

- 스크립트 전무 (`Assets/Scripts/` 없음)
- 몬스터 스프라이트 없음
- 스킬 VFX 없음
- 타일맵 실제 배치 없음
- 세로/가로 씬 스케일링 및 방향 전환 처리

---

## 8. 다음 작업 순서 (기획서 §15)

1. 플레이어블 캐릭터 확정 및 8방향 코어 에셋 세트 완성
2. 캔버스·피벗·프레임·장비 레이어 컨벤션 고정
3. 전방 베기 / 관통 직선 / 광역 폭발 판정 + VFX 구현
4. 추격형·원거리형·정예 몬스터 스프라이트 제작
5. 타일맵 1지역 + 보스 공간 그레이박스 연결
6. 세로 자동 사냥과 가로 직접 플레이가 **동일한 캐릭터 데이터**를 공유하도록 구성
7. 경험치·드랍·보스 보상·세이브 데이터 연결
8. 반응형 UI와 세로/가로 전환

---

## 9. 미결정 사항

`docs/decisions/OPEN_DECISIONS.md` 참조. 총 9건이며, 결정 시 해당 문서를 갱신하고 이 절의 링크를 유지합니다.

---

## 10. 범위 밖 (현재 MVP에서 제외)

멀티플레이, 카드/덱빌딩/포커 핸드, 대규모 오픈월드, 복잡한 제작 경제, 다중 플레이어블 캐릭터, 무거운 실시간 라이팅·대량 파티클.

과거에 탐색했던 홀덤 덱빌딩 RPG, 협동 탈출 플랫포머 등은 `docs/game_design_plan_EN.md` §16에 보관되어 있으며 **현재 MVP에 다시 섞지 않습니다.**
