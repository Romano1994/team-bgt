---
paths:
  - "bgt-fe/src/**/__dialog/**/*.{ts,tsx}"
  - "bgt-fe/src/**/__popup/**/*.{ts,tsx}"
---

# A-03. 팝업/다이얼로그 상세 화면

> **강제 규칙 (MUST)** — 이 케이스로 작업할 때 반드시 지킨다.
> - Dialog 는 `isOpen`/`onClose` + 결과 콜백으로 부모와 통신한다 — 저장형은 `onRefresh`(저장 성공 시 반드시 호출 + 팝업 자체 재조회), 선택형은 `onApply`/`onSelect` 로 값을 돌려준다.
> - 목록 그리드의 상세 진입 PK 는 숨김 컬럼(`Visible:0`)으로 선언한다 — 미선언 시 `onDblClick` 의 `row` 에 PK 가 안 실려 진입 가드가 막힌다(예: cntrct-ctrt-lst PK 6종).
> - IBSheet 컬럼 `Name` 은 BE 응답 VO 의 JSON 키(camelCase)와 정확히 일치시킨다 — VO 는 `@JsonProperty("camelCase")` 로 고정. UPPER_SNAKE 혼용 금지. → [C-13](../c-api/c-13-new-api.md)
> - 팝업 안 그리드 저장의 삭제행 누락은 [B-07](../b-feature/b-07-grid-cud-save.md) 을 동일 적용한다.
> - 하단 버튼은 `DialogButtonGroup` 안에 **좌 [취소/닫기](outlined·normal) → 우 [확정](contained·primary)** 순으로 두고, 확정 버튼에 `ControlConfirmIcon` + `autoFocus` 를 단다. 생 `Box`/`Stack` 금지. → §5
> - 버튼 라벨은 성격대로 고른다 — 선택 팝업 `취소`+`선택` / 입력·저장 팝업 `취소`+`저장` / 읽기전용 조회 팝업 `닫기` 단독. 문자열은 `TEXT.BTN.*` 상수.
> - `size` 는 단계값(`400`·`600`·`800`·`1000`·`1200`·`1400`·`1600`)만 쓴다. 임의값(`950` 등) 금지.
> - 상태성 안내·경고를 본문 `<Typography>` 로 깔지 않는다 — `showToast`/`showAlert`/`showConfirm` 으로 알린다. → §6
> - 조립·치환·서버원문 메시지에는 `{ isTranslated: true }` 를 붙인다(`useModal` 기본값이 `false` 라 다시 번역돼 `__` 폴백이 붙는다). 메시지 리터럴에 콜론(`:`) 금지. → §6

## 언제 쓰나
- 목록 화면에서 행을 더블클릭하거나 버튼을 눌러 **모달(Dialog)** 로 상세/등록 화면을 띄우고, 저장 후 부모 목록을 새로고침하는 경우.

## 참고 원본 (복사 기반)
```
bgt-fe/src/pages/template/dialog/
├── index.tsx                      # 목록 + onRefresh 전달
├── __components/ (SearchBox, GridBox, CustomButtons)
└── __dialog/
    ├── FirstDialog.tsx            # Dialog 래퍼 + 내부 컨텐츠 컴포넌트
    └── __first/ (FirstSearchBox, FirstGridBox, ...)
```
실제 예(cst 안정): `cst/cst-fe/src/pages/cm/partnercompany/` (목록 + `__components/PartnerCompanyPopup.tsx` Dialog. 단 선택형 팝업이라 저장→onRefresh 는 부모의 조건 감시 재조회로 대체)

## 레시피

### 1. Dialog 래퍼 패턴 — `template/dialog/__dialog/FirstDialog.tsx:43`
- 바깥은 `@amxis/design-system` 의 `Dialog`, 내용은 별도 컨텐츠 컴포넌트로 분리한다.
- `isOpen` / `onClose` + 결과 콜백(저장형 `onRefresh`, 선택형 `onApply`/`onSelect`)으로 부모와 통신.

### 2. 부모에서 open 상태와 onRefresh 관리
- 부모(`index.tsx`)가 다이얼로그 open state 를 가지고, `onRefresh={() => handleSearch(condition)}` 를 내려준다.

