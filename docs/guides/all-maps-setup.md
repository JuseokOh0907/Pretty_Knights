# 전 구역 하이어라키 세팅 가이드

> **한 테마만 되어 있던 것을 셋으로 늘리고, 던전 입구에서 순환을 닫는다.**
> Goblin 은 이미 검증됐다 ([`portal-area-setup.md`](portal-area-setup.md) 참조).
> 이 문서는 **Orc · Vampire 8개 구역 + 던전 입구의 포탈 3개**를 같은 모양으로 만드는 절차다.
>
> 오브젝트 자동 배치의 설계 근거는 [`prop-scatter-setup.md`](prop-scatter-setup.md),
> 구역 전환의 근거는 [`../decisions/006-area-transition.md`](../decisions/006-area-transition.md).

---

## 0. 지금 어디까지 되어 있나

씬(`Ingame_Horizontal`)에 구역이 **13개** 있고 전부 `AreaAnchor` + `WalkableArea` 는 붙어 있다.
아래가 빈 칸이다.

| 구역 | AreaAnchor 의 Definition | WalkableArea 의 Floor/Guide | FloorProps | Arrivals | Portals |
|---|---|---|---|---|---|
| `Goblin1F` `Goblin2F` `Goblin3F` | ✅ | ✅ | ✅ | ✅ | (자동) |
| `Goblin/Rewards` | ✅ | ✅ | — | ✅ | ✅ |
| `Dungeon` | ✅ | ✅ | — | 하나뿐 | **❌ 3개 필요** |
| `Orc1F` `Orc2F` `Orc3F` | **❌** | ✅ | **❌** | **❌** | (자동) |
| `Orc/Rewards` | **❌** | **❌ 둘 다 비어 있다** | — | **❌** | **❌** |
| `Vampire1F` `Vampire2F` `Vampire3F` | **❌** | ✅ | **❌** | **❌** | (자동) |
| `Vampire/Rewards` | **❌** | **❌ 둘 다 비어 있다** | — | **❌** | **❌** |

> **"(자동)" 은 층 포탈에 손댈 것이 없다는 뜻이다.** 층을 잇는 포탈은 씬에 두지 않는다.
> 메인 토템이 어디에 뽑히든 **그 자리에** `FloorProps` 가 꺼진 포탈을 함께 만들고,
> 토템을 부수면 켜진다. 목적지는 `AreaDefinition.Next Area` 가 정한다.
> 씬에 손으로 두는 포탈은 **던전 입구 3개와 보상방 3개, 총 6개뿐이다.**

### ⚠ 정의가 비면 그 구역은 없는 것과 같다

`AreaRegistry` 는 `AreaDefinition` 이 빈 `AreaAnchor` 를 **등록조차 하지 않는다.**
등록되지 않으면 포탈도 디버그 이동도 그 구역을 찾지 못한다.

더 나쁜 것은 **끄지도 못한다**는 점이다. `AreaRegistry` 가 시작할 때
"활성 구역 하나만 남기고 끄기" 를 하는데 그 대상이 등록된 것뿐이라,
지금은 Orc·Vampire 층이 켜진 채로 함께 그려지고 `WalkableArea` 가 서로 자리를 뺏는다.

---

## 1단계 · 에셋을 도구로 만든다

인스펙터로 채우지 않는다. 구역 13개 × (다음 층 · 탈출 · 배치 프로필) 링크는
**오타를 컴파일러가 잡아주지 못하고, 그 포탈을 실제로 밟기 전까지 드러나지 않는다.**

**`Ingame_Horizontal` 을 열어 둔 상태에서** 순서대로 실행한다.

```
Pretty Knights > Areas > 2. 구역 정의 점검 (변경 없음)
Pretty Knights > Areas > 3. AreaDefinition · 배치 프로필 생성/갱신
```

씬이 열려 있어야 하는 이유는 **배치 개수를 바닥 칸 수로 계산하기 때문이다.**
층마다 넓이가 1,950칸에서 20,035칸까지 10배 넘게 차이 나 고정 개수는 의미가 없다.

