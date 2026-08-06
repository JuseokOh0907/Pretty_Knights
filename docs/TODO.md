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

**③ 플레이어 공격이 없다 — 다섯 가지가 여기 하나에 걸려 있다**

| 막힌 것 | 원인 |
|---|---|
| 몬스터 HP 밸런싱 | 플레이어 DPS 를 계산할 수 없다 |
| 데미지 공식 확정 (①) | 실제로 때려볼 수 없다 |
| 메인 토템 파괴 → 포탈 개방 | 부술 수단이 없다 |
| 서브 토템 파괴 → 인구 감소 | 부술 수단이 없다 |
| 오브젝트 파괴 → 드랍 | 부술 수단이 없다 |

**순서를 재조정했다** (2026-08-05). 아래 5번 스킬 판정을 먼저 하고,
**데미지 공식은 그 안에서 플레이어 스킬·몬스터 스킬과 함께 정한다.**
숫자만 놓고 정하는 것보다 실제로 때려보고 정하는 편이 정확하다.

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

### 3-1. 맵 오브젝트와 토템

실측·역할·구조는 [`docs/design/map-objects.md`](design/map-objects.md) 에 정리했다.

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
- [ ] `PropDefinition`(SO) — HP · 역할 · 드랍 · 인구 지분
- [ ] `Prop.prefab` + 18 배리언트 (콜라이더는 접지폭 × 0.5칸, `Visual` Y 오프셋은 실측표)
- [ ] `Destructible` — HP · 피격 · 파괴 이벤트
- [ ] `SpawnTotem` — 메인은 포탈 활성화, 서브는 `FloorPopulation` 목표 인구 감소
- [ ] `FloorPopulation` 에 목표 인구를 런타임에 낮추는 API.
      **살아 있는 개체는 회수하지 않는다** — 지금도 `alive.Count >= target` 이면
      채우기를 멈출 뿐이라 API 만 열면 그대로 동작한다
- [ ] `WalkableArea.IsAreaWalkable(center, size)` — 2×2칸 판정
- [ ] **배치 도구** `Pretty Knights > Props > 오브젝트 뿌리기`
      항목별 **정확한 개수** 지정 · 최소 간격 · 토템 주변 보호 반경 · 미리보기 메뉴 별도
      - **손으로 배치한 것은 건드리지 않는다** (히든 방 입구가 지워지면 안 된다)
      - 연결성 검사는 **필수 지점**(시작 → 토템 → 포탈)만 본다.
        히든 방은 필수 지점이 아니라 자동으로 예외가 된다
- [ ] **파괴 상태 세이브** — `areaId + 칸 좌표` 를 이름표로. 재시작 후에도 부순 것이 부서진 채
- [ ] 드랍 — 1단계는 경험치만(`AddExp` 가 이미 있다), 아이템은 시스템 생긴 뒤

**미결** — 서브 토템 층당 개수 · 감소량 방식 · 3F 토템 유무 · 나머지 12종 파괴 가능 여부
· 토템 파괴 시 기존 몬스터 처리 · `weapon_rack` 용도 · 층당 밀도

### 4. 경로 탐색 (그리드 A*)

현재 `MonsterController` 는 대상 방향으로 곧장 향하는 단순 조향이다.

**Vampire 3F 에서 반드시 터진다.** 벽:바닥이 `1 : 1.4` 라 몬스터가 벽에 붙어 진동한다.
Goblin·Orc 의 트인 보스방(`1 : 18`)에서는 드러나지 않다가 거기서만 망가진다.

- [ ] 방/구역 단위 그리드 A\*. `WalkableArea` 가 이미 통행 판정을 들고 있다
- [ ] Vampire 보스는 순간이동이라 예외 (경로 탐색 불필요)

### 5. 스킬 판정 시스템 ← **다음 작업** (2026-08-05 순서 조정)

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

- [ ] 범위 패턴 `Forward` / `Line` / `Cross` / `Area` / `Dash`
- [ ] **데미지 공식 확정** — 여기서 함께 정한다. 후보는
      감산 `ATK − DEF×1.5` / 비대칭 배율 / 감쇠율 `ATK × 100/(100+DEF×1.5)`
      (플레이어 Lv1 ATK 20 · DEF 40 기준으로 세 안의 결과가 크게 갈린다)
- [ ] 플레이어 스킬 3종 — 전방 베기 · 관통 직선 · 광역 폭발
- [ ] 몬스터 등급별 스킬
- [ ] 공격 버튼 UI
- [ ] `Destructible` 연결 — 오브젝트·토템도 같은 판정으로 부순다

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
