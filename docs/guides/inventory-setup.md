# 인벤토리 배치 가이드

> **마인크래프트 방식** — 칸이 고정이고 같은 아이템은 한 칸에 쌓인다 (2026-08-09 확정).
> 무게가 아니라 칸이라서 **얼마나 남았는지가 빈 칸으로 보인다.**
>
> 화면은 **왼쪽 격자 · 오른쪽 상세** 두 덩어리다.
> 줍는 것은 **사용 키**를 그대로 쓴다 — 포탈과 같은 흐름이다.

---

## 0. 무엇이 생겼나

| | |
|---|---|
| `ItemDefinition` (SO) | 아이템 한 종류. **`itemId` 는 세이브에 들어가므로 정한 뒤 바꾸지 않는다** |
| `ItemDatabase` (SO) | `itemId` → 에셋. 세이브가 문자열만 들고 있어 필요하다 |
| `Inventory` | 칸 배열. `SaveData` 에 들어가 자동으로 저장된다 |
| `ItemPickup` | 바닥의 아이템·상자. `InteractableBehaviour` 상속 |
| `InventoryPanel` · `InventorySlotView` | 화면 |
| `PotionSettings` · `AutoPotion` | 포션 자동 사용. **스크립트를 따로 뒀다** |
| `PotionSettingsView` · `PotionWarningLabel` | 임계값 조절과 "포션이 없습니다" |

---

## 1. 입력 액션을 추가한다 — 먼저 할 것

**`InputSystem_Actions` 의 `Player` 맵에 `Inventory` 액션이 없다.** 없으면 화면을 열 수 없다.

1. `Assets/InputSystem_Actions.inputactions` 를 더블클릭
2. **Player** 맵 선택 → Actions 옆 `+`
3. 이름을 정확히 **`Inventory`** · Action Type **Button**
4. 바인딩 추가
   - 키보드 **I** (`<Keyboard>/i`)
   - 게임패드 **Y / △** (`<Gamepad>/buttonNorth`)
5. **Save Asset**

> 이름이 한 글자만 달라도 코드가 못 찾는다. 그때는 콘솔에
> `'Player/Inventory' 액션을 찾지 못했습니다` 가 뜬다.

---

## 2. 아이템을 만든다

`Create > Pretty Knights > Item Definition` → `Assets/Data/Items/` 아래.

검증용으로 셋만 만들어도 충분하다.

| itemId | 이름 | 분류 | 최대 | 사용 | 회복 |
|---|---|---|---|---|---|
| `material_scrap` | 잡동사니 | Material | 99 | 끔 | — |
| `potion_small` | 작은 물약 | Consumable | 20 | **켬** | 30 |
| `potion_large` | 큰 물약 | Consumable | 10 | **켬** | 120 |
| `key_goblin` | 고블린 열쇠 | Key | 1 | 끔 | — |

**포션에는 `Auto Use` 를 켠다.** 이게 "자동으로 마실 것" 을 정한다.
언제 마실지는 플레이어가 정하므로 여기 없다 (7절).

- **`Icon` 을 비워도 동작한다** — 칸이 빈 것처럼 보일 뿐이다. 아트는 나중에
- `Description` 은 오른쪽 상세에 그대로 나온다
- **열쇠(Key)는 버릴 수 없다.** 버리면 그 던전을 영영 못 여는 상황이 생긴다

### 만들 때마다 목록을 갱신한다

```
Pretty Knights > Data > 4. 아이템 목록 점검 (변경 없음)
Pretty Knights > Data > 5. 아이템 목록 갱신
```

`Assets/Data/ItemDatabase.asset` 이 만들어진다.

> ⚠ **갱신을 빠뜨리면 그 아이템은 세이브에서 사라진다.**
> 인벤토리는 `itemId` 문자열만 저장하므로 표에 없으면 불러올 때 풀리지 않는다.
> 증상이 "가끔 아이템이 없어진다" 라 원인을 찾기 어렵다.

