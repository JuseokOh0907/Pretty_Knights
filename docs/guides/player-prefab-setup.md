# 플레이어 프리팹 · 애니메이터 조립 가이드

> 직접 조립하기 위한 절차서. 수치는 실측에서 나온 값이므로 그대로 쓰면 된다.
> 근거: `docs/decisions/001-animator-blend-tree.md`, `003-runtime-architecture.md`, `004-idle-state.md`

---

## 0. 미리 알아둘 수치

| 항목 | 값 | 근거 |
|---|---|---|
| 캐릭터 PPU | 256 | 임포트 설정 |
| 셀 크기 | 256 × 256 (트림 후 184 × 232) | 실측 |
| 접지선 | 셀 바닥에서 **32px** 위 | walk·run·idle 세 동작 모두 최하단이 일치 |
| **Visual 오프셋 Y** | **0.375** | (128 − 32) ÷ 256 |

`0.375` 는 트림 전후가 같다. 중심 대칭으로 자르므로 그림이 움직이지 않기 때문이다.

---

## 1. Animator Controller 만들기

`Assets/Art/Characters/` 에 `Knight.controller` 생성.
기존 방향별 컨트롤러 24개는 **지우지 말고 그대로 둔다** (통합이 검증된 뒤 정리).

### 1-1. 파라미터 3개

| 이름 | 타입 | 역할 |
|---|---|---|
| `MoveX` | Float | 바라보는 방향 X |
| `MoveY` | Float | 바라보는 방향 Y |
| `Speed` | Float | 이동 속도. **상태 전이는 이것만으로 가른다** |

> `MoveX` / `MoveY` 는 **0으로 돌아가지 않는다.** 멈춰도 마지막 방향을 유지한다.
> 2D Simple Directional 트리는 입력이 (0,0) 이면 8개 모션을 평균내 방향이 뭉개진다.
> `PlayerAnimatorDriver` 가 이 규칙을 지킨다.

### 1-2. 스테이트 3개 — 전부 블렌드 트리

`Idle` / `Walk` / `Run` 각각 아래를 반복한다.

1. Animator 창 빈 공간에서 **우클릭 → Create State → From New Blend Tree**
   - `Create State → Empty` 로 만들면 안 된다. Motion 필드는 기존 에셋을 고르는
     오브젝트 선택창이라 거기서 새 트리를 만들 수 없다.
2. 스테이트 이름을 `Idle` / `Walk` / `Run` 으로 바꾼다
3. 스테이트를 **더블클릭**해 트리 안으로 들어간다 (브레드크럼이 `Base Layer > Idle`)
4. 트리 노드를 선택하고 인스펙터에서 **Blend Type: `2D Simple Directional`**
5. Parameters 좌/우에 `MoveX` / `MoveY`
6. Motion 목록의 **`+` → Add Motion Field** 를 8번,
   또는 Project 창에서 **클립 8개를 다중 선택해 목록으로 드래그**
7. **Pos X / Pos Y 를 아래 표대로 직접 입력한다**

> 맨 아래 **Compute Positions 는 쓰지 않는다.**
> 클립 이름이나 루트 모션에서 좌표를 추정하는 기능인데
> 스프라이트 클립은 루트 모션이 없어 전부 (0,0) 으로 뭉갠다.

| # | 클립 접두어 | Pos X | Pos Y | 방향 |
|---|---|---|---|---|
| 1 | `Front` | 0 | −1 | 아래 |
| 2 | `Front-right` | 0.707 | −0.707 | 아래-오른쪽 |
| 3 | `Right` | 1 | 0 | 오른쪽 |
| 4 | `Back-right` | 0.707 | 0.707 | 위-오른쪽 |
| 5 | `Back` | 0 | 1 | 위 |
| 6 | `Back-left` | −0.707 | 0.707 | 위-왼쪽 |
| 7 | `Left` | −1 | 0 | 왼쪽 |
| 8 | `Front-left` | −0.707 | −0.707 | 아래-왼쪽 |

클립은 접두어 + `_Idle` / `_Walk` / `_Run` 이다. 예: `Back-right_Walk`.

**Idle 을 기본 스테이트로 지정한다** (우클릭 → Set as Layer Default State).

### 1-3. 전이

전부 **Has Exit Time 끄고**, Transition Duration `0.05`, Interruption Source `Current State`.

