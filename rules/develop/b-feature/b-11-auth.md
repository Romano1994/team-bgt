---
paths:
  - "bgt-fe/src/**/*.{ts,tsx}"
---

# B-11. 권한 적용

> **강제 규칙 (MUST)** — 이 케이스로 작업할 때 반드시 지킨다.
> - 기능/저장 버튼은 일반 Button 대신 `AuthButton`+`requiredAuthLevel={ButtonAuthLevel.WRITE}`, 조회는 `SearchButton` 을 쓴다.
> - 그리드 헤더 버튼은 `wrapGridButtons` 항목에 `disabled: !hasAuthButton(ButtonAuthLevel.WRITE)` 를 걸고, 다운로드 등 조회성 동작은 `READ` 로 한다(다운로드에 WRITE 요구 금지).
> - 탭은 `AuthTabs` 에 각 탭 `mnuUrl` 을 지정하고 메뉴/프로그램 관리에 동일 URL 을 등록한다.
> - 조회 버튼 우측에 기능/저장 버튼이 있으면 `SearchButton` 에 `hasDivider` 를 추가하고, 단독이면 생략한다.
> - BE 의 `@RequiredAuth`/`@AuthLevel` 와 레벨을 맞춰 이중 방어한다.

## 언제 쓰나
- 버튼(조회/저장/기능)이나 탭, 업로더에 메뉴 권한(읽기/쓰기)을 적용할 때.

## 핵심 컴포넌트/훅
- `AuthButton` (`@/components/auth`), `SearchButton` (`@/components/common`)
- `AuthTabs` (`@/components/auth`) — 탭별 `mnuUrl` 로 권한 체크
- `AuthUploader` (`@/components/common`) — 업로더 권한
- 훅: `useAuth()` → `hasAuthButton(ButtonAuthLevel.WRITE | READ)`
- `ButtonAuthLevel` (`@amxis/pms-com`)
- 전체 가이드: `bgt-fe/src/docs/ko/features/Auth.md`

## 레시피

### 1. 버튼 권한
- 기능/저장 버튼은 `AuthButton` + `requiredAuthLevel={ButtonAuthLevel.WRITE}`.
- 조회 버튼은 `SearchButton` 사용.
- 조회 버튼 우측에 다른 기능/저장 버튼이 있으면 `hasDivider` 추가가 표준 — 조회 버튼 오른쪽에 구분선(`|`)을 표시한다. 조회 버튼만 단독이면 생략한다(`SearchButton` 기본값은 `false`).

### 2. 그리드 헤더 버튼 권한
- `wrapGridButtons` 항목의 `disabled: !hasAuthButton(ButtonAuthLevel.WRITE)` (다운로드는 `READ`).

### 3. 탭 권한
- `AuthTabs` 에 각 탭 `mnuUrl` 지정. 메뉴/프로그램 관리에 동일 URL 등록 필수.

### 4. 검증
- `yarn build:local` + `npx tsc --noEmit`.

## 코드 예제

### 검색/기능/저장 버튼 (AuthButton) — cst 실사용 `at/attendancemnl/__components/CustomButtons.tsx:57`(기능)·`:83`(저장)
```tsx
<SearchButton hasDivider onSearch={onSearch} /> {/* 조회는 SearchButton (우측에 기능/저장 있으면 hasDivider) */}
<AuthButton
  appearance="outlined" priority="normal"
  buttonLabel={getText('btn', '기능')}
  requiredAuthLevel={ButtonAuthLevel.WRITE}
  onClick={() => logInfo('기능 버튼 클릭!')}
/>
<AuthButton
  appearance="contained" priority="primary"
  buttonLabel={getText('btn', TEXT.BTN.SAVE)}
  requiredAuthLevel={ButtonAuthLevel.WRITE}
  onClick={onSave}
/>
```

### 그리드 버튼 권한 — `template/default/__components/GridBox.tsx:36`
```tsx
const headerButton = wrapGridButtons(gridName, [
  { variant: 'add',      label: getText('btn', TEXT.BTN.ADD_ROW), disabled: !hasAuthButton(ButtonAuthLevel.WRITE), onClick: () => addGridFirstRow(gridName, { useYn: 'Y' }) },
  { variant: 'delete',   label: getText('btn', TEXT.BTN.DEL_ROW), disabled: !hasAuthButton(ButtonAuthLevel.WRITE), onClick: () => deleteGridRows(gridName) },
  { variant: 'download', label: getText('btn', TEXT.BTN.DOWNLOAD), disabled: !hasAuthButton(ButtonAuthLevel.READ),  onClick: () => downloadGridExcel(gridName, gridOptions, { fileName: title }) },
]);
```

### 탭 권한 — `template/tabs/index.tsx:25`
```tsx
{ content: <FirstTab .../>, mnuUrl: `${MODULE_CODE}/template/tabs/first` }
```

## 흔한 실패와 가드
- **일반 Button 사용** → 권한 미적용. 반드시 `AuthButton`/`SearchButton`.
- **`hasDivider` 누락/오용** → 조회 버튼 우측에 기능 버튼이 있으면 `hasDivider` 추가가 표준(구분선 `|`), 조회 단독이면 생략(`SearchButton` 기본값 `false`).
- **`mnuUrl` 미등록** → 탭 권한 체크 실패. 메뉴/프로그램 관리에 등록.
- **다운로드에 WRITE 요구** → 조회성 동작은 `READ`.
- **BE 권한과 불일치** → 서버 측 `@RequiredAuth`/`@AuthLevel` (`bgt-be/.../wsf/annotation/`) 와 맞춰 이중 방어.

## 검증 방법
```powershell
yarn build:local; npx tsc --noEmit
```

## 관련 문서
- [A-05 탭](../a-archetype/a-05-tabs.md) · [B-10 파일 첨부](./b-10-file-attach.md)
- 기능 상세: `bgt-fe/src/docs/ko/features/Auth.md`
