---
name: pms-screen-finder
description: 사용자가 ASIS PMS의 메뉴 path(예 "PMS > 예산/내역 > 도급 관리 > 도급내역 집계표") 또는 화면 코드(예 MPMC10001)를 주면, C:\workspace\bgt\PMS 전체에서 해당 화면 패키지의 로컬 경로를 찾아 반환한다. Use when user gives a PMS/ASIS menu breadcrumb or a screen code and wants the local screen package path, asks "이 메뉴 path 화면 패키지 찾아줘", "PMS에서 이 메뉴 화면 어디 있어", "화면 코드 MPMxxxxx 위치", or needs to locate an ASIS PMS screen's source before migrating to BGT.
---

# PMS Screen Finder

## 목적

ASIS PMS의 **메뉴 path(breadcrumb)** 또는 **화면 코드**를 받아, `C:\workspace\bgt\PMS` 전체에서 그에 맞는 **화면 패키지의 로컬 경로**를 찾아 반환한다.

핵심 전제 (확인됨):
- **1순위 출처: 화면 레지스트리 파일 `C:\workspace\bgt\docs\ASIS_메뉴.md`.** ASIS 전체 화면이 파이프 구분 표(`TLIST_ID|SYSTEM_ID|SCREEN_ID|PSCREEN_ID|SCREEN_NM|SCREEN_URL|SCREEN_URL_E|HELP_URL`)로 등록돼 있다. `SCREEN_NM`(화면명)·`SCREEN_ID`(화면 코드)·`PSCREEN_ID`(상위 화면 → 메뉴 트리 재구성)·`SCREEN_URL`(웹 화면 상대경로)를 담는다. 메뉴 path·화면명·화면 코드 어떤 입력이든 **이 파일에서 먼저 화면 코드/URL을 확정**한 뒤 PMS 소스에서 경로를 도출한다. (MPOST WinForms 화면도 `SYSTEM_ID=TPMS`로 포함됨.)
- 위 파일에 없거나 매칭이 불확실할 때만 소스 기반 보조 검색을 쓴다: **breadcrumb의 마지막 세그먼트(leaf) = 화면 제목**이고, 화면 제목은 소스에 들어 있다(`<h3>`, 페이지 헤더, `this.Text=`, 캡션 등). (PMS 소스 자체에는 정적 메뉴 트리가 없어 breadcrumb 직접 매핑은 레지스트리 파일로만 가능하다.)
- **화면 코드**(MPMC10001 등)는 폴더·파일명에 그대로 박혀 있어 가장 정확하게 매칭된다.

## 입력

- 메뉴 path: `PMS > 예산/내역 > 도급 관리 > 도급내역 집계표` — 마지막 `>` 뒤가 화면명, 앞 세그먼트는 disambiguation용.
- 또는 화면 코드: `MPMC10001`, `PMC10001`, `NPMN30320` 등.

## 절차

### Step 0: 화면 레지스트리 조회 (1순위, 항상 먼저)

`C:\workspace\bgt\docs\ASIS_메뉴.md`에서 입력을 먼저 매칭한다.

- **메뉴 path / 화면명 입력**: leaf(마지막 세그먼트)를 `SCREEN_NM` 컬럼에서 Grep한다. 표기 변형 주의 — 일부 메뉴명 끝에 `ⁿ` 등 마커가 붙으므로 **부분 일치**로 검색한다(예: `도급계약관리`). 부모 세그먼트가 모호하면 `PSCREEN_ID`를 거슬러 올라가 메뉴 트리를 재구성해 후보를 좁힌다.
- **화면 코드 입력**: `SCREEN_ID`에서 직접 매칭한다.
- 매칭되면 `SCREEN_ID`(= 화면 코드)와 `SCREEN_URL`을 확보한다.
  - `SCREEN_URL`이 있으면(웹 화면) 상대경로 `/PMS.WEB/PMS.WEB.{모듈}/{코드}.aspx`가 모듈 폴더와 대상 파일을 거의 그대로 알려준다 → **Step 3**로 바로 간다.
  - `SCREEN_URL`이 비어 있으면(MPOST WinForms 등) 확보한 **화면 코드로 Step 2A**(코드 매칭)를 수행한다.
- **이 파일에서 못 찾으면** Step 1(입력 판별) → Step 2B(소스 제목 검색)로 폴백한다.

