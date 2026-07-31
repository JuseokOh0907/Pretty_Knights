# HANDOFF — Cowork 세션 → Claude Code 인수인계

> **작성일:** 2026-08-01
> **작성 주체:** Claude (Cowork 데스크톱 세션)
> **수신 대상:** Claude Code

---

## ⚠️ 이 파일의 처리 방법 (Claude Code 필독)

이 파일은 **일회성 인수인계 문서**입니다. 다음 절차를 반드시 수행하십시오.

1. 이 문서를 끝까지 읽는다.
2. 아래 내용 중 **지속적으로 유지되어야 할 사실·결정·컨벤션**을
   `CLAUDE.md` 또는 `docs/` 아래 적절한 문서에 반영한다.
   (대부분은 이미 반영되어 있으므로, 누락분만 보완하면 된다.)
3. 반영이 끝나면 **이 파일을 삭제한다.**

   ```bash
   git rm HANDOFF.md
   git commit -m "chore: HANDOFF.md 흡수 후 제거"
   ```

4. 사용자에게 "HANDOFF.md 내용을 인수하고 삭제했습니다" 라고 한 줄로 보고한다.

이 파일이 저장소에 남아 있으면 안 됩니다. 인수 즉시 제거하십시오.

---

## 1. 이 세션에서 무슨 일이 있었나

사용자(Juseok)가 모바일 2D 헌팅 RPG 기획서(`game_design_plan_EN.md`)를 공유했고,
그 기획을 실제 Unity 프로젝트와 연결하는 작업을 시작했습니다.
작업 주체를 Cowork 세션에서 **Claude Code로 이관하기 위해** 이 저장소가 정비되었습니다.

이 세션에서 실제로 수행된 것:

- Unity 프로젝트 실경로 확인 및 접근
- 프로젝트 구조·패키지·씬·에셋 현황 조사
- Unity용 `.gitignore` 작성
- `CLAUDE.md` 작성 (프로젝트 가이드)
- 기획서를 `docs/game_design_plan_EN.md` 로 저장소에 편입
- `docs/decisions/OPEN_DECISIONS.md` 작성 (미결정 9건 추적)
- git 저장소 초기화 및 최초 커밋

---

## 2. 이름과 경로

**프로젝트명은 `Pretty_Knights` 로 통일되었습니다 (2026-08-01 확정).**

작업 초기에 `Gamble_Yuusha` 라는 이름이 쓰였으나, GitHub 저장소명에 맞춰 정리했습니다.
**대화 기록이나 오래된 문서에 등장하는 `Gamble_Yuusha` 는 전부 폐기된 이름입니다.**

```
저장소     https://github.com/JuseokOh0907/Pretty_Knights
로컬 경로   C:\Git\Pretty_Knights
productName Pretty_Knights
```

> 로컬 폴더명 변경은 사용자가 Unity를 종료한 상태에서 직접 수행합니다.
> 인수 시점에 폴더가 아직 `C:\Git\Gamble_Yuusha` 라면 변경 여부를 확인하십시오.

`C:\Git` 은 사용자의 프로젝트 루트 폴더이며, 형제 프로젝트로
Diggers, Ghost_GomokuKing, HellEscape, PlanetDigger, ToMeetAlice 등이 있습니다.
GitHub 계정은 **`JuseokOh0907`** 입니다 (팀 프로젝트는 `likelion-ugm-07-final` 조직 사용).

---

## 3. 조사로 드러난 프로젝트 실태

기획서에 적힌 내용과 실제 저장소 상태 사이의 갭입니다. **이것이 이 인수인계의 핵심 정보입니다.**

### 3.1 코드가 전혀 없다

`Assets/Scripts/` 디렉터리가 존재하지 않습니다. C# 스크립트가 단 하나도 없습니다.
현재 이 프로젝트는 **아트 에셋 저장소에 가깝습니다.**
따라서 다음 단계는 아키텍처 설계부터 시작해야 합니다.

### 3.2 캐릭터 아트 현황

| 항목 | 상태 |
|---|---|
| Knights 걷기 8방향 PNG | ✅ 8장 |
| Knights 걷기 Animation Clip | ✅ 8개 |
| Knights 걷기 Animator Controller | ⚠️ **8개** — 방향마다 별도 컨트롤러 |
| Knights 달리기 8방향 PNG | ✅ 8장 |
| Knights 달리기 클립 / Animator | ❌ 없음 |
| 공격 · 스킬 · 피격 모션 | ❌ 없음 |

**⚠️ 이미 결정된 사항:** 걷기 애니메이션이 방향당 하나씩 총 8개의 Animator Controller로 분리되어 있습니다.
동작이 늘어나면 `동작 × 8` 로 증가해(계획된 8개 동작 기준 64개) 관리가 무너집니다.

사용자와 협의하여 **단일 컨트롤러 + 블렌드 트리 방식으로 재구성하기로 확정했습니다.**
상세는 `docs/decisions/001-animator-blend-tree.md` 를 참조하십시오.
기존 `.anim` 클립 8개는 그대로 재사용하고, 컨트롤러만 통합합니다.

### 3.3 몬스터 아트가 없다

