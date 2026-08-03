# Boot 씬 · GameRoot 배치 가이드

> 작성해둔 런타임 계층(Core / Data / Save)을 **처음으로 실행시키는** 작업이다.
> 근거: `docs/decisions/003-runtime-architecture.md`

---

## 왜 필요한가

`GameRoot` 는 세이브·플레이어 상태·씬 전환을 들고 `DontDestroyOnLoad` 로 상주한다.
이게 씬에 없으면 `PlayerStatsDefinition` 도 `SaveService` 도 한 줄도 실행되지 않는다.
지금이 정확히 그 상태다 — **코드는 있는데 한 번도 돌아본 적이 없다.**

```
Boot (상주)                     ← 이번에 만든다
 └ GameRoot [DontDestroyOnLoad]
     ├ SaveService
     ├ PlayerRuntimeState        ← 세로/가로가 공유하는 캐릭터 데이터
     └ SceneFlow                 ← 게임플레이 씬을 Additive 로 교체

Ingame_Vertical   ┐ 둘 중 하나만 Additive 로 적재
Ingame_Horizontal ┘  Player.prefab 인스턴스는 여기 산다
```

### 프리팹과 데이터는 다른 것이다

"플레이어"라는 말이 두 가지를 가리키므로 먼저 갈라둔다.

| | 정체 | 사는 곳 | 씬 전환 시 |
|---|---|---|---|
| `Player.prefab` | 화면에 보이는 **몸**. Transform · Rigidbody2D · 스프라이트 | 게임플레이 씬 | **파괴된다** |
| `GameRoot.PlayerState` | 레벨 · 경험치 · HP **데이터**. 순수 C# 객체 | Boot (상주) | **살아남는다** |

기획서 §15-6 의 "세로와 가로가 동일한 캐릭터 데이터를 공유한다"가 이 분리의 실체다.
**몸은 씬마다 새로 만들어지고 데이터는 하나만 유지된다.**

`Player.prefab` 은 이미 `Ingame_Vertical` 에 배치되어 있으므로 이번 작업에서 건드리지 않는다.

---

## 1. PlayerStatsDefinition 에셋 만들기

`Assets/Data/` 폴더를 만들고 그 안에서:

**우클릭 → Create → Pretty Knights → Player Stats Definition**

이름은 `Knight_Stats` 정도로. 기본값이 이미 들어 있으니 그대로 두어도 동작한다.

| 항목 | 기본값 | 비고 |
|---|---|---|
| Display Name | `Knight` | |
| Max Level | 50 | |
| Base Stats | VIT 20 / ATK 10 / DEF 5 / AGI 8 / FOC 5 | **미결정 #3 확정 전 임시안** |
| Growth Per Level | VIT 4 / ATK 2 / DEF 1 / AGI 1 / FOC 1 | |
| Exp Base · Exponent | 25 · 1.6 | `필요경험치 = 25 × 레벨^1.6` |
| Hp Per Vitality | 5 | 레벨1 최대 HP = 20 × 5 = **100** |
| Walk Speed | 2.5 | 월드 유닛/초 |
| Run Speed Multiplier | 1.6 | |

## 2. Boot 씬 만들기

`Assets/Scenes/` 에 새 씬을 만들고 이름을 **`Boot`** 로 한다.

**새 씬의 `Main Camera` 와 `Directional Light` 를 삭제한다.**

> 이유: `Ingame_Vertical` 에 이미 Main Camera 와 AudioListener 가 있다.
> Boot 를 남겨두면 Additive 로 씬이 얹힐 때 **AudioListener 가 두 개**가 되어
> 콘솔에 계속 경고가 뜬다. Directional Light 는 URP 2D 에서 쓰지 않는다.
>
> 카메라 없는 씬이라 잠깐 "No cameras rendering" 이 뜰 수 있는데,
> 곧바로 게임플레이 씬이 올라오므로 문제되지 않는다.

## 3. GameRoot 배치

1. Boot 씬에 빈 GameObject 를 만들고 이름을 **`GameRoot`** 로
2. `GameRoot` 컴포넌트 추가
3. 인스펙터 연결

| 필드 | 값 |
|---|---|
| **Player Stats** | 1번에서 만든 `Knight_Stats` ← **비우면 스탯 계산이 전부 죽는다** |
| Start Mode | `Vertical` |
| Target Frame Rate | 60 (0이면 건드리지 않음) |

## 4. Build Settings 등록

`File > Build Profiles` (또는 Build Settings) → Scenes In Build 에 **순서대로** 추가한다.

```
0  Assets/Scenes/Boot.unity              ← 반드시 0번
1  Assets/Scenes/Ingame_Vertical.unity
2  Assets/Scenes/Ingame_Horizontal.unity
```

