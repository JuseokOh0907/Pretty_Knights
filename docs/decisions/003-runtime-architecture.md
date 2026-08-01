# 003. 런타임 아키텍처 — Boot 씬 + Additive 적층, 하이브리드 애니메이션

- **상태:** 확정
- **결정일:** 2026-08-01

## 배경

`Assets/Scripts/` 가 비어 있는 상태에서 프로토타입을 시작한다.
기획서 §15-6 의 제약이 구조를 규정한다.

> 세로 자동 사냥과 가로 직접 플레이가 **동일한 캐릭터 데이터**를 공유해야 한다.

타깃이 모바일(iOS/Android)이므로 메모리·GC·드로우콜이 설계 단계에서부터 제약이다.

## 결정

### 1) 씬 구조 — Boot 씬 + Additive 적층

```
Boot (상주)
 └ GameRoot [DontDestroyOnLoad]
     ├ SaveService
     ├ PlayerRuntimeState      ← 씬이 바뀌어도 살아 있는 캐릭터 데이터
     └ SceneFlow               ← 게임플레이 씬을 Additive 로 교체

Ingame_Vertical   ┐ 둘 중 하나만 Additive 로 적재
Ingame_Horizontal ┘
```

- 캐릭터 데이터가 **구조적으로** 씬 비종속이 된다. 규율이 아니라 구조가 제약을 강제한다.
- 씬 전환마다 매니저를 재초기화하지 않으므로 모바일 전환 스파이크가 작다.
- 비용: 에디터에서 게임플레이 씬을 단독 실행하면 `GameRoot` 가 없다.
  → Boot 자동 삽입 에디터 스크립트가 필요하다 (후속 작업).

### 2) 데이터 — ScriptableObject 는 정의 전용, 런타임 상태는 분리

| 계층 | 형태 | 역할 |
|---|---|---|
| `PlayerStatsDefinition` | ScriptableObject | 기초 스탯·성장 곡선·경험치 커브. **읽기 전용** |
| `MonsterDefinition` | ScriptableObject | 몬스터 종별 정의. **읽기 전용** |
| `PlayerRuntimeState` | 순수 C# `[Serializable]` | 레벨·경험치·현재 HP. 저장 대상 |
| `SaveData` | 순수 C# `[Serializable]` | JSON 직렬화 단위 |

**런타임 값을 ScriptableObject 에 쓰지 않는다.** 에디터 플레이 중 SO 필드를 바꾸면
플레이 종료 후에도 값이 남아 원본이 오염된다. 이 함정은 재현이 어렵고 디버깅 비용이 크다.
SO 는 항상 입력으로만 쓰고, 계산 결과는 런타임 객체에 담는다.

### 3) 애니메이션 — 하이브리드

| 대상 | 방식 | 이유 |
|---|---|---|
| 플레이어 | Animator + 2D 블렌드 트리 (결정 001) | 상태 전이가 복잡하고 인스턴스가 1개뿐 |
| 몬스터 | 경량 스프라이트 프레임 교체 | 자동 사냥에서 동시 수십 마리. `Animator` 인스턴스 비용을 감당하지 않는다 |

기존 `.anim` 클립 8개는 플레이어 블렌드 트리에서 그대로 재사용한다.

### 4) 모바일 기본값

- 몬스터·데미지 숫자·이펙트는 **오브젝트 풀링**. 전투 중 `Instantiate`/`Destroy` 금지
- Y-소팅은 카메라의 `TransparencySortMode.CustomAxis (0,1,0)`.
  매 프레임 `sortingOrder` 를 쓰는 방식을 쓰지 않는다
- 세이브는 `persistentDataPath` JSON + **임시 파일 교체 방식**.
  모바일은 앱이 예고 없이 종료되므로 `OnApplicationPause(true)` 에서도 저장한다
- UI 는 uGUI. 정적 캔버스와 동적 캔버스를 분리해 리빌드 비용을 끊는다

## 폴더 구조

```
Assets/Scripts/
├── PrettyKnights.asmdef
├── Core/       GameRoot, SceneFlow, ServiceRegistry, GameMode
├── Data/       StatBlock, PlayerStatsDefinition, MonsterDefinition, PlayerRuntimeState
├── Save/       SaveData, SaveService
├── Characters/ EightDirection, 이동·애니메이션·몬스터 (후속)
├── World/      스폰·풀링·오브젝트 관리 (후속)
└── UI/         HUD (후속)
```

## 보류된 것

- **PPU / 스케일 기준 미확정.** 사용자 검토 중 (타일 64 기준, 캐릭터는 검토 중).
  선행 작업으로 **투명 픽셀 트림과 텍스처 압축 정리**가 필요하다.
  → 그때까지 **모든 코드는 PPU 에 의존하지 않게 작성한다.**
    거리·범위·속도는 월드 유닛으로만 다루고, 픽셀 상수를 코드에 넣지 않는다.
- **타일맵 작업 정지 중** (아트 수정 중). `World/` 의 타일 배치·팔레트 작업은 재개 지시 후.

## 텍스처 메모리 경고 (근거 수치)

캐릭터 시트는 2048×256 이고 셀 하나가 256×256, 실제 그림은 약 140×192 다.

| 항목 | RGBA32 기준 |
|---|---|
| 걷기 8방향 | 약 16.8 MB |
| 걷기 + 달리기 | 약 33.6 MB |
| 계획된 8개 동작 전부 | **130 MB 이상** |

중저가 안드로이드에서 감당할 수 없다.
**투명 여백 트림(약 41% 회수) + SpriteAtlas + ASTC 6×6** 적용 시 10 MB 안쪽으로 내려간다.
이는 선택 사항이 아니라 출시 전 필수 항목이다.

## 미해결

- 블렌드 트리 파라미터를 `MoveX/MoveY` 로 할지 `Direction` int 로 할지 (결정 001 미해결분)
- 게임플레이 씬 단독 실행 시 Boot 자동 삽입 에디터 스크립트 형태
- 스탯 5종(Vitality/Attack/Defense/Agility/Focus)은 **임시값**이며 미결정 #3 확정 시 교체