`Maps/` 아래의 Goblin / Orc / Vampire 는 **맵 테마**이지 몬스터가 아닙니다.
몬스터 스프라이트는 하나도 없습니다. 기획서 §12가 "급한 컨텐츠 작업"으로 지목한 항목입니다.

### 3.4 스킬 VFX가 없다

기획서 §15의 3번 항목(전방 베기 / 관통 직선 / 광역 폭발)은 착수 전입니다.
판정 시스템도, VFX도 존재하지 않습니다.

### 3.5 씬은 껍데기일 가능성이 높다

`Title_Scene` / `Ingame_Vertical` / `Ingame_Horizontal` 세 씬이 생성되어 있으나
스크립트가 없으므로 실제 로직은 없습니다. 내용 확인이 필요합니다.

### 3.6 폴더명 오타 — 해결됨

`Assets/Art/Maps/Vampire/TIles` (대문자 I) 오타가 있었으나 사용자가 Unity 에디터에서 수정했습니다.
현재 Goblin / Orc / Vampire 세 테마 모두 `Tiles` 로 정상이며 원격에도 반영되어 있습니다.
추가 조치 불필요.

### 3.7 `Assets/Art/Maps/Base` 는 비어 있음

용도가 정의되지 않은 빈 폴더입니다. 사용자에게 의도를 확인하십시오.

---

## 4. 기술 스택 확인 결과

```
Unity        6000.3.20f1 (Unity 6)
렌더 파이프라인  URP 17.3.0 + 2D Renderer
입력          Input System 1.19.0 (activeInputHandler: 1 — 신규 전용)
화면 방향      defaultScreenOrientation: 4 (AutoRotation), 4방향 전부 허용
productName   Pretty_Knights
companyName   DefaultCompany  ← 출시 전 변경 필요 (미해결)
```

2D 관련 패키지가 폭넓게 설치되어 있습니다: Aseprite Importer, PSD Importer,
2D Animation, SpriteShape, Tilemap Extras.
**Aseprite Importer가 있다는 점**은 스프라이트 파이프라인을 PNG 수동 슬라이싱 대신
`.aseprite` 원본 직접 임포트로 바꿀 여지가 있다는 뜻입니다. 사용자에게 원본 파일 보유 여부를 확인할 가치가 있습니다.

---

## 5. 사용자가 이 세션에서 내린 결정

- 작업을 Cowork에서 **Claude Code로 이관**한다.
- 커밋 범위는 **전체 포함** — `Assets/Art` 전부를 저장소에 넣는다.
  (`Library/`, `Temp/`, `Logs/`, `UserSettings/` 만 `.gitignore` 로 제외)
- `.gitignore` 는 원격에 있던 **GitHub 공식 Unity 템플릿을 채택**하고 에디터·OS 규칙만 추가했다.
- 프로젝트명은 **`Pretty_Knights` 로 통일**한다 (`Gamble_Yuusha` 폐기).
- 방향 애니메이션은 **블렌드 트리 방식**으로 간다 (`docs/decisions/001-animator-blend-tree.md`).
- 파일 삭제가 필요한 작업은 **반드시 사용자 승인을 먼저 받는다.**

---

## 6. 확정되지 않은 것

기획서 §14의 9개 항목이 전부 미결입니다. `docs/decisions/OPEN_DECISIONS.md` 에 표로 정리되어 있습니다.
그중 **당장 코드 작성을 막는 것**은 다음 세 가지입니다.

1. **#2 카메라·타일 투영·충돌·가림 규칙** — 타일맵 그리드와 Y-소팅 구현이 여기 걸립니다.
2. **#8 세로/가로 기준 해상도 및 전환 동작** — 씬/캔버스 구조가 여기 걸립니다.
3. **#3 스탯 목록과 공식** — 캐릭터 데이터 모델이 여기 걸립니다.

Claude Code에서 첫 작업을 시작하기 전에 이 세 가지를 사용자와 먼저 정리하는 것을 권합니다.

---

## 7. 인수 후 권장 첫 행동

1. `CLAUDE.md` 를 읽고 내용에 동의하는지 사용자에게 확인받는다.
2. 위 §6의 미결정 3건을 사용자와 정리한다.
3. `Assets/Scripts/` 아키텍처를 설계한다.
   핵심 제약: **세로 자동 사냥과 가로 직접 플레이가 동일한 캐릭터 데이터를 공유해야 한다** (기획서 §15-6).
   즉 캐릭터 스탯·스킬·장비·인벤토리는 씬에 종속되지 않는 계층에 있어야 합니다.
4. 방향 애니메이션을 단일 Animator + 블렌드 트리로 재구성한다 (이미 확정된 방침).
5. **이 파일을 삭제한다.**

---

## 8. 남은 잡무

- 로컬 폴더명 `C:\Git\Gamble_Yuusha` → `C:\Git\Pretty_Knights` 변경 (Unity 종료 후 사용자가 수행)
- `companyName` 이 `DefaultCompany` 상태 — 출시 전 변경 필요
- `Assets/Art/Maps/Base` 가 빈 폴더 — 용도 확인 필요
- 씬 3개의 실제 내용 확인 (비어 있을 가능성 높음)
