# 몬스터 스폰 · 드랍 배치 가이드

> **몬스터가 안 나오는 이유는 씬에 스포너가 하나도 없기 때문이다** (2026-08-09 확인).
> `MonsterSpawner` 0개 · `FloorPopulation` 0개. 코드와 정의 10종은 준비되어 있다.
>
> 프리팹 조립은 [`monster-prefab-setup.md`](monster-prefab-setup.md),
> 층 구조는 [`all-maps-setup.md`](all-maps-setup.md) 를 본다.

---

## 0. 층마다 방식이 다르다

| 층 | 붙일 것 | 왜 |
|---|---|---|
| **1F** | `MonsterSpawner` 를 **지점마다** | 그 테마 몬스터를 소개하는 층이다. **자리가 곧 설계**다 |
| **2F** | `FloorPopulation` 을 **층에 하나** | 파밍 층이라 넓다 (Goblin 2F 만 20,035칸). 스포너를 찍을 수 없다 |
| **3F** | `MonsterSpawner` 를 **보스 자리에 하나** | 보스는 자리가 정해져 있다 |
| 보상방 · 던전 입구 | **없음** | 안전 구역 |

**층 오브젝트가 꺼지면 스포너도 함께 꺼진다.** 다른 층의 몬스터가 뒤에서 도는 일은 없다.

---

## 1. 1F — 지점마다 스포너

층 루트 아래에 묶음용 오브젝트를 하나 두고 그 자식으로 찍는다.

```
Goblin1F  (GameObject)                      (기존)
 ├─ [C] AreaAnchor · WalkableArea · FloorProps      (기존)
 ├─ 1Floor / 1FGuide / 1FHiddenRewards / Arrivals   (기존)
 │
 └─ Spawners  (GameObject)                  ← 추가. 묶음용이라 [C] Transform 하나뿐
      ├─ Spawn_Grunt_01  (GameObject)       ← 추가
      │    ├─ [C] Transform                 위치가 곧 스폰 자리
      │    └─ [C] MonsterSpawner
      │             Monster Prefab       → Monster_Temp        ★
      │             Definition           → Monster_goblin_hob  ★
      │             Max Alive            → 1
      │             Respawn Cooldown     → 8
      │             Activation Distance  → 20
      │             Check Interval       → 0.2
      │             Placement Search Radius → 2
      │
      ├─ Spawn_Grunt_02 … 같은 구성, 위치만 다르게
      └─ Spawn_Grunt_03
```

**몇 개를 찍나** — 1F 는 안내 층이므로 **6~10개**로 시작한다.
길목과 갈림길에 두고 막다른 곳에는 두지 않는다.

> `Activation Distance` 20 은 화면(가로 기준 약 30유닛)보다 좁다.
> **플레이어가 다가와야 나타난다** — 멀리서 미리 채워 두면 도착했을 때 이미 몰려 있다.

### 스포너를 벽 위에 찍어도 된다

`Placement Search Radius` 안에서 설 수 있는 자리를 찾아 옮긴다.
못 찾으면 경고를 남기고 그 번은 건너뛴다.

---

## 2. 2F — 층에 하나

```
Goblin2F  (GameObject)                      (기존)
 ├─ [C] AreaAnchor · WalkableArea · FloorProps      (기존)
 └─ [C] FloorPopulation                     ← 추가. 층 루트에 직접 붙인다
          Monster Prefab   → Monster_Temp   ★
          Entries          → 크기 2 ★
              [0] Definition → Monster_goblin_hob    · Weight 3
              [1] Definition → Monster_goblin_shaman · Weight 1
          Target Population → 12       ← 토템이 있으면 이 값은 안 쓰인다 (아래)
          Spawn Interval    → 2.5
          Min Spawn Distance → 14
          Max Spawn Distance → 26
          Despawn Distance   → 45
          Distribution       → FarWeighted
          Cluster Size / Radius → 3 / 4   (Clustered 일 때만)
```

`Entries` 의 `Weight` 는 **비율**이다. 위 예는 잡몹 3 : 정예 1.

### ⚠ 목표 인구는 토템이 정한다

`FloorProps` 가 뿌린 **토템이 지분을 얹는다** (`docs/design/map-objects.md` §1 — 메모리 할당 모델).
토템이 하나라도 등록되면 **인스펙터의 `Target Population` 은 무시된다.**

```
메인 토템 파괴  →  기본 점유가 풀린다  →  목표 0  →  리스폰이 멈춘다
서브 토템 파괴  →  추가 점유만 빠진다  →  목표 감소
```

**살아 있는 개체는 줄지 않는다.** 목표만 내려가고 실제 수는 잡히면서 수렴한다.
그래서 토템을 부순 직후에도 남은 몬스터를 정리해야 한다.

