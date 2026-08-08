# 오브젝트 배치 · 토템 · 보상방 세팅 가이드

> 코드는 전부 들어갔다. 여기서는 **에셋과 씬만** 만진다.
> 설계 근거는 [`docs/design/map-objects.md`](../design/map-objects.md) 와
> [`docs/decisions/006-area-transition.md`](../decisions/006-area-transition.md).
>
> 표기 — `[C]` 는 컴포넌트, 들여쓰기는 부모-자식. `★` 는 **반드시 손으로 연결**할 칸.

---

## 0. 순서

에셋 → 프리팹 → 프로필 → 씬 → 검증. **이 순서를 지키지 않으면 연결할 대상이 없어 막힌다.**

| | 무엇을 | 절 |
|---|---|---|
| 1 | `PropDefinition` 18종 (도구가 만든다) | 1 |
| 2 | `Prop.prefab` **하나** | 2 |
| 3 | `FloorScatterProfile` 층별 | 3 |
| 4 | `AreaDefinition` — 보상방 3종 · 던전 입구 · 기존 보강 | 4 |
| 5 | 씬 — `FloorProps` · `Rewards` 구역 · Breakable 타일맵 | 5 |
| 6 | Boot — `SkillIndicatorPool` | 6 |
| 7 | 검증 | 7 |

---

## 1. `PropDefinition` 18종

```
Pretty Knights > Props > 5. 오브젝트 정의 점검 (변경 없음)
Pretty Knights > Props > 6. PropDefinition 생성/갱신
```

`Assets/Data/Props/` 에 18개가 생긴다. 도구가 채우는 것:

- `propId` · `displayName` · `theme` · `role`
- `maxHp` · `expReward` · `populationShare` — **임시값** (플레이어 DPS 확정 후 재조정)
- `sprite` — propId 로 경로를 만들어 자동 연결
- `colliderSize` — 실측 접지폭 × 0.5칸
- `visualOffsetY` — 실측표 값

**직접 채울 것:** `Broken Sprite` (부서진 그림), `Drop Table`. 비워도 동작한다 —
부서진 그림이 없으면 감춰지고, 드랍 테이블이 없으면 `expReward` 만 준다.

역할은 실측표대로 배정되어 있다.

| 테마 | MainTotem | SubTotem |
|---|---|---|
| Goblin | `06_scrap_totem` | `01_twisted_stump` |
| Orc | `05_war_totem` | `01_scorched_stump` |
| Vampire | `06_bloodstone_altar` | `03_sarcophagus` |

---

## 2. `Prop.prefab` — 하나만 만든다

**배리언트 18개를 만들지 않는다.** 스프라이트·콜라이더 크기·`Visual` Y 는
`Destructible.Bind` 가 정의에서 읽어 채운다.

`Assets/Prefabs/Props/Prop.prefab` 으로 저장한다. **루트와 `Visual` 둘뿐이다.**

```
Prop  (GameObject · 루트)
 ├─ [C] Transform              (0, 0, 0)   ← 여기가 접지점이다
 ├─ [C] CapsuleCollider2D      Direction: Horizontal
 │                             Size · Offset 은 비워도 된다 (Bind 가 채운다)
 │                             ★ Is Trigger **꺼짐** — 켜면 통행을 막지 못한다
 ├─ [C] Destructible
 │         Definition → **비움** (배치할 때 꽂힌다)
 │         View / Colliders → 비움 (자동 탐색)
 │
 └─ Visual  (GameObject · 자식)
      ├─ [C] Transform         Y 는 비워도 된다 (Bind 가 채운다)
      └─ [C] SpriteRenderer    Sprite 비움 · Draw Mode: Simple
```

> `SpawnTotem` 은 붙이지 않는다. 토템일 때만 배치 코드가 자동으로 붙인다.

---

## 3. `FloorScatterProfile` — 층별

`Assets/Data/Scatter/` 에 `Create > Pretty Knights > Floor Scatter Profile`.
**층마다 하나씩** 만든다 (Goblin 먼저, 검증 후 복제).

