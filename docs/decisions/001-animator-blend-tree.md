# 001. 방향 애니메이션은 블렌드 트리로 구성한다

- **상태:** 확정
- **결정일:** 2026-08-01

## 배경

`Assets/Art/Characters/Animation_Knights-Walking/Animator/` 에는 걷기 동작 하나에
Animator Controller가 **8개** 존재한다. 방향마다 하나씩이다.

```
01_front_walk_8x1_1.controller
02_front_faces-screen-right_walk_8x1_0.controller
...
08_front_faces-screen-left_walk_8x1_0.controller
```

이 구조는 동작이 늘어날 때 컨트롤러 수가 `동작 × 8` 로 증가한다.
계획된 동작만 세어도 걷기·달리기·공격·스킬 3종·피격 = 8개 동작이므로
그대로 두면 **64개 컨트롤러**가 된다. 상태 전이 규칙도 컨트롤러마다 중복 작성해야 한다.

## 결정

**캐릭터당 Animator Controller는 하나로 통합하고, 방향은 블렌드 트리 파라미터로 처리한다.**

- 방향 파라미터: `MoveX`, `MoveY` (float) 를 사용한 2D 블렌드 트리
  - 또는 `Direction` (int, 0~7) 단일 파라미터 방식
  - 8방향 스프라이트가 이산적이므로 **2D Simple Directional** 이 자연스럽다
- 상태 파라미터: `Speed` (float), `IsAttacking` / `AttackTrigger` (trigger), `IsHit` 등
- 동작 추가 = 상태 하나 추가. 컨트롤러 개수는 늘지 않는다.

## 이유

- 신규 동작 1개 추가 비용이 컨트롤러 8개 → 상태 1개로 줄어든다.
- 상태 전이 규칙을 한 곳에서만 관리한다.
- 스크립트가 방향별 컨트롤러를 교체할 필요 없이 파라미터만 갱신하면 된다.
- 지금 스크립트가 전혀 없으므로 **재작업 비용이 가장 싼 시점이다.**

## 영향 범위

- 기존 `Animator/` 아래 8개 컨트롤러는 통합 컨트롤러로 대체된다.
  기존 `.anim` 클립 8개는 **그대로 재사용**한다 (블렌드 트리의 모션 슬롯에 배치).
- Running은 클립·Animator가 아직 없으므로 처음부터 통합 컨트롤러에 편입한다.
- 방향 판정 로직은 Input System의 이동 벡터에서 파생시킨다.

## 후속 확정 (2026-08-01, 실동작 검증 완료)

**방향 파라미터는 `MoveX` / `MoveY` float 두 개를 쓰고, 값이 0으로 돌아가지 않는다.**

`Direction` int 방식은 채택하지 않았다. 2D Simple Directional 트리를 그대로 쓰려면
방향이 벡터여야 하고, int 로 가면 블렌드 트리 대신 코드에서 상태를 골라야 한다.

정지 시 마지막 방향 유지는 **별도 파라미터를 두지 않고 latch 방식**으로 해결했다.

| 파라미터 | 규칙 |
|---|---|
| `MoveX` / `MoveY` | 입력이 0이 되어도 **마지막으로 향했던 방향을 유지**한다. (0,0) 이 절대 들어가지 않는다 |
| `Speed` | 실제 이동 속도. **상태 전이는 이것만으로 가른다** |

`LastMoveX/LastMoveY` 를 따로 캐싱하면 Idle 트리와 Walk 트리가 서로 다른 파라미터를
봐야 해서 트리마다 배선이 갈라진다. latch 방식은 세 트리가 같은 파라미터를 공유한다.

구현: `Assets/Scripts/Characters/PlayerAnimatorDriver.cs`
조립 절차: `docs/guides/player-prefab-setup.md`

검증됨 — 8방향 전부에서 손을 떼도 바라보던 방향이 유지된다.

## 미해결

- 공격·스킬 중 방향 고정 규칙 (`ForceFacing` 을 언제 걸고 언제 풀 것인가)
- 방향별 컨트롤러 24개 제거 시점