> `Target Population` 은 **토템이 없는 층의 기본값**이다. 지금 2F 에는
> `FloorScatterProfile` 이 메인 1 + 서브 3 을 뿌리므로 토템 쪽이 이긴다.

### 왜 플레이어 주변 고리인가

넓은 층에 미리 채워두면 대부분이 화면 밖에서 돌아 비용만 든다.
`Min Spawn Distance` 14 는 **눈앞에 튀어나오지 않게** 하는 값이고,
`Despawn Distance` 45 는 지나온 몬스터를 회수한다.

---

## 3. 3F — 보스 하나

```
Goblin3F  (GameObject)
 └─ Spawners  (GameObject)
      └─ Spawn_Boss  (GameObject)
           └─ [C] MonsterSpawner
                    Monster Prefab      → Monster_Temp          ★
                    Definition          → Monster_goblin_king   ★
                    Max Alive           → 1
                    Respawn Cooldown    → 999   ← 사실상 안 살아난다
                    Activation Distance → 30    ← 보스방은 넓다
                    Placement Search Radius → 3
```

보스를 잡으면 보상방 포탈이 열려야 하지만 **그 판정이 아직 없다.**
지금은 `AreaTransition` 우클릭 → 디버그 이동으로 들어간다
([`all-maps-setup.md`](all-maps-setup.md) 7절).

---

## 4. 드랍 — 도구로 만든다

**몬스터에는 드랍 표 필드 자체가 없었다** (2026-08-09 추가).
오브젝트만 `DropTable` 을 쓰고 있어 몬스터는 확정 경험치만 줬다.

```
Pretty Knights > Data > 2. 드랍 표 점검 (변경 없음)
Pretty Knights > Data > 3. 드랍 표 생성/연결
```

만들어지는 것 — `Assets/Data/Drops/` 에 **6종**

| 표 | 붙는 곳 | 내용 |
|---|---|---|
| `Drop_Monster_Normal` | 등급 Normal | 잡동사니 35% 1~3 · 이빨 10% 4~8 |
| `Drop_Monster_Elite` | 등급 Elite | 전리품 50% 8~16 · 희귀 15% 20~35 |
| `Drop_Monster_Boss` | 등급 Boss | 보스 전리품 100% 60~120 · 희귀 50% 100~200 |
| `Drop_Prop_Common` | 나머지 오브젝트 | 부스러기 25% 1~2 |
| `Drop_Prop_Rich` | 금맥 바위 · 보급 더미 등 | 광석 60% 5~12 · 숨은 한 줌 10% 15~25 |
| `Drop_Prop_Totem` | 토템 전부 | 토템 조각 80% 20~40 |

연결은 **등급과 역할에서 기계적으로** 정해진다. 28곳을 손으로 물리면 반드시 하나를 빠뜨리고,
빠뜨린 곳은 "왜 이것만 경험치가 적지" 로 나타나 찾기 어렵다.

> **이미 표가 물려 있으면 덮어쓰지 않는다.** 손으로 특별하게 준 것을 되돌리지 않는다.

### 지금 드랍은 경험치만 준다

아이템 시스템(인벤토리·아이템 SO·줍기)이 아직 없다.
표는 **확정 보상 위에 얹히는 변동분**이다 — 같은 몬스터를 잡아도 수확이 달라지는 것이
파밍의 결을 만든다. 아이템이 생기면 `DropTable.Entry` 에 참조를 더하면 되고
**부르는 쪽(`MonsterController` · `Destructible`)은 바뀌지 않는다.**

두 곳의 보상 계산을 같은 모양으로 맞춰 두었다.

```
확정 경험치  +  표를 굴린 값   →  AddExp
```

---

## 5. 확인

1. Boot 에서 재생 → Goblin1F
2. 스포너 근처로 걸어간다 → **20유닛 안에 들어가면 나타난다**
3. `GameRoot` 우클릭 → **상태 로그** 로 경험치를 적어 둔다
4. 한 마리 잡는다 → 다시 상태 로그 → **확정 경험치보다 많거나 같으면** 표가 도는 것이다
   (표는 확률이라 안 나오는 날도 있다. 서너 마리 잡아보면 갈린다)
5. 2F 로 올라가 가만히 서 있는다 → **2.5초마다 한 마리씩** 주변에 채워진다
6. 메인 토템을 부순다 → **더 채워지지 않는다.** 살아 있는 것은 그대로다

### 안 되면

