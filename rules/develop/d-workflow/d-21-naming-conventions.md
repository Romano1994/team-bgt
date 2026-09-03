# D-21. 파일·네이밍 규칙 (FE/BE 파일 생성 시)

> **강제 규칙 (MUST)** — 새 파일/폴더/클래스를 만들 때 반드시 지킨다.
> - **FE 파일명은 BGT `template/*` 를 유일한 기준으로 한다.** cst·기존 bgt 화면이 `__type.ts`·`__function.ts`(단수)·`__component/`(단수)여도, **신규는 복수형** `__types.ts`·`__functions.ts`·`__components/` 로 만든다. 단수 네이밍을 그대로 베끼지 않는다.
> - 화면/슬라이스 폴더는 **kebab-case**(`cntrct-bid-lst`), FE 컴포넌트 파일은 **PascalCase**(`SearchBox.tsx`), 내부 폴더/유틸은 **`__` 접두사**(`__components/`, `__utils/__api.ts`).
> - BE 클래스는 **PascalCase 슬라이스명 + 고정 접미사**(`DescCdController`/`DescCdServiceImpl`/`DescCdRepository`, VO 는 `{Sel|Cud}RequestVO`/`{Sel|Cud}ResponseVO`).
> - BE 패키지 슬라이스명은 **소문자 압축(구분자 없음)** `desccd`·`cntrctbid`, XML 은 **kebab-case** `cntrct-desc.xml`.
> - 확신이 안 서면 **가장 가까운 배포 슬라이스/템플릿의 실제 파일명을 그대로 따른다**(임의 신설 금지).

## 언제 쓰나
- 새 화면/기능/API 를 만들며 **파일·폴더·클래스·VO·XML 이름을 정할 때**. (기존 파일 수정은 그 파일의 기존 규칙을 따른다.)

## FE 네이밍 (원천: `bgt-fe/src/pages/template/*`)

```
src/pages/{module}/{screen-kebab}/     # 화면 폴더: kebab-case
  index.tsx                            # 엔트리, default export 필수
  __components/                        # 내부 컴포넌트(__ 접두사, 복수)
    SearchBox.tsx  GridBox.tsx  CustomButtons.tsx   # PascalCase
    index.ts                           # barrel
  __utils/                             # 내부 로직(__ 접두사, 복수)
    __api.ts  __types.ts  __functions.ts            # __ 접두사, 복수
    index.ts
```

| 대상 | 규칙 | 예 |
| --- | --- | --- |
| 화면/모듈 폴더 | kebab-case | `cntrct-bid-lst`, `std-cd` |
| 엔트리 | `index.tsx` (default export) | — |
| 내부 폴더 | `__components/`, `__utils/` (복수) | — |
| 컴포넌트 파일 | PascalCase, 역할 접미사 | `SearchBox`, `GridBox`, `FormBox`, `GridBoxDetail` |
| 유틸 파일 | `__` 접두사 + 복수 | `__api.ts`, `__types.ts`, `__functions.ts`, `__constants.ts` |
| barrel | 폴더마다 `index.ts` | — |

### `__` 접두사의 의미 (역할)
`__` 는 **"이 화면 전용(로컬)"** 표식이다. 공용은 `src/apis`·`src/types`·`src/utils`·`src/components` 의 기존 모듈을 먼저 재사용하고, 화면 특화만 `__` 폴더에 둔다(재사용성이 보이면 상위 공용 위치로 승격). (원천: `bgt-fe/src/docs/ko/development/Front.md` §4·§5)

| 폴더/파일 | 역할 |
| --- | --- |
| `index.tsx` | 메뉴의 메인 화면 파일(조회/저장 오케스트레이션). **default export 필수**(라우팅) |
| `__components/` | 이 화면에서만 쓰는 내부 컴포넌트(`SearchBox`/`GridBox`/`FormBox`/`CustomButtons` 등) |
| `__utils/` | 이 화면 전용 api/타입/유틸 — `__api.ts`(callApi 호출), `__types.ts`(행 타입+zod 스키마), `__functions.ts`(검증 등 순수 함수) |
| `__tabs/` | 탭 분할 UI일 때 탭별 화면 컴포넌트(**필요 시에만** 생성) → [A-05](../a-archetype/a-05-tabs.md) |
| `__dialog/`·`__popup/` | 팝업/다이얼로그 상세 화면 묶음 → [A-03](../a-archetype/a-03-dialog-popup.md) |
| 각 폴더 `index.ts` | barrel — `import { GridBox } from './__components'` 로 경로 단순화 |

