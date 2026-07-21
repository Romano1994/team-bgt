---
name: standard-refactor
description: standard-verify가 만든 docs/verifier 보고서를 읽어 표준 위반 항목을 bgt-fe-implementer·bgt-be-implementer에게 위임해 수정한다. 고칠 ID를 명시하면 그것만, 미지정이면 전체를 수정한다. Use when user wants to fix or refactor findings from a standard-verify report, mentions "표준 수정", "리펙토링", "보고서 반영", "standard refactor", or names finding IDs (FE2, BE3) to fix.
---

# BGT 표준 수정 (보고서 기반)

`/standard-verify`가 만든 `docs/verifier` 보고서의 위반 항목을 implementer에게 위임해 고친다. **재검증은 하지 않는다** — 다시 확인하려면 사용자가 `/standard-verify`를 재호출한다.

## 파라미터

| 파라미터 | 설명 | 예시 |
|---------|------|------|
| `screen` | 화면 path (보고서 탐색용) | `bgt/cm/cntrct-bid/cntrct-bid-dtl` |
| `ids` | (선택) 고칠 지적 ID 목록 | `FE2 BE3 BE4` |

`ids` 미지정 = **보고서 전체 항목** 수정. `screen` 없으면 요청한다.

## Phase 0: 보고서 탐색·선택

1. `leaf` = `screen`의 마지막 세그먼트. `Glob`으로 `docs/verifier/{leaf}-*.md` 중 **최신 timestamp** 파일을 읽는다. 없으면 "먼저 `/standard-verify {screen}` 실행 필요"를 안내하고 멈춘다.
2. 대상 항목 확정: `ids`가 있으면 그 ID만, 없으면 FE/UIUX/BE 전 항목. `없음`/`해당 없음` 섹션은 건너뛴다.
3. 항목을 도메인별로 분류: `FE#`·`UI#` → FE 묶음, `BE#` → BE 묶음.

## Phase 1: implementer 병렬 위임

FE 묶음과 BE 묶음은 독립적이므로 **한 메시지에서 병렬** 디스패치한다. 한쪽이 비면 그쪽은 생략한다.

- **`bgt-fe-implementer`** — FE/UIUX 항목. 각 항목의 `파일:줄`·문제·제안을 그대로 전달한다. 보고서에 적힌 최소 수정만 하고 범위 밖은 손대지 말 것을 명시. 완료 후 자체 게이트(`yarn build:local` + `npx tsc --noEmit`) 통과.
- **`bgt-be-implementer`** — BE 항목. `docs/sp` 배포 SP 시그니처·커서 alias 기준으로 고친다(alias 통일은 SP↔resultMap↔VO↔JSON 키를 한 값으로). 완료 후 자체 게이트(`gradlew.bat test`) 통과.

**수정 불가 가드**: UIUX `③`(라이브 확인 필요)처럼 **소스만으로 판정·수정 불가한 항목은 임의로 바꾸지 않는다**. implementer는 해당 항목을 `수정 불가—라이브 확인 필요`로 반환한다. `②`(soft 권장)는 보고서 제안대로 반영하되 판단이 필요하면 근거를 남긴다.

## Phase 2: 결과 보고 (채팅)

수정을 마치면 채팅으로 간략히 보고한다(파일 안 씀 — 보고서는 검사 이력으로 유지).

- 수정 완료: `ID → 파일:줄` 목록
- 미수정: `ID → 사유`(수정 불가/게이트 실패 등)
- 게이트 결과(FE tsc/build, BE test)
- 재검사가 필요하면 `/standard-verify {screen}` 재실행 안내