| 증상 | 원인 |
|---|---|
| 아무것도 안 나온다 | 씬에 스포너가 없다. 이 문서 1~3절 |
| 1F 만 나오고 2F 는 안 나온다 | `FloorPopulation` 의 `Entries` 가 비었거나 `Weight` 가 전부 0 |
| 2F 에서 한두 마리만 나오고 만다 | 토템이 목표를 정한다. 메인 토템이 이미 부서졌는지 확인 |
| 몬스터가 안 보이는데 로그는 뜬다 | **아트가 없다.** 지금은 임시 프리팹이라 정상 |
| 벽 안에 스폰된다 | `WalkableArea` 의 `Breakable` 이 비었다. 히든 방 벽을 안 물렸다 |
| 히든 방 안에 스폰된다 | `NoSpawnZone` 이 없다. 지금 씬에 0개다 — 6절 |
| 히든 방 **절반에만** 스폰된다 | 사각형 여러 개인데 `Room Id` 를 안 줬다. 들어간 칸만 풀렸다 |
| 경험치가 안 오른다 | `PlayerStatsDefinition` 이 `GameRoot` 에 안 물렸다 |

---

## 6. 히든 방 봉인 — `NoSpawnZone`

**지금 씬에 0개다.** 없으면 히든 방 안에 몬스터가 뿌려지고, 몬스터는 오브젝트를
부술 수 없어 영영 갇힌 채 층 인구 상한을 차지한다. 토템 지분 모델이 그대로 샌다.

```
Goblin1F
 └─ HiddenRooms  (GameObject)               ← 추가. 묶음용
      ├─ Room_A  (GameObject)
      │    └─ [C] NoSpawnZone
      │             Mask                  → 비움 (사각형이면)
      │             Size                  → (8, 6)
      │             Blocks Monsters       → 켬
      │             Blocks Props          → 켬
      │             Reveal On Player Enter → 켬
      │             Room Id               → 비움 (사각형 하나면 필요 없다)
      └─ Room_B …
```

### 들키면 풀린다

플레이어가 구역 안에 들어오면 **몬스터 차단만** 자동으로 풀린다.
벽을 뚫고 들어간 방이 계속 비어 있으면 층의 일부가 죽은 공간이 된다.

- **콜라이더를 쓰지 않는다.** 같은 도형 판정을 0.25초마다 물어본다 —
  트리거와 결과가 같고 레이어를 맞출 일이 없다
- 히든 상자를 열었을 때처럼 밖에서 열고 싶으면 `NoSpawnZone.Reveal()` 을 부른다
- **해제는 세이브에 남는다.** 층을 껐다 켜도 다시 봉인되지 않는다
- **테마를 완전 클리어하면 다시 봉인된다.** 벽도 함께 되살아나므로 짝이 맞는다

> **오브젝트 배치는 들켜도 계속 막힌다.** 배치는 그 층의 지형이라
> 다시 왔을 때 달라지면 안 된다 (`docs/design/map-objects.md` §4).
> 몬스터만 풀린다.

### 직사각형이 아닌 방 — 두 가지 방법

| 방 모양 | 방법 |
|---|---|
| ㄱ자 · ㅗ자 같은 직교 형태 | **사각형 여러 개**를 겹쳐 놓고 **`Room Id` 를 같게 준다** |
| 곡선 · 들쭉날쭉한 형태 | **`Mask` 에 타일맵**을 물린다. **칠한 칸이 곧 구역**이다 |

**`Room Id` 를 반드시 같게 준다.** 안 주면 플레이어가 들어간 사각형만 풀리고
나머지 절반에는 계속 몬스터가 안 나온다 — 한 방인데 반쪽만 살아나는 것이
겉으로는 "가끔 안 나온다" 로 보여 원인을 찾기 어렵다.

**타일맵 방식이 대체로 낫다.** 맵을 그리는 것과 같은 손놀림이고, 판정이
칸 조회 한 번이라 사각형 여러 개를 겹치는 것보다 오히려 싸다.
`1FHiddenRewards` 옆에 `1FNoSpawn` 을 하나 더 만들어 방 안쪽을 칠하면 된다
(렌더러는 꺼도 된다 — 판정에만 쓴다).

> `Mask` 를 물리면 `Size` 는 무시된다. 기즈모 상자도 안 그린다 —
> 칠한 칸이 에디터에 그대로 보이기 때문이다.

---

## 7. 아직 안 한 것
- **몬스터 아트** — `Monster_Temp` 는 Knights 스프라이트를 색만 바꾼 것이다
- **경로 탐색** — `MonsterController` 는 대상 쪽으로 곧장 향하는 단순 조향이라
  **Vampire 3F(벽:바닥 1:1.4)에서 반드시 벽에 붙어 떤다** (`../TODO.md` 4절)
- **보스 처치 → 보상방 포탈**
- **아이템 드랍** — 표는 준비됐고 아이템 시스템이 남았다