---

## 3. `GameRoot` 에 물린다

Boot 씬 `GameRoot` 의 `GameRoot` 컴포넌트에 칸이 둘 생겼다.

```
GameRoot  (GameObject)
 ├─ [C] GameRoot
 │        Player Stats     → PlayerStatsDefinition   (기존)
 │        Combat Settings  → CombatSettings          (기존)
 │        Item Database    → ItemDatabase            ★ 추가
 │        Inventory Slots  → 30                       6 × 5 격자
 │
 ├─ [C] AreaTransition · InteractionHub · SkillIndicatorPool · SkillImpactPool  (기존)
 │
 └─ [C] AutoPotion                                    ← 추가
          Check Interval    → 0.2
          Use Cooldown      → 1
          Warning Interval  → 6
```

> `AutoPotion` 을 `GameRoot` 에 두는 이유는 **몸이 씬마다 새로 생겨도 계속 돌아야 하기 때문**이다.
> 게임플레이 씬에 두면 전환 중에 마시지 못한다.

---

## 4. 화면을 만든다

`UIRoot/Canvas` 아래. **`FadeOverlay` 보다 위, 나머지보다 아래**에 둔다 —
가방은 HUD 를 덮어야 하고 페이드는 가방까지 덮어야 한다.

```
Canvas
 ├─ TopBar / Controls / SkillBar / AttackButton / InteractButton / EscapeButton   (기존)
 │
 ├─ InventoryPanel  (GameObject)                 ← 추가
 │    ├─ [C] RectTransform      Anchor Stretch 전체 · 여백 0
 │    ├─ [C] CanvasGroup        ★ 필수. 알파로 열고 닫는다
 │    ├─ [C] Image              어두운 배경 (검정 알파 200 정도)
 │    ├─ [C] InventoryPanel
 │    │        Input Actions → ★ InputSystem_Actions
 │    │        Slot Root     → 아래 SlotGrid       ★
 │    │        Detail Icon / Name / Category / Description → 오른쪽 것들  ★
 │    │        Use Button / Discard Button        ★
 │    │        Empty Hint    → 아래 EmptyHint
 │    │        Group         → 비워도 자동
 │    │
 │    ├─ SlotGrid  (GameObject)                   ← 왼쪽
 │    │    ├─ [C] RectTransform
 │    │    │        Anchor (0,0.5)~(0,0.5) · Pivot (0,0.5)
 │    │    │        Pos (120, 0)   Size (760, 640)
 │    │    ├─ [C] GridLayoutGroup
 │    │    │        Cell Size → (120, 120)
 │    │    │        Spacing   → (8, 8)
 │    │    │        Constraint → Fixed Column Count · **6**
 │    │    │
 │    │    └─ Slot  (GameObject) × **30**         ← 하나 만들고 29번 복제
 │    │         ├─ [C] RectTransform  (그리드가 크기를 정한다)
 │    │         ├─ [C] Image          칸 배경
 │    │         ├─ [C] Button
 │    │         ├─ [C] InventorySlotView
 │    │         │        Icon / Count Label / Selected Frame → 아래 자식들
 │    │         ├─ Icon  (GameObject)   [C] RectTransform · Image  (Raycast 끔)
 │    │         ├─ Count (GameObject)   [C] TextMeshProUGUI  오른쪽 아래 정렬
 │    │         └─ Frame (GameObject)   [C] Image  테두리. **꺼진 채로 저장**
 │    │
 │    └─ Detail  (GameObject)                     ← 오른쪽
 │         ├─ [C] RectTransform
 │         │        Anchor (1,0.5)~(1,0.5) · Pivot (1,0.5)
 │         │        Pos (-120, 0)   Size (520, 640)
 │         ├─ [C] Image                  패널 배경
 │         ├─ DetailIcon  (GameObject)   [C] Image        크게. 위쪽 가운데
 │         ├─ DetailName  (GameObject)   [C] TextMeshProUGUI
 │         ├─ DetailCategory (GameObject)[C] TextMeshProUGUI   "소모품 · 3개"
 │         ├─ DetailDescription (GameObject) [C] TextMeshProUGUI  여러 줄
 │         ├─ UseButton  (GameObject)
 │         │    ├─ [C] CanvasGroup       ★ 흐려지는 데 쓴다
 │         │    ├─ [C] Image · Button
 │         │    └─ Label  [C] TextMeshProUGUI  "사용"
 │         ├─ DiscardButton  (GameObject)   위와 같은 구성 · "버리기"
 │         ├─ EmptyHint  (GameObject)   [C] TextMeshProUGUI  "아이템을 고르세요"
 │         │
 │         └─ PotionSettings  (GameObject)        ← 오른쪽 아래. 7절
 │              ├─ [C] RectTransform  Anchor 아래쪽 스트레치 · Height 160
 │              ├─ [C] PotionSettingsView
 │              │        Auto Use Toggle   → 아래 AutoUseToggle   ★
 │              │        Threshold Slider  → 아래 ThresholdSlider ★
 │              │        Threshold Label   → 아래 ThresholdLabel  ★
 │              │        Stock Label       → 아래 StockLabel
 │              ├─ AutoUseToggle    (GameObject)  [C] Toggle  "포션 자동 사용"
 │              ├─ ThresholdSlider  (GameObject)  [C] Slider
 │              │        Min Value → 0.05 · Max Value → 0.95 · Whole Numbers 끔
 │              ├─ ThresholdLabel   (GameObject)  [C] TextMeshProUGUI
 │              └─ StockLabel       (GameObject)  [C] TextMeshProUGUI
 │
 ├─ PotionWarning  (GameObject)                  ← 추가. 가방 밖이다
 │    ├─ [C] RectTransform
 │    │        Anchor (0.5, 1)~(0.5, 1) · Pivot (0.5, 1)
 │    │        Pos (0, -220)   Size (720, 80)
 │    ├─ [C] CanvasGroup        ★ Alpha 0 으로 저장
 │    ├─ [C] Image              반투명 띠 (선택)
 │    ├─ [C] PotionWarningLabel
 │    │        Hold Duration → 2 · Fade Duration → 0.6
 │    └─ Label  (GameObject)   [C] TextMeshProUGUI  가운데 정렬
 │
 └─ FadeOverlay                                  (기존) ★ 맨 아래 유지
```