### Step 1: 입력 판별

- 화면 코드 패턴(`^[A-Z]{2,6}\d{4,5}` 형태)이면 → **Step 2A**.
- 한글 breadcrumb이면 → leaf를 화면명으로 추출, 부모 세그먼트는 보관 → **Step 2B**.

### Step 2A: 화면 코드 매칭 (정확)

- Glob `C:\workspace\bgt\PMS\**\*{코드}*` 로 폴더명/파일명에 코드가 포함된 항목을 찾는다.
- 보통 1개 패키지로 수렴한다. 가장 신뢰도 높음.

### Step 2B: 화면명(제목) 매칭

- Grep로 PMS 전체 소스에서 leaf 화면명을 검색한다. 띄어쓰기 변형을 함께 시도한다.
  - 원문 그대로 → 공백 제거판 → 공백 유연(`도급내역.{0,3}집계`).
  - 대상: `*.aspx, *.ascx, *.cs, *.resx, *.vbs` (소스). `images/`·바이너리는 제외.
- **제목 위치**(`<h3>…</h3>`, 페이지 헤더, `this.Text="…"`, 메뉴 캡션)에서 나온 파일을 우선한다. 버튼/링크 텍스트로만 나온 파일은 후순위(연관 화면).
- 부모 세그먼트(예 "예산/내역")로 모듈을 좁힐 수 있으면 후보 랭킹에 사용한다. `*Biz.cs` 등 비즈니스 로직 파일은 화면이 아니므로 제외한다.
- **leaf가 하위 요소면**(입력에 `Tab`/`탭`/`팝업`/`(상단)`/`(하단)` 등 표시): leaf는 화면 전체가 아니라 그 안의 탭/팝업 라벨이다. 이때는 **leaf 바로 앞 세그먼트(상위 화면명)와 leaf를 모두** 검색한다 — 상위 화면명이 패키지를, leaf 라벨이 패키지 안 특정 파일을 가리킨다.

### Step 3: 패키지 경로 도출

찾은 파일이 속한 서브시스템 규칙으로 패키지 경로를 만든다.

