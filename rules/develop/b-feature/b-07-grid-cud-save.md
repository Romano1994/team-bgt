---
paths:
  - "bgt-fe/src/**/*.{ts,tsx}"
---

# B-07. 그리드 저장(CUD)

> **강제 규칙 (MUST)** — 이 케이스로 작업할 때 반드시 지킨다.
> - 저장 행 수집은 `getGridJsonData`/`getGridSaveJsonData`(=`getSaveJson`→`getRowsByStatus('Added,Changed,Deleted')`)로 삭제행까지 모아 `sState`→`flagCd` `C`/`U`/`D` 로 전송한다.
> - `getGridData`(=`getDataRows`)는 삭제행을 누락하므로 저장 수집에 쓰지 않는다.
> - 필수값 검증은 `flagCd === 'D'` 행을 skip 한다(삭제행은 값이 비어도 됨).
> - 인라인 수정/삭제는 확인 없이 마킹(`deleteGridRows`)하고, 저장 시 `MSG_SAVE_CONFIRM` 한 번 → 성공 후 재조회로 마무리(낙관적 갱신 금지). 별도 즉시 삭제 API만 `MSG_DELETE_CONFIRM`+`MSG_DELETE_SUCCESS`.
> - CUD SP 인자는 배포 시그니처와 정확히 일치시킨다(임의 추가/누락 시 `ORA-06550`). → C-15

## 언제 쓰나
- 그리드에서 추가/수정/삭제한 행을 한 번에 서버로 보내 저장(C/U/D)할 때. (거의 모든 편집 그리드)

## 핵심 규칙
- 행 상태 컬럼 **`sState`**: `C`(추가) / `U`(수정) / `D`(삭제) / `''`(변경없음).
- **삭제행은 `getGridData`(=IBSheet `getDataRows`)로 수집되지 않는다.** 삭제행이 빠지면 서버가 삭제 처리를 못 한다.
- 저장 데이터 수집은 **`getGridJsonData` / `getGridSaveJsonData`(=`getSaveJson`)** 사용.
- 서버 전송 시 `sState` → **`flagCd`** 로 매핑(C/U/D). BE 의 CUD SP 가 flagCd 로 분기.

## 수정·삭제 이후 동작 표준 (cst 실측 기준)

cst 에는 명문 규칙이 없으나, 프로젝트 전체를 보면 아래가 **최빈 패턴**이며 BGT 의 표준으로 따른다.

### 1) 행 수정/삭제는 인라인 편집 → 저장 시 일괄 반영 (즉시 API 호출 안 함)
- 셀 수정은 그리드 인라인 편집으로 `sState='U'` 마킹.
- 행 삭제는 **`deleteGridRows(gridName)`** 로 행을 삭제 마킹(`sState='D'`, 신규행은 그리드에서 제거). **삭제 버튼 단계에서는 확인창을 띄우지 않는다.** 실제 DB 삭제는 저장(handleSave)의 CUD 흐름에서 함께 일어난다.
- 근거: 대부분의 `__components/GridBox.tsx` 가 `variant: 'delete' → onClick: () => deleteGridRows(gridName)` 패턴 (예: `cst/.../cm/partnercompany/__components/GridBox.tsx:50`).

### 2) 저장(수정/삭제 포함) 성공 후 동작 → **성공 토스트 + 재조회**
- 흐름: `isGridDataChanged` 확인 → 검증(삭제행 skip) → `showConfirm(MSG_SAVE_CONFIRM)` → 저장 API → **성공 시 `MSG_SAVE_SUCCESS` 토스트 + 재조회**.
- 재조회는 화면 데이터를 서버 확정 상태로 갱신하기 위함이며, **낙관적(클라이언트) 갱신은 하지 않는다.**
- 근거(최빈): `onSuccess: () => fetchGridData()` + `successMessage: MSG_SAVE_SUCCESS`
  - `cst/.../eq/eq-lms-mng/index.tsx:76` · `cst/.../at/attendancemnl/index.tsx:121` · `cst/.../pl/std-mlstn/index.tsx:116`(`fetchAll()`) · `cst/.../at/attendance-cwms-card/index.tsx:131`

### 3) 별도 삭제(마스터/일괄 즉시 삭제 API)는 확인창을 띄운다
- 그리드 행 마킹이 아니라 별도 "삭제" 액션으로 즉시 삭제 API 를 호출하는 경우:
  - `showConfirm(MSG_DELETE_CONFIRM)` → 취소 시 중단 → 삭제 API(`flagCd='D'`) → **성공 시 `MSG_DELETE_SUCCESS` 토스트 + 재조회**.
- 근거: `cst/.../tp/mom/index.tsx:334`(`showConfirm(MSG_DELETE_CONFIRM)`), `cst/.../at/attendance-cwms-card/index.tsx:137`(`fetchDelete` → `MSG_DELETE_SUCCESS` → `fetchGridData()`).