> **함정(실측)**: cst 는 대부분 **복수**(`__components/` 다수)지만 단수 잔재가 섞여 있다(`__type.ts`·`__function.ts` 다수, `__component/` 4개). 게다가 **bgt-fe 자체에도 단수 화면**이 있다(`sc/std-cd/std-cd-desc`·`std-cd-wktp` 등이 `__component/`·`__type.ts`·`__function.ts` 사용). 즉 '항상 복수'가 코드에서 지켜지는 건 아니다 — **신규 파일은 BGT 템플릿 기준 복수형**(`__types.ts`/`__functions.ts`/`__components/`)으로 만들고, cst·기존 단수 화면을 참고 복사할 때 파일명을 **복수형으로 바꾼다**.

## BE 네이밍 (원천: 배포 슬라이스 예 `bgt-be/.../sc/stdcd/desccd/`)

```
com.amxis.bgt.{도메인}.{서브}.{슬라이스}/     # 슬라이스: 소문자 압축(desccd, cntrctbid)
  controller/  {Pascal}Controller.java        # DescCdController
  service/     {Pascal}Service.java + {Pascal}ServiceImpl.java
  repository/  {Pascal}Repository.java
  model/       {Pascal}{Sel|Cud}RequestVO.java / ...ResponseVO.java
```

| 대상 | 규칙 | 예 |
| --- | --- | --- |
| 패키지 슬라이스 | 소문자 압축(구분자 없음) | `desccd`, `cntrctbid`, `ebgtmgmtcstrntype` |
| 하위 패키지 | `controller` / `service` / `repository` / `model` | — |
| Controller | `{Pascal}Controller` | `DescCdController` |
| Service | `{Pascal}Service` + `{Pascal}ServiceImpl` | `DescCdService(Impl)` |
| Repository | `{Pascal}Repository` | `DescCdRepository` |
| 조회 VO | `{Pascal}SelRequestVO` / `{Pascal}SelResponseVO` | `DescCdSelRequestVO` |
| 저장 VO | `{Pascal}CudRequestVO` / `{Pascal}CudResponseVO` | `DescCdCudRequestVO` |
| MyBatis XML | `sql/primary/core/{도메인}/{슬라이스}/{kebab}.xml` | `cntrct-desc.xml` |

> VO 접미사는 **조회=`Sel`, 저장=`Cud`** 로 구분한다(트리 조회 등 변형은 `TreeSel...`). MyBatis namespace 는 Repository 전체 경로와 일치시킨다. → [C-13](../c-api/c-13-new-api.md)

## 흔한 실패와 가드
- **cst 단수 파일명 복사** → BGT 템플릿(복수)과 불일치·barrel import 깨짐. `__types.ts`/`__functions.ts`/`__components/` 로 만든다.
- **화면 폴더를 camelCase/PascalCase 로 생성** → 라우팅/기존 폴더와 불일치. kebab-case.
- **VO 접미사 임의(`XxxDto`/`XxxParam`)** → 조회/저장 구분 불명확. `{Sel|Cud}RequestVO`/`ResponseVO`.
- **패키지 슬라이스에 하이픈/카멜** → 배포 SP·기존 패키지와 불일치. 소문자 압축.
- **애매하면 새 이름 창작** → 가장 가까운 배포 슬라이스/템플릿의 실제 파일명을 그대로 따른다.

## 검증 방법
```powershell
# FE: import 경로/barrel 깨짐 확인
npx tsc --noEmit
# BE: 컴파일/스캔
gradlew.bat test
```

## 관련 문서
- [A-01 단일 그리드](../a-archetype/a-01-single-grid-crud.md) · [D-16 신규 화면 체크리스트](./d-16-new-screen-checklist.md) · [C-13 신규 API](../c-api/c-13-new-api.md)
- 기본 폴더 구조: `bgt-fe/src/docs/ko/development/Front.md`, BE 흐름: `bgt-be/docs/Server.md`