| 전이 | 조건 |
|---|---|
| Idle → Walk | `Speed` Greater `0.05` |
| Walk → Idle | `Speed` Less `0.05` |
| Walk → Run | `Speed` Greater `3.0` |
| Run → Walk | `Speed` Less `3.0` |

`3.0` 은 걷기 2.5 와 달리기 4.0(2.5 × 1.6) 사이 값이다. 속도를 바꾸면 여기도 바꾼다.

---

## 2. 프리팹 구조

`Assets/Prefabs/Player.prefab`

**게임오브젝트는 `Player` 와 `Visual` 둘뿐이다.** 나머지는 전부 컴포넌트다.

```
Player  (GameObject)                    ← 루트가 발밑이다
   │
   ├─ [C] Transform            (0, 0, 0)
   ├─ [C] Rigidbody2D          Dynamic / Gravity Scale 0 / Freeze Rotation Z 체크
   ├─ [C] CapsuleCollider2D    Offset (0, 0.12) / Size (0.35, 0.25)
   ├─ [C] CharacterMotor       Move Speed 2.5 / Acceleration 0.08
   ├─ [C] PlayerController     Input Actions: InputSystem_Actions
   │                           Walk Speed 2.5 / Run Multiplier 1.6
   │
   └─ Visual  (GameObject)               ← Player 의 직속 자식. 스프라이트 담당
         ├─ [C] Transform            (0, 0.375, 0)   ← 이 값이 핵심
         ├─ [C] SpriteRenderer       Sprite: 01_front (Idle 아무거나)
         ├─ [C] Animator             Controller: Knight / Apply Root Motion 끄기
         └─ [C] PlayerAnimatorDriver
```

`[C]` 는 컴포넌트다. `PlayerController` 를 자식 오브젝트로 빼면 안 된다 —
내부에서 `GetComponent<CharacterMotor>()` 로 **같은 오브젝트의** 모터를 찾기 때문에
자식에 있으면 모터를 못 찾아 이동 입력이 전달되지 않는다.

`Animator` 가 `Visual` 에 있어야 하는 이유도 같다.
클립이 `m_Sprite` 를 **경로 없이**(`path:` 가 빈 값) 애니메이션하므로
`SpriteRenderer` 와 반드시 같은 오브젝트여야 한다.

**왜 스프라이트를 자식으로 두는가**
스프라이트 피벗이 Center 라 루트에 바로 붙이면 캐릭터 몸통 한가운데가 좌표 원점이 된다.
그러면 콜라이더·스폰 위치·스킬 거리 계산이 전부 공중 기준이 된다.
피벗을 재임포트로 바꾸면 클립 참조가 흔들리므로, **구조로 해결한다.**
루트를 발밑에 두고 그림만 `0.375` 올리면 임포트 설정을 하나도 안 건드리고 끝난다.

### 컴포넌트 연결

`PlayerController` 의 인스펙터에서:
- `Input Actions` ← `Assets/InputSystem_Actions.inputactions`
- `Motor` ← 루트의 `CharacterMotor` (비워두면 자동으로 찾는다)
- `Animator Driver` ← `Visual` 의 `PlayerAnimatorDriver` (비워두면 자식에서 자동으로 찾는다)

---

## 3. 확인

1. 씬에 프리팹을 놓고 재생
2. WASD 로 8방향 이동 — 각 방향에서 **바라보는 그림이 맞는지**
3. 손을 떼면 **그 방향 그대로 Idle** 이 되는지 (엉뚱한 방향을 보면 latch 가 깨진 것)
4. Shift 로 달리기 전환이 되는지
5. 이동 중 **발밑이 위아래로 흔들리지 않는지**

3번이 `MoveX/MoveY` latch 가 동작하는지 가르는 지점이고,
5번이 트림 결과를 가르는 지점이다.

---

## 4. 아직 정하지 않은 것

- **Y-소팅 기준점.** `SpriteRenderer` 가 `Visual`(발밑 +0.375)에 있으므로
  정렬 기준도 그 위치가 된다. 캐릭터끼리는 오프셋이 같아 순서가 맞지만,
  프롭·타일과의 상대 순서는 미결정 #2(카메라·투영·가림 규칙)가 확정된 뒤에 맞춘다.
- 방향별 컨트롤러 24개 정리 시점 — 블렌드 트리 통합이 검증된 뒤.
