---
paths:
  - "bgt-fe/src/**/__dialog/**/*.{ts,tsx}"
  - "bgt-fe/src/**/__popup/**/*.{ts,tsx}"
---

# A-03. 팝업/다이얼로그 상세 화면

> **강제 규칙 (MUST)** — 이 케이스로 작업할 때 반드시 지킨다.
> - Dialog 는 `isOpen`/`onClose`/`onRefresh` 3개 prop 으로 부모와 통신하고, 저장 성공 시 반드시 `onRefresh()`(부모 목록 갱신) + 팝업 자체 재조회를 호출한다.
> - 목록 그리드의 상세 진입 PK 는 숨김 컬럼(`Visible:0`)으로 선언한다 — 미선언 시 `onDblClick` 의 `row` 에 PK 가 안 실려 진입 가드가 막힌다(예: cntrct-ctrt-lst PK 6종).
> - IBSheet 컬럼 `Name` 은 BE 응답 VO 의 JSON 키(camelCase)와 정확히 일치시킨다 — VO 는 `@JsonProperty("camelCase")` 로 고정. UPPER_SNAKE 혼용 금지. → [C-13](../c-api/c-13-new-api.md)
> - 팝업 안 그리드 저장의 삭제행 누락은 [B-07](../b-feature/b-07-grid-cud-save.md) 을 동일 적용한다.

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
- `isOpen` / `onClose` / `onRefresh` 3개 prop 으로 부모와 통신.

### 2. 부모에서 open 상태와 onRefresh 관리
- 부모(`index.tsx`)가 다이얼로그 open state 를 가지고, `onRefresh={() => handleSearch(condition)}` 를 내려준다.

### 3. 팝업 내부는 독립 화면
- 팝업 컨텐츠는 자체 `Form.Provider`, 자체 그리드, 자체 조회/저장을 가진다. (A-01 구조를 그대로 팝업 안에 둔 형태)

### 4. 저장 성공 시 부모+자기 자신 모두 갱신
- `onRefresh()` (부모 목록) 호출 + 팝업 자체 재조회.

### 5. 검증
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
