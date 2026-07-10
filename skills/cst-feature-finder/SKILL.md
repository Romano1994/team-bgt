---
name: cst-feature-finder
description: 사용자가 설명하는 기능이 cst 프로젝트(cst/cst-fe/src/pages)에서 어느 화면에 구현되어 있는지 찾아, 파일명과 메뉴 path를 알려준다. Use when user wants to find which cst screens implement a specific feature, asks "cst에서 X 기능 쓰는 화면", "cst에 X 있어?", or wants to check cst feature usage before migrating to BGT.
---

# CST Feature Finder

## 목적

사용자가 설명하는 기능이 `cst/cst-fe/src/pages` 내 어느 화면에서 사용되고 있는지 찾아, 파일 경로와 화면 url(`PMX_메뉴.md`의 `MNU_URL`), 메뉴 path를 반환한다.

## 절차

### Step 1: 검색 키워드 도출

사용자 설명에서 검색에 쓸 키워드를 뽑는다.

- 컴포넌트/훅 이름: `useOzReport`, `useApproval`, `IBSheet`
- API 함수명: `getAttdnwkList`, `callApi`
- 도메인 용어: `급여`, `출역`, `보험`, `OZ`, `승인`
- 파일명 패턴: `GridBox`, `SearchBox`, `Modal`

### Step 2: cst 페이지 검색

Explore 에이전트를 spawning하여 `cst/cst-fe/src/pages` 전체를 검색한다.

**검색 범위**: `cst/cst-fe/src/pages/**/*.tsx`  
**검색 대상**: Step 1의 키워드들  
**목표**: 해당 기능을 사용하는 `index.tsx` 파일 목록 확보

키워드 하나로 결과가 너무 많으면 키워드를 조합(AND)하여 좁힌다.  
결과가 없으면 유사 용어로 재검색한다.

### Step 3: 화면 url · 메뉴 path 추출

찾은 각 `index.tsx` 파일의 상단 주석 블록을 읽어 다음을 추출한다.

```tsx
/**
 * Name   : 화면 이름
 * Path   : /at/attendance-cwms          ← 화면 route (cst-fe 내부 경로)
 * Desc   : 시공 > 통합출역 > ...         ← 메뉴 path
 */
```

**메뉴 레지스트리 파일 `C:\workspace\bgt\docs\PMX_메뉴.md`**는 PMX 전체 메뉴를 파이프 구분 표(주요 컬럼 `MNU_NM`=메뉴명, `MNU_URL`=경로, `UPPR_MNU_ID`=상위 메뉴 ID, `MNU_LV_NO`=레벨)로 등록한 정적 출처다. 화면 url 과 메뉴 path 는 이 파일을 기준으로 결정한다.

#### 화면 url (반환 항목)

- 화면 route(주석 `Path` 또는 파일 폴더 경로)를 `PMX_메뉴.md`의 `MNU_URL`에서 Grep한다. `MNU_URL`에는 `cst/` 접두사가 붙으므로(예: 페이지 `at/attendance-cwms` ↔ `MNU_URL = cst/at/attendance-cwms`) 접두사를 고려해 매칭한다.
- 매칭된 행의 `MNU_URL` 원문(예: `cst/at/attendance-cwms`)을 **그대로 화면 url로 반환**한다.
- `PMX_메뉴.md`에 매칭되는 행이 없으면 주석의 `Path` 값을 화면 url로 대신 쓰고 표에 `(메뉴 미등록)`을 덧붙인다.

#### 메뉴 path

- 주석에 `Desc` 필드가 있으면 그 값을 메뉴 path로 쓴다.
- 없으면 위에서 매칭한 `MNU_URL` 행의 `MNU_NM`에서 시작해 `UPPR_MNU_ID`를 거슬러 올라가며 상위 행의 `MNU_NM`을 이어 메뉴 path를 만든다(예: `시공 > 출역관리 > 출역등록/집계`).
- `menu.ts`가 있으면 보조로 함께 참고한다.

### Step 4: 결과 보고

| 파일 경로 | 화면 url | 메뉴 path | 사용 위치 |
|-----------|----------|-----------|-----------|
| `cst/cst-fe/src/pages/at/attendance-cwms/index.tsx` | cst/at/attendance-cwms | 시공 > 통합출역 > 출역보고서 > 근로자 퇴직공제 | 그리드 하단 출력 버튼 |
| ... | ... | ... | ... |

- **화면 url**: `C:\workspace\bgt\docs\PMX_메뉴.md`의 `MNU_URL` 값(Step 3 매칭 결과). 미등록 시 주석 `Path` + `(메뉴 미등록)`.
- **메뉴 path**: 주석의 `Desc` 필드(없으면 `PMX_메뉴.md`로 재구성)
- **사용 위치**: 코드가 아닌 화면 관점의 위치. 아래 용어를 사용한다.
  - 검색 영역, 그리드(목록), 상세 폼, 모달, 탭, 툴바/버튼 영역, 헤더, 푸터
  - 예: "검색 영역 - 프로젝트 선택", "그리드 행 클릭 후 상세 폼", "저장 버튼 클릭 시 승인 모달"
  - 여러 위치라면 쉼표로 나열한다.

결과가 없으면 "cst에서 해당 기능을 사용하는 화면을 찾지 못했습니다."라고 명확히 알린다.

## 주의사항

- `__components`, `__utils` 하위 파일이 아닌 **각 화면의 `index.tsx`** 기준으로 보고한다.
- `node_modules` 는 검색에서 제외한다.
- 동일 기능이 여러 화면에 있으면 모두 나열한다.
- 사용자가 말한 기능이 특정 **메뉴명**에 가깝다면, `C:\workspace\bgt\docs\PMX_메뉴.md`의 `MNU_NM`을 직접 검색해 `MNU_URL`(→ `cst/` 접두사 제거 후 cst 페이지 폴더)로 화면을 역추적하는 것도 보조 탐색 수단으로 활용한다.

## UIUX 표준 안내 (UI 화면 결과일 때)

찾은 대상이 **UI 화면·컴포넌트**라면(대부분), 답변 **맨 끝에** 아래 블록을 덧붙여 BGT UIUX 표준을 안내한다. UI가 아닌 결과(순수 hook·util·API 함수 등)에는 붙이지 않는다. cst는 참고 원본이므로, BGT로 이관·재구현할 때 이 표준을 적용한다.

> **🎨 UIUX 표준 참고** — 이 화면을 BGT로 이관·재구현할 때는 `.claude/rules/UIUX/` 표준을 따른다(cst UI를 그대로 복사하지 말 것).
> - 핵심 MUST: `.claude/rules/UIUX/00-core.md` (Primary `#037AF2`, 세로/가로 간격, 타이포, 버튼 순서, 그리드 셀 상태 등)
> - 토큰(색상·타이포·스페이싱·아이콘·해상도): `01-foundation-tokens.md`
> - 레이아웃·화면 패턴·폼·그리드: `02-layout-and-patterns.md`
> - 컴포넌트별 규격: `03-components.md`
> 공통 UI는 `@amxis/design-system`, 그리드는 IBSheet로 구현한다.
