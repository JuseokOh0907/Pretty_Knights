# UI 루트 · 모드 전환 버튼 설정 가이드

> 모바일이므로 세로 ↔ 가로 전환은 버튼으로 한다.
> UI 는 **Boot 씬에 상주**시키고 게임플레이 씬이나 플레이어 프리팹에 두지 않는다.

---

## 왜 Boot 에 두는가

| | 플레이어 프리팹 | 씬마다 | **Boot 상주** |
|---|---|---|---|
| 씬 전환 시 | **파괴됨** | **언로드됨** | 살아남음 |
| 캔버스 관리 | 1벌 | **2벌** | 1벌 |
| 카메라 연동 | 필요 | 필요 | **불필요** |

모드 전환 버튼은 **전환을 요청하는 주체**다. 그런데 전환되는 씬 안에 있으면
자기가 언로드되면서 사라진다. 플레이어 프리팹도 씬마다 새로 생기므로 같은 문제다.

`GameRoot` 와 같은 상주 계층에 두는 것이 구조적으로 맞다.

## 1. UIRoot 만들기

Boot 씬에 빈 GameObject `UIRoot` 를 만들고, 그 **자식**으로 Canvas 를 만든다.

```
UIRoot  (GameObject)                 ← [C] UIRoot 스크립트
   └─ Canvas  (GameObject)
        ├─ [C] Canvas          Render Mode: Screen Space - Overlay
        ├─ [C] CanvasScaler    UI Scale Mode: Scale With Screen Size
        ├─ [C] GraphicRaycaster
        └─ ModeSwitchButton  (Button)
```

**`Screen Space - Overlay` 를 반드시 쓴다.**
`Screen Space - Camera` 는 카메라 참조를 요구하는데, 씬마다 카메라가 바뀌므로
전환할 때마다 다시 물려줘야 하고 놓치면 UI 가 통째로 안 보인다.
Overlay 는 그 문제가 없고 모바일 성능 차이도 없다.

`UIRoot` 컴포넌트의 `Canvas` / `Scaler` 는 비워도 자식에서 자동으로 찾는다.

| 필드 | 기본값 |
|---|---|
| Portrait Reference | `1080 × 1920` |
| Landscape Reference | `1920 × 1080` |
| Portrait Match | `0` (너비 기준) |
| Landscape Match | `1` (높이 기준) |
| Portrait Only / Landscape Only | 모드별로 켜고 끌 오브젝트 (선택) |

모드가 바뀔 때 `UIRoot` 가 기준 해상도와 Match 를 자동으로 갈아 끼운다.
버튼 위치가 방향에 따라 크게 달라져야 하면, 패널 두 개를 만들어
`Portrait Only` / `Landscape Only` 에 넣으면 된다. 캔버스와 EventSystem 은 하나로 유지된다.

## 2. EventSystem — 여기서 제일 많이 막힌다

Boot 씬에 **EventSystem 을 하나만** 만든다 (`GameObject > UI > Event System`).

> ⚠️ **기본으로 붙는 `Standalone Input Module` 은 이 프로젝트에서 동작하지 않는다.**
>
> `activeInputHandler: 1` 로 신규 Input System 전용이기 때문이다.
> 인스펙터에 "Replace with InputSystemUIInputModule" 버튼이 뜨면 눌러서 교체한다.
> 안 뜨면 `Standalone Input Module` 을 지우고
> **`Input System UI Input Module`** 을 직접 추가한다.
>
> 이걸 놓치면 **버튼이 아무 반응도 하지 않는다.** 에러도 안 난다.

EventSystem 도 `UIRoot` 아래 두어 함께 상주시킨다.
씬마다 만들면 중복 경고가 뜬다 — Boot 의 카메라를 지웠던 것과 같은 문제다.

## 3. 버튼

Canvas 아래에 `GameObject > UI > Button` 을 만들고 `ModeSwitchButton` 을 붙인다.
**연결할 것이 없다.** 지금 모드의 반대쪽으로 알아서 전환한다.

| 필드 | 용도 |
|---|---|
| To Landscape Icon | 가로로 갈 수 있을 때(=지금 세로) 켜지는 오브젝트 |
| To Portrait Icon | 세로로 갈 수 있을 때 켜지는 오브젝트 |

둘 다 선택이다. 아이콘이나 텍스트를 각각 넣어 두면 버튼이 알아서 바꿔 준다.

전환 중에는 버튼이 **스스로 비활성화**된다. 연타로 씬 전환이 중첩되면 꼬인다.

## 4. 확인

1. Boot 에서 재생 → 버튼이 보이고 눌리는지
2. 누르면 콘솔에 `[GameRoot] 씬 전환 완료 — Vertical` / `Horizontal` 이 번갈아 뜨는지
3. 전환 중 연타 → 버튼이 회색으로 죽는지
4. 방향이 바뀌면 UI 크기가 어색하지 않은지 (Game 뷰 해상도를 세로/가로로 바꿔 확인)

## 5. 알아두실 제약

**모드별 위치는 따로 저장되지 않는다.**
`WorldLocation` 이 위치를 하나만 들고 있어서, 가로에서 세로로 갔다가 돌아오면
가로에서 서 있던 자리가 아니라 씬의 기본 위치에서 시작한다.

레벨·경험치·HP 는 그대로 유지된다 (버튼이 전환 직전에 저장한다).
모드별 위치가 필요해지면 `WorldLocation` 을 모드당 하나씩 두도록 바꾸면 된다.
