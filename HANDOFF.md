# 인계 — 2026-08-09

> **다음 세션은 `CLAUDE.md` §0 대로 이 파일을 흡수하고 지운다.**
> 여기에는 문서에 없는 **진행 중인 상태**만 적는다.
> 무엇이 동작하는지는 `CLAUDE.md` §7, 할 일은 `docs/TODO.md` 를 본다.

---

## 지금 어디까지 왔나

**전 구역이 배선되고 순환이 닫혔다.** 구역 13개가 모두 등록되고, 던전 입구에서
세 테마로 나가 보상방으로 돌아온다. 인벤토리와 포션 자동 사용도 들어왔다.

이번 세션에 들어간 것 (전부 커밋·푸시됨, `d1afd5c` 까지)

| | |
|---|---|
| 구역 | 정의 13 · 배치 프로필 9 · 생성 도구 `Areas > 2·3` |
| HUD | 위/아래 두 줄 재배치 · 스킬 4칸 · 탈출 버튼 |
| 이펙트 | 타격 임팩트(진행도 래스터화) → **검격은 아트로 전환** (결정 008 §8) |
| 전투 | 반원(180°) · 드랍 표 6종 · `RewardGrant` 로 보상 경로 통합 |
| 아이템 | `ItemDefinition`·`ItemDatabase`·`Inventory`·`ItemPickup` · 포션 자동 사용 |
| 히든 방 | `NoSpawnZone` 6개 배치 · **칸 단위 봉인 해제** |
| 버그 | 모드 전환 좌표 유실 · 시체 미제거 · 토템 지분 복원 · 경고 문구 터치 삼킴 |

---

## 바로 다음에 할 일

### 1. 몬스터 스포너 정리 ← **작업 중이던 것**

씬에 `MonsterSpawner` 가 **0개**다. `FloorPopulation` 은 Goblin2F 에 하나 붙었다.

- [ ] 1F 에 `MonsterSpawner` 6~10개, 3F 보스 자리에 1개
- [ ] `Monster_Temp` 프리팹에 **`MonsterHealthBar` 부착** (아직 0개)
- [ ] `monster_health_*` 3장을 **PPU 192** 로 (지금 100)

절차는 [`docs/guides/monster-spawn-setup.md`](docs/guides/monster-spawn-setup.md),
검증은 [`docs/guides/verify-spawn-drop.md`](docs/guides/verify-spawn-drop.md).

> **프리팹의 `MonsterController.definition` 은 신경 쓰지 않아도 된다.**
> 스포너가 `Spawn(definition, point)` 로 덮어쓴다. 프리팹은 하나가 맞다.

### 2. UI 마무리

- [ ] **`PotionSettingsView` 가 씬에 0개** — 포션 임계값 조절 UI.
      슬라이더 핸들이 타원으로 늘어나던 것은 `Handle` 앵커를 `(0, 0.5)` 로
      묶고 36×36 고정 + `Preserve Aspect` 로 해결한다
- [ ] `ItemDefinition` 4종의 `Icon` 이 비어 있다. 포션 아트 3장은 들어와 있다
      (`Art/Objects/Interactive/Items/health_potion_*_64.png`)
- [ ] `InteractButton` 에 **`Icon` 칸이 새로 생겼다** — 자식 Image 를 물려야
      포탈/줍기가 그림으로 갈린다 (글자는 없애기로 했다)
- [ ] 미연결 아트: `attack_button_pressed` · `skill_slot_pressed` ·
      `start_button_*` · `player_hud_frame` · `player_health_*` · `boss_health_*`
- [ ] **플레이어 HP HUD 가 통째로 없다.** 아트는 다 있다

### 3. 지금 고쳐야 세이브가 안 꼬이는 것

- [ ] **`potion_midium` 오타** (`medium`). `itemId` 는 세이브에 들어가고
      한 번 정하면 못 바꾼다 — 바꾸면 그 아이템을 가진 세이브에서 사라진다.
      **아직 세이브가 없는 지금이 마지막 기회다.** 고치면 `Data > 5. 아이템 목록 갱신` 재실행

### 4. 검격 아트

`Attack Effect` 가 비어 있어 임시로 판정 범위가 그려지고 있다 (그 부채꼴이다).
PixelLab 명령서대로 뽑으면 끝난다 — [`docs/guides/skill-effect-art.md`](docs/guides/skill-effect-art.md).
아트가 들어오면 `PlayerAttack` 의 `Show Range When No Art` 를 끈다.

---

## 아직 정하지 않은 것

- **데미지 공식** — `CombatSettings` 의 세 안 중 하나. 실제로 때려보고 고른다
- **예고 시간** — 짧아서 못 피한다는 판단이 나왔다. 레벨 디자인에서 조정 예정
- **정렬** — 마지막에 한 번에 (`docs/TODO.md` "정렬 일괄 지정").
  임시값이 늘었다: 인디케이터 50 · 몬스터 체력바 90 · 타격 이펙트 100

---

## 이번에 겪은 함정 (같은 걸 또 밟지 않도록)

전부 [`docs/pitfalls.md`](docs/pitfalls.md) 에 있다. 특히 최근 것 셋

- **화면에서 사라진 것은 대개 "지워진 것" 이 아니라 "카메라 경계 밖" 이다** —
  몸이 없는 것과 화면 밖인 것을 먼저 갈라야 한다
- **치우는 책임을 부르는 쪽에 맡기면 빠뜨린 쪽이 생긴다** —
  `FloorPopulation` 만 시체를 안 치워 2층에서만 증상이 났다
- **알파 0 이어도 레이캐스트는 막힌다** — 안 보이는 문구가 터치를 삼킨다

여기 없는 것 하나 더

- **`MonsterDefinitionBuilder` 를 다시 돌리면 손으로 맞춘 `Telegraph Duration` 이
  등급 기본값으로 덮인다.** 밸런싱 값을 지키려면 도구의 표를 함께 고친다

---

## 확인해 볼 것

- `joystick_knob.png` 만 PPU 144 다. 나머지 UI 아트는 100 —
  지금은 크기를 직접 지정해 문제없지만 `Set Native Size` 를 누르면 100으로 줄어든다
- `Ingame_Horizontal` 이 통째로 재직렬화됐다 (diff 238,000줄).
  구역 13 · 정의 13/13 · `FloorProps` 9 · Tilemap 38 로 내용은 확인했다
- `Map/Goblin/Rewards/Guide` 에 방 밖으로 뻗은 타일 (셀 x −572 · y 472).
  지워도 될 것 같다 — 카메라 경계가 `Floor` 기준이라 아직 증상은 없다
