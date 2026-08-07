# 플레이어 기본 공격 · 상호작용 배선 가이드

> 목표는 **한 번 실행해서 때리고 이동하는 것까지 보는 것**이다.
> 구역 전환 쪽 배치는 [`portal-area-setup.md`](portal-area-setup.md) 와 겹치므로 그쪽을 먼저 본다.
> 설계 근거는 [`docs/decisions/006-area-transition.md`](../decisions/006-area-transition.md) 와 `CLAUDE.md` §5.

---

## 이번에 붙는 것

```
Assets/Scripts/Combat/
    IDamageable      몬스터와 파괴 가능 오브젝트가 함께 쓰는 인터페이스
    SkillShape       범위 계산 한 벌 (Forward / Line / Cross / Area / Dash)
    PlayerAttack     기본 공격. 스킬 3종이 이 위에 얹힌다
Assets/Scripts/Data/
    CombatSettings   데미지 공식. 세 안을 재생 중에 바꿔가며 비교한다
Assets/Scripts/UI/
    AttackButton     공격 버튼 + 쿨타임 표시
```

### 왜 `CombatSettings` 가 SO 인가

데미지 공식이 아직 확정되지 않았다. 플레이어 Lv1 ATK 20 · DEF 40 을 넣으면 세 안의 결과가 크게 갈린다.

| 모델 | ATK 20 vs DEF 15 | 성질 |
|---|---|---|
| `Subtract` | `20 − 22.5` → **0** | DEF 14 이상이 기본공격 무적이 된다 |
| `AsymmetricSubtract` | 때리는 쪽에 따라 다름 | 시트 숫자를 살리되 공식이 두 벌 |
| `Attenuate` (기본) | `20 × 100/122.5` = **16.3** | 절대 0 이 되지 않는다 |

**숫자만 놓고 고르는 것보다 실제로 때려보고 고르는 편이 정확하다.**
ScriptableObject 는 재생 중에 고쳐도 즉시 반영되므로, 플레이 중에 모델을 바꿔가며 손맛을 비교한다.

---

## 1단계 · `CombatSettings` 에셋

`Assets/Data/` 에서 `Create > Pretty Knights > Combat Settings` → `CombatSettings.asset`

| 항목 | 값 | 비고 |
|---|---|---|
| Model | `Attenuate` | 먼저 이걸로 시작한다 |
| Defense Multiplier | 1.5 | 시트의 안 |
| Defense Multiplier Against Player | 0.25 | 비대칭 모델에서만 |
| Attenuation Constant | 100 | 클수록 방어력 영향이 작다 |
| **Minimum Damage** | **1** | **0 으로 두면 감산 모델에서 무적이 생긴다** |

---

## 2단계 · `MonsterDefinition` 10종 생성

```
Pretty Knights > Data > 0. 몬스터 정의 점검 (변경 없음)
Pretty Knights > Data > 1. MonsterDefinition 생성/갱신
```

`Assets/Data/Monsters/` 에 `Monster_goblin_hob.asset` 등 10개가 생긴다.
값의 출처는 `docs/design/monster-definitions.xlsx` 이며 그 표가 코드에 들어 있다.

- **이미 있으면 지우지 않고 값만 덮어쓴다.** 프리팹·스포너가 물고 있던 참조가 끊기지 않는다
- `frames`(스프라이트)는 건드리지 않는다. 몬스터 아트가 나오면 인스펙터에서 채운다
- 표를 고칠 일이 생기면 **시트도 함께 고칠 것.** 둘이 어긋나면 어느 쪽이 사실인지 알 수 없어진다

---

## 3단계 · Boot 씬

`GameRoot` 오브젝트에 컴포넌트 셋이 더 붙는다. **루트는 여전히 `GameRoot` 와 `UIRoot` 둘뿐이다.**

```
Boot.unity
│
├─ GameRoot  (GameObject)
│    ├─ [C] Transform
│    ├─ [C] GameRoot                        (기존)
│    │        Player Stats      → Knight_Stats
│    │        Combat Settings   → CombatSettings      ← 이번에 추가
│    │        Start Mode        → Horizontal
│    │
│    ├─ [C] AreaTransition                  (포탈 가이드에서 추가)
│    └─ [C] InteractionHub                  (포탈 가이드에서 추가)
│             Input Actions  → InputSystem_Actions   ★
│
└─ UIRoot  (GameObject)
     └─ Canvas  (GameObject)
          ├─ ModeSwitchButton      (기존)
          ├─ Controls              (기존, 조이스틱 — 왼쪽 아래)
          │
          ├─ InteractButton        (포탈 가이드에서 추가 — 오른쪽 아래)
          │
          ├─ AttackButton  (GameObject)     ← 이번에 추가
          │    ├─ [C] RectTransform
          │    │        Anchor 오른쪽 아래 · Pivot (1, 0)
          │    │        Pos (-220, 520)   Size (300, 300)
          │    │        ※ InteractButton(-220, 240) 위에 놓는다. 겹치면 오발동한다
          │    ├─ [C] CanvasGroup           쿨타임 중 흐려지는 데 쓴다
          │    ├─ [C] Image                 버튼 배경 · Raycast Target 켬
          │    ├─ [C] Button
          │    ├─ [C] AttackButton
          │    │        Button / Group   → 비워도 자동 탐색
          │    │        Cooldown Fill    → 아래 Fill 을 연결 ★
          │    │        Dimmed Alpha     → 0.45
          │    │
          │    └─ Fill  (GameObject)
          │         ├─ [C] RectTransform    Stretch 전체 · 여백 0
          │         └─ [C] Image            Image Type: **Filled**
          │                                 Fill Method: Radial 360
          │                                 Raycast Target **끔**
          │
          └─ FadeOverlay           (포탈 가이드에서 추가 — 형제 중 맨 아래)
```

