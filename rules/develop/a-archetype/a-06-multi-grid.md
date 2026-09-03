---
paths:
  - "bgt-fe/src/**/*.{ts,tsx}"
---

# A-06. 멀티 그리드 화면

> **강제 규칙 (MUST)** — 이 케이스로 작업할 때 반드시 지킨다.
> - 독립 조회는 순차 await 금지 — `Promise.all([initSecond, initThird])` 로 병렬 실행한다.
> - 병렬 조회 에러는 하나만 잡지 말고 `throwIfMultiError(responses)` 로 묶어 `processCatch` 한다.
> - 병렬/콜백 안에서 언어코드는 state 스냅샷 대신 `langCdRef.current` 를 쓴다.
> - `handleReset` 은 모든 그리드에 `setData([])` + `setIsInit(false)` + `resetGrid` 를 빠짐없이 적용한다.

## 언제 쓰나
- 한 화면에 그리드가 3개 이상이고, 첫 그리드 선택 시 나머지 그리드들이 함께 조회되는 경우.
- 여러 조회를 병렬로 실행하고 에러를 한 번에 처리해야 할 때.

## 참고 원본 (복사 기반)
```
bgt-fe/src/pages/template/multi-grid/
├── index.tsx                      # 3개 그리드 오케스트레이션
└── __components/
    ├── SearchBox.tsx
    ├── FirstGridBox.tsx           # 선택 → onSelect
    ├── SecondGrid.tsx
    └── ThirdGridBox.tsx           # 저장 대상
```

## 레시피

### 1. 그리드별 state 3쌍
- 각 그리드마다 `gridName` / `data` / `isInit` 를 둔다. (`GRID_ID.first/second/third`)

### 2. 그리드별 init 함수 분리
- `initFirst/initSecond/initThird` 로 분리. 각각 `setIsXxxInit(false)` → fetch → `throwIfError` → set.

### 3. 첫 그리드 선택 시 나머지 병렬 조회 — `template/multi-grid/index.tsx:142`
- `Promise.all([initSecond, initThird])` 후 `throwIfMultiError(responses)` 로 일괄 에러 처리.

### 4. langCd 는 ref 로
- 병렬 조회에서 최신 언어코드가 필요하면 `langCdRef.current` 사용 (state 클로저 스냅샷 방지).

### 5. 검증
- `yarn build:local` + `npx tsc --noEmit`.

## 코드 예제

### 선택 시 병렬 조회 + 일괄 에러 — `template/multi-grid/index.tsx:142`
```tsx
async function handleSelect(row: FirstListItem) {
  try {
    const params = { ...row, baseYm: condition.baseYm };
    const responses = await Promise.all([initSecond(params), initThird(params)]);
    throwIfMultiError(responses);
  } catch (error) {
    logError('handleSelect() - error', error);
    processCatch(error);
  }
}
```

### init 함수 패턴 (langCdRef) — `template/multi-grid/index.tsx:110`
```tsx
async function initSecond(params: Omit<SecondListParams, 'langCd'>) {
  setIsSecondGridInit(false);
  const response = await fetchSecondList({ langCd: langCdRef.current, ...params });
  const data = getApiData(response);
  throwIfError(response);
  setSecondGridData(data);
  setIsSecondGridInit(true);
  return response;
}
```

## 흔한 실패와 가드
- **순차 await 로 N개 그리드 조회** → 느림. 독립 조회는 `Promise.all` + `throwIfMultiError`.
- **에러 한 개만 처리** → 병렬 조회는 `throwIfMultiError` 로 묶어서 `processCatch`.
- **reset 시 일부 그리드 누락** → `handleReset` 에서 모든 그리드 `setData([])` + `setIsInit(false)` + `resetGrid` 빠짐없이.
- **langCd state 스냅샷 오류** → 병렬/콜백에서는 `langCdRef.current`.

## 검증 방법
```powershell
yarn build:local; npx tsc --noEmit
```

## 관련 문서
- [A-02 마스터-디테일](./a-02-master-detail.md) · [B-09 비동기 로드](../b-feature/b-09-grid-async-load.md)