| 칸 | Goblin 1F | Goblin 2F | Goblin 3F (보스) |
|---|---|---|---|
| **Prop Prefab** ★ | `Prop` | `Prop` | `Prop` |
| **Portal Prefab** ★ | **Blue** | **Red** | 비움 |
| Min Spacing | 3 | 3 | 5 |
| Wall Clearance | 1 | 1 | 2 |
| Protected Radius | 4 | 4 | 6 |
| Seed | 아무 값 | 다른 값 | 다른 값 |

**항목(Entries)** — 권장 시작값이다. `0. 배치 개수만 계산` 으로 보고 조정한다.

| 정의 | Goblin 1F (12,147칸) | Goblin 2F (20,035칸) | Goblin 3F (5,742칸) |
|---|---|---|---|
| `06_scrap_totem` (Main) | **1** | **1** | **0** |
| `01_twisted_stump` (Sub) | **2** | **3** | 0 |
| `02_moss_boulder` | 20 | 35 | 6 |
| `03_gold_vein_rock` | 15 | 25 | 4 |
| `04_mushroom_cluster` | 25 | 40 | 5 |
| `05_supply_pile` | 20 | 35 | 5 |
| **합계** | 약 83 | 약 139 | 약 20 |

대략 **바닥 100~150칸당 1개**다. 듬성듬성하지만 길이 자연스럽게 굽는 밀도.

**3F 는 토템을 두지 않는다.** 보스 처치가 그 역할을 대신하고 보상 포탈이 열린다.
보스방은 회피 공간이 필요하므로 밀도를 낮추고 간격을 넓게 잡는다.

---

## 4. `AreaDefinition`

### 4-1. 기존 3종 보강

| | Area_Goblin_1F | Area_Goblin_2F | Area_Goblin_3F |
|---|---|---|---|
| **Next Area** ★ | `Area_Goblin_2F` | `Area_Goblin_3F` | **비움** |
| **Next Arrival Id** ★ | `from_1f` | `from_2f` | — |
| **Scatter Profile** ★ | 1F 프로필 | 2F 프로필 | 3F 프로필 |
| Is Boss Floor | 끔 | 끔 | **켬** |
| Is Reward Room | 끔 | 끔 | 끔 |

`Next Area` 가 **메인 토템이 열 포탈의 목적지**다. 토템이 어디에 뽑히든 링크가 어긋나지 않는다.
3F 는 메인 토템이 없으므로 비운다.

### 4-2. 보상방 3종 — 새로 만든다

| 파일명 | Area Id | Display Name | Is Reward Room |
|---|---|---|---|
| `Area_Goblin_Reward` | **190** | 고블린 보상방 | **★ 켬** |
| `Area_Orc_Reward` | **290** | 오크 보상방 | **★ 켬** |
| `Area_Vampire_Reward` | **390** | 뱀파이어 보상방 | **★ 켬** |

**`Is Reward Room` 이 핵심이다.** 여기를 나가는 순간 그 테마를 완전 클리어한 것으로 보고
파괴 기록을 지우고 클리어 횟수를 올린다 → 다음 입장 시 배치가 새로 뽑힌다.

`Scatter Profile` 은 비운다. 보상방은 손으로 꾸민다.

### 4-3. 던전 입구 — 씬에 이미 있다

`Map/Dungeon` 에 `BasicFloor` · `Guide` · `WalkableArea` 가 이미 있다.
이것을 **던전 입구 #3** 으로 쓰면 순환이 닫힌다.

| 파일명 | Area Id | Display Name |
|---|---|---|
| `Area_Dungeon_Entrance` | **3** | 던전 입구 |

그러면 흐름이 이렇게 이어진다.

```
던전 입구 #3  ──[테마 선택 포탈 ×3, 수동]──▶  각 테마 1F
1F ──[Blue]──▶ 2F ──[Red]──▶ 3F
3F 보스 처치 ──[Gold, 시체 자리에 생성]──▶ 보상방 #190
보상방 ──[Blue, 수동]──▶ 던전 입구 #3   ← 여기서 테마 초기화
```