> **`PotionWarning` 은 `InventoryPanel` 밖에 둔다.** 가방을 닫은 채 싸우는 동안
> 떠야 하는 문구다. 가방 안에 두면 가방을 열어야만 보인다.

**칸 번호를 손으로 넣지 않는다.** `SlotGrid` 의 자식 순서가 곧 번호다 —
서른 칸에 하나만 틀려도 엉뚱한 아이템이 선택되고 원인이 눈에 안 보인다.

마지막으로 `UIRoot` 의 **`Landscape Only` 에 `InventoryPanel` 과 `PotionWarning` 을 추가**한다.

> `CanvasGroup` 의 `Alpha` 를 **0 으로 저장**한다. 1 로 저장하면 시작하자마자 가방이 떠 있다.

---

## 5. 바닥에 아이템을 놓는다

**포탈과 같은 구성이다.** 사용 키로 줍는다.

```
Goblin1F/Items/  (묶음용)
 └─ Pickup_Potion  (GameObject)
      ├─ [C] Transform
      ├─ [C] SpriteRenderer        아이템 그림 (선택)
      ├─ [C] BoxCollider2D         ★ Is Trigger 켬 · Size (1.5, 1.5)
      └─ [C] ItemPickup
               Item          → potion_small     ★
               Count         → 3
               Extra Drops   → 비움 (상자면 DropTable 을 넣는다)
               Prompt Label  → 비움 (아이템 이름으로 자동)
               Reveal On Take → 비움 (상자로 히든 방을 열려면 그 NoSpawnZone)
```

