---
name: feature-finder
description: 사용자가 설명하는 기능이 cst 참고 프로젝트(cst/cst-fe/src/pages)와 신규 bgt 프로젝트(bgt-fe/src/pages) 중 어느 화면에 구현돼 있는지 함께 찾아, 프로젝트·파일 경로·화면 url·메뉴 path를 알려준다. Use when user wants to find which screen implements a feature across BOTH cst and bgt, asks "이 기능 어디 있어?", "cst/bgt에서 X 쓰는 화면", "bgt에 X 이미 있어?", or wants to check if a feature already exists in bgt (재구현 방지) or find the cst reference before migrating.
---

# Feature Finder (cst + bgt)

## 목적

사용자가 설명하는 기능이 **cst 참고 프로젝트**(`cst/cst-fe/src/pages`)와 **신규 bgt 프로젝트**(`bgt-fe/src/pages`) 중 어느 화면에서 사용되는지 **양쪽 모두** 찾아, 프로젝트 구분·파일 경로·화면 url·메뉴 path·사용 위치를 반환한다.

이렇게 하면 (1) 기능이 이미 bgt에 있는지(재구현 방지) 와 (2) 참고할 cst 원본이 어디인지를 한 번에 확인한다.

## 절차

### Step 1: 검색 키워드 도출

사용자 설명에서 검색 키워드를 뽑는다.

- 컴포넌트/훅 이름: `useGrid`, `useApiProcess`, `IBSheet`, `CheckSelectInput`
- API 함수명: `getCntrctBidList`, `callApi`
- 도메인 용어: `계약`, `입찰`, `보증`, `승인`, `첨부`
- 파일명/폴더 패턴: `cntrct`, `estm`, `-lst`, `-dtl`

### Step 2: 두 프로젝트 페이지 검색

Explore 에이전트로 **두 경로를 함께** 검색한다(cst와 bgt를 각각 지시).

- **cst 범위**: `cst/cst-fe/src/pages/**/*.tsx`
- **bgt 범위**: `bgt-fe/src/pages/**/*.tsx`
- **목표**: 기능을 사용하는 각 화면의 `index.tsx` 목록 확보(프로젝트별로 구분)

키워드 하나로 결과가 많으면 조합(AND)으로 좁히고, 없으면 유사 용어로 재검색한다. `node_modules`는 제외한다.

### Step 3: 프로젝트별 화면 url · 메뉴 path 추출

찾은 `index.tsx`마다 **프로젝트에 맞는 방식**으로 메타데이터를 뽑는다.

#### cst 화면 (cst/cst-fe)

상단 주석 블록을 읽는다.

```tsx
/**
 * Name : 화면 이름
 * Path : /at/attendance-cwms      ← 화면 route
 * Desc : 시공 > 통합출역 > ...     ← 메뉴 path
 */
```

- **화면 url**: 주석 `Path`(또는 폴더 경로)를 `C:\workspace\bgt\docs\PMX_메뉴.md`의 `MNU_URL`에서 Grep한다. `MNU_URL`에는 `cst/` 접두사가 붙는다(예: `at/attendance-cwms` ↔ `cst/at/attendance-cwms`). 매칭 행의 `MNU_URL` 원문을 그대로 반환. 미매칭이면 주석 `Path` + `(메뉴 미등록)`.
- **메뉴 path**: 주석 `Desc`. 없으면 `PMX_메뉴.md`에서 `MNU_NM` → `UPPR_MNU_ID`를 거슬러 상위 `MNU_NM`을 이어 재구성.

#### bgt 화면 (bgt-fe) — cst와 다름, 아래 규칙을 쓴다

bgt 페이지에는 cst식 `Name/Path/Desc` 헤더 블록이 **없다**. 별도 bgt 메뉴 레지스트리도 없다.

- **화면 url**: `bgt-fe/src/pages` 하위 **폴더 경로**를 그대로 쓴다(예: `cm/cntrct-ctrt/cntrct-ctrt-lst`).
- **메뉴 path / 화면명**: 아래 순서로 확보한다.
  1. 파일 최상단 `// ASIS: ...` 주석의 **괄호 한글 breadcrumb**을 우선 사용한다.
     예: `// ASIS: MPOST.OE.MPMO10110\UCMPMO10110 (수수기성.계약관리 - 리스트)` → `수수기성.계약관리 - 리스트`.
  2. 그 주석의 **ASIS 화면 코드**(예 `UCMPMO10110`)를 `C:\workspace\bgt\docs\ASIS_메뉴.md`의 `SCREEN_ID`에서 Grep해 `SCREEN_NM`을 얻고, `PSCREEN_ID`를 거슬러 상위 `SCREEN_NM`을 이어 메뉴 path를 보강한다.
  3. `// ASIS:` 주석이 없으면 파일의 한글 doc 주석(예 `/** 계약관리 목록 메인 페이지 */`)과 폴더 경로로 화면명을 대신한다.

### Step 4: 결과 보고 (프로젝트 통합 표)

| 프로젝트 | 파일 경로 | 화면 url | 메뉴 path / 화면명 | 사용 위치 |
|----------|-----------|----------|--------------------|-----------|
| bgt | `bgt-fe/src/pages/cm/cntrct-ctrt/cntrct-ctrt-lst/index.tsx` | cm/cntrct-ctrt/cntrct-ctrt-lst | 수수기성 > 계약관리(리스트) | 그리드 목록 + 검색 영역 |
| cst | `cst/cst-fe/src/pages/at/attendance-cwms/index.tsx` | cst/at/attendance-cwms | 시공 > 통합출역 > 출역보고서 | 그리드 하단 출력 버튼 |

- 프로젝트가 다르면 표에서 **프로젝트 컬럼으로 구분**하고, bgt 결과를 먼저(이미 구현됨), cst 결과를 뒤(참고 원본)에 둔다.
- **사용 위치**: 코드가 아닌 화면 관점(검색 영역 / 그리드(목록) / 상세 폼 / 모달 / 탭 / 툴바·버튼 / 헤더·푸터). 여러 곳이면 쉼표로 나열.
- 한쪽에만 있으면 그 사실을 명시한다: "bgt에는 이미 있음(재구현 불필요)" 또는 "cst 참고 원본만 있음(bgt 미구현)".
- 양쪽 모두 없으면 "cst·bgt 어느 쪽에서도 해당 기능을 사용하는 화면을 찾지 못했습니다."라고 알린다.

## 주의사항

- `__components`, `__utils`, `__popup`, `__tabs`, `_utils`, `_components` 하위가 아닌 **각 화면의 `index.tsx`** 기준으로 보고한다. (단 `__popup/*/index.tsx`는 그 자체가 화면이므로 포함한다.)
- 스코프는 두 프로젝트의 **프론트엔드 페이지**로 한정한다(cst-feature-finder와 동일 범위). `bgt-be` 등 백엔드는 사용자가 요청할 때만 별도 확인한다.
- 동일 기능이 여러 화면에 있으면 프로젝트별로 모두 나열한다.
- 사용자가 말한 게 특정 **메뉴명**에 가깝다면, cst는 `PMX_메뉴.md`의 `MNU_NM`, bgt는 `ASIS_메뉴.md`의 `SCREEN_NM`을 직접 검색해 역추적하는 것도 보조 수단으로 쓴다.