### 3. 팝업 내부는 독립 화면
- 팝업 컨텐츠는 자체 `Form.Provider`, 자체 그리드, 자체 조회/저장을 가진다. (A-01 구조를 그대로 팝업 안에 둔 형태)

### 4. 저장 성공 시 부모+자기 자신 모두 갱신
- `onRefresh()` (부모 목록) 호출 + 팝업 자체 재조회.

### 5. 팝업 UI 규약 (룰북 D1~D14)
BGT 정본 = `bgt-fe/src/components/codefind/modals/CodefindEmp.tsx`(껍데기 `:178-186` · 버튼 `:238-255`).
아래는 그 파일에 이미 다 들어 있다 — 새로 만들지 말고 복사한다.

| # | 항목 | 통과 기준 |
| --- | --- | --- |
| D1 | 껍데기 | `position="basic"` · `type={'modal'}` · `size` 단계값 · 본문은 `customContentArea` (children 직접 삽입 금지) |
| D2 | 제목 | `customHeaderTitle={<CustomDialogTitle title={getText(...)} />}` (`@/components/modals/common/CustomDialogTitle`) |
| D3 | 본문 골격 | 선택·입력 = `DialogContentContainer` → `DialogContent` / 조회·그리드 = `ContentWrapper` → `ContentBox`. 생 `Stack`·`Box` 를 본문 최상단에 나열하지 않는다 |
| D4 | 버튼 래퍼 | `DialogButtonGroup`(`@/components/layout/layouts`) |
| D5 | 버튼 구성 | 좌 [취소/닫기] outlined·normal → 우 [확정] contained·primary + `ControlConfirmIcon`(`@amxis/design-system/icon`) + `autoFocus` |
| D6 | 라벨 성격 | 선택=취소/선택 · 저장=취소/저장 · 조회=닫기 단독. `TEXT.BTN.*` 상수 사용 |
| D7 | 인라인 안내문 | 상태성 안내·경고를 본문 `<Typography>` 로 깔지 않음 → §6 |
| D8 | isTranslated | 조립·치환·서버원문 메시지 전부 `{ isTranslated: true }` → §6 |
| D9 | 메시지 콜론 | 메시지 리터럴에 `:` 없음 → §6 |
| D10 | 그리드 | 고정 높이는 **상수**로 선언(`const RESULT_GRID_HEIGHT = 380;`) · 건수는 `IBSheetGridWrapperHeader left={{ title, total }}` 에만(본문에 요약 텍스트 중복 금지) · `Events: {}` 존재 |
| D11 | props | `isOpen`/`onClose` 보유. 부모 목록 갱신은 `onRefresh`, 값 반환은 `onApply?`/`onSelect` 콜백 |
| D12 | 상태 초기화 | `isOpen === false` 에서 폼·그리드 리셋(`reset()` + `resetGrid(gridName)`) |
| D13 | 권한 | 권한이 걸린 액션은 `AuthButton` + `requiredAuthLevel` → [B-11](../b-feature/b-11-auth.md) |
| D14 | 다국어 | 하드코딩 한글 0(`getText('lbl'\|'btn'\|'msg'\|'grd', ...)`) · 버튼 라벨은 `TEXT.BTN` 상수 |

> 레거시 팝업이 이 규칙을 어기고 있다고 해서 신규가 따라갈 근거가 되지 않는다.

### 6. 팝업 메시지 3규칙
1. **인라인 안내문 금지** — 검증 실패·건수 요약은 `showToast(..., { usecase: 'warning' })`, 놓치면 안 되는 경고·서버 오류는 `showAlert`, 되돌릴 수 없는 실행 직전은 `showConfirm`. `<Typography>` 는 라벨·설명문 용도로만.
2. **`{ isTranslated: true }`** — `useModal` 의 기본값이 `isTranslated = false`(`bgt-fe/src/hooks/pms/useModal.ts:34,72`)라 넘긴 문자열을 **또 번역한다**. `getText` 결과를 이어붙이거나 `${}` 로 건수를 끼우면 등록 키와 달라져 **문장 통째로 `__` 폴백**이 붙는다.
   ```tsx
   // ✗ 조립인데 옵션 없음 → 화면에 __ 가 붙는다
   showToast(getText('msg', '적용했습니다.') + ` (${n})`);
   // ⭕
   showToast(getText('msg', '적용했습니다.') + ` (${n})`, { isTranslated: true });
   ```
