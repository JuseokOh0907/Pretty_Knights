# 화면 조이스틱 설정 가이드

> **코드를 쓸 필요가 없다.** Input System 에 `On-Screen Stick` 이 들어 있고,
> `Move` 액션에 `<Gamepad>/leftStick` 바인딩이 이미 있다.
> 이미지만 나중에 갈아 끼우면 된다.

---

## 왜 조이스틱인가

이동 속도를 **스틱 기울기**로 정하기로 했다 (`PlayerController`).

```
끝까지 밀면  4.0  →  Run
절반쯤 밀면  2.4  →  Walk
살짝 밀면    0.4  →  Walk (아주 느리게)
```

D-Pad 나 버튼은 입력 크기가 항상 1이라 이 구조가 성립하지 않는다.
걷기를 페널티가 아니라 **선택**으로 만들려면 아날로그 입력이어야 한다.

## 1. 계층

조이스틱은 **가로 모드에서만** 쓴다. 세로는 자동 사냥이라 직접 조작이 없다.

```
UIRoot
 └─ Canvas
     ├─ ModeSwitchButton
     └─ Controls  (GameObject)              ← UIRoot 의 Landscape Only 에 등록
          └─ JoystickArea  (GameObject)
               ├─ [C] RectTransform
               ├─ [C] Image                  배경 원. Raycast Target 켬
               │
               └─ Handle  (GameObject)
                    ├─ [C] RectTransform
                    ├─ [C] Image             손잡이. Raycast Target 켬
                    └─ [C] On-Screen Stick   ← 여기에 붙인다
```

> **`On-Screen Stick` 은 배경이 아니라 손잡이에 붙인다.**
> 이 컴포넌트가 자기 `RectTransform` 을 직접 움직이기 때문이다.

만든 뒤 `Controls` 를 `UIRoot` 컴포넌트의 **`Landscape Only`** 배열에 넣는다.
그러면 세로로 전환할 때 자동으로 꺼진다.

### `Controls` 는 빈 컨테이너다 — 만드는 법에 주의

캔버스 안의 오브젝트는 **`RectTransform`** 이어야 한다.
`GameObject > Create Empty` 로 만들면 일반 `Transform` 이 붙는 경우가 있고,
그러면 자식 UI 의 앵커 계산이 어긋난다.

**확실한 방법: `GameObject > UI > Image` 로 만들고 이름을 바꾼 뒤 `Image` 컴포넌트를 제거한다.**
`RectTransform` 만 남은 순수 컨테이너가 된다.

> Canvas 를 우클릭해 `Create Empty` 를 하면 Unity 가 `RectTransform` 을 붙여 주기도 한다.
> 만든 뒤 **인스펙터 맨 위가 `Rect Transform` 인지** 확인하고, `Transform` 이면 위 방법으로 다시 만든다.

**지금 단계에서는 생략해도 된다.** `Controls` 는 공격·스킬 버튼이 늘어날 때
한 번에 껐다 켜기 위한 묶음이다. 조이스틱 하나뿐이라면
`JoystickArea` 를 `Landscape Only` 에 직접 넣어도 동작은 같다.

## 2. On-Screen Stick 설정

| 필드 | 값 |
|---|---|
| **Control Path** | **`<Gamepad>/leftStick`** |
| Movement Range | `100` (손잡이가 움직일 수 있는 반경, 픽셀) |
| Behaviour | 아래 참조 |

`Control Path` 는 드롭다운에서 `Gamepad > Left Stick` 을 고르면 된다.
**이 경로가 `Move` 액션의 기존 바인딩과 맞물려 별도 배선이 필요 없다.**

### Behaviour 선택

| 값 | 동작 | 언제 |
|---|---|---|
| `Relative Position With Static Origin` | 스틱이 제자리에 고정 | 기본값. 위치가 눈에 보여 초보자에게 안전 |
| **`Exact Position With Dynamic Origin`** | **누른 자리에 스틱이 생김** | 엄지 위치를 안 봐도 되어 모바일에서 편하다 |

동적 원점을 쓰면 `Dynamic Origin Range` 로 반응 영역을 정한다.
이 경우 `JoystickArea` 를 **화면 왼쪽 절반만큼 크게** 잡고 이미지 알파를 0으로 두면
어디를 눌러도 그 자리에 스틱이 뜬다.

처음에는 고정 원점으로 감을 잡고, 손에 익으면 동적으로 바꾸는 편을 권한다.

## 3. 배치 — 앵커를 모서리에 박는다

모드 전환 버튼과 같은 문제가 여기서도 생긴다.
기준 해상도가 모드마다 바뀌므로 **중앙 앵커면 위치가 밀린다.**

`JoystickArea` 선택 → `RectTransform` 앵커 프리셋 →
**`Alt` + `Shift` 를 누른 채 좌측 하단** 클릭.

```
Anchors  Min (0, 0)   Max (0, 0)
Pivot    (0, 0)
Pos X    220
Pos Y    220
Width    320
Height   320
```

왼손 엄지가 닿는 자리다. 오른손잡이 기준이며, 나중에 좌우 반전 옵션을 넣을 수 있다.

## 4. 확인

**에디터에서도 마우스 드래그로 동작한다.** 실기가 없어도 검증할 수 있다.

1. Boot 에서 재생 → 가로 모드로 진입
2. 조이스틱을 드래그 → 캐릭터가 그 방향으로 이동
3. **살짝만 밀어보기** → 걷기 애니메이션
4. **끝까지 밀어보기** → 달리기 애니메이션
5. 손을 떼기 → 마지막 방향을 유지한 채 Idle
6. 모드 전환 버튼으로 세로로 → **조이스틱이 사라지는지**

3·4번이 이번 설계의 핵심이다. 기울기에 따라 Walk/Run 이 갈리지 않으면
`PlayerController` 의 `Move Speed` 가 달리기 상한으로 들어가지 않은 것이다.

## 5. 자주 걸리는 것

| 증상 | 원인 |
|---|---|
| 드래그해도 아무 반응 없음 | `EventSystem` 의 Input Module 이 `Standalone` 이다. `Input System UI Input Module` 로 교체 |
| 스틱은 움직이는데 캐릭터가 안 감 | `Control Path` 가 `<Gamepad>/leftStick` 이 아니다 |
| 손잡이가 안 움직임 | `On-Screen Stick` 을 배경에 붙였다. 손잡이로 옮긴다 |
| 터치가 안 먹힘 | `Image` 의 `Raycast Target` 이 꺼져 있다 |
| 세로에서도 보임 | `Controls` 를 `UIRoot` 의 `Landscape Only` 에 안 넣었다 |
| 항상 달리기만 됨 | 키보드로 테스트 중이다. 키보드는 입력 크기가 항상 1이라 정상 |

## 6. 아직 없는 것

- **세이프 에리어** — 노치·둥근 모서리 기기에서 좌하단이 가려질 수 있다
- 좌우 반전 옵션 (왼손잡이)
- 공격 버튼 — 스킬 판정 시스템이 붙은 뒤