> 요약: **인라인 수정/삭제는 확인 없이 마킹 → 저장 시 `MSG_SAVE_CONFIRM` 한 번 → 성공 후 재조회.**
> **별도 즉시 삭제만 `MSG_DELETE_CONFIRM` + `MSG_DELETE_SUCCESS`.** 어떤 경우든 성공 후에는 재조회로 마무리한다.

## 레시피

### 1. 변경 여부 확인
```tsx
if (!isGridDataChanged(gridName)) {
  showToast(MSG_NO_CHANGES_DETECTED);
  return;
}
```

### 2. 저장 데이터 수집 (삭제행 포함)
- `getGridJsonData<T>(gridName)` 또는 `getGridSaveJsonData<T>(gridName, options)` 사용.
- 내부적으로 `getSaveJson()` → `getRowsByStatus('Added,Changed,Deleted')` 기반이라 삭제행이 포함된다.
- 검색해보면: `bgt-fe/src/utils/grid.ts:255`(`getGridSaveJsonData`), `:1097`(`getRowsByStatus('Added,Changed,Deleted')`), `:1243`(`toGridSavePayload` → `flagCd`).

### 3. 검증
- 필수값/중복 검사. 단, **`flagCd === 'D'` 인 행은 검증 skip** (삭제행은 값이 비어도 됨).

### 4. 저장 호출 → 재조회
```tsx
const isConfirm = await showConfirm(MSG_SAVE_CONFIRM);
if (!isConfirm) return;
const response = await saveXxx(saveData);
processResponse(response, { successMessage: MSG_SAVE_SUCCESS, onSuccess: () => fetchList() });
```

## 코드 예제

### 삭제행 포함 수집 + 삭제행 검증 skip — cst `at/attendancemnl` (`index.tsx:114`·`__utils/__function.ts:73`)
```tsx
// index.tsx:214 — 변경검사→검증→확인→저장→재조회
const handleSave = async () => {
  if (!checkSaveValid()) return;                 // 내부에서 삭제행 skip 검증(아래)
  const isConfirm = await showConfirm(MSG_SAVE_CONFIRM);
  if (!isConfirm) return;
  fetchSave();
};

// index.tsx:114 — getGridJsonData(name, 2): 저장 대상(추가/수정/삭제=삭제행 포함) 수집
const fetchSave = async () => {
  const gridName = getGridName(GRID_ID.master);
  const updData = getGridJsonData<AtAttendanceMnlListGridRow>(gridName, 2);
  const response = await saveAttendanceMnl(makeSavePayload(userId, updData));
  processResponse(response, {
    successMessage: MSG_SAVE_SUCCESS,
    onSuccess: () => fetchGridData(),            // 저장 후 재조회
  });
};

// __utils/__function.ts:73 — 삭제행은 필수값 검증 skip (수집 전이라 sState==='D')
export function hasEmptyTime(items: AtAttendanceMnlListGridRow[]): boolean {
  return items.some((item) => isEmptyString(item.frstInptTm) && item.sState !== 'D');
}
```

### sState → flagCd 매핑 — `bgt-fe/src/utils/grid.ts:1243`
```ts
export function toGridSavePayload<T extends { sState?: GridRowStatus }, P>(items: T[]): P {
  const savePayload = items.map((item) => {
    const { sState, ...rest } = item;
    return { ...rest, flagCd: sState || '' }; // C/U/D
  });
  return savePayload as P;
}
```

### BE 측: flagCd 루프 처리 — `desccd/service/DescCdServiceImpl.java:47`
```java
@Transactional(rollbackFor = Exception.class)
public DescCdCudResponseVO saveDescCdCud(List<DescCdCudRequestVO> requestVOs) {
  for (DescCdCudRequestVO requestVO : requestVOs) {
    descCdRepository.saveDescCdCud(requestVO);                       // SP가 flagCd로 C/U/D 분기
    ProcedureUtil.checkResult(requestVO.getResultCode(), requestVO.getResultMsg());
  }
  // ... 마지막 결과 반환
}
```
> `CommonDtoUtil.initBaseFields(requestVOs)` 가 Controller 에서 세션 작성자(frstWrtrId/lastWrtrId)와 flagCd 를 채운다. (`desccd/controller/DescCdController.java:71`)

## 흔한 실패와 가드
- **`getGridData`/`getDataRows` 로 저장 수집** → 삭제행 누락. 반드시 `getGridJsonData`/`getGridSaveJsonData`.
- **삭제행을 필수값 검증에 포함** → 빈 값으로 검증 실패. `flagCd === 'D'` skip.
- **SP 인자 임의 추가/누락** → `ORA-06550`. BE CUD SP 시그니처와 정확히 일치해야 함. → [C-15](../c-api/c-15-save-param-mapping.md)

## 검증 방법
- FE: `yarn build:local` + `npx tsc --noEmit`.
- BE: `gradlew.bat test` (SP 호출은 배포된 시그니처 일치 필수).

## 관련 문서
- [C-14 CUD SP](../c-api/c-14-select-vs-cud.md) · [C-15 저장 파라미터 매핑](../c-api/c-15-save-param-mapping.md)
- 기능 상세: `bgt-fe/src/docs/ko/features/Grid.md`
