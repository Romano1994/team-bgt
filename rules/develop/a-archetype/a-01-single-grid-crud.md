---
paths:
  - "bgt-fe/src/**/*.{ts,tsx}"
---

# A-01. 단일 그리드 조회/CRUD 화면

> **강제 규칙 (MUST)** — 이 케이스로 작업할 때 반드시 지킨다.
> - 그리드 `Events: {}` 는 비어 있어도 반드시 유지한다(누락 시 IBSheet 오류). `getPresetCol('SEQ')`/`getPresetCol('sState')` 도 유지.
> - 저장 행 수집은 `getGridJsonData`/`getGridSaveJsonData`(=`getSaveJson`) 로 한다 — `getGridData`(=`getDataRows`)는 삭제행을 누락한다. → [B-07](../b-feature/b-07-grid-cud-save.md)
> - 재조회는 `isInit` 토글 + `resetGrid` 로 1회 로드(누적·빈-로드 clobber 회피). → [B-09](../b-feature/b-09-grid-async-load.md)
> - FE API는 `BusinessCode.BGT` 를 쓴다(템플릿의 `CST` 아님).
> - `index.tsx` 컴포넌트는 **default export 필수**(라우팅 연결).
> - 검색영역은 `useFormEffect(onReset)` + `onResetCondition={handleReset}` 표준을 유지한다(조건 변경 시 그리드 자동 초기화).

## 언제 쓰나
- 검색 조건으로 목록을 조회하고, 한 개의 그리드에서 행 추가/수정/삭제 후 저장하는 가장 기본적인 화면.
- 마스터-디테일이 아니고, 팝업도 없고, 폼 입력이 아니라 그리드 편집이 중심일 때.

## 참고 원본 (복사 기반)
```
bgt-fe/src/pages/template/default/
├── index.tsx                      # 페이지 컨테이너 (조회/저장 오케스트레이션)
├── __components/
│   ├── SearchBox.tsx              # 검색 조건 + 버튼
│   ├── GridBox.tsx                # IBSheet 그리드
│   └── CustomButtons.tsx          # 조회/기능/저장 버튼
└── __utils/
    ├── __api.ts                   # fetchList / saveList (API 호출)
    ├── __types.ts                 # SearchForm 스키마 + ListItem 타입
    ├── __functions.ts             # 검증 등 순수 함수
    └── index.ts                   # 배럴 export
```
> 새 화면은 `template/default` 폴더를 통째로 `src/pages/{module}/{url}` 로 복사해서 시작한다.

## 레시피

### 1. 폴더 복사 + 라우팅
- `template/default` → `src/pages/{module}/{screen}` 복사.
- `index.tsx` 의 `export default` 컴포넌트명을 화면에 맞게 변경. **default export 필수** (라우팅 연결).
- 검증: `npx tsc --noEmit` 로 import 경로 깨짐 확인.

### 2. 타입/스키마 정의 (`__utils/__types.ts`)
- `ListItem` 을 그리드 행 컬럼에 맞게 정의.
- `createSearchSchema` 를 검색 조건에 맞게 zod 스키마로 정의. 필수값은 `requiredString(...)`, 선택값은 `optionalString()`.

### 3. API 연결 (`__utils/__api.ts`)
- 템플릿은 mock 을 반환한다. 실제 API 호출로 교체한다. **BGT 는 `BusinessCode.BGT`** 사용 (템플릿의 `CST` 아님).
- 함수명은 호출하는 Controller 메소드명과 일치시킨다 (검색 편의).

### 4. 그리드 컬럼 정의 (`__components/GridBox.tsx`)
- `useGridOptions()` 의 `Cols` 를 실제 컬럼으로 교체. `getPresetCol('SEQ')`, `getPresetCol('sState')` 는 유지.
- `Events: {}` 는 비어 있어도 **반드시 존재**해야 한다.

### 5. 검색 조건 UI (`__components/SearchBox.tsx`)
- `conditions` 에 `Form.ProjectInput`, `Form.Input`, `Form.CodeSelect` 등을 배치.

### 6. 저장 검증 (`index.tsx` `checkSaveValid`)
- 변경 없음 → `isGridDataChanged` 로 막고 `MSG_NO_CHANGES_DETECTED` 토스트.
- 필수값/형식 검증 후 `showConfirm(MSG_SAVE_CONFIRM)` → 저장 → 성공 시 재조회.

### 7. 검증
- `yarn build:local` + `npx tsc --noEmit`.

## 코드 예제

