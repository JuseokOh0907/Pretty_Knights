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
   ├─ Visual  (GameObject)
   │     ├─ [C] Transform            (0, 0.375, 0)   ← 플레이어와 동일
   │     ├─ [C] SpriteRenderer       임시 스프라이트
   │     ├─ [C] Animator             임시 컨트롤러 (없어도 동작한다)
   │     └─ [C] DirectionalAnimatorDriver
   │
   └─ HealthBar  (GameObject)        ← 아래 1-1절. 자식 4개를 손으로 만든다
```

`DirectionalAnimatorDriver` 는 플레이어 전용이 아니다.
속도 벡터를 받아 `MoveX` / `MoveY` / `Speed` 를 갱신할 뿐이라 몬스터도 그대로 쓴다.

---

## 1-1. 체력바 (아트 연결판, 2026-08-09)

**아트가 들어왔으므로 자식 넷을 손으로 만든다.** 비워 두면 코드가 단색 사각형을
대신 만들지만 그건 아트가 없던 때의 대체물이다.

`Monster` 루트 아래에 빈 게임오브젝트 `HealthBar` (`Create Empty`) 를 만들고,
그 아래에 다시 **빈 게임오브젝트 넷**을 만들어 각각 `SpriteRenderer` 를 붙인다.
`HealthBar` 를 포함해 **게임오브젝트는 다섯 개**다.

```
HealthBar  (GameObject)
   ├─ [C] Transform            (0, 0, 0)   ← Offset Y 를 코드가 덮어쓴다
   ├─ [C] MonsterHealthBar
   │
   ├─ Track  (GameObject)      빈 홈. 늘어나지 않는다
   │     ├─ [C] Transform          (0, 0, 0) / Scale (1, 1, 1)
   │     └─ [C] SpriteRenderer     monster_health_track_bg   Order 90
   │
   ├─ Trail  (GameObject)      깎인 잔상. 늦게 따라온다
   │     ├─ [C] Transform          (0, 0, 0) / Scale (1, 1, 1)
   │     └─ [C] SpriteRenderer     monster_health_fill       Order 91
   │
   ├─ Fill   (GameObject)      남은 체력. 즉시 줄어든다
   │     ├─ [C] Transform          (0, 0, 0) / Scale (1, 1, 1)
   │     └─ [C] SpriteRenderer     monster_health_fill       Order 92
   │
   └─ Frame  (GameObject)      테두리. 절대 늘어나지 않는다
         ├─ [C] Transform          (0, 0, 0) / Scale (1, 1, 1)
         └─ [C] SpriteRenderer     monster_health_frame      Order 93
```

**자식의 그리기 순서를 손으로 준다.** 코드는 **자기가 만든** 렌더러에만
`Sorting Order` 를 넣으므로, 손으로 만든 넷은 인스펙터에서 채워야 한다.
잔상이 채움에 가리면 깎인 자리가 안 보이고, 테두리는 맨 위여야 홈을 덮는다.

```
[C] MonsterHealthBar
       Owner   → 비움 (부모에서 MonsterController 를 찾는다)
       Frame   → Frame 의 SpriteRenderer
       Track   → Track 의 SpriteRenderer
       Trail   → Trail 의 SpriteRenderer
       Fill    → Fill 의 SpriteRenderer
       Offset Y            → -0.3   루트가 접지점이므로 발밑
       Width / Height      → 무시된다 (아트가 있으면 스프라이트에서 읽는다)
       Fill Color          → 흰색   ← 아트의 원래 색을 살린다
       Low Ratio / Low Color → 0.3 / 주황
       Track Color         → 무시된다 (코드가 만든 사각형 전용)
       Trail Color         → 밝은 흰기 (알파 0.9)
       Trail Delay / Speed → 0.25 / 1.2
       Hide When Full      → 켬
       Linger After Full   → 1.5
       Sorting Layer / Order → Default / 90
