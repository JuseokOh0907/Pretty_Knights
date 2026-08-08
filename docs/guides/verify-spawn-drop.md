# 지금 상태에서 스폰·토템·드랍 검증하기

> 2026-08-09 기준 **실제 파일을 훑어 확인한 상태**다.
> 몬스터 아트·애니메이션은 나중이므로 **임시 프리팹으로 연결성만** 본다.
> 배치 자체의 설명은 [`monster-spawn-setup.md`](monster-spawn-setup.md) 에 있다.

---

## 0. 지금 어디까지 되어 있나

### ✅ 되어 있는 것 — 손댈 필요 없다

| | |
|---|---|
| 씬 구역 | `AreaAnchor` **13/13** · `FloorProps` **9** · `DestructibleTilemap` **6** |
| Boot | `GameRoot` 에 `AreaTransition` · `InteractionHub` · `SkillIndicatorPool` · **`SkillImpactPool`** |
| HUD | `TopBar` · `SkillBar`(4) · `AttackButton` · `InteractButton` · `EscapeButton` · `FadeOverlay` |
| 데이터 | `Areas` 13 · `Monsters` 10 · `Props` 18 · `Scatter` 9 |
| 임시 몬스터 | `Monster_Temp` — **이미 빨간색** (`Color 1,0,0,1`) |

### ❌ 아직 없는 것

| | 이번에 하나 | |
|---|---|---|
| `Assets/Data/Drops` 폴더 | **한다** | 도구를 아직 안 돌렸다 |
| 몬스터 정의의 `dropTable` | **한다** | 필드가 방금 생겨 아직 직렬화도 안 됐다 |
| `FloorPopulation` (씬 0개) | **한다** | 2F 하나만 |
| `MonsterSpawner` (씬 0개) | 아니오 | 아트 검증 뒤에 |
| `NoSpawnZone` (씬 0개) | 아니오 | 히든 방에 들어가지만 않으면 이번 검증엔 영향 없다 |
| `MonsterDefinition.frames` (전부 0) | 아니오 | 아트가 정면 10장뿐이다. 8방향이 나온 뒤에 |

> `Art/Monsters` 에 10장, `Art/PixelLab` 에 90장이 들어와 있지만 **아직 아무 데도 안 물렸다.**
> 이번 검증은 그것과 무관하게 돌아간다.

---

## 1. 스크립트가 새로 컴파일되게 둔다

`MonsterDefinition` 에 `dropTable` 필드가 방금 생겼다. 유니티로 돌아가
컴파일이 끝날 때까지 기다린다. **콘솔에 에러가 없어야 다음으로 간다.**

---

## 2. 드랍 표를 만든다

```
Pretty Knights > Data > 2. 드랍 표 점검 (변경 없음)
Pretty Knights > Data > 3. 드랍 표 생성/연결
```

`Assets/Data/Drops/` 에 **6종**이 생기고, 등급·역할로 **몬스터 10 + 오브젝트 18** 에 연결된다.

로그가 이렇게 나와야 한다.

```
[DropTable] 표 6종 (새로 6개) → Assets/Data/Drops
  몬스터 10종 · 오브젝트 18종에 연결했습니다.
```

**몬스터가 0종으로 나오면** 1번이 안 끝난 것이다 — 필드가 없으면 연결할 자리도 없다.

---

## 3. `Goblin2F` 에 `FloorPopulation` 하나

층 루트(`AreaAnchor` 가 붙어 있는 그 오브젝트)에 직접 붙인다.

```
Goblin2F  (GameObject)
 ├─ [C] AreaAnchor · WalkableArea · FloorProps      (기존)
 └─ [C] FloorPopulation                             ← 이것만 추가
          Monster Prefab      → Monster_Temp                ★
          Entries → 크기 1
              [0] Definition  → Monster_goblin_hob · Weight 1   ★
          Target Population   → 12    ← 안 쓰인다. 토템이 이긴다
          Spawn Interval      → 1     ← 기본 2.5 는 기다리기 지루하다
          Min Spawn Distance  → 8     ← 기본 14 는 화면 밖이라 안 보인다
          Max Spawn Distance  → 16
          Despawn Distance    → 45
          Distribution        → Uniform
```

**2F 를 고르는 이유는 거기에만 토템이 있기 때문이다.**
`Goblin_2F` 프로필이 메인 1 + 서브 3 을 뿌리고 지분이 4씩이라 **목표가 16** 이다.

> `Monster_Temp` 의 `Definition` 칸에 이미 무언가 들어 있는데 신경 쓰지 않아도 된다.
> 스폰할 때 `Spawn(definition, point)` 가 덮어쓴다.

**씬을 저장한다.**

---

## 4. 2F 로 간다

시작 구역은 `Goblin1F` 이므로 2F 까지 걸어갈 수 없다 (메인 토템을 부숴야 포탈이 열린다).
검증이니 바로 간다.

1. Boot 에서 재생
2. `GameRoot` 의 **`AreaTransition`** 인스펙터에서 `Debug Destination` → `Area_Goblin_2F`
3. 같은 컴포넌트 우클릭 → **디버그 이동**

콘솔에 이게 떠야 한다.

```
[FloorProps] Goblin 고블린 소굴 2층 (#102) — 오브젝트 139개 생성 (시드 …)
```

---

## 5. 검증 — 다섯 가지

인구를 볼 HUD 가 없다. **재생 중 `Goblin2F` 의 `FloorPopulation` 우클릭 → `인구 상태`**.

### ① 스폰이 되는가

가만히 서서 기다린다. 1초마다 한 마리씩 **빨간 기사**가 주변에 나타난다.