| 밀도 | 값 | 근거 |
|---|---|---|
| 일반 층 | 바닥 **145칸당 1개** | 검증된 Goblin 1F·2F 의 개수를 그대로 재현한다 |
| 보스 층 | 바닥 **287칸당 1개** | 절반 밀도. 예고를 보고 피할 공간이 있어야 한다 |

만들어지는 것

- `Assets/Data/Areas/` — **13개** (`#3` · `101~103` · `190` · `201~203` · `290` · `301~303` · `390`)
- `Assets/Data/Scatter/` — **9개** (테마 3 × 층 3). 보상방과 던전 입구는 자동 배치 대상이 아니다

**이미 있는 배치 프로필은 건드리지 않는다.** 개수·시드·간격은 손으로 다듬는 값이라
덮어쓰면 받아들인 배치가 조용히 다시 뽑힌다. 비어 있는 프리팹 칸만 채운다.

도구가 채워 주는 링크

```
#101 → next #102 (from_1f)   escape #3 (from_escape)   scatter Goblin_1F
#102 → next #103 (from_2f)   escape #3                 scatter Goblin_2F
#103 → next #190 (from_boss) escape #3                 scatter Goblin_3F   보스 층
#190 →                       escape #3                 (배치 없음)         보상방
```

Orc(`2xx`)·Vampire(`3xx`)도 같은 모양이다.
`#103 → #190` 은 **보스를 잡으면 시체 자리에 생길 Gold 포탈의 목적지**다.

---

## 2단계 · 층 하나의 완성형 계층

**`Orc1F` 을 예로 든다.** 나머지 5개 층(`Orc2F` `Orc3F` `Vampire1F` `Vampire2F` `Vampire3F`)도
숫자와 이름만 바꿔 똑같이 만든다.

층 루트 아래 **게임오브젝트는 5개**다 — `1Floor` `1FGuide` `1FHiddenRewards` `Arrivals` 그리고 `Arrivals` 의 자식 하나.
(`Orc3F` 처럼 히든 방이 없는 층은 4개다.)

```
Orc1F  (GameObject)                       ← 이 단위로 켜고 끈다
 ├─ [C] Transform
 ├─ [C] AreaAnchor                        (이미 있음 · 칸만 채운다)
 │        Definition        → Area_Orc_1F              ★
 │        Floor             → 1Floor 의 Tilemap        ★ 직접 연결할 것
 │        Walkable          → 비움 (같은 오브젝트에서 자동)
 │        Fallback Arrival  → from_entrance            ★
 │
 ├─ [C] WalkableArea                      (이미 있음)
 │        Floor        → 1Floor 의 Tilemap             (연결됨)
 │        Guide        → 1FGuide 의 Tilemap            (연결됨)
 │        Breakable    → 1FHiddenRewards 의 Tilemap    ★ 지금 비어 있다
 │        Max Attempts → 24
 │
 ├─ [C] FloorProps                        ← 추가
 │        Anchor            → 비움 (같은 오브젝트에서 자동)
 │        Rebuild On Enable → 끔   (켜면 층에 들어올 때마다 다시 뽑혀 지형이 바뀐다)
 │        Log Build         → 켬
 │
 ├─ 1Floor  (GameObject)                  (기존)
 │    ├─ [C] Transform
 │    ├─ [C] Tilemap
 │    └─ [C] TilemapRenderer
 │
 ├─ 1FGuide  (GameObject)                 (기존) 부술 수 없는 벽
 │    ├─ [C] Transform
 │    ├─ [C] Tilemap
 │    ├─ [C] TilemapRenderer
 │    ├─ [C] TilemapCollider2D            Used By Composite 켬
 │    ├─ [C] CompositeCollider2D
 │    └─ [C] Rigidbody2D                  Body Type: Static
 │
 ├─ 1FHiddenRewards  (GameObject)         (기존 — 타일만 있고 콜라이더가 없다)
 │    ├─ [C] Transform
 │    ├─ [C] Tilemap
 │    ├─ [C] TilemapRenderer
 │    ├─ [C] TilemapCollider2D            ← 추가 · Used By Composite 켬
 │    ├─ [C] CompositeCollider2D          ← 추가
 │    ├─ [C] Rigidbody2D                  ← 추가 · Body Type: Static
 │    └─ [C] DestructibleTilemap          ← 추가
 │             Hp Per Cell   → 60
 │             Defense       → 0
 │             Damaged Tint  → 기본값
 │
 └─ Arrivals  (GameObject)                ← 추가. 묶음용이라 [C] Transform 하나뿐
      └─ from_entrance  (GameObject)      ← 추가
           ├─ [C] Transform               위치는 던전 입구에서 내려설 자리
           └─ [C] ArrivalPoint
                    Arrival Id → from_entrance
                    Facing     → (0, -1)
```