3. **메시지 리터럴에 콜론(`:`) 금지** — `escapePath`(`bgt-fe/src/utils/text.ts`)가 조회 경로의 콜론을 지워 등록해도 영원히 미조회가 된다. 목록 구분은 괄호로.

### 7. 검증
- `yarn build:local` + `npx tsc --noEmit`.

## 코드 예제

### Dialog 래퍼 — `template/dialog/__dialog/FirstDialog.tsx:43`
```tsx
export const FirstDialog = (props: FirstDialogProps) => {
  const { isOpen, onClose } = props;
  const { getText } = useText();
  return (
    <Dialog
      open={isOpen}
      handleClose={onClose}
      position="basic"
      type={'modal'}
      size={1400}
      title={getText('lbl', 'First Dialog')}
      customContentArea={<FirstDialogContent {...props} />}
    />
  );
};
```

### 저장 후 부모+자기 갱신 — `template/dialog/__dialog/FirstDialog.tsx:108`
```tsx
async function handleSave() {
  const rows = getGridJsonData<FirstListItem>(gridName);
  if (!checkSaveValid(rows)) return;
  const isConfirm = await showConfirm(MSG_SAVE_CONFIRM);
  if (!isConfirm) return;
  const response = await saveFirst(rows);
  processResponse(response, {
    successMessage: MSG_SAVE_SUCCESS,
    onSuccess: () => {
      onRefresh();            // 부모 목록 갱신
      handleSearch(condition); // 팝업 자체 갱신
    },
  });
}
```

## 흔한 실패와 가드
- **팝업이 닫혀도 그리드 상태가 남음** → 팝업 컨텐츠를 `isOpen` 기준으로 언마운트하거나(=Dialog 가 customContentArea 를 조건부 렌더), open 시 초기 조회를 트리거.
- **저장 후 부모 미갱신** → 반드시 `onRefresh()` 호출.
- **팝업 안 그리드 저장에서 삭제행 누락** → [B-07](../b-feature/b-07-grid-cud-save.md) 동일 적용.
- **목록 행 더블클릭으로 상세 진입이 안 됨(상세 미오픈)** → 상세 조회/식별용 **PK를 목록 그리드의 숨김 컬럼(`Visible:0`)으로 선언**한다. IBSheet `onDblClick` 의 `row` 에 PK가 실리려면 그 PK가 컬럼으로 선언돼 있어야 안전하다(미선언 시 진입 가드 `if (!row.cntrYr) return` 가 막힐 수 있음). ASIS(DevExpress)도 PK를 그리드 셀로 보유해 `GetRowCellValue` 로 읽는다. 예: `cntrct-ctrt-lst/_utils/columns.ts` 의 PK 6종(cntrYr/mktDivs/seq/yrDivs/chanSeq/contrlv) 숨김 컬럼.
- **그리드 표시/더블클릭 키가 BE 응답과 안 맞음** → IBSheet 컬럼 `Name` 은 **BE 응답 VO 의 JSON 키와 정확히 일치**해야 한다. BGT 표준은 응답 VO 전 필드에 **`@JsonProperty("camelCase")`** 를 달아 직렬화 키를 camelCase 로 고정하고(예: `CntrctBidListResponseVO`), FE 그리드 `Name`·행 타입도 같은 camelCase 로 맞춘다. UPPER_SNAKE 혼용/누락은 셀 공백·더블클릭 키 불일치를 부른다. → [C-13](../c-api/c-13-new-api.md)

## 검증 방법
```powershell
yarn build:local; npx tsc --noEmit
```

## 관련 문서
- [A-01 단일 그리드](./a-01-single-grid-crud.md) · [B-12 모달/토스트](../b-feature/b-12-modal-toast.md)
- 기능 상세: `bgt-fe/src/docs/ko/features/Modal.md`
- §5·§6 근거(룰북 원문): `docs/pmx-uiux-audit/rules/rulebook/05-dialog-standards.md` — 팝업 277개 전수 스캔 기반. 이 문서와 어긋나면 원문이 이긴다.
