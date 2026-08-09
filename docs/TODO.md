# 할 일

> 마지막 갱신 2026-08-09.
> **실행까지 가는 배치 순서는 [`docs/guides/run-setup.md`](guides/run-setup.md) 하나로 묶었다.**
> 현재 무엇이 동작하는지는 [`CLAUDE.md` §7](../CLAUDE.md) 을 본다.
> 왜 그렇게 정했는지는 `docs/decisions/` 를 본다.

---

## 바로 다음에 할 일

> **전 구역이 배선되고 순환이 닫혔다** (2026-08-09). 구역 13개가 모두 등록되고,
> 던전 입구에서 세 테마로 나가 보상방으로 돌아온다.
> **세로 모드도 코드는 다 나왔고 씬 조립이 남았다** (아래 ⓪) — 지금 작업 중이던 것.

### ⓪ 세로 모드 씬 조립 ← **작업 중이던 것**

`Ingame_Vertical` 은 `Global Light 2D` 하나뿐인 빈 씬이다.
결정 근거 [`decisions/009-vertical-mode.md`](decisions/009-vertical-mode.md),
조립 절차는 [`guides/vertical-stage-setup.md`](guides/vertical-stage-setup.md) 하나로 묶여 있다.

- [ ] **카메라 둘 · `StageViewport`** — 전투 화면을 상단 25% 띠(480px)로.
      화면을 지우기만 하는 배경 카메라가 한 대 더 필요하다 (뷰포트 밖은 안 지워진다)
- [ ] **사냥터 `Grid`** — Campsite 타일 20×20칸 + `Guide` 벽
- [ ] **`ObstacleField`** — `WalkableArea` 와 나란히. 몬스터가 아니라 **장애물**을 파밍한다
- [ ] **`Player_Vertical` 프리팹** — `Player.prefab` 복제 + `Visual` **Scale 1.5** + `AutoBattle` 부착.
      원본은 건드리지 않는다
- [ ] **`ShopView` 하단 배치** — 스킬 자리를 대신 채운다. 카드 6개(2열×3행), `CurrencyBar`
- [ ] **`ShopOffer` 에셋에 실제 값 채우기** (가격 · 효과량) — 사용자가 직접
- [ ] `PlayerHudView` 도 여기서 함께 배치하면 두 모드가 한 번에 끝난다 (아래 ② 참조)

### ① 가로 — 몬스터를 실제로 세운다

씬에 `FloorPopulation` 은 **9개 층 전부**에 붙었다. `MonsterSpawner` 는 **0개**다.

- [ ] 보스 자리처럼 **지점을 지정해야 하는 스폰**에 `MonsterSpawner` 배치 (3F 3개 층)
- [ ] `Monster_Temp` 프리팹에 **`MonsterHealthBar` 부착** (프리팹·씬 통틀어 0개).
      아트(PPU 192)는 이미 준비됐다 — 자식 넷을 손으로 붙이는 것만 남았다
      ([`guides/monster-prefab-setup.md`](guides/monster-prefab-setup.md) 1-1절)

절차는 [`guides/monster-spawn-setup.md`](guides/monster-spawn-setup.md),
검증은 [`guides/verify-spawn-drop.md`](guides/verify-spawn-drop.md).

> **프리팹의 `MonsterController.definition` 은 신경 쓰지 않아도 된다.**
> 스포너가 `Spawn(definition, point)` 로 덮어쓴다. 프리팹은 하나가 맞다.

### ② UI 마무리

- [ ] **`PotionSettingsView` 배치** (씬에 0개) — 포션 임계값 조절 UI.
      슬라이더 핸들이 타원으로 늘어나던 것은 `Handle` 앵커를 `(0, 0.5)` 로 묶고
      **36×36 고정 + `Preserve Aspect`** 로 해결한다
- [x] **플레이어 HP HUD** — `PlayerHudView` 작성 완료 (체력 · 레벨 · ATK/DEF/AGI).
      아트가 **구멍 뚫린 액자**라 체력 홈(401 × 39)이 `player_health_fill` 과 크기가 같다
- [ ] **`PlayerHudView` 를 Boot 씬에 배치** — [`guides/hud-layout.md`](guides/hud-layout.md) 2-1절.
      **세로 전용 패널에 넣지 않는다** — 두 모드에 다 뜬다
- [ ] 보스 HP 바 — `boss_health_*` 3장 미연결. 전용 뷰가 아직 없다
- [ ] 눌림 상태 아트 미연결 — `attack_button_pressed` · `skill_slot_pressed` · `start_button_*`
- [x] `InteractButton` 의 `Icon` 에 자식 Image 연결 — 포탈/줍기가 그림으로 갈린다 (글자는 없앰)
- [x] `ItemDefinition` 4종 아이콘 연결
- [x] `ISkillBar` 구현체 — `PlayerSkillBar` 작성 완료. **`Player.prefab` 배치는 남았다**
- [ ] **`PlayerSkillBar` 를 `Player.prefab` 에 부착** — 붙어야 스킬 버튼 4개의 잠김이 풀린다.
      슬롯에 넣을 `PlayerSkillDefinition` 에셋(전방 베기 등)이 아직 하나도 없다

