---
paths:
  - "bgt-fe/src/**/*.{ts,tsx}"
---

# A-04. 폼 입력 화면

> **강제 규칙 (MUST)** — 이 케이스로 작업할 때 반드시 지킨다.
> - 검색폼(`searchForm`)과 데이터폼(`dataForm`)을 각각 `useForm` 으로 분리한다 — 합치면 검색 reset 이 본문 값을 날린다.
> - 본문 `Form.Provider` 는 `disabled={!isDataLoaded}` 로 조회 전 입력을 차단한다(빈 화면 저장 버그 방지).
> - 저장은 `dataForm.handleSubmit(handleSave)` 로 zod 검증 통과 시에만 실행한다.
> - `maxLength` 는 숫자 직접 대신 `getSafeMaxLength(...)` 를 쓰고, 날짜 표기는 `YYYY.MM.DD` 형식을 쓴다.
> - `Form.CodeSelect` 옵션 label null 크래시 주의 — SP 코드컬럼은 CODE/NAME/NAME_ENG 규약. → [B-08](../b-feature/b-08-code-dropdown.md)

## 언제 쓰나
- 그리드가 아니라 **단건 레코드를 폼(입력 필드들)으로** 조회/수정/저장하는 화면. (사업 기본정보, 상세 설정 등)
- react-hook-form + zod 검증, `Form.*` 입력 컴포넌트 + `Table.*` 레이아웃 조합.

## 참고 원본 (복사 기반)
```
bgt-fe/src/pages/template/form/
├── index.tsx                      # 검색폼 + 데이터폼 2개 Form.Provider
├── __components/
│   ├── SearchBox.tsx
│   └── FormBox.tsx                # Table 레이아웃 + Form.* 입력들
└── __utils/ (createDataSchema, fetchData, saveData ...)
```

## 레시피

### 1. 폼이 둘이다: 검색폼 + 데이터폼
- 검색 조건용 `searchForm` 과 본문 입력용 `dataForm` 을 **각각** `useForm` 으로 만든다.
- 본문 영역은 별도 `Form.Provider` 로 감싸고 `disabled={!isDataLoaded}` 로 조회 전 입력 차단.

### 2. 데이터폼 기본값
- `createDefaultValues(dataSchema)` 로 스키마 기반 기본값 생성. 조회 성공 시 `dataForm.reset(data)`.

### 3. 저장은 데이터폼 핸들서브밋
- `onSave={dataForm.handleSubmit(handleSave)}` 로 zod 검증 통과 시에만 저장.

### 4. 본문 레이아웃 (`FormBox.tsx`)
- `Table.Line` / `Table.Row` / `Table.Header` / `Table.Cell` 로 라벨-입력 격자.
- 입력: `Form.Input`, `Form.DatePicker`, `Form.PeriodPicker`, `Form.CodeSelect`, `Form.Select`, `Form.PercentInput` 등.

### 5. 검증
- `yarn build:local` + `npx tsc --noEmit`.

## 코드 예제

### 검색폼/데이터폼 분리 — `template/form/index.tsx:37`
```tsx
// 검색폼
const searchForm = useForm<SearchForm>({ resolver: zodResolver(searchSchema), defaultValues: condition });
// 데이터폼
const dataForm = useForm<DataForm>({
  resolver: zodResolver(dataSchema),
  defaultValues: createDefaultValues(dataSchema),
});
const [isDataLoaded, setIsDataLoaded] = useState(false);
```

### 조회 → reset, 저장 → 재조회 — `template/form/index.tsx:60`
```tsx
async function handleSearch(params: SearchForm) {
  setIsDataLoaded(false);
  const response = await fetchData(params);
  processResponse(response, {
    onSuccess: ({ data }) => {
      dataForm.reset(data);   // 폼에 값 채우기
      setCondition(params);
      setIsDataLoaded(true);
    },
  });
}
```

### 본문 렌더 — 데이터폼 Provider + disabled — `template/form/index.tsx:112`
```tsx
<Form.Provider formId={`${PAGE_ID}-data`} form={dataForm} disabled={!isDataLoaded}>
  <FormBox />
</Form.Provider>
```

### 라벨-입력 격자 — `template/form/__components/FormBox.tsx:43`
```tsx
<Table.Row>
  <Table.Header labelText={getText('lbl', '사업승인명')} width={TABLE_HEADER_WIDTH} />
  <Table.Cell>
    <Form.Input name={'bizApprNm'} type="text" maxLength={getSafeMaxLength(300)} />
  </Table.Cell>
  <Table.Header labelText={getText('lbl', '사업승인일자')} width={TABLE_HEADER_WIDTH} />
  <Table.Cell>
    <Form.DatePicker name={'bizCnfYmd'} size="small" />
  </Table.Cell>
</Table.Row>
```

## 흔한 실패와 가드
- **날짜 표기 형식** → 날짜 형식은 `YYYY.MM.DD` 형식을 사용한다.
- **검색폼과 데이터폼을 하나로 합침** → 검색 reset 이 본문 값을 날리는 등 충돌. 반드시 분리.
- **조회 전 입력 가능** → `disabled={!isDataLoaded}` 누락. 빈 화면에서 저장되는 버그.
- **maxLength 직접 숫자** → DB 바이트 한계 고려해 `getSafeMaxLength(...)` 사용 패턴 따름.
- **드롭다운 label null 크래시** → `Form.CodeSelect` 옵션 label 이 null 이면 페이지 크래시. SP 코드컬럼은 CODE/NAME/NAME_ENG 규약. → [B-08](../b-feature/b-08-code-dropdown.md)

## 검증 방법
```powershell
yarn build:local; npx tsc --noEmit
```

## 관련 문서
- [B-08 코드 드롭다운](../b-feature/b-08-code-dropdown.md) · [A-01 단일 그리드](./a-01-single-grid-crud.md)
- 기능 상세: `bgt-fe/src/docs/ko/features/Form.md`