`SceneFlow` 가 씬을 **이름으로** 찾는다 (`Ingame_Vertical` / `Ingame_Horizontal`).
등록하지 않으면 `[SceneFlow] '...' 씬을 불러오지 못했습니다` 가 뜬다.

## 5. 실행 확인

**Boot 씬을 열고 재생한다.** 게임플레이 씬을 단독 재생하면 안 된다 (아래 6절 참조).

### 5-1. 씬이 올라오는지

Hierarchy 에 `Boot` 와 `Ingame_Vertical` 이 **동시에** 보이면 성공이다.
`GameRoot` 는 `DontDestroyOnLoad` 항목으로 빠진다.

### 5-2. 시작 로그

재생하자마자 콘솔에 아래가 떠야 한다. **안 뜨면 `GameRoot` 가 씬에 없거나 비활성이다.**

```
[GameRoot] 시작 — 신규 플레이
  Lv 1  EXP 0/25  HP 100/100
  스탯 : VIT 20 / ATK 10 / DEF 5 / AGI 8 / FOC 5
  세이브 : C:\Users\...\Pretty_Knights\save.json  (존재 False)
[GameRoot] 씬 전환 완료 — Vertical
```

두 번째 줄까지 나오면 `SceneFlow` 의 Additive 적재까지 성공한 것이다.
`GameRoot` 인스펙터의 **Log Lifecycle** 을 끄면 이 로그가 사라진다.

### 5-3. 데이터가 도는지

재생 중 `GameRoot` 컴포넌트를 **우클릭**하면 검증 메뉴가 나온다.

| 메뉴 | 확인 내용 |
|---|---|
| **상태 로그** | 레벨·경험치·HP·스탯·세이브 경로 출력 |
| **경험치 +100** | 레벨업이 도는지. 레벨1→2 필요치는 25 이므로 여러 번 오른다 |
| **피해 10** | HP 감소 |
| **지금 저장** | 세이브 파일 생성 |
| **세이브 삭제** | 다음 실행을 신규 플레이로 |

기대 출력:

```
[GameRoot] Lv 1  EXP 0/25  HP 100/100
  스탯 : VIT 20 / ATK 10 / DEF 5 / AGI 8 / FOC 5
  신규 플레이 : True
```

### 5-4. 세이브가 실제로 써지는지

재생을 멈추면 `OnApplicationQuit` 에서 저장된다. 파일 위치:

```
C:\Users\<사용자>\AppData\LocalLow\DefaultCompany\Pretty_Knights\save.json
```

> `DefaultCompany` 는 `ProjectSettings` 의 `companyName` 이 아직 기본값이라 그렇다
> (출시 전 변경 대상으로 `CLAUDE.md` 에 기록되어 있다).

다시 재생하면 `신규 플레이 : False` 가 되고 레벨·경험치가 이어져야 한다.
**이게 확인되면 Data → Save → 복원까지 전 구간이 검증된 것이다.**

---

## 6. 자주 걸리는 것

| 증상 | 원인 |
|---|---|
| `ServiceRegistry ... 등록되어 있지 않습니다` | 게임플레이 씬을 단독 재생했다. **항상 Boot 에서 재생** |
| `PlayerStatsDefinition 이 비어 있습니다` | 3번 인스펙터 연결 누락 |
| `'Ingame_Vertical' 씬을 불러오지 못했습니다` | Build Settings 미등록 |
| AudioListener 중복 경고 | Boot 의 Main Camera 를 안 지웠다 |
| 상태 로그가 재생 중에만 된다는 경고 | 정상. 편집 모드에서는 데이터가 없다 |
| 시작 로그가 아예 안 뜸 | GameRoot 가 씬에 없거나 비활성. Log Lifecycle 체크도 확인 |

## 7. 이 작업 뒤에 남는 것

- **`PlayerStatsDefinition` 의 속도가 아직 몸에 연결되어 있지 않다.**
  현재 `PlayerController` 가 자기 인스펙터 값(`walkSpeed 2.5`)을 쓰고 있어
  정의와 값이 이중으로 존재한다. 정의를 고쳐도 캐릭터가 안 바뀐다.
  다음 단계에서 `Player.prefab` 이 `GameRoot.PlayerState` 를 참조하도록 잇는다.
  **이것이 몸과 데이터를 잇는 첫 실물 연결이다.**
- Boot 를 거치지 않고 게임플레이 씬을 단독 재생할 때 자동으로 Boot 를 끼우는
  에디터 스크립트 (편의 기능, 필수 아님)
- HUD — 지금은 컨텍스트 메뉴로만 상태를 본다
