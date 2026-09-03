---
paths:
  - "bgt-fe/src/**/*.{ts,tsx}"
---

# B-20. 엑셀 다운로드 / 업로드

> **강제 규칙 (MUST)** — 이 케이스로 작업할 때 반드시 지킨다.
> - 그리드 → 엑셀 다운로드는 `downloadGridExcel(gridName, gridOptions, { fileName })`(`@/utils`)만 쓴다. `sheet.exportData`/`down2Excel` 직접 호출 금지(util 이 확장자·raw 모드·visible 범위를 처리).
> - `fileName` 은 **화면 제목(`title`)** 을 넘긴다. 생략하면 파일명이 타임스탬프(`1737….xlsx`)가 되어 사용자가 무슨 파일인지 모른다. cst/bgt 실사용 100% 가 `{ fileName: title }`.
> - `.xlsx` 확장자는 util 이 자동으로 붙인다 — `title` 에 `.xlsx` 를 중복으로 넣지 않는다.
> - 다운로드 버튼도 권한을 건다: `disabled: !hasAuthButton(ButtonAuthLevel.READ)`. → B-11
> - 엑셀 업로드/복호화는 `useAIP` 훅(`decrypt`/`uploadExcel`)을 쓴다. 새 fetch/업로드 로직 신설 금지. → C-13

## 언제 쓰나
- 조회 그리드에 **엑셀 다운로드 버튼**을 붙일 때(거의 모든 목록 화면).
- 암호화된/일반 엑셀 파일을 **업로드해서 파싱**하거나 복호화할 때.

## 참고 원본 (복사 기반)
```
bgt-fe/src/utils/grid.ts:538           # downloadGridExcel (그리드 → 엑셀, 공통 util 이라 bgt 유지)
bgt-fe/src/hooks/pms/useAIP.ts         # decrypt / download / uploadExcel (엑셀 업로드·복호화)
bgt-fe/src/hooks/useDextUploader.ts    # Dext5 업로드 UI(파일선택/업로드/롤백)
cst/cst-fe/src/pages/cm/cstrn-type/__component/GridBoxMst.tsx:45  # 다운로드 버튼 실사용 (cst 안정)
```
> 기본 가이드: `bgt-fe/src/docs/ko/features/Grid.md`(다운로드), `bgt-fe/src/docs/ko/features/AIP.md`(업로드/복호화).
> 백엔드 업로드 처리: `ExcelAIPController` / `ExcelUtil`(Apache POI).

## 레시피 — 그리드 엑셀 다운로드

### 1. GridBox/Grid 버튼 배열에 다운로드 버튼 추가
- `useGridOptions()` 로 만든 `gridOptions` 와 시트명 `gridName`, 화면 `title` 을 그대로 넘긴다.
- 버튼에 권한 `disabled` 를 건다.

### 2. 검증
- `npx tsc --noEmit` — `downloadGridExcel` import 경로(`@/utils`)와 `gridOptions` 타입 확인.

## 레시피 — 엑셀 업로드/복호화

### 1. `useAIP` 훅 호출
- 복호화만: `const blob = await decrypt(file)`.
- 업로드+파싱: `uploadExcel(file, { headerIndex, useIndexAsKey })` → 배열/객체 반환.
- 파일 선택은 `browseFile('.xls,.xlsx')`(`@/utils/common`) 또는 `useDextUploader` UI.

### 2. 검증
- `yarn build:local` + `npx tsc --noEmit`.

## 코드 예제

### 다운로드 버튼 — cst `cm/cstrn-type/__component/GridBoxMst.tsx:45`
```tsx
const headerButton: IBSheetGridHeaderRightMember[] = [
  {
    label: getText('btn', TEXT.BTN.DOWNLOAD),
    variant: 'download',
    disabled: !hasAuthButton(ButtonAuthLevel.READ),
    onClick: () => downloadGridExcel(gridName, gridOptions, { fileName: title }),
  },
  // ... refresh 등
];
```

### 다운로드 옵션 — `utils/grid.ts:538`
```tsx
// options 는 선택. 기본값:
//   mode = 'displayed'  (보이는 대로. 'raw' 면 Width/Align 제거 후 ASIS 형태)
//   fileName = Date.now()   → 표준은 title 전달
//   downRows/downCols = 'Visible', merge = 1
downloadGridExcel(gridName, gridOptions, { fileName: title });          // 표준
downloadGridExcel(gridName, gridOptions, { fileName: title, mode: 'raw' }); // ASIS 형태
```

### 업로드/복호화 — `hooks/pms/useAIP.ts`
```tsx
const { decrypt } = useAIP();
async function handleSelect() {
  const file = await browseFile('.xls,.xlsx');
  if (!file) return;
  const blob = await decrypt(file);   // 서버 releaseExcel 로 복호화
  // ...파싱/그리드 반영
}
```

## 흔한 실패와 가드
- **`sheet.exportData`/`down2Excel` 직접 호출** → 확장자 누락·visible 범위·raw 모드 처리가 빠진다. `downloadGridExcel` 만 사용.
- **`fileName` 생략** → 타임스탬프 파일명(`1737….xlsx`). 화면 `title` 을 넘긴다.
- **`title` 에 `.xlsx` 중복** → `title.xlsx.xlsx`. 확장자는 util 이 자동으로 붙인다.
- **다운로드 버튼에 권한 미적용** → 무권한 사용자도 데이터 반출. `hasAuthButton(ButtonAuthLevel.READ)` 로 `disabled`.
- **업로드용 fetch 신설** → `useAIP`(`releaseExcel`/`uploadExcel`) 를 재사용.

## 검증 방법
```powershell
# bgt-fe 에서
yarn build:local
npx tsc --noEmit
```

## 관련 문서
- [B-11 권한 적용](./b-11-auth.md) · [A-01 단일 그리드](../a-archetype/a-01-single-grid-crud.md) · [C-13 신규 API](../c-api/c-13-new-api.md)
- 기능 상세: `bgt-fe/src/docs/ko/features/Grid.md`, `bgt-fe/src/docs/ko/features/AIP.md`
