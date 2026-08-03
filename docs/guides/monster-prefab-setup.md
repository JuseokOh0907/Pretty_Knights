# 몬스터 임시 프리팹 조립 가이드

> 몬스터 아트가 아직 없다. **비주얼만 임시로 대체한** 프리팹으로 행동을 먼저 검증한다.
> 구조는 `Player.prefab` 과 동일하므로 나중에 스프라이트만 갈아 끼우면 된다.
> 근거: `docs/decisions/005-dungeon-and-monster-design.md`

---

## 1. 프리팹 구조

**게임오브젝트는 `Monster` 와 `Visual` 둘뿐이다.** 플레이어와 같다.

```
Monster  (GameObject)                   ← 루트가 발밑
   │
   ├─ [C] Transform            (0, 0, 0)
   ├─ [C] Rigidbody2D          Dynamic / Gravity Scale 0 / Freeze Rotation Z 체크
   ├─ [C] CapsuleCollider2D    Offset (0, 0.12) / Size (0.35, 0.25)
   ├─ [C] CharacterMotor       Acceleration 0.08 (Move Speed 는 정의가 덮어씀)
   ├─ [C] MonsterController    Definition ← 4번에서 만들 SO
   │
   └─ Visual  (GameObject)
         ├─ [C] Transform            (0, 0.375, 0)   ← 플레이어와 동일
         ├─ [C] SpriteRenderer       임시 스프라이트
         ├─ [C] Animator             임시 컨트롤러 (없어도 동작한다)
         └─ [C] DirectionalAnimatorDriver
```

`DirectionalAnimatorDriver` 는 플레이어 전용이 아니다.
속도 벡터를 받아 `MoveX` / `MoveY` / `Speed` 를 갱신할 뿐이라 몬스터도 그대로 쓴다.

## 2. 임시 비주얼

몬스터 스프라이트가 없으므로 **Knights 스프라이트를 색만 바꿔 쓴다.**
행동을 검증하는 것이 목적이므로 이걸로 충분하다.

1. `Visual` 의 `SpriteRenderer` 에 `Animation_Knights-Idle/01_front` 를 넣는다
2. `Color` 를 눈에 띄는 색으로 (예: 붉은 기 도는 `FF8080`)
3. `Sorting Layer` / `Order in Layer` 는 플레이어와 같게

> **Animator 는 비워도 된다.** `DirectionalAnimatorDriver` 는 `Animator` 를 요구하지만
> 컨트롤러가 없으면 파라미터 설정이 무시될 뿐 에러는 나지 않는다.
> 움직임만 먼저 보고, 나중에 몬스터 전용 컨트롤러를 붙이면 된다.
>
> 플레이어의 `Knight.controller` 를 그대로 재사용해도 된다.
> 클립이 Knights 스프라이트라 색만 다른 기사가 걸어다니게 되지만, 방향 전환 검증에는 오히려 낫다.

## 3. 프리팹으로 저장

`Assets/Prefabs/Monsters/Monster_Temp.prefab` 으로 저장한다.

## 4. MonsterDefinition 에셋 만들기

`Assets/Data/Monsters/` 에서 **우클릭 → Create → Pretty Knights → Monster Definition**

이름은 `Goblin_Grunt` 정도로. 기본값으로도 동작한다.

| 항목 | 기본값 | 의미 |
|---|---|---|
| Monster Id | `goblin_grunt` | |
| Tier | Normal | Normal / Elite / Boss |
| Stats | VIT 10 / ATK 4 / DEF 1 / AGI 5 / FOC 0 | `ATK` 가 그대로 플레이어 피해량 |
| Hp Per Vitality | 5 | 최대 HP = 10 × 5 = **50** |
| Move Speed | 1.8 | 플레이어 2.5 보다 느리다 |
| Detect Range | 6 | 이 안에 들어오면 추격 |
| Attack Range | 0.9 | 이 안이면 멈추고 공격 |
| Attack Cooldown | 1.2 | 초 |
| Exp Reward | 12 | 처치 시 지급 |
| Frames | 비움 | 몬스터 아트가 생기면 채운다 |

만든 뒤 `Monster_Temp` 프리팹의 `MonsterController > Definition` 에 연결한다.

## 5. 씬에 놓고 확인

`Ingame_Vertical` 에 프리팹을 몇 개 끌어다 놓고 재생한다.

### 씬 뷰에서 보이는 것

프리팹을 선택하면 기즈모 원 세 개가 그려진다.

| 색 | 의미 |
|---|---|
| 노랑 | 배회 반경 (`Wander Radius`, 기본 3) |
| 주황 | 감지 범위 (`Detect Range`) |
| 빨강 | 공격 범위 (`Attack Range`) |

### 확인할 것

1. **배회** — 플레이어가 멀리 있으면 노란 원 안을 어슬렁거린다. 배회 속도는 정의의 절반이다
2. **추격** — 주황 원 안에 들어가면 곧장 따라온다
3. **놓침** — 주황 원의 **1.5배**를 벗어나야 놓는다. 경계에서 붙잡았다 놓았다 하지 않도록 한 것이다
4. **공격** — 빨간 원 안에서 멈추고 쿨타임마다 플레이어 HP 를 깎는다.
   `GameRoot` 우클릭 → **상태 로그** 로 HP 가 줄었는지 본다
5. **방향** — 추격·공격 중 몬스터가 플레이어 쪽을 바라본다

## 6. 아직 없는 것

| | 상태 |
|---|---|
| **경로 탐색** | 없다. 대상 방향으로 곧장 향하는 단순 조향이라 **오브젝트나 벽 모서리에 걸린다.** 결정 005 대로 방 단위 그리드 A\* 로 교체 예정 |
| 스포너 · 리스폰 | 없다. 지금은 씬에 직접 놓는다. 상한·쿨타임·보주 연동은 다음 작업 |
| 피격 판정 | 플레이어가 몬스터를 때릴 수단이 없다. `TakeDamage(float)` 는 열려 있으니 스킬 판정이 붙으면 연결된다 |
| 몬스터 아트 | 없다. 임시 비주얼 |

## 7. 죽는 것까지 확인하려면

플레이어 공격이 없으므로 인스펙터에서 직접 확인한다.
재생 중 `MonsterController` 의 `Current Hp` 는 읽기 전용으로 표시되지 않으므로,
당장은 **`Exp Reward` 가 지급되는지**를 다른 경로로 보긴 어렵다.

스킬 판정이 붙기 전까지는 **배회·추격·공격 세 가지만** 검증 대상으로 삼는다.
