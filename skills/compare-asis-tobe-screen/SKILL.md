---
name: compare-asis-tobe-screen
description: BGT(TOBE) 화면과 특정 기능을 받아 대응되는 ASIS(PMS/TEMS) 화면을 역추적해 1:1로 비교한다. ASIS↔TOBE 테이블·칼럼 매핑과 화면 UI·사용자 상호작용(트리거)을 정리한다. ASIS 화면을 못 찾으면 그 사실만 알리고 즉시 중단한다. Use when user wants to compare an ASIS screen against its BGT counterpart 1:1, verify a migration, or asks "이 화면 asis랑 비교해줘", "asis tobe 매핑", "테이블 칼럼 대응", "이 기능 asis랑 같은지 확인", and names a BGT 화면 + 기능.
---

# ASIS ↔ TOBE 화면 1:1 비교

BGT(TOBE) 화면 하나와 그 안의 **특정 기능**을 받아, 대응되는 ASIS(PMS/TEMS) 화면을 역추적하고 **데이터(테이블·칼럼)**와 **화면 UI·사용자 상호작용**을 1:1로 대조한다. 마이그레이션이 원본과 동등한지 확인하는 용도다.

> **강제 규칙 (MUST)**
> - **ASIS 화면을 확정하지 못하면 비교를 진행하지 않는다.** "ASIS 화면을 찾을 수 없습니다"와 시도한 단서(주석·화면코드·검색어)만 보고하고 **즉시 중단**한다. TOBE만으로 비교표를 만들거나 ASIS를 추측으로 채우지 않는다.
> - 데이터 매핑은 **실제 소스 근거**로만 채운다(ASIS: SQL/XML/SP 호출, TOBE: MyBatis XML + `bgt-be/docs/sp`). 근거 없는 칼럼은 `?`로 남기고 추측하지 않는다.
> - **읽기 전용.** 이 스킬은 코드를 수정하지 않는다(보고만). 실제 이식은 `migrate-feature-asis-to-bgt`가 한다.
> - PMS/TEMS는 참조만 하고 수정하지 않는다.

## 파라미터

| 파라미터 | 필수 | 설명 | 예시 |
|---------|------|------|------|
| `bgt_screen` | ✅ | 비교 기준이 되는 BGT(TOBE) 화면 (url·폴더·화면명) | `cm/cntrct-ctrt/cntrct-ctrt-lst`, `/cm/cntrct-bid/cntrct-bid-lst` |
| `feature_name` | ✅ | 비교할 특정 기능 (상세). 화면 전체면 `전체` | `발주형식 다중선택`, `보증금액 계산`, `전체` |

파라미터가 없으면 작업 시작 전에 반드시 요청한다.

## 절차

### Step 1: TOBE(BGT) 화면 확정 + 기능 파악

- `bgt_screen` url/폴더 세그먼트 → `bgt-fe/src/pages/{경로}` 디렉터리로 매핑한다. (모호하면 `feature-finder` 규칙으로 화면 `index.tsx`를 확정)
- 그 화면에서 `feature_name`에 해당하는 부분을 파악한다:
  - **UI**: 관련 컴포넌트/컨트롤(검색조건·그리드 컬럼·폼필드·버튼·팝업), 사용자 상호작용(이벤트 핸들러, `onChange`/`onDblClick`/`useUpdateEffect` 등 트리거).
  - **데이터**: FE가 호출하는 API(`__utils/__api.ts` 엔드포인트)와 응답/전송 키(그리드 `Name`, VO 필드, 폼 필드명).

### Step 2: ASIS 역추적 (여기서 중단 판정)

TOBE 화면에서 ASIS 원본을 역추적한다. (`feature-finder`의 bgt→ASIS 규칙 + `pms-screen-finder` 재사용)

1. BGT 화면 파일 최상단 **`// ASIS: ...` 주석**에서 ASIS 화면코드·breadcrumb를 확보한다.
   예: `// ASIS: MPOST.OE.MPMO10110\UCMPMO10110 (수수기성.계약관리 - 리스트)`.