각 층의 `Escape To` 도 이 `Area_Dungeon_Entrance` 로 채우면 탈출 스킬이 붙을 때 바로 동작한다.

#### 씬 쪽도 함께 만들어야 한다

`AreaDefinition` 만 만들면 등록되지 않는다. `Map/Dungeon` 에 붙일 것이 셋 더 있다.

```
Dungeon  (GameObject)
 ├─ [C] AreaAnchor                  ← 추가
 │        Definition       → ★ Area_Dungeon_Entrance
 │        Floor            → ★ BasicFloor 의 Tilemap
 │        Fallback Arrival → ★ from_reward
 ├─ [C] WalkableArea                (기존)
 │
 ├─ BasicFloor · Guide              (기존)
 │
 ├─ Arrivals  (GameObject)          ← 추가
 │    └─ from_reward  (GameObject)
 │         └─ [C] ArrivalPoint   Arrival Id → from_reward
 │
 └─ Portals  (GameObject)           ← 추가. 테마 선택 포탈 3개
      ├─ Portal_to_Goblin   → Area_Goblin_1F  / from_entrance
      ├─ Portal_to_Orc      → Area_Orc_1F     / from_entrance
      └─ Portal_to_Vampire  → Area_Vampire_1F / from_entrance
```

> ⚠ **도착 지점 이름은 "어디서 왔는가" 다.** 던전 입구에 내려서는 사람은 **보상방에서 온 것**이므로
> `from_reward` 다. `from_entrance` 로 지으면 "입구에서 와서 입구에 도착했다" 가 되어 읽을 수 없다.
>
> 각 테마 1F 의 `from_entrance` 는 맞다 — 그쪽은 **던전 입구에서** 온 자리다.
> 같은 이름이 두 구역에 있어도 동작에는 문제가 없지만(id 는 구역 안에서만 유일하면 된다)
> 나중에 어느 쪽을 가리키는지 헷갈린다.

---

## 5. 씬 — `Ingame_Horizontal`

### 5-1. 층마다 `FloorProps`

`Goblin1F` · `Goblin2F` · `Goblin3F` **각 루트**에 붙인다. `AreaAnchor` 와 나란히 놓인다.

```
Goblin1F  (GameObject)
 ├─ [C] AreaAnchor          (기존)
 ├─ [C] WalkableArea        (기존)
 ├─ [C] FloorProps          ← 추가
 │        Anchor          → 비움 (자동)
 │        Rebuild On Enable → **끔**
 │        Log Build       → 켬 (검증 동안)
 ├─ 1Floor · 1FGuide · 1FHiddenRewards
 ├─ Arrivals · Portals
 └─ Monsters
```

> `Rebuild On Enable` 을 켜면 층에 들어갈 때마다 다시 뽑는다.
> **부순 것이 되살아나므로 평소에는 꺼 둔다.** 배치를 눈으로 바꿔가며 볼 때만 켠다.

### 5-2. `Rewards` 를 구역으로

지금 `Map/Goblin/Rewards` 에 `Floor` · `Guide` 만 있다. 구역이 되려면 셋이 더 필요하다.

```
Rewards  (GameObject)
 ├─ [C] AreaAnchor              ← 추가
 │        Definition       → ★ Area_Goblin_Reward
 │        Floor            → ★ Rewards/Floor 의 Tilemap
 │        Fallback Arrival → ★ from_boss
 ├─ [C] WalkableArea            ← 추가
 │        Floor → ★ Rewards/Floor · Guide → ★ Rewards/Guide
 │
 ├─ Floor · Guide               (기존)
 │
 ├─ Arrivals  (GameObject)      ← 추가
 │    └─ from_boss  (GameObject)
 │         └─ [C] ArrivalPoint   Arrival Id → from_boss
 │
 ├─ Portals  (GameObject)       ← 추가
 │    └─ Portal_to_entrance  (Blue_Portal 인스턴스)
 │         └─ [C] Portal
 │              Destination            → ★ Area_Dungeon_Entrance
 │              Destination Arrival Id → ★ from_reward   ← 4-3 에서 만든 지점
 │
 └─ Items  (GameObject)         보상 아이템은 여기에 손으로 배치
```