| 서브시스템 | 패키지 루트 | 패키지(화면) 단위 | 화면 코드 예 | 대표 파일 |
|-----------|------------|------------------|------------|----------|
| 03_MPOST | `PMS\03_MPOST\MPOST_Application\NMPOST.root\NMPOST\01.Project\MPOST_APP\` | `MPOST.{모듈}.{코드}\` 폴더 | `MPMC10001` | `UC{코드}.cs`, `Form*.cs` |
| 01_pmsoldweb | `PMS\01_pmsoldweb\PMS.WEB\PMS.WEB.{모듈}\` | 모듈 폴더 + `{코드}*.aspx` 파일군 | `PMC10001` | `{코드}.aspx(.cs)` |
| 02_pmsnewweb | `PMS\02_pmsnewweb\GsEnC.PMS.Framework\Web\PMS.Web\{모듈}\` | 모듈 폴더 + `{코드}*.aspx` 파일군 | `NPMN30320` | `{코드}.aspx(.cs/.designer.cs)` |

### Step 4: 구성 파일 맵·대상 파일 지목

마이그 참고용으로, 패키지 폴더의 파일을 나열해 **역할 맵**을 만들고 leaf가 가리키는 **대상 파일을 콕 집는다**. 역할은 파일명 규칙으로 분류한다.

| 서브시스템 | 메인(진입점) | 탭/섹션 | 팝업 | 기타 |
|-----------|------------|--------|------|------|
| 03_MPOST | `UC{코드}.cs` | `UC{코드}S{n}.cs` (S1, S2 …) | `*_POP*.cs`, `Form*Pop*.cs` | `Form{이름}.cs` (예 `FormGyan`=기안) |
| 01/02 웹 | `{코드}.aspx` | 한 aspx 내 탭이면 탭 라벨을 파일 안에서 찾아 지목 | `{코드}P*.aspx` | 접미사 변형 `{코드}{F/U/_E …}.aspx` (역할은 파일 상단 1줄 확인해 보강) |

- leaf가 탭/팝업이면 그 라벨이 캡션으로 박힌 파일(`S{n}` 또는 해당 aspx 패널)을 **대상 파일**로 지목하고 화면 내 위치(상단/하단 탭 등)를 함께 적는다.
- 역할 분류가 애매한 파일만 상단 1~2줄을 확인해 보강한다. **전수 분석은 하지 않는다.**

### Step 5: 결과 보고

| 메뉴 path | 서브시스템 | 화면 코드 | 패키지 경로 | 대상 파일(leaf) | 구성요소 맵 | 매칭 근거 |
|-----------|-----------|----------|------------|----------------|------------|----------|
| … | 02_pmsnewweb | NPMN30320 | `…\Web\PMS.Web\NY` | `NPMN30320.aspx` | 단일 화면(연관 `NPMN30310`) | `<h3>도급내역 집계표</h3>` |

- **대상 파일(leaf)**: leaf가 가리키는 실제 파일. leaf가 화면 전체면 메인 파일, 탭/팝업이면 그 파일.
- **구성요소 맵**: 패키지 안 탭·팝업·폼을 파일명과 역할로 한 줄 요약.
- 후보가 여럿이면 모두 나열하고 매칭 근거(제목/버튼 등)를 함께 적는다.
- 못 찾으면 명확히 알리고, 시도한 검색어·변형을 보고한 뒤 부모 세그먼트나 유사어로 재시도를 제안한다.

## 비용·주의

- **레지스트리 파일(`ASIS_메뉴.md`) 조회가 가장 싸고 정확하다** — 단일 파일 Grep 1회로 화면 코드/URL이 나오면 PMS 전체 검색을 건너뛴다. 항상 Step 0을 먼저 시도한다.
- 한글 제목 검색(폴백)은 PMS 전체 ripgrep 1회(콜드 ~10–20초, 이후 캐시로 빠름) + 후보 1~3개 파일 확인이면 끝난다. **자원 소비 적음.**
- 메뉴 라벨과 화면 내 제목이 다르면 leaf로 못 찾을 수 있다 → 공백/유사어 변형, 부모 세그먼트를 병행한다.
- MPOST는 동일 화면이 `01.Project\MPOST_APP`, `WebApp`, `MPOST Smart Client Framework`에 중복 존재한다 → `01.Project\MPOST_APP`을 대표로 보고하고 나머지는 "중복 사본"으로만 표기한다.
- **역할 분리**: 이 스킬은 **위치 + 구성**까지만 준다. 프로시저명·API·파라미터·권한 같은 백엔드 딥 분석은 하지 않는다 — 그건 `migrate-asis-to-bgt`가 `asis_path`(= 여기서 준 패키지 경로)를 받아 Phase 1에서 자체 수행한다.

## 예시 (worked)

입력: `PMS > 예산/내역 > 도급 관리 > 도급내역 집계표`

1. leaf = `도급내역 집계표`.
2. `rg -l "도급내역 집계표"` → `…\Web\PMS.Web\NY\NPMN30320.aspx`, `NPMN30310.aspx`, `…\PMS.Biz.NY\NYBiz.cs`.
3. `NYBiz.cs`는 로직이라 제외. `NPMN30320.aspx`는 `<h3>도급내역 집계표</h3>`(제목) → 대표 화면. `NPMN30310`은 연관 화면.
4. 결과: 서브시스템 `02_pmsnewweb`, 코드 `NPMN30320`, 패키지 `PMS\02_pmsnewweb\GsEnC.PMS.Framework\Web\PMS.Web\NY`, 대상 파일 `NPMN30320.aspx(.cs/.designer.cs)`, 구성요소 맵 = 단일 화면(연관 `NPMN30310`).

### 예시 2 — leaf가 탭인 경우

입력: `... > 도급기성수금 > (하단) 일반기성내역 Tab`

1. leaf `일반기성내역` 뒤 `Tab` 표시 → 하위 요소. 상위 세그먼트 `도급기성수금`(화면명)과 leaf를 함께 검색한다.
2. 상위 화면명으로 패키지를 잡는다(MPOST면 `MPOST.{모듈}.{코드}` 폴더).
3. 패키지 안에서 `일반기성내역` 캡션이 박힌 파일을 **대상 탭**으로 지목하고, 구성요소 맵(탭 `UC…S{n}`, 팝업 `*Pop*`, 폼 `Form*`)을 함께 보고한다. 위치 = 하단 탭.
