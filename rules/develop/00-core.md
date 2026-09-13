# 개발 표준 — 핵심 강제 규칙 (BGT)

> 출처: BGT 개발 케이스 레시피(`.claude/rules/develop/`). 상세는 각 케이스 문서(a/b/c/d), 기본 개발 흐름은 `bgt-fe/src/docs/ko/Intro.md`·`bgt-be/docs/Server.md`.
> 대상: `bgt-fe`·`bgt-be` 코드를 만들거나 고칠 때 전부. 새 화면/기능/API는 **먼저 `README.md` 라우터에서 케이스를 매칭**하고 그 문서의 "참고 원본"을 복사 기반으로 작업한다.

## MUST (공통)

1. **범위** — 코드 수정은 `bgt-fe`·`bgt-be`로 제한. 그 밖 디렉터리 수정이 필요하면 사용자에게 먼저 확인.
2. **복사 기반** — 새로 만들지 말고 `template/*`(FE) 또는 유사 배포 SP(BE)를 복사·치환한다. CST 용어→BGT 용어(임의 용어 신설 금지).
3. **BusinessCode** — FE API는 `BusinessCode.BGT`(템플릿의 `CST` 아님).
4. **그리드 Events** — `Events: {}` 는 비어도 반드시 유지(누락 시 IBSheet 오류). → A-01
5. **그리드 저장 수집** — 삭제행 포함하려면 `getGridJsonData`/`getSaveJson`(=`getGridSaveJsonData`) 사용. `getDataRows`는 삭제행 누락. flagCd `C`/`U`/`D` 전송. → B-07
6. **그리드 1회 로드** — `isInit`(reloadKey) 토글 + `resetGrid` 로 빈-로드 clobber 회피. → B-09
7. **SP 호출** — CALL 인자는 **배포 SP 시그니처와 정확히 일치**(임의 인자 추가 시 ORA-06550). 새 쿼리 신설 금지 — 맞는 SP 없으면 사용자에게 확인. → C-13/C-15
8. **커서 매핑** — MyBatis resultMap 의 `column` 은 **배포 SP 커서 alias 와 정확히 일치**(다르면 조용히 전부 null). 커서 OUT 은 resultMap 만(resultType 금지). → C-19
9. **JSON 키** — 응답 VO '소문자1+대문자'(mSupply/rShare 등)는 `@JsonProperty("동일명")` identity 핀. SP alias ↔ VO ↔ FE 키를 5계층 한 이름으로. → C-19
10. **검증** — FE: `yarn build:local` + `npx tsc --noEmit`. BE: `gradlew.bat test`. bootRun java 강제종료 금지(graceful shutdown). → D-18
11. **네이밍** — 새 파일은 `template/*`(FE)·배포 슬라이스(BE) 파일명을 기준으로. FE 는 복수형(`__types.ts`/`__functions.ts`/`__components/`) — cst 단수(`__type.ts`) 복사 금지. 화면폴더 kebab, 컴포넌트 PascalCase, BE VO 는 `{Sel|Cud}RequestVO`/`ResponseVO`. → D-21
12. **아키타입 필수 골격** — 마스터-디테일은 디테일 조회를 조건 state + `useUpdateEffect` 로 분리(저장·검증은 디테일 기준). 팝업은 `isOpen`/`onClose`/`onRefresh` 3-prop + 목록 진입 PK 를 숨김컬럼(`Visible:0`)으로. 폼은 searchForm/dataForm 분리 + 본문 `disabled={!isDataLoaded}`. 탭은 각 탭 `mnuUrl`. 멀티그리드는 `Promise.all` + `throwIfMultiError`. → A-02~A-06
13. **권한 버튼** — 기능·저장은 `AuthButton` + `requiredAuthLevel={ButtonAuthLevel.WRITE}`, 조회는 `SearchButton`, 다운로드 등 조회성은 `READ`. 일반 Button 금지. → B-11
14. **검증·피드백** — 스키마는 `@/utils` zod 헬퍼(`requiredString`/`optionalString`/`requiredDate` …) + `createDefaultValues(schema)`(수기 defaultValues 금지). `await showConfirm(...)` 반환값 반드시 분기. 메시지는 `MSG_*` 상수(하드코딩 금지). → B-22, B-12
15. **조회조건 레이아웃** — `SearchBox` 는 전 필드 `labelAlign="left"` + **열 단위** 동일 `labelMinWidth`(화면 전체 단일값 금지), 조건을 전폭 묶음 `<Grid container item xs={12}>` 로 감싸지 않는다(한 행 `xs` 합 ≤ 12), 버튼영역은 DS 가 그리므로 자리 예약 칸·띄우기 `sx` 금지. → B-23
16. **코드 드롭다운** — SP 코드 컬럼 규약은 `CODE`/`NAME`/`NAME_ENG`, 코드명 null 금지(label null 이면 페이지 크래시). 새 코드는 `COMMON_CODE_KEYS`/`BGT_CODE_QUERY_MAP` 에 등록 후 사용. → B-08

