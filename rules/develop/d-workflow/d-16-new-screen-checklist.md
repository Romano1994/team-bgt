# D-16. 신규 화면 0부터 만들기 체크리스트

> **강제 규칙 (MUST)** — 이 케이스로 작업할 때 반드시 지킨다.
> - §0에서 화면 유형을 먼저 결정해 A-01~A-06 아키타입에 매칭하고, 처음부터 만들지 말고 `bgt-fe/src/pages/template/{유형}` 또는 유사 화면을 통째로 복사한다.
> - `index.tsx`의 `export default` 컴포넌트를 유지한다(라우팅 필수). FE는 `callApi` + `BusinessCode.BGT`, 함수명=Controller 메소드명.
> - BE 슬라이스(Model→Repository→XML→Service→Controller)는 배포 SP 시그니처와 일치시킨다. → C-13
> - 그리드는 1회 로드(isInit 토글, → B-09), 저장은 삭제행 포함 수집(`getGridJsonData`)+flagCd 매핑(→ B-07), 공통코드 label null 가드(CODE/NAME/NAME_ENG).
> - 완료 전 `yarn build:local` + `npx tsc --noEmit`(+ API 작업 시 `gradlew.bat test`)를 통과시키고, CST 특화 용어는 BGT 용어로 치환(임의 용어 신설 금지)한다.

## 언제 쓰나
- 새 업무 화면을 처음부터 만들 때의 전체 흐름. 어떤 케이스든 이 순서를 뼈대로 삼는다.

## 0. 화면 유형 결정 → 케이스 매칭
| 화면 | 케이스 |
| --- | --- |
| 단일 그리드 조회/CRUD | [A-01](../a-archetype/a-01-single-grid-crud.md) |
| 좌-마스터 / 우-디테일 | [A-02](../a-archetype/a-02-master-detail.md) |
| 목록 + 상세 팝업 | [A-03](../a-archetype/a-03-dialog-popup.md) |
| 단건 폼 입력 | [A-04](../a-archetype/a-04-form.md) |
| 탭 | [A-05](../a-archetype/a-05-tabs.md) |
| 그리드 3개+ | [A-06](../a-archetype/a-06-multi-grid.md) |

## 1. 템플릿 복사
- `bgt-fe/src/pages/template/{유형}` → `bgt-fe/src/pages/{module}/{screen}` 통째로 복사.
- 가장 빠른 시작은 유사 화면 복사. 없으면 위 템플릿.
- ☑ 검증: 복사 직후 `npx tsc --noEmit` (import 경로 깨짐 없는지)

## 2. 라우팅 연결
- `index.tsx` 컴포넌트명 변경, **`export default` 유지** (라우팅 필수).
- 메뉴/프로그램 관리에 화면 URL 등록 (권한·탭 `mnuUrl` 일치). → [B-11](../b-feature/b-11-auth.md)

## 3. 타입/스키마 정의
- `__utils/__types.ts`: 그리드 행 타입(`ListItem`), 검색 스키마(`createSearchSchema`).

## 4. API 작성 (BE + FE 연결) → [C-13](../c-api/c-13-new-api.md)
- BE: `Model → Repository → XML → Service → Controller`. SP 시그니처 일치. `@Operation`/`@Schema` 필수.
- FE: `__utils/__api.ts` 의 mock 을 실제 `callApi` 로 교체, **`BusinessCode.BGT`**.

## 5. 검색/그리드/폼 연결
- SearchBox conditions, GridBox Cols(또는 FormBox Table), 공통코드는 [B-08](../b-feature/b-08-code-dropdown.md).
- 그리드 1회 로드 패턴 준수. → [B-09](../b-feature/b-09-grid-async-load.md)

## 6. 저장 구현 → [B-07](../b-feature/b-07-grid-cud-save.md)
- 변경검사(`isGridDataChanged`) → 검증(삭제행 skip) → `showConfirm` → 저장(`getGridJsonData`) → 재조회.

## 7. 권한/모달
- 버튼은 `AuthButton`/`SearchButton`(조회 우측에 버튼이 더 있으면 `hasDivider`). 확인/토스트는 `useModal`. → [B-11](../b-feature/b-11-auth.md), [B-12](../b-feature/b-12-modal-toast.md)

## 8. 최종 검증
```powershell
# bgt-fe
yarn build:local
npx tsc --noEmit
# bgt-be (API 작업 시)
gradlew.bat test
```

## 전체 체크리스트
- [ ] 화면 유형에 맞는 템플릿 복사
- [ ] `export default` 컴포넌트 유지 + 라우팅/메뉴 등록
- [ ] 타입/검색 스키마 정의
- [ ] BE 슬라이스(Model→Repo→XML→Service→Controller) 작성, SP 시그니처 일치
- [ ] FE `callApi` + `BusinessCode.BGT`, 함수명=Controller 메소드명
- [ ] 그리드 `Events: {}` 존재, `SEQ`/`sState` 프리셋 컬럼
- [ ] 그리드 1회 로드(isInit 토글) + 재조회 resetGrid
- [ ] 저장: 삭제행 포함 수집(`getGridJsonData`), flagCd 매핑
- [ ] 권한 버튼/탭 mnuUrl
- [ ] 공통코드 label null 가드 (CODE/NAME/NAME_ENG)
- [ ] `yarn build:local` + `npx tsc --noEmit` (+ `gradlew.bat test`) 통과
- [ ] CST 특화 용어를 BGT 용어로 치환, 임의 용어 신설 안 함

## 관련 문서
- [README 목차](../README.md) · 기본 가이드: `bgt-fe/src/docs/ko/development/Front.md`, `bgt-be/docs/Server.md`