2. 주석이 없으면 `docs/ASIS_메뉴.md`에서 화면명(`SCREEN_NM`)으로 역검색해 `SCREEN_ID`/`SCREEN_URL`을 얻는다.
3. 확보한 화면코드로 ASIS 패키지 경로를 확정한다:
   - **PMS**: `pms-screen-finder` 절차(Step 2A 코드 매칭 → Step 3 패키지 도출).
   - **TEMS**: `docs/ASIS_메뉴.md`에 없을 수 있으므로 `C:\workspace\bgt\TEMS`(Biz/CS/*.xml)에서 화면명·기능 키워드로 소스 검색.

> **🛑 중단 조건** — 위 1~3으로 ASIS 화면(패키지·대상 파일)을 특정하지 못하면 **여기서 멈춘다.** 아래를 보고하고 종료한다:
> - "ASIS 화면을 찾을 수 없습니다."
> - 시도한 단서: `// ASIS:` 주석 유무, 검색한 화면코드/화면명, 검색어 변형.
> - 다음 제안: 사용자에게 ASIS 메뉴 path·화면코드를 직접 요청.
>
> **TOBE 정보만으로 비교표를 만들지 않는다.**

### Step 3: ASIS 데이터(테이블·칼럼) 추출

ASIS 소스에서 `feature_name`에 해당하는 데이터 접근부를 찾아 **테이블명·칼럼명**을 수집한다.

- **PMS 웹(01/02)**: `*.aspx.cs`/`*Biz.cs`/MyBatis `*.xml`의 SELECT/INSERT/UPDATE 대상 테이블·칼럼, 호출 SP.
- **PMS MPOST(03, WinForms)**: SP 호출 인자와 DataSet/그리드 컬럼명, `UC{코드}.cs`의 바인딩 컬럼.
- **TEMS**: `Biz`/`CS`/`*.xml`의 쿼리·SP·바인딩 컬럼.
- 기능에 직접 관련된 테이블·칼럼만 뽑는다(전수 덤프 금지).

### Step 4: TOBE 데이터(테이블/SP·칼럼) 추출

BGT 데이터 계층은 SP 기반이다. 아래 순서로 확정한다.

1. `bgt_screen`에 대응하는 `bgt-be` 슬라이스의 MyBatis XML에서 **CALL하는 SP 패키지·프로시저명**을 확보한다.
2. `bgt-be/docs/sp`의 해당 SP 텍스트 본문을 읽어 **실제 물리 테이블/칼럼**을 뽑는다.
   - 물리 테이블을 알 수 있으면 **테이블명**을 쓴다.
   - SP 본문에서 물리 테이블이 불명확하면 **SP명 + 예상 칼럼**(커서 alias / 파라미터명)으로 대신한다.
3. 화면 표시용 **FE 키**(그리드 `Name`, VO 필드)를 병기해 DB↔화면을 잇는다.
4. `bgt-be/docs/sp`에 매칭 SP가 없으면 그 사실을 비고에 명시한다(임의 추정 금지).

### Step 5: 1:1 비교표 작성

**① 데이터 매핑표**

| 항목(기능) | ASIS 테이블.칼럼 | TOBE 테이블(또는 SP).칼럼 | FE 표시키 | 상태 |
|---|---|---|---|---|
| 계약연도 | `TB_XXX.CNTR_YR` | `PKG_BGT_CM_CNTRCT_CTRT` / `TB_...CTRT_YR` | `ctrtYr` | ✅ 일치 |
| 지분율 | `TB_XXX.SHARE_RT` | `SP …` (물리테이블 불명) 예상 `rShare` | `rShare` | ⚠ 이름 상이 |
| 공고시간 | `TB_XXX.NOTI_TM` | — | — | ❌ TOBE 누락 |

- 상태: ✅ 일치 / ⚠ 이름·타입 상이 / ❌ 누락 / `?` 근거 불충분.

**② UI · 사용자 상호작용표**

비개발자가 화면만 보고 이해할 수 있게 쓴다. **화면에 실제로 보이는 라벨·칼럼명·버튼명**으로 항목을 가리키고, 동작은 "무엇을 하면 무엇이 보인다"로 서술한다. 컴포넌트명·이벤트명·핸들러·파일명 같은 코드 용어는 쓰지 않는다.

| 화면 항목(라벨) | ASIS 화면 동작 | TOBE 화면 동작 | 상태 |
|---|---|---|---|
| 발주형식 | 여러 개를 골라 선택. 선택을 바꾸면 아래 목록이 다시 조회됨 | 버튼을 눌러 팝업에서 여러 개 선택. 바꾸면 목록이 비워짐 | ✅ 동등 |
| 내역코드(칼럼) | 칸을 더블클릭하면 표준내역코드 선택 창이 뜸 | 칸을 더블클릭하면 선택 창이 뜸 | ✅ 동등 |
| USD / WON | 통화를 바꾸면 금액 칼럼이 외화↔원화로 바뀜 | 해당 선택이 화면에 없음 | ❌ 누락 |

- 동작은 **화면에서 관찰되는 것만** 적는다: "값을 바꾸면 다시 조회된다 / 창이 뜬다 / 칼럼이 바뀐다 / 금액이 자동 계산된다" 등.
- 항목은 라벨이 없으면 **화면상 위치**로 가리킨다(예: "상단 오른쪽 첫 버튼", "그리드 3번째 칼럼").
- "어떤 값을 바꾸면 무엇이 일어나는가"(재조회/창 열림/칼럼 전환/자동 계산 등)를 반드시 적는다.

**③ 차이 요약**: ❌/⚠ 항목만 모아 "TOBE 누락 / 이름 상이 / 동작 차이"로 3줄 이내 요약.

### Step 6: 결과 보고

- **기본은 채팅**으로 위 3개 표 + 차이 요약을 출력한다.
- 사용자가 보고서를 원하면 `docs/migration/{화면}/{화면}_compare_{YYYYMMDD}.md`로 저장한다(기존 analysis 문서 컨벤션 재사용).

## 흔한 실패와 가드

- **ASIS 못 찾았는데 비교 강행** → 금지. Step 2 중단 조건을 따른다.
- **TOBE 물리테이블을 SP 안 읽고 추측** → `bgt-be/docs/sp` 본문 근거로만. 불명확하면 SP명+예상칼럼.
- **UI만 비교하고 트리거 누락** → "값 변경 → 결과" 상호작용을 반드시 표에 넣는다(사용자 핵심 요구).
- **전수 덤프** → `feature_name` 관련 테이블·칼럼·컨트롤만. 화면 전체(`전체`)여도 기능 블록 단위로 끊어 정리.
- **코드 수정** → 이 스킬은 보고만. 이식은 `migrate-feature-asis-to-bgt`.

## 관련 스킬

- 위치 찾기: `pms-screen-finder`(ASIS 패키지), `feature-finder`(BGT/cst 화면).
- 실제 이식: `migrate-feature-asis-to-bgt`, `migrate-asis-to-bgt`.
- SP↔VO↔FE 키 정합 상세가 필요하면 `.claude/rules/develop/c-api/c-19-sp-cursor-alias-mapping.md`.
