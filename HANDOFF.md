# 인계 — 2026-08-08

> **다음 세션은 `CLAUDE.md` §0 대로 이 파일을 흡수하고 지운다.**
> 여기에는 문서에 없는 **진행 중인 상태**만 적는다.
> 무엇이 동작하는지는 `CLAUDE.md` §7, 할 일은 `docs/TODO.md` 를 본다.

---

## 지금 어디까지 왔나

**Goblin 테마 한 바퀴가 돈다.** 1F → 2F → 3F → 보상방 → 던전 입구까지
포탈로 오갈 수 있고, 보상방을 나가면 테마가 초기화된다.

이번 세션에 들어간 것 (전부 커밋·푸시됨)

| | |
|---|---|
| 전투 | `SkillShape`(무상태) · `PlayerAttack` · `IDamageable` · `IAreaDamageable` · `CombatSettings` |
| 예고 | 픽셀 래스터화 인디케이터 · 몬스터 3단계 공격 |
| 오브젝트 | `PropDefinition`(18) · `FloorScatterProfile` · `PropScatterer` · `FloorProps` · `Destructible` · `SpawnTotem` |
| 히든 방 | `DestructibleTilemap` · `NoSpawnZone` |
| 세이브 | `WorldProgress` — 부순 것 · 테마 클리어 횟수 · 재배치 시드 |
| 도구 | 배치 미리보기 · 연결성 검사 · 정의 생성기 2종 |

---

## 바로 다음에 할 일

### 1. Orc · Vampire 로 복제

Goblin 만 되어 있다. 나머지 6개 층 + 보상방 2개가 남았다.

- `AreaDefinition` — 201~203 / 301~303 · 290 / 390
- 씬의 `AreaAnchor` 에 정의 연결 — **지금 9개가 비어 있어 등록조차 안 된다**
  (재생하면 `[AreaRegistry] AreaDefinition 이 비어 등록되지 않은 구역이 N개` 경고가 뜬다)
- `FloorScatterProfile` 6개, `FloorProps` 부착

절차는 [`docs/guides/prop-scatter-setup.md`](docs/guides/prop-scatter-setup.md).

### 2. 던전 입구의 테마 선택 포탈 3개

`Map/Dungeon` 은 `AreaAnchor` + `Area_Dungeon_Entrance`(#3) 까지 되어 있다.
**각 테마 1F 로 가는 포탈 3개가 없어 순환이 아직 닫히지 않았다.**

### 3. 보스 처치 → Gold 포탈

보스 처치 판정이 없어 **3F 에서 보상방으로 갈 정상 경로가 없다.**
지금은 `AreaTransition` 우클릭 → 디버그 이동으로만 들어간다.
`SpawnTotem` 이 메인 토템에서 하는 일과 같은 흐름이라 구조는 그대로 쓰면 된다.

### 4. 데미지 숫자

부술 수 있는 벽의 **발견성이 여기 걸려 있다** — 때렸을 때 숫자가 뜨는 것이
"이 벽은 부술 수 있다" 는 유일한 신호다.

---

## 아직 정하지 않은 것

- **데미지 공식** — `CombatSettings` 에 세 안(감산 / 비대칭 / 감쇠율)이 들어 있고
  재생 중에 바꿔가며 비교할 수 있다. **실제로 때려보고 고른다.**
  고른 것이 미결 #3(스탯 공식)의 답이 된다
- **서브 토템 층당 개수와 점유량** — 지금 프로필의 값은 임시다
- **층당 오브젝트 밀도** — 바닥 100~150칸당 1개로 시작했다
- **정렬** — 마지막에 한 번에 잡기로 했다 (`docs/TODO.md` "정렬 일괄 지정")

---

## 이번에 겪은 함정 (같은 걸 또 밟지 않도록)

- **`AreaDefinition` 이 비면 그 구역은 없는 것과 같다.** `AreaRegistry` 가 등록조차 하지 않아
  포탈도 디버그 이동도 못 찾는다. 지금은 시작 시 경고가 뜬다
- **프리팹 자식만 옮기면 콜라이더가 안 따라온다.** 그림과 판정이 따로 놀아
  "포탈 위에 섰는데 버튼이 안 뜬다" 가 된다. 루트를 옮겨야 한다
- **도착 지점 이름은 "어디서 왔는가" 다.** 던전 입구에 내려서는 사람은 보상방에서 온 것이므로
  `from_reward` 다. `from_entrance` 로 지으면 읽을 수 없다
- **`Tile` 의 기본 `flags` 가 `LockColor`** 라 `SetTileFlags(TileFlags.None)` 을 먼저
  부르지 않으면 `SetColor` 가 에러도 경고도 없이 무시된다
- **인터페이스 참조에는 Unity 의 널 비교가 걸리지 않는다.** `UnityEngine.Object` 로
  캐스팅해야 파괴된 대상을 걸러낼 수 있다

---

## 확인해 볼 것

- `Map/Goblin/Rewards/Guide` 에 방 밖으로 멀리 뻗은 타일이 있다
  (셀 x −572 · y 472 까지). 실수로 칠한 것으로 보이며 지워도 될 것 같다
