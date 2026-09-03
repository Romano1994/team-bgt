# 개발 표준 규칙 (BGT) — 케이스 레시피

`bgt-fe`/`bgt-be` 코드를 만들거나 고칠 때 **그대로 따라 하는 실전 케이스 레시피**이자 개발 표준이다.
사람 대상 개념 설명이 아니라, "이 상황이면 → 이 파일을 복사 → 이 단계로 수정 → 이렇게 검증" 까지 결정론적으로 적었다.

> **강제(enforcement)**: 이 도메인의 핵심 규칙 `00-core.md`는 루트 `CLAUDE.md`에서 `@import`로 상시 로드된다.
> 개별 케이스 문서(아래 목차)는 화면/기능/API 작업 시 **해당 케이스를 매칭해 온디맨드로 읽는다**. 각 문서 상단의 `강제 규칙 (MUST)` 콜아웃이 그 케이스의 비협상 규칙이다.

## 사용 규칙 (에이전트)

1. 작업이 들어오면 먼저 **어떤 케이스인지** 아래 목차에서 매칭한다.
2. 각 문서의 **참고 원본(실제 경로)** 을 먼저 읽고, 그 파일을 복사 기반으로 작업한다.
3. CST 특화 용어/도메인은 BGT 용어로 치환한다. (임의 용어 신설 금지 — `CLAUDE.md` 참조)
4. 코드 수정 범위는 `bgt-fe`, `bgt-be` 로 제한한다.
5. 검증은 각 문서의 "검증 방법" 절을 따른다. (FE: `yarn build:local` + `npx tsc --noEmit`, BE: `gradlew.bat test`)

> 세션 시작 시 필독: `bgt-fe/src/docs/ko/Intro.md`, `bgt-be/docs/Server.md`.
> 이 케이스 문서들은 그 기본 가이드를 **상황별 레시피로 구체화**한 것이다.

## 목차

### A. 화면 아키타입 (`a-archetype/`)
| # | 케이스 | 참고 템플릿 |
| --- | --- | --- |
| A-01 | [단일 그리드 조회/CRUD 화면](./a-archetype/a-01-single-grid-crud.md) | `template/default` |
| A-02 | [마스터-디테일(2그리드) 화면](./a-archetype/a-02-master-detail.md) | cst `pl/mlstn-class` |
| A-03 | [팝업/다이얼로그 상세 화면](./a-archetype/a-03-dialog-popup.md) | `template/dialog` |
| A-04 | [폼 입력 화면](./a-archetype/a-04-form.md) | `template/form` |
| A-05 | [탭 화면](./a-archetype/a-05-tabs.md) | `template/tabs` |
| A-06 | [멀티 그리드 화면](./a-archetype/a-06-multi-grid.md) | `template/multi-grid` |

### B. 공통 기능 (`b-feature/`)
| # | 케이스 | 핵심 |
| --- | --- | --- |
| B-07 | [그리드 저장(CUD)](./b-feature/b-07-grid-cud-save.md) | flagCd C/U/D, 삭제행 수집 |
| B-08 | [공통코드 드롭다운](./b-feature/b-08-code-dropdown.md) | `useCode`, `Form.CodeSelect` |
| B-09 | [그리드 비동기 로드/reloadKey](./b-feature/b-09-grid-async-load.md) | 빈-로드 clobber 회피 |
| B-10 | [파일 첨부](./b-feature/b-10-file-attach.md) | `AtchFile`, FileController |
| B-11 | [권한 적용](./b-feature/b-11-auth.md) | AuthButton/AuthTabs/useAuth |
| B-12 | [모달/토스트](./b-feature/b-12-modal-toast.md) | Alert/Confirm/Loading/Toast |
| B-20 | [엑셀 다운로드/업로드](./b-feature/b-20-excel-download-upload.md) | downloadGridExcel(fileName=title), useAIP 업로드 |
| B-22 | [zod 검증 표준](./b-feature/b-22-zod-validation.md) | utils/zod.ts 헬퍼, createSearchSchema/createDataSchema, createDefaultValues |

### C. API / 백엔드 (`c-api/`)
| # | 케이스 | 핵심 |
| --- | --- | --- |
| C-13 | [신규 API(프로시저) 등록·호출](./c-api/c-13-new-api.md) | FE→Controller→Service→Repo→XML→SP |
| C-14 | [조회 SP vs CUD SP](./c-api/c-14-select-vs-cud.md) | CURSOR OUT vs flagCd 루프 |
| C-15 | [저장 파라미터 매핑](./c-api/c-15-save-param-mapping.md) | 이름→코드, SP 시그니처 일치 |
| C-19 | [SP 커서 alias ↔ VO ↔ FE 키 정합](./c-api/c-19-sp-cursor-alias-mapping.md) | 5계층 한 이름, identity 핀, 직렬화/역직렬화 테스트 |

### D. 작업 흐름 (`d-workflow/`)
| # | 케이스 | 핵심 |
| --- | --- | --- |
| D-16 | [신규 화면 0부터 만들기 체크리스트](./d-workflow/d-16-new-screen-checklist.md) | 복사→라우팅→연결→검증 |
| D-17 | [as-is(PMS/TEMS) 기능 이식](./d-workflow/d-17-as-is-migration.md) | UI 유지, 스택 재구현 |
| D-18 | [에이전트 자율 루프(/loop) 주의사항](./d-workflow/d-18-agent-loop-precautions.md) | 강제종료 금지, 쓰기 차단, 블로커 조기분류 |
| D-21 | [파일·네이밍 규칙](./d-workflow/d-21-naming-conventions.md) | FE 복수형 정규화, BE 슬라이스/VO 접미사, kebab XML |

## 문서 공통 구조

각 케이스 문서는 다음 절을 가진다.

- **강제 규칙 (MUST)** — 상단 콜아웃. 이 케이스의 비협상 규칙
- **언제 쓰나** — 이 케이스로 매칭되는 조건
- **참고 원본** — 복사 기반이 되는 실제 파일 경로
- **레시피** — 1→2→3 단계 (검증 포함)
- **코드 예제** — 실제 파일 발췌 (경로 명시)
- **흔한 실패와 가드** — 알려진 함정
- **검증 방법** — 빌드/타입체크/테스트
- **관련 문서**