```
[FloorPopulation] 'Goblin2F' — 살아 있음 7 / 목표 16
  목표의 출처 : 토템 지분 합 16
```

> **"인스펙터 값 12 (토템 없음)"** 으로 나오면 토템이 등록되지 않은 것이다.
> `FloorPopulation` 이 층 루트가 아니라 자식에 붙었는지 확인한다 —
> `SpawnTotem` 은 부모 방향으로만 찾는다.

### ② 드랍이 도는가

한 마리 잡는다. 콘솔에 이렇게 나온다.

```
[보상] Goblin Hob — 경험치 12 + 잡동사니 2 = 14
[보상] Goblin Hob — 경험치 12 (드랍 없음)
```

**표는 확률이라 안 나오는 날도 있다.** 서너 마리 잡으면 갈린다.
`Drop_Monster_Normal` 은 잡동사니 35% · 이빨 10% 다.

> **아이템은 아직 없다.** 드랍은 경험치로만 나타난다 — 인벤토리도 아이템 SO 도
> 줍기도 없기 때문이다. 표가 도는지는 이 로그로만 확인한다.
> 로그를 끄려면 `GameRoot` 의 `Log Lifecycle` 을 끈다.

### ③ 서브 토템 — 목표가 준다

서브 토템(뒤틀린 그루터기)을 하나 부순다. `인구 상태`.

```
살아 있음 16 / 목표 12
목표의 출처 : 토템 지분 합 12
```

**살아 있는 수는 그대로다.** 목표만 준다.

### ④ 메인 토템 — 리스폰이 멈춘다

메인 토템(고철 토템)을 부순다. 같은 자리에 **빨간 포탈**이 켜진다.

```
살아 있음 16 / 목표 0
목표의 출처 : 메인 토템이 부서져 0
```

남은 것을 다 잡으면 **다시 안 나온다.**

> ⚠ **부수는 순간 몬스터가 사라지지는 않는다.** 줄어드는 것은 목표치뿐이다 —
> 증발하면 그 행동이 공짜가 되어 "지금 부술까, 정리하고 부술까" 라는 판단이 사라진다
> (`../design/map-objects.md` §1). 신호는 **"사라진다" 가 아니라 "다시 안 나온다"** 다.

### ⑤ 나갔다 와도 0인가 ← **가장 중요**

`Debug Destination` 을 `Area_Goblin_1F` 로 바꿔 디버그 이동 → 다시 `Area_Goblin_2F` 로.

```
목표의 출처 : 메인 토템이 부서져 0
```

**여기서 목표가 4로 되살아나면 회귀다** (2026-08-09 에 고친 버그).
복원이 `SpawnTotem.Start` 보다 먼저 일어나 부숴 둔 토템이 지분을 되살리던 문제다.

---

## 6. 끝나면

- `GameRoot` 우클릭 → **세이브 삭제** (검증 중 부순 것이 남는다)
- `FloorPopulation` 의 `Spawn Interval` 과 거리를 **기본값(2.5 / 14 / 26)으로 되돌린다**.
  검증용으로 좁혀 둔 값이라 그대로 두면 몬스터가 눈앞에서 튀어나온다

## 7. 여기서 막히면

| 증상 | 원인 |
|---|---|
| 드랍 표 도구가 "몬스터 0종" | 스크립트 컴파일이 안 끝났다 (1번) |
| 몬스터가 아예 안 나온다 | `Entries` 가 비었거나 `Weight` 0. 또는 `Monster Prefab` 이 비었다 |
| 목표 출처가 "인스펙터 값" | `FloorPopulation` 이 층 루트에 안 붙었다 |
| 몬스터가 안 보이는데 로그는 뜬다 | 스폰 거리가 넓다. `Min/Max` 를 8/16 으로 |
| 몬스터가 벽을 뚫고 다닌다 | 알려진 문제. 경로 탐색 미구현 (`../TODO.md` 4절) |
| 죽은 몬스터가 그대로 서 있다 | 2026-08-09 이전 버그. `FloorPopulation` 이 `Died` 를 안 들었다 |
| 어느 순간부터 리스폰이 멈춘다 | 같은 원인. 시체가 인구 상한을 차지했다 |
| 예고가 너무 짧아 못 피한다 | `MonsterDefinition.Telegraph Duration` — **아래 8절** |

---

## 8. 예고 시간을 조정할 때

`MonsterDefinition` 의 `Telegraph Duration` 이다. 기본값은 등급에서 나온다.

| 등급 | 기본 |
|---|---|
| Normal | 0.30 |
| Elite | 0.45 |
| Boss | 0.70 |

> ⚠ **`Pretty Knights > Data > 1. MonsterDefinition 생성/갱신` 을 다시 돌리면
> 손으로 맞춘 값이 등급 기본값으로 덮인다.** 도구가 이 칸을 쓰기 때문이다.
> 밸런싱한 값을 지키려면 `MonsterDefinitionBuilder` 의 표를 함께 고치거나,
> 도구를 다시 돌리지 않는다.

예고가 짧아 못 피하는 것은 **길이만의 문제가 아닐 수 있다.**
`Attack Range` 가 넓고 `Move Speed` 가 빠르면 예고가 떠도 벗어날 거리가 안 나온다.
셋을 함께 본다.
| 잡아도 경험치가 안 오른다 | `GameRoot` 에 `PlayerStatsDefinition` 이 안 물렸다 |
| 토템을 못 부순다 | 공격이 안 닿는다. `Prop` 프리팹의 콜라이더 `Is Trigger` 가 켜져 있는지 |