- **가방이 차면 줍히지 않고 그 자리에 남는다.** 넣은 만큼만 줄고 나머지가 남는다
- **주워간 것은 세이브에 남는다.** 칸 좌표로 짚으므로 에디터에서 위치를 크게 옮기면 기록이 끊긴다
- 테마를 완전 클리어하면 다시 놓인다 — 새 회차이므로

### 상자로 쓰려면

`Item` 을 비우고 `Extra Drops` 에 `DropTable` 을 넣는다. 경험치와 아이템이 함께 나오고
로그도 몬스터·오브젝트와 같은 모양이다.

`Reveal On Take` 에 그 층의 `NoSpawnZone` 을 넣으면 **상자를 열면 히든 방 봉인이 풀린다.**

---

## 6. 몬스터가 아이템을 떨구게 하려면

`DropTable` 의 항목에 칸이 늘었다.

```
Entry
   Chance    0.25
   Min/Max Exp   1 / 3
   Item      → potion_small     ← 추가
   Min/Max Count 1 / 1          ← 추가
   Label     (아이템이 있으면 그 이름을 쓴다)
```

`Pretty Knights > Data > 3. 드랍 표 생성/연결` 이 만든 표 6종에는 아직 아이템이 비어 있다.
**인스펙터에서 채운다** — 어떤 몬스터가 무엇을 떨구는지는 기획이라 도구가 정할 수 없다.

> 가방이 가득 차면 로그로 알린다. 바닥에 떨어뜨리지 않는 이유는
> 줍는 것과 사라지는 규칙이 아직 없어서다.

---

## 7. 포션 자동 사용

**둘로 나뉘어 있다.** 주인이 다르기 때문이다.

| 정하는 것 | 어디서 | 누가 |
|---|---|---|
| **무엇을** 마실지 | `ItemDefinition` 의 `Auto Use` | 기획 |
| **언제** 마실지 | `PotionSettings` — 화면의 슬라이더 | **플레이어** |

그래서 스크립트도 아이템·인벤토리와 따로 두었다. 설정은 아이템이 몇 종이든 하나뿐이고,
나중에 옵션 화면이 생기면 그쪽이 이것만 만지면 된다.

### 어느 포션을 마시나

**낭비가 가장 적은 것**을 고른다 — 잃은 만큼을 덮는 포션 중 가장 작은 것.
그런 것이 없으면 가장 큰 것을 마신다.

```
HP 200/500  (잃은 것 300)
  작은 물약 30    ← 300 을 못 덮는다
  큰 물약  120    ← 이것도 못 덮는다  →  가장 큰 것(120)을 마신다

HP 400/500  (잃은 것 100)
  작은 물약 30    ← 못 덮는다
  큰 물약  120    ← 덮는다  →  이걸 마신다
```

큰 포션을 긁힌 상처에 쓰면 정작 필요할 때 없고, 작은 것만 홀짝이면 마시는 사이에 죽는다.

### 포션이 없으면

`PotionWarning` 에 **"포션이 없습니다 (HP 50% 이하 자동 사용)"** 가 뜬다.

- **6초에 한 번만 뜬다.** 임계값 아래에 있는 동안 검사가 계속 돌기 때문에
  간격이 없으면 화면이 도배된다
- 한 번 마시면 경고 간격이 초기화된다

> **HP 가 가득이면 마시지 않는다.** 그때 마시면 포션이 조용히 사라진다.
> 손으로 쓸 때도 같다 — 사용 버튼을 눌러도 아무 일이 안 일어난다.

---

## 8. 확인