> `AttackButton` 도 `UIRoot` 의 **Landscape Only** 배열에 넣는다.

---

## 4단계 · `Player.prefab`

루트에 컴포넌트 하나만 더 붙는다.

```
Player  (프리팹 루트)
 ├─ [C] Rigidbody2D · CapsuleCollider2D          (기존)
 ├─ [C] CharacterMotor · PlayerController        (기존)
 ├─ [C] PlayerHitReaction                        (기존)
 ├─ [C] PlayerAttack                             ← 추가
 │        Input Actions  → InputSystem_Actions   ★
 │        Player / Animator Driver → 비워도 자동 탐색
 │        Shape          → Forward
 │        Shape Params   → Range 1.6 · Width 1 · Angle 100 · Forward Offset 0
 │        Origin Forward Offset → 0.3
 │        Cooldown       → 0.45
 │        Auto Aim       → 끔
 │        Target Layers  → Everything
 │        Draw Last Swing → 켬
 │        Log Hits       → 켬 (검증 동안만)
 │
 └─ Visual  (자식)
      └─ [C] SpriteRenderer · Animator · DirectionalAnimatorDriver   (기존)
```

`Target Layers` 를 `Everything` 으로 두어도 안전하다. 실제 판정은 **`IDamageable` 을 들고 있는지**로 거르고, `ContactFilter2D.useTriggers` 가 꺼져 있어 포탈·상호작용 트리거는 애초에 걸리지 않는다.

---

## 5단계 · 몬스터 프리팹 확인

**때리려면 몬스터에 트리거가 아닌 `Collider2D` 가 있어야 한다.**

```
Monster  (프리팹 루트)
 ├─ [C] Rigidbody2D
 ├─ [C] CapsuleCollider2D        ★ Is Trigger **꺼짐** — 켜져 있으면 공격이 통과한다
 ├─ [C] CharacterMotor
 └─ [C] MonsterController
          Definition → Monster_goblin_hob 등
          Knockback On Hit → 3
```

`MonsterController` 는 이제 `IDamageable` 을 구현한다. 넉백은 **VIT 를 무게로 읽어** 자동 감쇠하므로 보스는 덜 밀린다.

---

## 6단계 · 실행 확인

Boot 씬에서 재생한다.

| 확인할 것 | 기대 |
|---|---|
| 공격 버튼 | 누르면 즉시 판정. 0.45초 동안 흐려지고 Fill 이 채워진다 |
| 키보드 공격 | 마우스 왼쪽 버튼도 같은 동작 (`Player/Attack` 액션) |
| 방향 | **바라보던 방향으로 나간다.** 손을 떼도 방향이 유지되므로 그 방향 그대로 |
| 판정 범위 | 씬 뷰에서 Player 선택 → 마지막 휘두름이 빨간 부채꼴로 남는다 |
| 데미지 | 콘솔에 `[PlayerAttack] Hobgoblin 에 16.3 (ATK 20 vs DEF 8)` |
| 넉백 | 맞은 몬스터가 뒤로 밀린다 |
| 처치 | HP 0 → 경험치. `GameRoot` 우클릭 → 상태 로그로 확인 |
| 포탈 | 트리거 안에 들어가면 버튼이 뜨고, 눌러야 이동 |
| 전환 중 | 페이드 도는 동안 조이스틱·공격·상호작용이 전부 잠긴다 |

### 데미지 공식 비교하기

재생 중에 `CombatSettings.asset` 을 선택하고 `Model` 을 바꾸면 **다음 타격부터 즉시 반영된다.**

```
Attenuate  → 잡몹도 보스도 꾸준히 깎인다. 대신 방어력의 의미가 옅다
Subtract   → DEF 14 이상이 아예 안 깎인다. 스킬이 ATK 를 못 올리면 진행 불가
```

`Log Hits` 를 켜두면 콘솔에 `(ATK … vs DEF …)` 가 함께 찍히므로 숫자로도 비교된다.
**여기서 고른 것이 §9 미결 #3(스탯 공식)의 답이 된다.**

---

## 아직 안 한 것

- **VFX** — 지금은 기즈모뿐이다. 예고(지면 인디케이터) → 임팩트 → 반응 3요소는 `CLAUDE.md` §5
- **다단히트** — 기본 공격은 1히트다. 고정 간격 다단히트는 `SkillInstance` 가 붙을 때
- **스킬 3종** — 전방 베기 · 관통 직선 · 광역 폭발. `SkillShape` 는 이미 다섯 패턴을 전부 계산한다
- **몬스터 스킬** — `MonsterController.PerformAttack` 이 아직 거리 판정으로 직접 때린다.
  `SkillShape` 로 옮기면 "보스가 범위 공격을 예고하고 쓴다" 가 된다
- **파괴 가능 오브젝트** — `IDamageable` 은 준비됐고 `Destructible` 이 아직 없다
