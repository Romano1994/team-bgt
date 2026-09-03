---
paths:
  - "bgt-fe/src/**/*.{ts,tsx}"
---

# B-12. 모달 / 토스트

> **강제 규칙 (MUST)** — 이 케이스로 작업할 때 반드시 지킨다.
> - `await showConfirm(...)` 반환값을 반드시 분기해 취소 시 중단한다(결과 무시 금지).
> - 공통 메시지는 하드코딩하지 말고 `@/constants` 의 `MSG_*`(`MSG_SAVE_CONFIRM`/`MSG_SAVE_SUCCESS` 등)를 쓴다.
> - 성공 토스트는 `processResponse(response, { successMessage: MSG_SAVE_SUCCESS })` 로 처리하고 별도 `showToast` 를 중복 호출하지 않는다.
> - `showLoadingExternal()` 뒤에는 반드시 `hideLoadingExternal()` 로 닫는다(try/finally 권장).
> - 저장/삭제 성공 후 동작(확인 메시지·재조회) 표준은 → B-07 을 따른다.

## 언제 쓰나
- 저장/삭제 확인, 경고/에러 안내, 로딩 표시, 간단한 성공 토스트가 필요할 때.

## 핵심 훅
- `useModal()` → `showConfirm`, `showToast` (그 외 alert/loading 계열)
- 메시지 상수: `@/constants` 의 `MSG_SAVE_CONFIRM`, `MSG_SAVE_SUCCESS`, `MSG_DELETE_CONFIRM`, `MSG_DELETE_SUCCESS`, `MSG_NO_CHANGES_DETECTED`, `MSG_SEARCH_CONFIRM`, `MSG_PROJECT_REQUIRED` 등
  - 저장/삭제 성공 후 동작(확인 메시지·재조회) 표준은 [B-07](./b-07-grid-cud-save.md) "수정·삭제 이후 동작 표준" 참조
- 로딩(외부 호출): `showLoadingExternal` / `hideLoadingExternal` (`@/hooks`)
- 전체 가이드: `bgt-fe/src/docs/ko/features/Modal.md`

## 레시피

### 1. 확인 다이얼로그
```tsx
const isConfirm = await showConfirm(MSG_SAVE_CONFIRM);
if (!isConfirm) return;   // 취소 시 중단
```

### 2. 토스트 (성공/경고/에러)
```tsx
showToast(MSG_NO_CHANGES_DETECTED);                          // 기본
showToast('이름은 필수 입력 항목입니다.', { usecase: 'warning' });
showToast(`중복된 표준아이템코드[${dup}]가 존재합니다.`, { usecase: 'error' });
```
- `processResponse(response, { successMessage: MSG_SAVE_SUCCESS })` 는 성공 토스트를 자동 처리.

### 3. 로딩
- API 함수 내부에서 직접 제어할 때 `showLoadingExternal()` / `hideLoadingExternal()`.
- `callApi(request, { isShowLoading: false })` 로 글로벌 로딩 끄기 가능.

## 코드 예제

### 저장 흐름의 확인+성공 — `template/default/index.tsx:90`
```tsx
async function handleSave() {
  const rows = getGridJsonData<ListItem>(gridName);
  if (!checkSaveValid(rows)) return;                 // 내부에서 경고 토스트
  const isConfirm = await showConfirm(MSG_SAVE_CONFIRM);
  if (!isConfirm) return;
  const response = await saveList(rows);
  processResponse(response, {
    successMessage: MSG_SAVE_SUCCESS,                 // 성공 토스트 자동
    onSuccess: () => handleSearch(condition),
  });
}
```

### 변경 없음/필수값 경고 — `template/default/index.tsx:113`
```tsx
if (!isGridDataChanged(gridName)) { showToast(MSG_NO_CHANGES_DETECTED); return false; }
if (hasEmptyName(rows)) { showToast('이름은 필수 입력 항목입니다.', { usecase: 'warning' }); return false; }
```

## 흔한 실패와 가드
- **확인 결과 무시** → `await showConfirm` 반환값을 반드시 분기.
- **메시지 하드코딩** → 공통 메시지는 `@/constants` 의 `MSG_*` 사용(다국어/일관성).
- **성공 토스트 중복** → `processResponse` 의 `successMessage` 를 쓰면서 별도 `showToast` 까지 호출하지 않기.
- **로딩 닫기 누락** → `showLoadingExternal` 후 반드시 `hideLoadingExternal` (try/finally 권장).

## 검증 방법
```powershell
yarn build:local; npx tsc --noEmit
```

## 관련 문서
- [A-01 단일 그리드](../a-archetype/a-01-single-grid-crud.md) · [B-07 그리드 저장](./b-07-grid-cud-save.md)
- 기능 상세: `bgt-fe/src/docs/ko/features/Modal.md`