### 조회/저장 오케스트레이션 — `template/default/index.tsx:67`
```tsx
async function handleSearch(params: SearchForm) {
  setIsGridInit(false);                     // 그리드 초기화
  const response = await fetchList(params);  // 데이터 조회
  processResponse(response, {
    onSuccess: ({ data = [] }) => {
      setGridData(data);
      setIsGridInit(true);
      setCondition(params);                  // 재조회용 조건 보관
    },
  });
}

async function handleSave() {
  const rows = getGridJsonData<ListItem>(gridName);
  if (!checkSaveValid(rows)) return;
  const isConfirm = await showConfirm(MSG_SAVE_CONFIRM);
  if (!isConfirm) return;
  const response = await saveList(rows);
  processResponse(response, {
    successMessage: MSG_SAVE_SUCCESS,
    onSuccess: () => handleSearch(condition), // 저장 후 재조회
  });
}
```

### 그리드 1회 초기화 — `template/default/__components/GridBox.tsx:59`
```tsx
useEffect(() => {
  initGrid({ id, ref: gridRef, data, isInit });
}, [isInit]);   // isInit 토글로 1회 로드 (B-09 참조)
```

## 동작: 초기화 버튼 + 조회 조건 변경 시 그리드 초기화 (표준)

검색영역(SearchBox)은 다음 두 가지를 **표준**으로 갖춘다.

1. **초기화 버튼**: `SearchArea` 에 `onResetCondition` 을 넘기면 검색영역에 초기화 버튼이 노출된다. 클릭 시 `handleReset` = `reset()`(폼 초기화) + `onReset()`(그리드 초기화)이 실행된다.
2. **조건 변경 시 자동 초기화**: 조회로 그리드에 데이터가 채워진 뒤 **검색 조건을 바꾸면, 그 즉시 이미 검색된 데이터가 초기화(그리드 비워짐)된다.** 변경된 조건과 화면에 남은 이전 결과가 어긋나는 것을 막기 위한 의도된 동작이며, 새 조건 결과를 보려면 **조회 버튼을 다시 눌러야** 한다.

- 메커니즘: `SearchBox` 의 **`useFormEffect(onReset)`** 이 `react-hook-form` 의 `useWatch` 로 폼 값을 감시하다가, 값이 실제로 바뀌면(deep compare) `onReset` 을 호출한다. `onReset`(= 페이지의 `handleReset`)은 `setGridData([])` / `setIsGridInit(false)` / `resetGrid(gridName)` 로 그리드를 비운다.
- 구현 위치(BGT): `bgt-fe/src/hooks/utils/useFormEffect.ts`, `template/default/__components/SearchBox.tsx:32`(`useFormEffect`), 동일 파일의 `onResetCondition={handleReset}`.
- **cst 근거(표준 확인)**: cst SearchBox **96개 파일**이 `useFormEffect` 를 사용. 대표 예 `cst/cst-fe/src/pages/cm/partnercompany/__components/SearchBox.tsx:43`(`useFormEffect(onReset)`) · 같은 파일 `:81`(`onResetCondition={handleReset}` → 초기화 버튼) · `:60`(`handleReset = reset() + onReset()`).

```tsx
// SearchBox.tsx — 조건이 바뀌면 onReset 호출 (그리드 초기화)
useFormEffect(onReset);

// 초기화 버튼: SearchArea 에 onResetCondition 전달
// <SearchArea onResetCondition={handleReset} ... />
function handleReset() {
  reset();      // 검색 폼 초기화
  onReset();    // 그리드 초기화 (부모 handleReset 호출)
}
```
```tsx
// index.tsx — onReset 본체: 그리드 비우기
function handleReset() {
  setGridData([]);
  setIsGridInit(false);
  resetGrid(gridName);
}
```

> 따라서 "조건 변경 후 조회 버튼을 눌러야 다시 채워진다"가 표준 흐름이다. 조건만 바꾸고 조회하지 않으면 그리드는 빈 상태로 유지된다.

## 흔한 실패와 가드
- **그리드 `Events` 누락** → IBSheet 오류. 빈 객체라도 `Events: {}` 유지.
- **삭제한 행이 저장에서 누락** → `getGridData`(=`getDataRows`)는 삭제행을 빠뜨린다. 저장 수집은 `getGridJsonData`/`getGridSaveJsonData`(=`getSaveJson`) 사용. → [B-07](../b-feature/b-07-grid-cud-save.md)
- **재조회 시 데이터 누적/빈-로드 clobber** → `isInit` 토글 + `resetGrid` 패턴 준수. → [B-09](../b-feature/b-09-grid-async-load.md)
- **BusinessCode 를 CST 로 둠** → BGT 라우팅 안 됨. `BusinessCode.BGT`.

## 검증 방법
```powershell
# bgt-fe 에서
yarn build:local
npx tsc --noEmit
```

## 관련 문서
- [A-02 마스터-디테일](./a-02-master-detail.md) · [A-03 팝업](./a-03-dialog-popup.md)
- [B-07 그리드 저장](../b-feature/b-07-grid-cud-save.md) · [C-13 신규 API](../c-api/c-13-new-api.md)
- 기능 상세: `bgt-fe/src/docs/ko/features/Grid.md`, `bgt-fe/src/docs/ko/development/Front.md`