### 층마다 다른 것은 도착 지점 이름뿐이다

**`arrivalId` 는 "어디서 왔는가" 다.** "여기가 어디인가" 로 지으면 층마다 이름이 같아진다.

| 층 | 만들 `Arrivals` 자식 | `Arrival Id` |
|---|---|---|
| 1F | 1개 | `from_entrance` — 던전 입구에서 왔다 |
| 2F | 1개 | `from_1f` |
| 3F | 1개 | `from_2f` |
| 보상방 | 1개 | `from_boss` |

`Fallback Arrival` 에는 그 층의 유일한 도착 지점을 넣는다.
비워도 첫 번째 지점을 쓰지만, 나중에 지점이 늘면 어느 것이 잡힐지 알 수 없어진다.

### 히든 방 벽이 없는 층

`Orc3F` `Vampire3F` 에는 `HiddenRewards` 타일맵이 없다. 그 층은
`WalkableArea` 의 `Breakable` 을 비워 두고 `DestructibleTilemap` 도 만들지 않는다.

> ⚠ **Breakable 을 그린 칸은 `Guide` 에서 비워야 한다.** 겹쳐 있으면 벽을 부숴
> 그림이 사라져도 아래 `Guide` 콜라이더가 남아 지나갈 수 없다.
> 겉보기엔 뚫렸는데 못 가므로 원인을 찾기 어렵다.

---

## 3단계 · 보상방 (Orc · Vampire)

`Goblin/Rewards` 가 이미 되어 있으므로 그것을 보고 맞추면 된다.
**보상방에는 `FloorProps` 를 붙이지 않는다** — 아이템은 손으로 놓는다.

루트 아래 게임오브젝트는 **5개**다 (`Floor` `Guide` `Arrivals`+자식 `Portal`+자식).

```
Rewards  (GameObject)                     Map/Orc 아래
 ├─ [C] Transform
 ├─ [C] AreaAnchor                        (이미 있음)
 │        Definition       → Area_Orc_Reward       ★
 │        Floor            → Floor 의 Tilemap      ★
 │        Walkable         → 비움
 │        Fallback Arrival → from_boss             ★
 │
 ├─ [C] WalkableArea                      (이미 있으나 ★ 둘 다 비어 있다)
 │        Floor     → Floor 의 Tilemap             ★
 │        Guide     → Guide 의 Tilemap             ★
 │        Breakable → 비움
 │
 ├─ Floor  (GameObject)   [C] Transform · Tilemap · TilemapRenderer
 ├─ Guide  (GameObject)   [C] Transform · Tilemap · TilemapRenderer
 │                        ※ 콜라이더가 없다. 벽으로 막아야 하면 1FGuide 처럼 셋을 더한다
 │
 ├─ Arrivals  (GameObject)                ← 추가
 │    └─ from_boss  (GameObject)          ← 추가
 │         ├─ [C] Transform
 │         └─ [C] ArrivalPoint   Arrival Id → from_boss · Facing → (0, -1)
 │
 └─ Portal  (GameObject)                  ← 추가. 묶음용
      └─ Blue_Portal  (프리팹 인스턴스)    ← Assets/Prefabs/Portals/Blue_Portal.prefab
           ├─ [C] Transform               ★ 위치는 루트에서 옮긴다
           ├─ [C] BoxCollider2D           Is Trigger 켬 · Size (2, 2)   (프리팹 기본값)
           ├─ [C] Portal
           │        Prompt Label            → 비움 (목적지 이름이 자동)
           │        Interactable            → 켬
           │        Destination             → Area_Dungeon_Entrance   ★
           │        Destination Arrival Id  → from_reward             ★
           └─ portal_blue_entrance_animation_8x1_0  (GameObject)
                ├─ [C] Transform           ★ 건드리지 않는다 (0, 0, 0)
                ├─ [C] SpriteRenderer
                └─ [C] Animator
```

