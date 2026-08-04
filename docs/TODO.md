# 할 일

> 마지막 갱신 2026-08-05.
> 현재 무엇이 동작하는지는 [`CLAUDE.md` §7](../CLAUDE.md) 을 본다.
> 왜 그렇게 정했는지는 `docs/decisions/` 를 본다.

---

## 지금 막혀 있는 것

**① `MonsterDefinition` 에셋이 하나도 없다** ← 이게 없으면 스폰해도 아무것도 안 나온다

임시값을 [`docs/design/monster-definitions.xlsx`](design/monster-definitions.xlsx) 로 정리해 두었다.
확정본이 나오면 `.asset` **10종**(Goblin 4 · Orc 3 · Vampire 3)으로 변환한다.

**데미지 공식이 먼저 정해져야 한다.** 시트에 `DAMAGE = ATK − DEF×1.5` 가 들어왔는데,
플레이어 Lv1 ATK 20 · DEF 40 을 그대로 넣으면 양쪽 다 무너진다.

| 방향 | 결과 |
|---|---|
| 플레이어 → 몬스터 | DEF 14 이상이면 **데미지 0**. 10종 중 5종(2F 정예 2 · 3F 보스 3)이 기본공격 무적 |
| 몬스터 → 플레이어 | 감산량이 60 인데 최대 ATK 가 50 이라 **10종 전부 데미지 0** |

- [ ] 공식 확정 — 몬스터 ATK 재작성 / DEF 배율 비대칭 / 감산을 감쇠율(`ATK × 100/(100+DEF×1.5)`)로 교체
- [ ] 시트 갱신 (플레이어 DEF 40 반영 + 검증 열 수식 · 낡은 문구 정리)
- [ ] `Assets/Data/Monsters/` 에 `.asset` 10종 생성
- [ ] 임시 몬스터 프리팹에 연결해 배회·추격·공격 검증

**② 몬스터 아트가 없다**

`Maps/` 의 Goblin·Orc·Vampire 는 **맵 테마**이지 몬스터가 아니다.
지금은 Knights 스프라이트를 색만 바꿔 쓰는 임시 프리팹뿐이다.

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

- [ ] 9개 층에 `WalkableArea` 부착 (Floor / Guide 타일맵 연결)
- [ ] 2F 3개에 `FloorPopulation` 부착
- [ ] 1F·3F 에 `MonsterSpawner` 배치
- [ ] 가이드 작성

### 2. 카메라 경계를 층마다 바꾼다 — 코드 완료

`AreaRegistry.Activate()` 가 구역을 켤 때 `CameraFollow.SetBoundsSource()` 를 함께 부른다.
`CameraFollow` 도 `ServiceRegistry` 에 등록된다. **씬 배치만 남았다** (아래 3번과 같은 작업).

### 3. 구역 전환 (포탈) — 코드 완료, 씬 배치 남음

흐름: **시작 신전 → 숲(슬라임 튜토리얼) → 던전 입구(테마 선택) → 던전 층 이동**

설계 근거는 [`docs/decisions/006-area-transition.md`](decisions/006-area-transition.md),
배치 절차는 [`docs/guides/portal-area-setup.md`](guides/portal-area-setup.md).

- [x] AreaID 체계 — `테마번호 × 100 + 층` (Goblin 101~103 · Orc 201~203 · Vampire 301~303)
- [x] `AreaDefinition`(SO) · `AreaAnchor` · `SpawnPoint` · `AreaRegistry` · `AreaTransition` · `Portal`
- [x] 상호작용 계층 — `IInteractable` · `InteractableBehaviour` · `InteractionHub`
      (히든 상자·아이템이 같은 흐름을 쓴다)
- [x] `WorldLocation` 에 `areaId` 추가 + `GameRoot` 복원 시 구역 먼저 켜기
- [x] `ScreenFader` · `InteractButton`
- [x] 에디터 점검 — `Pretty Knights > Areas > 0. 포탈 링크 점검 (변경 없음)`
- [ ] **`Assets/Data/Areas/` 에 `AreaDefinition` 3개 생성** (101 · 102 · 103)
- [ ] **Boot 씬** — `AreaTransition` · `InteractionHub` · `InteractButton` · `FadeOverlay` 배치
- [ ] **Ingame_Horizontal** — `Map` 에 `AreaRegistry`, Goblin 3개 층에 `AreaAnchor` + `WalkableArea`,
      `SpawnPoint` · `Portal` 배치
- [ ] Orc · Vampire 로 복제 (Goblin 검증 후)

> 포탈은 **단방향**이다. 되돌아오는 길은 탈출 스킬뿐이며
> `AreaTransition.RequestEscape()` 는 만들어져 있으나 부르는 쪽이 아직 없다.
> `AreaDefinition.EscapeTo` 는 던전 입구 구역(#3)이 생긴 뒤 채운다.

### 4. 경로 탐색 (그리드 A*)

현재 `MonsterController` 는 대상 방향으로 곧장 향하는 단순 조향이다.

**Vampire 3F 에서 반드시 터진다.** 벽:바닥이 `1 : 1.4` 라 몬스터가 벽에 붙어 진동한다.
Goblin·Orc 의 트인 보스방(`1 : 18`)에서는 드러나지 않다가 거기서만 망가진다.

- [ ] 방/구역 단위 그리드 A\*. `WalkableArea` 가 이미 통행 판정을 들고 있다
- [ ] Vampire 보스는 순간이동이라 예외 (경로 탐색 불필요)

### 5. 스킬 판정 시스템

`CLAUDE.md` §5 의 원칙을 따른다. **판정과 VFX 를 분리하고, 플레이어와 몬스터가 같은 시스템을 쓴다.**
따로 만들면 범위 계산이 두 벌이 되고 "보스가 플레이어 스킬을 쓴다" 를 못 한다.

- [ ] 범위 패턴 `Forward` / `Line` / `Cross` / `Area` / `Dash`
- [ ] 플레이어 공격 (없어서 몬스터 HP 밸런싱을 못 하고 있다)
- [ ] 몬스터 등급별 스킬
- [ ] 공격 버튼 UI

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
