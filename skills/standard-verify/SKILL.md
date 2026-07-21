---
name: standard-verify
description: BGT 화면 하나를 개발 표준·UIUX 표준·과잉설계 관점에서 정적 검사하고 docs/verifier에 간략 보고서를 생성한다. 기존 bgt-fe-verifier·bgt-be-verifier·uiux-verifier를 화면 범위로 디스패치해 결과를 ID 태그로 합친다. Use when user wants to inspect or verify a BGT screen against dev/UIUX standards, mentions "표준 검사", "화면 검사", "검증 보고서", "standard verify", or gives a screen path/menu path to check.
---

# BGT 표준 검사 (화면 단위)

화면 하나를 기존 verifier 3종으로 정적 검사하고, 세 보고를 합쳐 `docs/verifier`에 간략 보고서 1개를 쓴다. **코드는 수정하지 않는다** — 수정은 `/standard-refactor`가 한다.

## 파라미터

| 파라미터 | 설명 | 예시 |
|---------|------|------|
| `screen` | 화면 path 또는 한글 메뉴 path | `bgt/cm/cntrct-bid/cntrct-bid-dtl`, `도급관리 > 도급입찰관리` |

없으면 요청한다. 경로는 `C:\workspace\bgt`(bgt-fe/bgt-be/docs 포함) 기준 상대경로로 다룬다.

## Phase 0: 범위 해석

1. **FE 폴더**: 화면 path 앞의 `bgt/`(또는 `com/bgt/`)를 떼고 `bgt-fe/src/pages/{나머지}` 로 매핑한다. 예: `bgt/cm/cntrct-bid/cntrct-bid-dtl` → `bgt-fe/src/pages/cm/cntrct-bid/cntrct-bid-dtl`(leaf). 한글 메뉴 path면 먼저 `docs/PMX_메뉴.md`(표: `MNU_NM`·`MNU_URL`·`UPPR_MNU_ID`)에서 `MNU_URL`을 확정해 위와 같이 매핑한다. `leaf` = 경로의 마지막 세그먼트.
2. **FE 검사 범위** = leaf 폴더 + **그룹(부모) 공유 `_components`/`_utils`**(있으면). 공유 컴포넌트에 위반이 자주 있으므로 포함한다.
3. **BE 슬라이스**(관례): 그룹명에서 `-`를 뗀다. `cm/cntrct-bid` → `bgt-be/src/main/java/com/amxis/bgt/cm/cntrctbid` + `bgt-be/src/main/resources/sql/primary/core/cm/cntrctbid/*.xml`. `Glob`으로 존재 확인한다. 없거나 애매하면 `codebase-locator`로 해당 화면의 be 파일을 찾는다. 그래도 없으면 BE는 `해당 없음`으로 둔다(FE 전용 화면).

## Phase 1: verifier 3종 디스패치

각 에이전트에 **Phase 0에서 해석한 경로를 검사 범위로 명확히 준다**(스스로 git diff 뒤지지 말 것). 독립적이므로 한 메시지에서 병렬로 디스패치한다.

- `bgt-fe-verifier` — FE 검사 범위(leaf + 공유 `_components`/`_utils`)
- `uiux-verifier` — 같은 FE 폴더 경로
- `bgt-be-verifier` — BE 슬라이스(자바 패키지 + xml). BE `해당 없음`이면 생략.

각 verifier는 채팅에 판정+지적을 반환한다(파일 안 씀). 그 출력을 그대로 수집한다.

## Phase 2: 보고서 합성 → docs/verifier

세 verifier 채팅 보고를 합쳐 **아래 포맷 그대로** 파일 1개로 쓴다. 등급은 verifier 출력을 보존한다(FE/BE: `BLOCKER`·`WARN`·`INFO`, UIUX: `①`·`②`·`③`). 각 섹션 내 지적에 순번 ID(`FE1`, `UI1`, `BE1`…)를 붙인다 — `/standard-refactor`가 이 ID로 수정 대상을 고른다.

- 파일: `docs/verifier/{leaf}-{YYYYMMDD}-{HHMM}.md` (`date`로 현재 시각 스탬프, 매 실행 새 파일 — 이력 보존)
- 지적 0건 섹션은 `없음`, BE 미해당은 `해당 없음`으로 표기.

```
# BGT Standard Verifier

대상: {module}/{group}/{leaf}
FE: bgt-fe/src/pages/{...}
BE: {be 자바 패키지}, {be xml}
상태: 정적 검사 완료

## FE
- FE1 [BLOCKER] `파일`:줄 - 문제 - 제안

## UIUX
- UI1 [①] `파일`:줄 - 문제 - 제안

## BE
- BE1 [BLOCKER] `파일`:줄 - 문제 - 제안

## 한계
- {tsc 범위밖 실패, SP 라이브 미검증, 렌더 미수행 등 verifier가 밝힌 한계}
```

끝에 채팅으로 한 줄 요약(`BLOCKER N · WARN N · ①/②/③ …`)과 보고서 경로를 알린다. 수정하려면 `/standard-refactor {screen}` 안내.