```

> **`Fill Color` 를 흰색으로 두는 것이 중요하다.** 아트를 넣고도 붉은색을 남겨 두면
> 스프라이트에 그 색이 한 번 더 곱해져 탁해진다.

### 크기는 인스펙터가 아니라 아트가 정한다

세 장의 PPU 를 **192** 로 맞춰 두었다 (`monster_health_frame` 이 192px → **정확히 1유닛**,
캐릭터 폭 0.72유닛보다 약간 넓다). 채움은 146px → 0.76유닛.

| 아트 | 픽셀 | PPU 192 기준 월드 크기 |
|---|---|---|
| `monster_health_frame` | 192 × 40 | 1.000 × 0.208 |
| `monster_health_track_bg` | 146 × 13 | 0.760 × 0.068 |
| `monster_health_fill` | 146 × 13 | 0.760 × 0.068 |

`Width` / `Height` 는 **코드가 사각형을 만들 때만** 쓰인다. 아트가 들어오면
`fill.sprite.bounds` 에서 폭을 읽으므로 인스펙터 숫자를 다시 맞출 일이 없다 —
손으로 맞춘 값은 아트를 갈아 끼울 때 반드시 어긋난다.

> 홈이 테두리 한가운데가 아니라면 `Track` · `Trail` · `Fill` 세 자식의
> **로컬 Y 만** 같은 값으로 밀어 맞춘다. X 는 건드리지 않는다 —
> 코드가 왼쪽 끝 고정으로 늘리려고 X 를 계산해 쓴다.

### 왜 Canvas 가 아닌가

한 층에 **16마리까지** 나온다. 마리마다 월드 스페이스 Canvas 를 두면
그만큼 배치가 쪼개져 모바일에서 비용이 커진다. `SpriteRenderer` 몇 장이면
다른 스프라이트와 함께 묶여 그려진다.

### 가득이면 숨는다

꽉 찬 바 16개가 늘 떠 있으면 화면이 시끄럽고, 무엇보다 **맞은 놈이 어느 놈인지**
안 보인다. 회복이 눈에 보이도록 가득 찬 뒤 1.5초는 더 보여준다.

### 채움은 즉시 줄고 잔상이 늦게 쫓아온다

그 사이의 밝은 띠가 곧 **"이번에 얼마나 깎였는가"** 다.
부드럽게 줄이면 정확하지만 얼마나 맞았는지는 못 읽는다.

> `Sorting Order 90` 은 임시값이다. 정렬 일괄 지정 때 확정한다
> ([`../TODO.md`](../TODO.md) "정렬 일괄 지정").
> 몸(0)보다 크고 타격 이펙트(100)보다 작다.

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

## 4. MonsterDefinition 에셋

**10종이 이미 `Assets/Data/Monsters/` 에 있다.**
`Pretty Knights > Data > 1. MonsterDefinition 생성/갱신` 이 만든 것이다.

> ⚠ **그 도구를 다시 돌리면 손으로 맞춘 `Telegraph Duration` 이 등급 기본값으로 덮인다.**
> 밸런싱 값은 인스펙터가 아니라 `MonsterDefinitionBuilder` 의 표에서 고친다
> ([`../pitfalls.md`](../pitfalls.md)).

> **프리팹의 `MonsterController.definition` 은 신경 쓰지 않아도 된다.**
> 스포너가 `Spawn(definition, point)` 로 덮어쓴다. 프리팹은 하나가 맞다.

새로 하나 만들어 볼 때만: `Assets/Data/Monsters/` 에서
**우클릭 → Create → Pretty Knights → Monster Definition**. 기본값으로도 동작한다.

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
| 스포너 · 리스폰 | `FloorPopulation` 이 9개 층에 붙어 있다. **`MonsterSpawner` 는 아직 0개** — 보스 자리처럼 지점을 지정해야 하는 스폰이 남았다 ([`monster-spawn-setup.md`](monster-spawn-setup.md)) |
| 피격 판정 | 된다. `PlayerAttack` 이 반원으로 훑어 `IDamageable` 을 때린다 |
| 몬스터 아트 | 없다. 임시 비주얼 |

## 7. 죽는 것까지 확인하려면

**때려서 확인한다.** `PlayerAttack` 이 붙어 있으므로 공격 버튼으로 직접 깎을 수 있고,
체력바가 줄어드는 것이 그대로 보인다. 죽으면 `RewardGrant` 가 경험치와 드랍을 준다.

절차는 [`verify-spawn-drop.md`](verify-spawn-drop.md) 에 있다.