**`FloorProps` 는 붙이지 않는다.** 보상방은 자동 배치 대상이 아니다.

### 5-3. Breakable 타일맵

층마다 히든 방 벽 타일맵에 붙인다.

```
1FHiddenRewards  (GameObject)
 ├─ [C] Tilemap · TilemapRenderer          (기존)
 ├─ [C] TilemapCollider2D                   ← 있어야 벽이 된다
 ├─ [C] CompositeCollider2D
 ├─ [C] Rigidbody2D          Static
 └─ [C] DestructibleTilemap                 ← 추가
          Hp Per Cell   → 60
          Defense       → 0
          Damaged Tint  → 어두운 적갈색
```

그리고 **같은 층 `WalkableArea` 의 `Breakable` 칸에 이 Tilemap 을 연결한다.**
빠뜨리면 히든 방 안에 몬스터가 스폰되고 도착 지점이 벽 안에 잡힌다.

> ⚠ **`Guide` 와 같은 칸에 겹치면 안 된다.** 부숴도 아래 콜라이더가 남아 못 지나가는데
> 겉보기엔 벽이 사라져 원인을 찾기 어렵다. Breakable 이 있는 칸은 `Guide` 에서 비운다.

> 이름이 `1FHIddenRewards`(Goblin 1F · Orc 1F) 와 `1FHiddenRewards`(Vampire) 로 갈려 있다.
> 이 김에 통일해두면 나중에 찾기 쉽다.

---

## 6. Boot — `SkillIndicatorPool`

몬스터 공격 예고가 화면에 뜨려면 필요하다. `GameRoot` 오브젝트에 컴포넌트로 추가한다.

```
GameRoot
 ├─ [C] GameRoot · AreaTransition · InteractionHub   (기존)
 └─ [C] SkillIndicatorPool                            ← 추가
          Sorting Layer  → Default
          Sorting Order  → 50      바닥 위 · 캐릭터 아래
          Depth          → 0.01
          Hostile Color  → 빨강 (알파 0.85)
          Prewarm        → 8
```

정렬은 [`docs/TODO.md`](../TODO.md) 의 "정렬 일괄 지정" 에서 마지막에 한 번에 잡는다.
지금은 임시값으로 두고 넘어간다.

---

## 7. 검증

```
Pretty Knights > Props > 0. 배치 개수만 계산 (변경 없음)   ← 밀도 확인
Pretty Knights > Props > 1. 미리보기 만들기               ← 눈으로 확인
Pretty Knights > Props > 3. 연결성 검사 (변경 없음)        ← 길이 막히는지
Pretty Knights > Props > 2. 미리보기 지우기               ← 확인 끝나면 치운다
Pretty Knights > Areas > 0. 포탈 링크 점검 (변경 없음)
```

미리보기는 `PropPreview` 컨테이너에만 만든다. **손으로 놓은 것은 건드리지 않는다.**
실제 배치는 재생 시 `FloorProps` 가 **같은 시드로 다시** 만든다.

### 재생해서 확인할 것

| 확인 | 기대 |
|---|---|
| 시작 로그 | `[FloorProps] ... 오브젝트 83개 생성 (시드 ...)` |
| 오브젝트 | 미리보기와 같은 자리에 같은 모습 |
| 때리기 | 데미지가 들어가고 HP 0 에 부서진다 |
| 부서진 뒤 | **통과할 수 있다** (콜라이더가 꺼진다) |
| 서브 토템 파괴 | 몬스터 목표 인구가 줄고, **이미 나온 것은 그대로** |
| 메인 토템 파괴 | 그 자리에 포탈이 나타나고 인구가 0 이 된다 |
| 히든 방 벽 | 때리면 어두워지고, 다 깎이면 칸이 사라진다 |
| 재시작 | 부순 것이 **부서진 채로** 남아 있다 |
| 보상방 → 입구 | 콘솔에 `테마 1 완전 클리어 — 1회차. 다음 입장 시 재배치됩니다` |

