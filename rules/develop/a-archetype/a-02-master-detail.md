---
paths:
  - "bgt-fe/src/**/*.{ts,tsx}"
---

# A-02. 마스터-디테일(2그리드) 화면

> **강제 규칙 (MUST)** — 이 케이스로 작업할 때 반드시 지킨다.
> - 디테일 조회는 마스터 클릭 핸들러에서 직접 호출하지 말고 조건 state + `useUpdateEffect` 로 분리한다(중복 조회/레이스 방지).
> - 마스터 재조회 시 `resetGrid`(mst+detail) 후 단일 로드한다 — 마스터/디테일 둘 다 `setGridData([])` 빈-로드 금지(즉시 마운트 그리드 clobber). → [B-09](../b-feature/b-09-grid-async-load.md)
> - 마스터 행 클릭 전 디테일 변경분은 `isGridDataChanged` + `confirmFocus`(또는 `showConfirm(MSG_SEARCH_CONFIRM)`) 로 보호한다(취소 시 조회 안 함).
> - 저장·변경검사·검증은 모두 **디테일 gridName** 기준으로 한다(마스터 아님).

## 언제 쓰나
- 좌측 마스터(트리/그리드)에서 행을 선택하면 우측 디테일 그리드가 그 키로 다시 조회되는 화면.
- 분류↔코드, 헤더↔라인 같은 1:N 구조. 저장은 보통 **디테일 그리드만** 대상.

## 참고 원본 (복사 기반)
```
cst/cst-fe/src/pages/pl/mlstn-class/           # cst 안정 참고 (마스터 트리 + 디테일 그리드, 저장=디테일)
├── index.tsx                      # 마스터(트리) + 디테일 오케스트레이션
└── __component/
    ├── SearchBox.tsx
    ├── GridBoxMst.tsx             # 마스터(트리) 그리드 + onGridRowClick
    └── GridBoxDetail.tsx          # 디테일 그리드 (편집/저장 대상)
```
> 참고 원본은 **cst 실화면**(안정)을 쓴다 — bgt-fe 실화면은 개발 중이라 경로·라인이 바뀐다. `mlstn-class` 는 **단수** `__component/`·`__type.ts` 를 쓰므로 구조·패턴만 가져오고, **신규 BGT 화면 파일명은 D-21 기준 복수형**(`__components/`·`__types.ts`·`__functions.ts`)으로 만든다. → [D-21](../d-workflow/d-21-naming-conventions.md)
> BGT 배포 BE 슬라이스 예: `bgt-be/.../sc/stdcd/desccd/` (Controller/Service/Repository/XML)

## 레시피

### 1. 그리드 2개 ID 선언 — `mlstn-class/index.tsx:35`
```tsx
const GRID_ID = {
  mst: 'mlstn-class-tree-grid',
  detail: 'mlstn-class-detail-grid',
};
```

### 2. 마스터 선택 → 디테일 조건 갱신
- 마스터 그리드에 `onGridRowClick` 연결. 선택 키를 state 로 저장.
- 디테일은 그 조건 state 가 바뀌면 `useUpdateEffect` 로 자동 조회.

### 3. 마스터 행 클릭 시 디테일 변경분 보호
- 디테일에 미저장 변경분이 있으면 `showConfirm(MSG_SEARCH_CONFIRM)` 후 이동.

### 4. 저장은 디테일만
- 저장/변경검사/검증 모두 **디테일 gridName** 기준.

### 5. 검증
- `yarn build:local` + `npx tsc --noEmit`.

## 코드 예제

### 마스터 선택 핸들러 (변경분 보호 포함) — `mlstn-class/index.tsx:207`
```tsx
const handleGridRowClick = useGridRowClickHandler(
  (bizFldCd: string, uprCd: string, evtParam: any) => {
    const executeSearch = () => setSearchConditionDetail({ bizFldCd, uprCd });
    // 디테일에 변경분이 있으면 confirmFocus 로 이동 차단·확인 (취소 시 조회 안 함)
    if (isGridDataChanged(getGridName(GRID_ID.detail))) {
      const isBlocked = confirmFocus(evtParam, {
        message: getText('msg', '저장 하지 않고 이동하시겠습니까?'),
        isPrevent: true,
        onFocus: executeSearch,
      });
      if (isBlocked) return true;
    } else {
      executeSearch(); // 변경 없으면 바로 조회
    }
  },
  { keyField1: 'bizFldCd', keyField2: 'mlstnClassCd' }
);
```

### 조건 변경 시 디테일 자동 조회 — `mlstn-class/index.tsx:63`
```tsx
useUpdateEffect(() => {
  if (isEmptyString(searchConditionDetail?.uprCd)) return;
  fetchMlstnClassDetailList();
}, [searchConditionDetail]);
```

### 마스터 재조회 시 누적/빈-로드 방지 — `mlstn-class/index.tsx:74` (+ `initGrid:240`)
```tsx
// 재조회 진입 시 시트 비우기는 initGrid(:240)·SearchBox 의 resetGrid 가 담당
const initGrid = () => {
  resetGrid(getGridName(GRID_ID.mst)); // 시트 비움 (빈-로드 없이 단일 로드)
  setIsGridDetailInit(false);
  setGridData([]);
};

const fetchMlstnClassTreeList = async (init: boolean = true) => {
  setIsGridInit(false);
  if (init) {
    // 디테일 상태만 초기화 — resetGrid 로 이미 비운 시트에 단일 로드만 수행
    setSearchConditionDetail({ uprCd: '' });
    setGridDataDetail([]);
  }
  const response = await getMlstnClassDetailList({ ...searchCondition, uprCd: '' });
  processResponse(response, {
    onSuccess: ({ data = [] }) => {
      setGridData(data as MlstnClassItem[]);
      setIsGridInit(true);
    },
  });
};
```

## 흔한 실패와 가드
- **디테일 조회를 마스터 클릭 핸들러 안에서 직접 호출** → 조건 state + `useUpdateEffect` 분리가 더 안전(중복 조회/레이스 방지).
- **마스터/디테일 둘 다 `setGridData([])` 빈-로드** → 즉시 마운트 그리드에서 빈 로드가 실제 데이터를 덮어쓸 수 있음. 위 예제처럼 `resetGrid` 후 단일 로드. → [B-09](../b-feature/b-09-grid-async-load.md)
- **저장 변경검사를 마스터 기준으로 함** → 편집은 디테일에서 일어나므로 디테일 gridName 으로 검사.

## 검증 방법
```powershell
yarn build:local; npx tsc --noEmit
```

## 관련 문서
- [A-01 단일 그리드](./a-01-single-grid-crud.md) · [B-09 비동기 로드](../b-feature/b-09-grid-async-load.md)
- [C-14 조회 SP vs CUD SP](../c-api/c-14-select-vs-cud.md)
