---
paths:
  - "bgt-fe/src/**/*.{ts,tsx}"
---

# B-09. 그리드 비동기 로드 / reloadKey

> **강제 규칙 (MUST)** — 이 케이스로 작업할 때 반드시 지킨다.
> - 그리드는 `isInit` 토글로 1회 로드한다: 조회 시작 시 `setIsInit(false)`, 데이터 set 후 `setIsInit(true)`, `useEffect([isInit])` 로 로드(매 data 의존성으로 initGrid 금지).
> - 재조회는 `resetGrid(gridName)` 로 시트를 비운 뒤 단일 로드만 하고, `setGridData([])` 빈-로드를 추가하지 않는다(빈-로드가 실데이터를 clobber).
> - 재조회 분기에서 `resetGrid` 를 누락하지 않는다(메모리 누적으로 멈춤).

## 언제 쓰나
- IBSheet 그리드가 마운트되자마자 데이터를 로드하는데, 비동기 조회 결과가 늦게 도착하면서 **빈 배열 로드가 실제 데이터를 덮어쓰는(clobber)** 현상이 의심될 때.
- 재조회 시 데이터가 안 바뀌거나, 누적되어 브라우저가 느려질 때.

## 증상 (레이스)
1. 그리드가 즉시 마운트되며 `initGrid({ data: [] , isInit: true })` 로 빈 로드.
2. 직후 또는 직전 도착한 실제 데이터 로드가 빈 로드에 의해 덮여 사라짐.
3. 결과: 데이터가 있는데 그리드는 비어 보임.

## 핵심 패턴
- 그리드는 **`isInit` 토글로 1회 로드**한다. 조회 시작 시 `setIsInit(false)`, 데이터 set 후 `setIsInit(true)`.
- 재조회 전에는 **`resetGrid(gridName)`** 로 시트를 비우고, 빈-로드(`setGridData([])`)를 **추가로 하지 않는다**. (resetGrid 로 이미 비웠으므로 단일 로드만 수행)

## 코드 예제

### 1회 초기화 — `template/default/__components/GridBox.tsx:59`
```tsx
useEffect(() => {
  initGrid({ id, ref: gridRef, data, isInit });
}, [isInit]);   // isInit 가 true 로 바뀔 때 1회 로드
```

### 조회 시작 시 init=false → set → init=true — `template/default/index.tsx:68`
```tsx
async function handleSearch(params: SearchForm) {
  setIsGridInit(false);                 // 로드 잠금
  const response = await fetchList(params);
  processResponse(response, {
    onSuccess: ({ data = [] }) => {
      setGridData(data);
      setIsGridInit(true);              // 여기서 1회 로드
    },
  });
}
```

### 재조회 시 resetGrid 후 단일 로드 (빈-로드 생략) — cst `mlstn-class/index.tsx:240`(`initGrid`)
```tsx
// 재조회 전 마스터 시트를 resetGrid 로 비우고 상태만 초기화 (빈-로드 생략, 디테일은 SearchBox 가 reset)
const initGrid = () => {
  resetGrid(getGridName(GRID_ID.mst));   // 시트 비움
  setIsGridDetailInit(false);
  setGridData([]);
};
// 트리 재조회(fetchMlstnClassTreeList:74)는 디테일 상태만 초기화 → 비운 시트에 단일 로드만
//   setSearchConditionDetail({ uprCd: '' }); setGridDataDetail([]);
```

## 흔한 실패와 가드
- **`setGridData([])` (빈-로드) + 직후 실제 데이터 로드** → 즉시 마운트 그리드에서 빈 로드가 실데이터를 clobber. resetGrid 후에는 빈-로드 생략.
- **`isInit` 토글 없이 data 의존성으로 매번 initGrid** → 누적/중복 로드. `useEffect([isInit])` 1회 패턴 유지.
- **재조회 시 resetGrid 누락** → 메모리 누적으로 멈춤. 재조회 분기에서 `resetGrid`.

## 검증 방법
```powershell
yarn build:local; npx tsc --noEmit
```
> MF remote 라 단독 렌더가 어렵다. 동작 확인은 호스트 통합 환경에서, 코드 검증은 빌드+tsc 로.

## 관련 문서
- [A-01 단일 그리드](../a-archetype/a-01-single-grid-crud.md) · [A-02 마스터-디테일](../a-archetype/a-02-master-detail.md)