1. 재생 → 가로 모드 → **I** 키
2. 격자 30칸이 뜨고 **조이스틱·공격 버튼이 안 먹는다** (열려 있는 동안 잠긴다)
3. 다시 **I** → 닫히고 조작이 돌아온다
4. 바닥의 아이템 위로 걸어간다 → **"작은 물약 ×3 줍기"** 버튼
5. 사용 키를 누른다 → 아이템이 사라지고 가방에 들어간다
6. 가방을 열어 칸을 누른다 → **오른쪽에 그림·이름·분류·설명**
7. 물약을 고르고 **사용** → HP 가 오르고 개수가 준다
8. HP 가 가득일 때 사용 → **아무 일도 없고 개수도 안 준다** (억울하지 않게)
9. 재생을 멈추고 다시 재생 → **가방이 그대로다**

### 포션 자동 사용

10. 가방 오른쪽 아래에서 **자동 사용 켜기 · 슬라이더를 50%** 로
11. `GameRoot` 우클릭 → **피해 10** 을 반복해 HP 를 50% 아래로
12. **포션이 저절로 줄고 HP 가 오른다.** `AutoPotion` 우클릭 → `지금 상태` 로 확인
13. 포션을 전부 버리고 다시 맞는다 → **"포션이 없습니다" 가 화면 위에** 뜬다
14. 계속 맞아도 **6초에 한 번만** 뜬다
15. 재생을 멈추고 다시 재생 → **슬라이더 값이 그대로다**

### 안 되면

| 증상 | 원인 |
|---|---|
| I 를 눌러도 안 열린다 | `Inventory` 액션이 없다 (1절). 콘솔에 에러가 떠 있다 |
| 시작하자마자 가방이 떠 있다 | `CanvasGroup` 의 Alpha 가 1 로 저장됐다 |
| 칸이 하나도 안 보인다 | `Slot Root` 가 비었거나 `Slot` 이 그 아래에 없다 |
| 아이템은 있는데 칸이 비어 보인다 | `ItemDefinition` 의 `Icon` 이 비었다 (정상) |
| 엉뚱한 칸이 선택된다 | 칸 순서와 다른 곳에 `InventorySlotView` 가 하나 더 있다 |
| 다시 켜니 아이템이 없다 | **`ItemDatabase` 갱신을 안 했다** (2절 ⚠) 또는 `GameRoot` 에 안 물렸다 |
| 사용 버튼이 늘 흐리다 | 그 아이템의 `Usable` 이 꺼져 있다 |
| 주웠는데 안 들어온다 | 가방이 가득 찼다. 콘솔에 남는다 |
| 가방이 열린 채 움직여진다 | `InventoryPanel` 이 `PlayerController` 를 못 찾았다 (Boot 를 거쳤는지) |
| 포션을 안 마신다 | 그 아이템의 `Auto Use` 가 꺼졌거나, 슬라이더가 지금 HP 보다 낮다 |
| 포션을 너무 자주 마신다 | 임계값이 높다. `AutoPotion` 의 `Use Cooldown` 도 본다 |
| 경고가 도배된다 | `Warning Interval` 이 0 이다 |
| 경고가 안 뜬다 | `PotionWarning` 이 `Landscape Only` 에 없거나 Alpha 가 0 으로 고정됐다 |
| 슬라이더가 안 움직인다 | 자동 사용이 꺼져 있다 — 꺼지면 슬라이더도 잠근다 |

---

## 8. 이 문서 범위 밖

- **끌어 옮기기** — `Inventory.Swap(a, b)` 는 있고 UI 가 아직 안 부른다
- **장비 착용** — `ItemCategory.Equipment` 는 갈래만 있다
- **회복 말고 다른 효과** — `ItemDefinition.Use` 한 곳만 늘리면 된다.
  화면은 `Usable` 만 보므로 안 바뀐다
- **세로 모드 가방** — 지금은 가로 전용이다
- **아이템 아트** — 아이콘 없이도 전부 동작한다