### ③ 검격 아트

`PlayerAttack.Attack Effect` 가 비어 있어 임시로 판정 범위가 그려지고 있다 (그 부채꼴이다).
PixelLab 명령서대로 뽑으면 끝난다 — [`guides/skill-effect-art.md`](guides/skill-effect-art.md).
아트가 들어오면 `Show Range When No Art` 를 끈다.

### ④ 보스 처치 → Gold 포탈 · 데미지 숫자

보스 처치 판정이 없어 **3F 에서 보상방으로 갈 정상 경로가 없다.**
지금은 `AreaTransition` 우클릭 → 디버그 이동으로만 들어간다.
`SpawnTotem` 이 메인 토템에서 하는 일과 같은 흐름이라 구조는 그대로 쓴다
(유일한 차이: 포탈을 미리 두지 않고 **시체 자리에 생성**한다).

데미지 숫자에는 부술 수 있는 벽의 **발견성이 걸려 있다** — 때렸을 때 숫자가 뜨는 것이
"이 벽은 부술 수 있다" 는 유일한 신호다. VFX 3요소의 "반응" 이기도 하다.

### 끝난 것 — 전 구역 배선

절차서는 [`guides/all-maps-setup.md`](guides/all-maps-setup.md) 에 남겨 둔다.

- [x] 생성 도구 — 구역 13개 + 배치 프로필 9개
- [x] 도구 실행 → `Assets/Data/Areas/` 13개 · `Assets/Data/Scatter/` 9개 · `Drops/` 6개
- [x] 씬의 `AreaAnchor` 13개 전부 `definition` 연결 (빈 것 0)
- [x] 9개 층에 `FloorProps` 부착 · `ArrivalPoint` 14개
- [x] 히든 방 벽 — `DestructibleTilemap` 6개 · `NoSpawnZone` 6개
- [x] **던전 입구에 포탈 3개** → #101 / #201 / #301
- [x] 던전 입구 도착 지점 (`AreaDefinition.EscapeTo` 는 도구가 #3 으로 채운다)

> **층을 잇는 포탈은 씬에 두지 않는다.** 메인 토템이 뽑힌 자리에 `FloorProps` 가
> 꺼진 포탈을 함께 만들고 토템을 부수면 켜진다. 손으로 두는 포탈은
> **던전 입구 3개 + 보상방 3개, 총 6개뿐이다.**

---

## 아직 정하지 않은 것

- **데미지 공식** — `CombatSettings` 에 세 안(감산 / 비대칭 배율 / 감쇠율)이 들어 있고
  재생 중에 바꿔가며 비교할 수 있다. **실제로 때려보고 고른다.**
  고른 것이 미결 #3(스탯 공식)의 답이 된다 — 배선은 [`guides/player-attack-setup.md`](guides/player-attack-setup.md)

  시트에 들어온 `DAMAGE = ATK − DEF×1.5` 를 그대로 쓰면 양쪽이 무너진다는 것까지는 확인했다.

  | 방향 | 결과 |
  |---|---|
  | 플레이어 → 몬스터 | DEF 14 이상이면 **데미지 0**. 10종 중 5종(2F 정예 2 · 3F 보스 3)이 기본공격 무적 |
  | 몬스터 → 플레이어 | 감산량이 60 인데 최대 ATK 가 50 이라 **10종 전부 데미지 0** |

- **서브 토템 층당 개수와 기본/추가 점유량** — 지금 프로필의 값은 임시다
- **층당 오브젝트 밀도** — 바닥 100~150칸당 1개로 시작했다
- **예고 시간** — 지금 값(Normal 0.30 / Elite 0.45 / Boss 0.70)은 **짧아서 못 피한다**는
  판단이 나왔다. 레벨 디자인에서 조정 예정.
  고칠 때는 인스펙터가 아니라 `MonsterDefinitionBuilder` 의 표를 고친다
- **정렬** — 마지막에 한 번에 잡기로 했다 (아래 "정렬 일괄 지정")

## 남아 있는 제약

**몬스터 아트가 없다.** `Maps/` 의 Goblin·Orc·Vampire 는 **맵 테마**이지 몬스터가 아니다.
지금은 Knights 스프라이트를 색만 바꿔 쓰는 임시 프리팹(`Monster_Temp`)뿐이다.

- [x] `Assets/Data/Monsters/` 에 `.asset` 10종 생성 (Goblin 4 · Orc 3 · Vampire 3)
- [ ] 공식 확정 — 감산 / 비대칭 배율 / 감쇠율 중 하나
- [ ] 시트 갱신 (플레이어 DEF 40 반영 + 검증 열 수식 · 낡은 문구 정리)
- [ ] 임시 몬스터 프리팹에 연결해 배회·추격·공격 검증

---

## 다음 작업 (순서대로)

### 1. 스폰을 씬에 붙인다

층 오브젝트마다 붙일 것이 다르다.

| 층 | 붙일 것 |
|---|---|
| 1F | `WalkableArea` + `MonsterSpawner`(지점마다) |
| 2F | `WalkableArea` + `FloorPopulation`(층에 하나) |
| 3F | `WalkableArea` + `MonsterSpawner`(보스 자리) |

`WalkableArea` 부착은 아래 3번(포탈 배치)과 같은 작업이다.
층 루트에 `AreaAnchor` 와 나란히 붙이므로 한 번에 처리한다.

**절차는 [`guides/monster-spawn-setup.md`](guides/monster-spawn-setup.md) 에 있다.**

> **표와 달리 9개 층 전부를 `FloorPopulation` 으로 갔다** (2026-08-09).
> 층 단위 인구 관리가 토템 지분 모델과 그대로 맞물리기 때문이다.
> `MonsterSpawner` 는 **지점을 지정해야 하는 스폰**(보스 자리)에만 남는다.

- [x] 13개 구역에 `WalkableArea` 부착 (Floor / Guide 타일맵 연결)
- [x] 가이드 작성
- [x] 9개 층에 `FloorPopulation` 부착
- [ ] 3F 보스 자리에 `MonsterSpawner` 배치
- [x] `NoSpawnZone` 에 **봉인 해제** — 들어가면 몬스터 차단만 풀리고 세이브에 남는다.
      직사각형이 아닌 방은 **타일맵 마스크** 또는 **같은 `Room Id` 의 사각형 여러 개**
- [x] 히든 방마다 `NoSpawnZone` 배치 (6개)

### 2. 카메라 경계를 층마다 바꾼다 — 코드 완료

`AreaRegistry.Activate()` 가 구역을 켤 때 `CameraFollow.SetBoundsSource()` 를 함께 부른다.
`CameraFollow` 도 `ServiceRegistry` 에 등록된다. **씬 배치만 남았다** (아래 3번과 같은 작업).

### 3. 구역 전환 (포탈) — 코드 완료, 씬 배치 남음

흐름: **시작 신전 → 숲(슬라임 튜토리얼) → 던전 입구(테마 선택) → 던전 층 이동**

설계 근거는 [`docs/decisions/006-area-transition.md`](decisions/006-area-transition.md),
배치 절차는 [`docs/guides/portal-area-setup.md`](guides/portal-area-setup.md).

- [x] AreaID 체계 — `테마번호 × 100 + 층` (Goblin 101~103 · Orc 201~203 · Vampire 301~303)
- [x] `AreaDefinition`(SO) · `AreaAnchor` · `ArrivalPoint` · `AreaRegistry` · `AreaTransition` · `Portal`
- [x] 상호작용 계층 — `IInteractable` · `InteractableBehaviour` · `InteractionHub`
      (히든 상자·아이템이 같은 흐름을 쓴다)
- [x] `WorldLocation` 에 `areaId` 추가 + `GameRoot` 복원 시 구역 먼저 켜기
- [x] `ScreenFader` · `InteractButton`
- [x] 에디터 점검 — `Pretty Knights > Areas > 0. 포탈 링크 점검 (변경 없음)`
- [x] **`Assets/Data/Areas/` 에 `AreaDefinition` 13개 생성**
- [x] **Boot 씬** — `AreaTransition` · `InteractionHub` · `InteractButton` · `FadeOverlay` 배치
- [x] **Ingame_Horizontal** — `Map` 에 `AreaRegistry`, 13개 구역에 `AreaAnchor` + `WalkableArea`,
      `ArrivalPoint` · `Portal` 배치
- [x] Orc · Vampire 로 복제 — **순환이 닫혔다**

> 포탈은 **단방향**이다. 되돌아오는 길은 탈출뿐이며
> `AreaTransition.RequestEscape()` 를 `EscapeButton` 이 부른다 (2026-08-09).
> `AreaDefinition.EscapeTo` 는 `Areas > 3` 도구가 #3 으로 채운다.

### 3-1. 맵 오브젝트와 토템

실측·역할·구조는 [`docs/design/map-objects.md`](design/map-objects.md),
**에셋·씬 세팅 절차는 [`docs/guides/prop-scatter-setup.md`](guides/prop-scatter-setup.md)** 에 있다.

**토템 파괴가 층의 진행을 만든다.** 메인/서브는 서로 다른 자산을 쓴다.

| 테마 | 메인 토템 (파괴 → 포탈) | 서브 토템 (파괴 → 몬스터 총량 감소) |
|---|---|---|
| Goblin | `06_scrap_totem` | `01_twisted_stump` |
| Orc | `05_war_totem` | `01_scorched_stump` |
| Vampire | `06_bloodstone_altar` | `03_sarcophagus` |

메인 토템은 층당 1개, 그 **자리가 곧 포탈이 생길 자리**다.
포탈은 런타임 생성이 아니라 같은 자리에 미리 두고 꺼 놓는다.

> **배치는 다시 와도 그대로여야 한다.** 탈출 스킬로 나갔다 돌아왔을 때
> 오브젝트가 사라지거나 재배치되면 길을 처음부터 다시 찾아야 한다.
> 오브젝트가 통행 경로를 만드는 이상 **배치는 그 층의 지형 그 자체**다.
> 이 요구 때문에 "진입 때마다 재배치" 안은 탈락했다.

- [x] 18종 실측 (알파 경계 · 접지폭 · Visual 오프셋) — 전량 PPU 64 / Pivot Center 통일
- [x] 메인/서브 토템 지정
- [x] 인구 모델 — **메인 토템이 기본 점유, 서브 토템이 추가 점유** (메모리 할당 모델).
      메인을 부수면 기본 점유가 풀려 층 인구가 0 이 되고 리스폰이 멈춘다
- [x] 3F 는 토템 없음 — 보스 처치 → 이펙트 → 그 자리에 금색 보상 포탈 (보상방 areaId 190/290/390)
- [x] 18종 전부 파괴 가능. 일부는 손으로 배치해 **히든 방** 입구를 막는다
- [x] `NoSpawnZone` — 히든 방 내부에 몬스터·오브젝트를 안 뿌린다.
      **`WalkableArea` 는 건드리지 않는다** (거기서 빼면 세이브 복원·도착 보정·A\* 가 깨진다).
      `FloorPopulation` 연결 완료, 배치 도구 쪽은 도구를 만들 때
- [x] SO 구조 확정 — `AreaDefinition` 에 `nextArea` · `nextArrivalId` 추가.
      메인 토템이 어디에 배치되든 포탈이 올바른 곳으로 이어진다
- [x] 파괴는 `Destroy` 가 아니라 **콜라이더 끄기 + 스프라이트 교체**.
      지우면 세이브의 파괴 목록을 복원할 대상이 사라진다
- [x] `PropDefinition`(SO) · `DropTable`(SO) · `FloorScatterProfile`(SO)
- [x] `AreaDefinition` 에 `nextArea` · `nextArrivalId` · `scatterProfile`
- [x] `Destructible` — `IDamageable` 구현. **파괴 시 콜라이더 끄고 스프라이트 교체**
- [x] `SpawnTotem` — 메인은 포탈 켜기, 서브는 지분 빼기
- [x] `FloorPopulation.AddShare` / `ClearTarget` — 목표만 줄고 살아 있는 개체는 그대로
- [x] `WalkableArea.IsAreaWalkable(center, size)` — 2×2칸 판정
- [x] **배치는 런타임 생성** (2026-08-08 전환) — `PropScatterer`(계산) + `FloorProps`(생성).
      씬에 굽지 않는 이유는 **재배치** 때문이다. 완전 클리어 시 배치가 새로 뽑혀야 한다
- [x] 에디터는 미리보기만 — `0. 개수 계산 / 1. 미리보기 만들기 / 2. 미리보기 지우기`.
      계산이 `PropScatterer` 한 곳이라 미리보기와 실제가 같다
- [x] 메인 토템 자리에 **꺼진 포탈**을 함께 만든다. 목적지는 `AreaDefinition.NextArea`
- [x] 층마다 `FloorProps` 부착 (`AreaAnchor` 와 나란히) — 9 / 9
- [x] **연결성 검사** `Pretty Knights > Props > 3. 검사 / 4. 막는 것 치우기`
      가중 탐색으로 최소 비용 경로가 지나는 자동 배치분이 곧 치울 목록이 된다.
      손으로 놓은 것은 통행 불가로 두어 자동으로 치우지 않는다
- [x] **`PropDefinition` 생성 도구** `Pretty Knights > Props > 5. 점검 / 6. 생성`
      18종의 역할·HP·경험치·지분. 접지폭과 `Visual` Y 는 프리팹에 넣는 값이라
      에셋이 아니라 로그에만 찍는다
- [x] 도구 실행해 `Assets/Data/Props/` 에 18종 생성
- [x] **프리팹은 하나** — 배리언트 18개를 만들지 않는다.
      스프라이트·콜라이더 크기·`Visual` Y 는 `Destructible.Bind` 가 정의에서 읽는다
- [x] `Prop.prefab` 하나 만들기
      (루트: `CapsuleCollider2D` · `Destructible` / 자식 `Visual`: `SpriteRenderer`)
      → 층별 `FloorScatterProfile` 의 `Prop Prefab` 에 연결
- [x] 층별 `FloorScatterProfile` 작성 (9개 층) → `AreaDefinition` 에 연결
- [x] `AreaDefinition.nextArea` 연결
- [x] **파괴 상태 세이브** — `WorldProgress`
      - 오브젝트: `{areaId, index, mainTotem}` — 시드가 같으면 생성 순서도 같다
      - 부술 수 있는 벽: `{areaId, cellX, cellY}` — 손으로 그린 것이라 순서가 없다
      - `clearCount` — 완전 클리어 횟수가 재배치 시드에 들어간다
- [x] 테마 경계 정산 — `AreaTransition.SettleDeparture`
      - 보상방을 나가면 **완전 클리어**: 전부 지우고 클리어 횟수 +1 → 다음 입장 시 재배치
      - 다른 테마로 넘어가면 **되살리되 메인 토템은 부서진 채**: 포탈은 열려 있고 파밍만 재개
      - 같은 테마 안 층 이동은 정산하지 않는다
- [x] 드랍 — 1단계는 경험치만. **몬스터에 `dropTable` 이 아예 없었다** (2026-08-09 추가).
      `Pretty Knights > Data > 3. 드랍 표 생성/연결` 이 표 6종을 만들고
      등급·역할로 28곳에 연결한다. 아이템은 시스템 생긴 뒤

**미결** — 서브 토템 층당 개수와 기본/추가 점유량 · `weapon_rack` 용도 · 층당 밀도
· 히든 방 입구 규격

### 3-2. 히든 방 · 부술 수 있는 벽 · 보상방

**히든 방은 부술 수 있는 벽으로 막는다.** 겉보기는 일반 벽과 같고,
때려보면 데미지가 들어가는 것으로 구분된다.

`Guide` 와 같은 타일을 쓰되 **레이어를 가른다.**

```
Goblin1F
 ├─ 1Floor       (기존)
 ├─ 1FGuide      (기존)      부술 수 없는 벽
 └─ 1FBreakable  ← 추가      Guide 와 같은 타일 · 같은 Sorting Order
      [C] Tilemap · TilemapRenderer · TilemapCollider2D
      [C] CompositeCollider2D · Rigidbody2D(Static)
      [C] DestructibleTilemap
```

레이어를 가르는 이유는 셋이다. 겉보기가 같아야 하니 **같은 타일 에셋**을 써야 하고,
한 타일맵에 섞으면 "어느 칸이 부술 수 있나" 를 별도 자료구조로 들고 있어야 하는데
맵을 다시 그리면 그게 조용히 어긋난다. 그리고 부서질 때 `SetTile(cell, null)` 하면
**콜라이더가 알아서 갱신**되어 통행이 열린다.

**층마다 타일맵 하나에 그 층의 모든 히든 방 벽을 담는다** (2026-08-08 확정).
방마다 나누지 않는 이유는 속성을 한 번에 채울 수 있고, 칸별 HP 를
`Dictionary<Vector3Int, float>` 로 들면 방이 몇 개든 각 칸이 독립적으로 부서지기 때문이다.
`DestructibleTilemap` 도 층당 하나면 된다.

> ⚠ **`Guide` 와 같은 칸에 겹치면 안 된다.** Breakable 을 부숴도 아래 `Guide` 타일이 남아 있으면
> 콜라이더가 그대로라 못 지나간다. 겉보기엔 벽이 사라졌으므로
> **"부쉈는데 왜 안 가지지"** 가 된다. Breakable 이 있는 칸은 `Guide` 에서 비워야 한다.

> Sorting Order 는 `Guide` 보다 **1 높게**. 같은 값이면 그리기 순서가 프레임마다 뒤집혀 깜빡인다.

- [x] 층마다 Breakable 타일맵 하나 (배치 완료)
- [ ] `Guide` 와 겹친 칸 점검 도구 — 부숴도 안 뚫리는 자리를 미리 잡는다
- [ ] Sorting Order 를 `Guide` + 1 로
- [x] `IAreaDamageable` — 범위를 통째로 받는 인터페이스.
      타일맵은 GameObject 하나라 `IDamageable` 로는 **어느 칸을 맞았는지 알 수 없다**
- [x] `DestructibleTilemap` — 칸별 HP · 범위 ∩ cellBounds 를 `SkillShape.Contains` 로 거른다
- [x] `PlayerAttack` 이 `IAreaDamageable` 도 찾는다 — 한 번의 휘두름이
      몬스터·오브젝트·벽을 전부 처리한다
- [x] **`WalkableArea` 에 breakable 레이어 추가** — 부수기 전까지는 벽이다
- [x] 손상 표시 — `SetTileFlags(TileFlags.None)` 후 `SetColor`
- [x] 층마다 Breakable 타일맵에 `DestructibleTilemap` 부착 + `WalkableArea` 의 `Breakable` 연결 (6개)
- [x] **파괴 칸 세이브** — `WorldProgress` 에 `{areaId, cellX, cellY}`
- [ ] 벽 파괴 애니메이션 (추후)

> **벽 강도를 방마다 다르게 하고 싶어지면** 타일 에셋 종류로 가르면 된다.
> `Dictionary<TileBase, float>` 로 타일별 HP 를 두면 한 타일맵 안에서
> 단단한 벽과 약한 벽을 섞을 수 있다. 지금 구조를 바꾸지 않아도 되므로 그때 얹는다.

> **인디케이터에는 부적합했던 `SetColor` 가 여기서는 맞다.** 조건이 반대다 —
> 판정 단위가 칸이고, 때릴 때만 갱신되고, 부서지면 끝이라 원복이 필요 없고,
> 게임플레이 타일맵이 아니라 전용 레이어다.

> **발견성은 데미지 숫자가 해결한다.** 벽을 때렸을 때 숫자가 뜨면 부술 수 있다는 뜻이므로
> 별도 단서를 심을 필요가 없다. 아래 5절의 데미지 숫자가 선행 조건이다.

**보상방**은 보스 처치 → 시체 자리에 `Gold_Portal` 생성 → #190.
`SpawnTotem` 이 메인 토템에서 하는 일과 **완전히 같은 흐름**이라 별도 구조가 필요 없다.

- [ ] 보스 처치 판정 → 시체 자리에 **Gold** 포탈 (유일한 런타임 생성 포탈)
- [x] `AreaDefinition` #190/#290/#390 (보상방) + `isRewardRoom` 플래그
- [x] 보상방에서 던전 입구(#3)로 가는 **Blue** 포탈 배치
- [ ] 보상방에 아이템 배치

**포탈 3종의 용도** (결정 006 §3-1)

| 구간 | 포탈 | 언제 |
|---|---|---|
| 1F → 2F | Blue | 1F 메인 토템 파괴 |
| 2F → 3F | Red | 2F 메인 토템 파괴 |
| 3F → 보상방 | Gold | 보스 처치. 시체 자리 |
| 보상방 → 입구 | Blue | 항상 |

층별 포탈은 `FloorScatterProfile.portalPrefab` 이 이미 층마다 다르므로 그대로 된다.

### 4. 경로 탐색 (그리드 A*)

현재 `MonsterController` 는 대상 방향으로 곧장 향하는 단순 조향이다.

**Vampire 3F 에서 반드시 터진다.** 벽:바닥이 `1 : 1.4` 라 몬스터가 벽에 붙어 진동한다.
Goblin·Orc 의 트인 보스방(`1 : 18`)에서는 드러나지 않다가 거기서만 망가진다.

- [ ] 방/구역 단위 그리드 A\*. `WalkableArea` 가 이미 통행 판정을 들고 있다
- [ ] Vampire 보스는 순간이동이라 예외 (경로 탐색 불필요)

### 4-1. 공격 예고 인디케이터 ← **다음 작업** (2026-08-08)

방식은 [`docs/decisions/007-skill-indicator.md`](decisions/007-skill-indicator.md).
**메시가 아니라 런타임 래스터화**다 — 폴리곤의 매끈한 가장자리가 64px 도트 위에서 튄다.

- [x] `SkillShape.Contains` · `SkillShape.LocalBounds` (판정과 같은 파일·같은 수학)
- [x] `SkillIndicatorRasterizer` — 64 px/unit · Point 필터 · 1px 테두리 · **8방향 캐시**
- [x] `SkillIndicator` · `SkillIndicatorPool` (Boot 상주, 풀링)
- [x] `MonsterDefinition.telegraphDuration` · `attackShape` · `attackShapeParams`.
      생성 도구가 등급별 기본값을 넣는다 (Normal 0.30 / Elite 0.45 / Boss 0.70)
- [x] `MonsterController` 를 **예고 → 판정 → 경직** 3단계로.
      원점·방향을 예고 시작 시점에 얼린다 — 범위가 따라오면 피할 방법이 없다
- [x] **Boot 씬에 `SkillIndicatorPool` 배치**
      ([`run-setup.md`](guides/run-setup.md) 2절)
- [x] `Pretty Knights > Data > 1. MonsterDefinition 생성/갱신` 실행 — 예고 시간이 들어갔다.
      **다시 돌리면 손으로 맞춘 값이 등급 기본값으로 덮인다** ([`pitfalls.md`](pitfalls.md))
- [ ] 플레이어 공격에도 인디케이터를 붙일지 (애니메이션으로 갈 예정이라 보류)

### 5. 스킬 판정 시스템 (2026-08-05 순서 조정)

`CLAUDE.md` §5 의 원칙을 따른다. **판정과 VFX 를 분리한다.**

**판정 코어는 하나, 스킬 목록은 플레이어와 몬스터로 나눈다** (2026-08-05 확정).
범위 계산이 두 벌이 되면 프리뷰 인디케이터·실제 데미지·AI 회피·스킬 설명 UI 가
서로 다른 답을 낼 수 있고, "몬스터 공격 범위 표시" 를 또 만들어야 한다.
갈라야 하는 것은 **누가 무엇을 들고 있는가**이지 범위를 어떻게 재는가가 아니다.

```
SkillShape (static)          범위 계산 한 벌 — Forward / Line / Cross / Area / Dash
    Evaluate(origin, facing, param, List<Collider2D> results)

SkillDefinition (SO)         목록만 분리
   ├ PlayerSkillDefinition   플레이어가 배우는 것. 스킬로만 ATK·DEF 가 오른다
   └ MonsterSkillDefinition  등급별 · 보스 패턴

SkillInstance                시전마다 하나. 여기만 상태를 갖는다
    경과 시간 · 이미 맞힌 대상 · 다단히트 타이머
```

### 코어를 공유해도 동시 판정이 깨지지 않게 하는 조건

보스 1 + 잡몹 12 + 플레이어가 같은 프레임에 판정해도 간섭이 없어야 한다.
**깨지는 원인은 코어를 공유해서가 아니라 코어가 상태를 들고 있을 때**다.

- **코어에 `currentCaster` 같은 필드를 두지 않는다** — A 의 판정이 끝나기 전에 B 가 덮어쓴다
- **결과 버퍼는 호출자가 넘긴다** — 정적 `List` 하나를 재사용하면 중첩 호출에서 섞인다
- **"이미 맞힌 대상" 은 `SkillInstance` 가 든다** — 관통 직선이 같은 몬스터를 두 번 때리지 않게 하는
  그 목록은 시전마다 따로여야 한다

Unity 게임플레이는 메인 스레드 단일이라 "동시" 는 같은 프레임 안의 순차 실행이다.
경합 조건은 없고 위 셋만 지키면 된다. 무상태 코어는 나중에 Jobs/Burst 로 그대로 옮겨간다.

- [x] 범위 패턴 `Forward` / `Line` / `Cross` / `Area` / `Dash` — `SkillShape`(무상태)
- [x] `IDamageable` — 몬스터와 파괴 가능 오브젝트가 함께 쓴다
- [x] **플레이어 기본 공격** — `PlayerAttack` + `AttackButton`(쿨타임 표시).
      키와 버튼이 같은 경로를 탄다
- [x] `CombatSettings` — 세 공식을 **재생 중에 바꿔가며 비교**할 수 있게 SO 로.
      배선은 [`docs/guides/player-attack-setup.md`](guides/player-attack-setup.md)
- [ ] **데미지 공식 확정** — 실제로 때려보고 고른다.
      감산 / 비대칭 배율 / 감쇠율. 고른 것이 미결 #3(스탯 공식)의 답이 된다
- [ ] 플레이어 스킬 3종 — 전방 베기 · 관통 직선 · 광역 폭발
- [x] `MonsterController.PerformAttack` 을 `SkillShape` 로 옮김 (4-1 에서 함께 처리)
- [ ] 다단히트 — `SkillInstance` 가 붙을 때. 매 프레임이 아니라 고정 간격
- [ ] 몬스터 등급별 스킬
- [ ] `Destructible` — `IDamageable` 을 구현하면 지금 판정으로 바로 부숴진다
- [x] **HUD 자리 확보** — `SkillButton` 4슬롯 · `EscapeButton` · `InteractButton` 흐림 처리.
      배치는 [`guides/hud-layout.md`](guides/hud-layout.md). **스킬보다 UI 를 먼저 잡았다** —
      나중에 넣으면 화면 전체를 다시 재야 한다
- [ ] **`ISkillBar` 구현체** — `Player.prefab` 에 붙여 `ServiceRegistry` 에 등록.
      이게 붙기 전까지 스킬 버튼 4개는 잠김으로 그려진다. 붙으면 HUD 는 그대로 두고 동작한다
- [ ] 스킬 키 입력 — 버튼과 **같은 `TryCast` 경로**를 타야 한다 (`InputSystem_Actions` 에 액션 추가)
- [ ] **데미지 숫자** — 가해·피격 양쪽. 수치에 따라 표시를 다르게 한다.
      VFX 3요소의 "반응" 이고, **부술 수 있는 벽의 발견성도 이것이 해결한다** (3-2 참조)
- [x] **임팩트(4~8프레임)** — `SkillImpactRasterizer` · `SkillImpact` · `SkillImpactPool`.
      메시가 아니라 **진행도로 굽는 도트 애니메이션** ([`decisions/008-impact-vfx.md`](decisions/008-impact-vfx.md)).
      Boot 씬 배치 완료. **검격만은 아트로 전환했다** (결정 008 §8) —
      `PlayerAttack.Attack Effect` 가 비어 있어 지금은 판정 범위가 그려진다
- [ ] VFX 3요소 나머지 — 플래시 · 사운드 · 햅틱

---

## 정렬(Sorting) 일괄 지정 — 마지막에 한 번에

> 지금까지 정렬을 하나도 손대지 않았다. **마지막에 몰아서 잡기로 했으므로**
> 그때 찾아 헤매지 않도록 현재 상태와 임시값을 전부 여기 모아 둔다 (2026-08-08).

### 지금 상태 — 전부 기본값

| 대상 | Sorting Layer | Order |
|---|---|---|
| `1Floor` `2Floor` `3Floor` `BasicFloor` `Floor` | Default | **0** |
| `1FGuide` `2FGuide` `3FGuide` `Guide` | Default | **0** |
| `1FHiddenRewards` `2FHiddenRewards` (히든방 벽) | Default | **0** |
| 인디케이터 (`SkillIndicatorPool`) | Default | **50** ← 코드에 박힌 임시값 |
| 몬스터 체력바 (`MonsterHealthBar`) | Default | **90** ← 임시값 |
| 타격 이펙트 (`SkillImpactPool`) | Default | **100** ← 임시값. 캐릭터 위여야 한다 |

**바닥과 벽이 같은 레이어·같은 Order 다.** 그리기 순서가 계층 순서에 의존하므로
바닥이 벽 위에 그려질 수도 있다. 지금 안 깨져 보이는 것은 우연이다.

### ⚠ Y-소팅이 꺼져 있다

`ProjectSettings/GraphicsSettings.asset`

```
m_TransparencySortMode: 0        (Default)
m_TransparencySortAxis: (0, 0, 1)
```

`CLAUDE.md` §4 의 **"캐릭터·프롭은 지면 위치 기준 Y-소팅"** 이 전혀 적용되지 않은 상태다.
`Custom Axis (0, 1, 0)` 으로 바꿔야 아래에 있는 것이 앞에 그려진다.

### ⚠ 그런데 Y-소팅만으로는 안 된다 — 접지점 문제

Y-소팅은 **렌더러의 위치**로 정렬하는데, 이 프로젝트는 `Visual` 자식에 스프라이트를 두고
위로 오프셋을 준다. 그 오프셋이 오브젝트마다 다르다 (0.375 ~ 0.891, `docs/design/map-objects.md` §2).

```
가시 장미  Visual Y 0.375      같은 자리에 서 있어도
그루터기   Visual Y 0.828      정렬이 뒤집힌다
```

두 갈래가 있다.

| 안 | 방식 | 언제 |
|---|---|---|
| **A. 수동 Y-소팅 컴포넌트** | 루트(접지점) Y 로 `sortingOrder` 를 매 프레임 계산 | 지금 바로 가능 |
| B. 피벗을 접지선에 | 스프라이트 피벗을 바닥에 두고 `Sprite Sort Point: Pivot` | **아트 하단 여백 7px 통일 후** |

B 가 더 싸지만 8/8 아트 교체가 선행 조건이다. 그때까지는 A 로 간다.

### 정할 것

- [ ] **Sorting Layer 목록 확정** — 권장안
      `Floor` → `Entities` → `Effects` → (나중) `Overhead`
- [ ] `Transparency Sort Mode` → `Custom Axis (0, 1, 0)`
- [ ] Y-소팅 방식 A/B 결정 및 구현
- [ ] 각 요소의 Layer·Order 지정 — 권장안

| 대상 | Layer | Order |
|---|---|---|
| 바닥 타일맵 | `Floor` | 0 |
| 벽 (`Guide`) | `Floor` | 10 |
| **히든방 벽 (Breakable)** | `Floor` | **11** ← `Guide` 보다 1 높게. 같으면 프레임마다 뒤집혀 깜빡인다 |
| 지면 인디케이터 | `Floor` | 20 |
| 플레이어 · 몬스터 · 오브젝트 · 포탈 | `Entities` | **0 고정** (Y-소팅이 여기서 순서를 정한다) |
| 몬스터 체력바 | `Effects` | 0 |
| 임팩트 이펙트 | `Effects` | 10 |
| 데미지 숫자 | `Effects` | 20 |

> **`Entities` 안은 Order 를 전부 0 으로 두어야 한다.** Y-소팅은 Layer 와 Order 가
> 같을 때만 적용된다. 하나라도 다르면 그것만 항상 앞이나 뒤로 간다.

- [ ] `SkillIndicatorPool` 의 코드 기본값(`Default` / 50)을 확정값으로 교체
- [ ] 인디케이터가 캐릭터에 가리지 않는지 실기 확인

### 겸사겸사

- [ ] `1FHiddenRewards` 이름 정리 — `HIdden` 오타는 사라졌다 (6개 모두 통일, 2026-08-09 확인).
      남은 문제는 **"Rewards" 가 보상방과 헷갈린다**는 것. `1FBreakable` 을 권한다

---

## 그 뒤

- [ ] **속성(원소)** — 데미지 공식을 바꾼다. 미결정 #3(스탯 공식)이 먼저 확정돼야 한다
- [ ] 보상 포탈 · 보스 전용 아이템
- [ ] 환생 — 전용 아이템을 모두 모으면. 환생마다 층 추가 예정
- [ ] 히든 아이템 · 히든 상자 (1F 안내, 2F 파밍)
- [ ] 시작 신전 · 숲 맵 (숲은 길을 강제하는 나무·바위 타일 추가 제작 필요)
- [ ] HUD (HP·경험치·레벨). 지금은 `GameRoot` 우클릭 컨텍스트 메뉴로만 확인한다
- [ ] 세로 모드 자동 사냥 설계. `Ingame_Vertical` 은 옛 테스트 맵이라 갈아엎어도 무방

---

## 실기 확인이 필요한 것

에디터에서는 안 드러난다.

- [ ] **세이프 에리어** — 노치·둥근 모서리·제스처 바. 조이스틱 여백이 100(약 6mm)이라 걸릴 수 있다
- [ ] 조이스틱 `Movement Range` — 걷기/달리기가 손가락으로 구분되는지
- [ ] 넉백 4 / 경직 0.12 체감
- [ ] ASTC 4×4 압축 아티팩트 (알파 경계)

---

## 잡무

`CLAUDE.md` §7 "남은 잡무 / 기술 부채" 에 정리되어 있다. 요약하면:

- `companyName` 이 `DefaultCompany`, Unity Cloud `projectName` 이 옛 이름
- Goblin 오브젝트 6장만 PPU 128 (배치 시점에 64로)
- 방향별 Animator Controller 24개 제거 (승인 후)
- `Gamble_Yuusha.slnx` 잔재
- [ ] `joystick_knob.png` 만 PPU 144 다 (나머지 UI 아트는 100).
      지금은 크기를 직접 지정해 문제없지만 `Set Native Size` 를 누르면 100 기준으로 줄어든다
- [ ] `Map/Goblin/Rewards/Guide` 에 방 밖으로 멀리 뻗은 타일이 있다 (셀 x −572 · y 472 까지).
      실수로 칠한 것으로 보이며 지워도 될 것 같다. **카메라 경계가 `Floor` 기준이라 아직 증상은 없다**