## 케이스 매칭 (진입점)

- 화면 아키타입(단일그리드/마스터-디테일/팝업/폼/탭/멀티그리드) → `a-archetype/` (A-01~06)
- 공통 기능(저장 CUD/코드 드롭다운/비동기 로드/첨부/권한/모달·토스트/엑셀/zod검증/조회조건 레이아웃) → `b-feature/` (B-07~12, B-20, B-22, B-23)
- API·백엔드(신규 SP/조회 vs CUD/저장 파라미터/커서 alias) → `c-api/` (C-13~19)
- 작업 흐름(신규 화면/ASIS 이관/에이전트 루프/파일·네이밍) → `d-workflow/` (D-16~18, D-21)

목차·요약·참고 원본은 `README.md`가 단일 출처.

> **케이스 문서(A/B/C·UIUX 01~03)는 `paths:` 스코프다.** 매칭 파일을 **읽을 때** 컨텍스트에 들어오고, `/compact` 후에는 사라진다(매칭 파일을 다시 읽으면 복귀). 따라서 계획 단계에서 케이스를 매칭했으면 그 문서를 **명시적으로 Read** 한다(파일명이 안 겹쳐 자동 로드가 안 되는 케이스일수록 더 중요). 이 `00-core` 와 `UIUX/00-core`·`d-workflow/*` 는 스코프 없이 상시 로드된다.
>
> **글롭 세분화 현황** — 대부분은 여전히 `bgt-fe/src/**/*.{ts,tsx}`(A·B) / `bgt-be/**/*.{java,xml}`(C) 로 넓게 잡혀 있다(범용 `index.tsx`/`GridBox.tsx`/Service 파일에 여러 관심사가 섞여 있어, 파일명으로 좁히면 MUST 규칙이 조용히 안 뜨는 게 더 위험하기 때문 — 이 프로젝트는 그런 조용한 누락으로 실제 버그를 겪은 이력이 있다). 아래 5개만 파일명·폴더 컨벤션이 뚜렷해 안전하게 좁혔다:
> - B-10(파일첨부) → `*Atch*`/`*Uploader*`/`*FileAttach*`/`FileGridBox.tsx`
> - B-22(zod 검증) → `__types.ts`/`__type.ts`
> - B-23(조회조건 레이아웃) → `*SearchBox.tsx`
> - A-03(팝업) → `__dialog/**`/`__popup/**`
> - A-05(탭) → `__tabs/**`
> - C-19(커서 alias) → `*.xml`(resultMap) + `**/model/*.java`(identity 핀 대상 VO)
>
> **선행 트리거** — 룰 주입은 tool result 에 붙으므로 **신규 파일 Write 시점엔 구조적으로 늦다**. 케이스 문서를 제때 받는 경로는 "복사할 참고 원본을 Read" 하는 것뿐이다: FE=`template/**`(broad 글롭에 걸림), BE=`bgt-be/docs/sp/*.txt`(C-13·C-15 트리거로 등록). 그래서 broad 글롭을 파일명으로 좁히지 않는다 — 좁히면 이 선행 커버가 깨진다. 같은 룰은 세션당 1회만 주입되므로(재주입 없음) 넓게 잡는 비용은 사실상 없다.