> ⚠ **프리팹은 루트를 옮긴다.** 자식(스프라이트)만 움직이면 `BoxCollider2D` 가
> 루트에 남아 그림과 판정이 따로 논다 — "포탈 위에 섰는데 버튼이 안 뜬다" 가 된다.

---

## 4단계 · 던전 입구 — 순환을 닫는다

`Map/Dungeon` 은 `AreaAnchor`(#3) 와 `Arrivals/from_reward` 까지 되어 있다.
**여기에 두 가지를 더한다.**

```
Dungeon  (GameObject)
 ├─ [C] Transform
 ├─ [C] AreaAnchor        Definition → Area_Dungeon_Entrance   (연결됨)
 │                        Floor      → BasicFloor 의 Tilemap    (연결됨)
 │                        Fallback Arrival → from_reward        (연결됨)
 ├─ [C] WalkableArea      Floor → BasicFloor · Guide → Guide     (연결됨)
 │
 ├─ BasicFloor  (GameObject)   [C] Transform · Tilemap · TilemapRenderer
 ├─ Guide       (GameObject)   [C] Transform · Tilemap · TilemapRenderer
 │
 ├─ Arrivals  (GameObject)                묶음용
 │    ├─ from_reward  (GameObject)        (있음) 보상방에서 돌아온 자리
 │    │    └─ [C] ArrivalPoint   Arrival Id → from_reward
 │    └─ from_escape  (GameObject)        ← 추가. 탈출 스킬로 나온 자리
 │         ├─ [C] Transform
 │         └─ [C] ArrivalPoint   Arrival Id → from_escape · Facing → (0, -1)
 │
 └─ Portals  (GameObject)                 ← 추가. 묶음용. 자식 3개
      ├─ Portal_to_Goblin   (Blue_Portal 프리팹 인스턴스)
      │      [C] Portal   Destination → Area_Goblin_1F   · Arrival Id → from_entrance
      ├─ Portal_to_Orc      (Blue_Portal 프리팹 인스턴스)
      │      [C] Portal   Destination → Area_Orc_1F      · Arrival Id → from_entrance
      └─ Portal_to_Vampire  (Blue_Portal 프리팹 인스턴스)
             [C] Portal   Destination → Area_Vampire_1F  · Arrival Id → from_entrance
```

**셋 다 Blue 를 쓴다.** 색은 난이도가 아니라 **구간**을 뜻한다 (결정 006 §3-1) —
1F 로 들어가는 문이므로 1F 로 가는 포탈과 같은 색이다.

`from_escape` 를 따로 두는 이유는 이름이 거짓말을 하지 않게 하기 위해서다.
탈출로 나온 사람은 보상방에서 온 것이 아니다.
`AreaDefinition.Escape Arrival Id` 에 도구가 이미 `from_escape` 를 넣어 두었다.

---

## 5단계 · 씬을 저장하기 전에

**켜 둘 층은 `Goblin1F` 하나다.** 나머지 12개는 전부 꺼서 저장한다.

둘 이상 켜져 있으면 `WalkableArea` 가 `OnEnable` 에서 자기를 등록하므로
**마지막 하나만 살아남아** 스폰·도착 보정이 엉뚱한 층 기준으로 돈다.
등록된 구역이면 `AreaRegistry` 가 경고와 함께 꺼주지만, 정의가 빈 구역은 끄지도 못한다.

---

## 6단계 · 점검 — 재생하기 전에 전부 잡는다

씬을 저장한 뒤 순서대로 실행한다. **셋 다 씬을 바꾸지 않는다.**

```
Pretty Knights > Areas > 2. 구역 정의 점검 (변경 없음)
Pretty Knights > Areas > 0. 포탈 링크 점검 (변경 없음)
Pretty Knights > Props > 0. 배치 개수만 계산 (변경 없음)
```

| 도구 | 잡아주는 것 |
|---|---|
| `Areas > 2` | 에셋 누락 · 씬 앵커와 짝이 안 맞음 · **Definition 이 빈 층 목록** · 바닥을 못 잼 |
| `Areas > 0` | areaId 중복 · 도착 지점 누락/중복/공백 · 포탈 목적지가 씬에 없음 · `Is Trigger` 꺼짐 |
| `Props > 0` | 층마다 몇 개가 놓이는지 · **요청한 개수만큼 자리를 못 찾은 층** |

`Props > 0` 에서 요청보다 훨씬 적게 놓이면 그 층은 좁거나 벽이 많은 것이다.
`Orc 1F` 는 벽:바닥이 `1 : 2.5` 라 여기서 가장 먼저 드러난다.

눈으로 확인하려면

```
Pretty Knights > Props > 1. 미리보기 만들기
Pretty Knights > Props > 3. 연결성 검사        길이 막혔는지
Pretty Knights > Props > 4. 막는 것 치우기     막은 자동 배치분만 치운다
Pretty Knights > Props > 2. 미리보기 지우기    ★ 재생 전에 반드시 지운다
```

> 미리보기는 **손으로 놓은 것을 건드리지 않는다.** `PropPreview` 컨테이너에만 만든다.
> 지우지 않고 재생하면 런타임 배치와 겹쳐 두 벌이 보인다.

---

## 7단계 · 한 바퀴 확인

Boot 씬에서 재생한다.

1. 콘솔에 `[AreaRegistry] AreaDefinition 이 비어…` **경고가 없어야 한다**
2. 각 층 진입 시 `[FloorProps] … 오브젝트 N개 생성 (시드 …)`
3. **메인 토템을 부순다** → 그 자리에 포탈이 켜진다 → 다음 층
4. 3F 는 토템이 없다. 지금은 보스 처치 판정이 없으므로
   `GameRoot` 의 `AreaTransition` 우클릭 → **디버그 이동**으로 보상방에 들어간다
   (보낼 곳은 `Debug Destination` 에 지정)
5. 보상방의 Blue 포탈 → 던전 입구
6. 던전 입구의 포탈 3개로 각 테마 1F 진입
7. 재생을 멈추고 다시 재생 → **마지막 구역에서 시작한다** (세이브에 `areaId` 가 들어갔다)

세이브를 되돌리려면 `GameRoot` 우클릭 → **세이브 삭제**.

### 테마를 넘어갈 때 무엇이 정산되는가

`AreaTransition.SettleDeparture` 가 처리한다. 확인해 볼 것.

| 상황 | 결과 |
|---|---|
| 보상방을 나간다 | **완전 클리어** — 파괴 기록을 전부 지우고 클리어 횟수 +1 → 다음 입장 때 배치가 새로 뽑힌다 |
| 다른 테마로 넘어간다 | 되살리되 **메인 토템은 부서진 채** — 포탈은 열려 있고 파밍만 재개된다 |
| 같은 테마 안 층 이동 | 정산하지 않는다 |

---

## 아직 안 한 것 (이 문서 범위 밖)

- **몬스터** — `MonsterSpawner` · `FloorPopulation` 이 씬에 하나도 안 붙었다.
  `MonsterDefinition` 10종은 있으나 **아트가 없어** 임시 프리팹(`Monster_Temp`)뿐이다
- **보스 처치 → Gold 포탈** — 3F 에서 보상방으로 갈 정상 경로가 아직 없다.
  `AreaDefinition #x03` 의 `Next Area` 가 이미 보상방을 가리키므로 배선은 준비되어 있다
- **탈출** — 부르는 쪽은 `EscapeButton` 이 되었다 ([`hud-layout.md`](hud-layout.md)).
  `Escape To` 와 `from_escape` 를 이번에 채우면 바로 동작한다
- **정렬(Sorting)** — 마지막에 한 번에 잡는다 ([`../TODO.md`](../TODO.md) "정렬 일괄 지정")

---

## 겪은 함정

같은 것을 또 밟지 않도록 [`../pitfalls.md`](../pitfalls.md) 에 모아 두었다. 이 작업과 직접 관련된 것

- `AreaDefinition` 이 비면 그 구역은 등록조차 되지 않는다
- 프리팹은 자식만 옮기면 콜라이더가 안 따라온다
- 도착 지점 이름은 "어디서 왔는가" 다
- Breakable 타일이 `Guide` 와 겹치면 부숴도 안 뚫린다
