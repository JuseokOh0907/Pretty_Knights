# 타일맵 콜라이더 설정 가이드

> 벽은 막고 바닥은 통과시키는 구성. 모바일 물리 비용까지 고려한 절차서.

---

## 1. 두 가지가 함께 있어야 한다

| | 어디에 | 역할 |
|---|---|---|
| **`Collider Type`** | **Tile 에셋** (`.asset` 파일) | 이 타일이 충돌을 만들지, 어떤 모양으로 만들지 |
| **`TilemapCollider2D`** | Tilemap **게임오브젝트의 컴포넌트** | 실제 물리 형상을 생성하는 주체 |

`TilemapCollider2D` 가 칸마다 놓인 타일의 `Collider Type` 을 읽어 형상을 만든다.

- 컴포넌트 없이 Tile 에셋만 `Sprite` 로 두면 → **아무것도 안 막힌다**
- 컴포넌트만 붙이고 타일이 전부 `None` 이면 → **아무것도 안 막힌다**
- 컴포넌트 + 타일이 `Sprite`/`Grid` → 막힌다

## 2. Collider Type 세 가지

Tile 에셋을 Project 창에서 선택하면 인스펙터에 나온다.

| 값 | 동작 | 쓰는 곳 |
|---|---|---|
| **None** | 충돌 없음 | **바닥, 장식** |
| **Sprite** | 스프라이트의 알파 외곽선을 따라감 | 모양이 불규칙한 프롭 |
| **Grid** | 칸 전체를 꽉 찬 사각형으로 | **벽** |

**벽은 `Grid` 를 권한다.** `Sprite` 는 외곽선을 따라가서 벽 가장자리가 미세하게
울퉁불퉁해지고 캐릭터가 걸린다. `Grid` 는 형상이 단순해 물리 비용도 싸다.

> 스프라이트를 타일맵에 끌어다 만든 타일은 기본값이 `Sprite` 다.
> **바닥 타일을 그대로 두면 바닥 전체가 벽이 된다.** 이게 "투명벽" 증상의 정체다.

## 3. 레이어 구조

```
Grid
├─ Field   (Tilemap + TilemapRenderer)                 바닥 — 타일 Collider Type: None
├─ Guide   (Tilemap + TilemapRenderer                  벽 — 타일 Collider Type: Grid
│           + TilemapCollider2D
│           + Rigidbody2D  [Body Type: Static]
│           + CompositeCollider2D)
└─ Deco    (Tilemap + TilemapRenderer)                 장식 — None
```

`TilemapCollider2D` 는 **벽 레이어에만** 붙인다. 바닥 레이어에는 붙이지 않는다.
타일 쪽을 `None` 으로 해두면 붙어 있어도 무해하지만, 애초에 안 붙이는 편이 명확하다.

## 4. CompositeCollider2D — 모바일에서 이게 핵심

`TilemapCollider2D` 만 쓰면 **타일 한 칸마다 콜라이더 형상이 하나씩** 생긴다.
18 × 10 맵이면 벽만 해도 수십 개, 지역이 커지면 수백 개가 된다.
모바일에서 물리 갱신 비용이 여기서 붙는다.

`CompositeCollider2D` 를 얹으면 **맞닿은 타일들이 하나의 큰 폴리곤으로 합쳐진다.**
벽 한 줄이 형상 1개가 된다.

### 붙이는 순서

1. 벽 Tilemap 오브젝트 선택
2. `TilemapCollider2D` 추가
3. `CompositeCollider2D` 추가
   → `Rigidbody2D` 가 **자동으로 함께 붙는다**
4. 그 `Rigidbody2D` 의 **Body Type 을 `Static`** 으로 바꾼다
   (기본값 Dynamic 이면 벽이 물리로 밀려 떠내려간다)
5. `TilemapCollider2D` 의 **`Used By Composite` 체크**
   → 이걸 안 켜면 합쳐지지 않고 그냥 두 개가 따로 논다
6. `CompositeCollider2D` 의 `Geometry Type` 은 `Outlines` 로 둔다

### 확인

Scene 뷰에서 벽을 보면 초록 외곽선이 **칸마다 격자로 보이면 실패**,
**벽 덩어리 바깥쪽만 한 줄로 감싸면 성공**이다.

## 5. 자주 걸리는 것

| 증상 | 원인 |
|---|---|
| 캐릭터가 아예 못 움직임 (투명벽) | 바닥 타일 Collider Type 이 `Sprite` |
| 벽이 밀려서 떠내려감 | `CompositeCollider2D` 가 붙인 `Rigidbody2D` 가 Dynamic |
| 합쳐지지 않고 칸마다 콜라이더 | `Used By Composite` 미체크 |
| 벽 모서리에 걸림 | 벽 타일이 `Sprite` — `Grid` 로 바꾼다 |
| 타일을 지웠는데 충돌이 남음 | 재생 중 변경. 정지 후 다시 확인 |

## 6. Tile 에셋은 공유된다

`Collider Type` 은 **Tile 에셋 단위**다. 같은 타일을 바닥에도 쓰고 벽에도 쓰면
둘이 같은 설정을 공유한다. 같은 그림을 통과 가능/불가능으로 나눠 쓰려면
**Tile 에셋을 두 개 만들어야 한다** (같은 스프라이트를 참조하는 별개 에셋).

---

## 미결정과의 관계

충돌 영역과 가림 규칙은 미결정 #2(카메라·타일 투영·충돌·가림)에 걸려 있다.
특히 **벽 위쪽 가림 아트와 지면 충돌 영역을 분리**하는 문제
(`CLAUDE.md` §4 "높은 오브젝트는 지면 충돌 영역과 상단 가림 아트를 분리한다")는
이 가이드 범위 밖이며 #2 가 확정된 뒤에 다룬다.