---

## 8. 안 되면 여기부터

| 증상 | 원인 |
|---|---|
| 오브젝트가 하나도 안 생긴다 | `AreaDefinition` 의 `Scatter Profile` 이 비었거나 `Prop Prefab` 이 없다 |
| 전부 같은 모습으로 나온다 | `Destructible` 이 프리팹 루트에 없다 (`Bind` 가 안 불린다) |
| 요청한 개수보다 적게 놓인다 | `Min Spacing` · `Wall Clearance` · `Protected Radius` 가 크다 |
| 오브젝트가 공중에 뜬다 | `PropDefinition` 의 `Visual Offset Y` 가 실측표와 다르다 |
| 부쉈는데 못 지나간다 | 콜라이더가 `Visual` 자식에 있다. **루트에 두어야 한다** |
| 메인 토템을 부숴도 포탈이 없다 | `Next Area` 가 비었거나 프로필에 `Portal Prefab` 이 없다 |
| 히든 방 벽을 때려도 반응이 없다 | `DestructibleTilemap` 미부착, 또는 `TilemapCollider2D` 없음 |
| 히든 방 벽을 부쉈는데 못 지나간다 | 같은 칸에 `Guide` 타일이 있다 |
| 히든 방 안에 몬스터가 스폰된다 | `WalkableArea` 의 `Breakable` 이 비었다 |
| 층에 들어갈 때마다 부순 것이 되살아난다 | `FloorProps` 의 `Rebuild On Enable` 이 켜져 있다 |
| 나갔다 오면 경험치를 또 준다 | 복원이 `Break(grantRewards: false)` 를 안 탔다 — 코드 문제이니 알릴 것 |
| **디버그 이동이 아무 반응이 없다** | 목적지 층의 `AreaAnchor` 에 `AreaDefinition` 이 안 꽂혀 있다 |
| 이동은 되는데 엉뚱한 자리에 선다 | 포탈의 `Destination Arrival Id` 가 그 구역에 없어 대체 지점으로 갔다. 콘솔에 경고가 있다 |
| 포탈을 눌러도 그 층으로 안 간다 | 같은 원인 |

### `AreaDefinition` 이 비면 그 구역은 존재하지 않는 것과 같다

`AreaRegistry` 는 **정의가 없는 앵커를 아예 등록하지 않는다.** 등록되지 않으면
포탈도 디버그 이동도 그 번호를 찾지 못한다. 씬에 오브젝트가 멀쩡히 있어도 그렇다.

재생하면 시작 시 경고가 뜬다.

```
[AreaRegistry] AreaDefinition 이 비어 등록되지 않은 구역이 9개 있습니다.
  Orc1F
  Orc2F
  ...
```

이동에 실패하면 **지금 등록된 번호 목록**을 함께 찍는다 — 무엇이 빠졌는지 바로 보인다.

```
[AreaTransition] areaId #190 (고블린 보상방) 에 해당하는 층 오브젝트를 찾지 못했습니다.
  그 층의 AreaAnchor 에 이 정의가 연결되어 있는지 확인하세요.
  지금 등록된 구역: 101, 102, 103
```

---

## 9. 아직 없는 것

- **보스 처치 → Gold 포탈** — 보스 처치 판정이 아직 없다. 지금은 3F 에서 보상방으로 갈 길이 없으므로,
  검증할 때는 `AreaTransition` 우클릭 → **디버그 이동**으로 보상방에 들어간다
- **던전 입구의 테마 선택 포탈 3개** — 손으로 배치한다
- **아이템 시스템** — 보상방의 `Items` 는 아직 놓을 것이 없다. `DropTable` 도 경험치만 준다
- **정렬** — 마지막에 한 번에 잡는다 (`docs/TODO.md`)
