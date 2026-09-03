---
paths:
  - "bgt-fe/src/**/*.{ts,tsx}"
---

# B-08. 공통코드 드롭다운

> **강제 규칙 (MUST)** — 이 케이스로 작업할 때 반드시 지킨다.
> - SP 코드 컬럼 규약을 `CODE`/`NAME`/`NAME_ENG` 로 맞추고 코드명은 null 대신 빈 문자열로 준다(label 이 null 이면 `Po`가 `label.length`에서 페이지 크래시).
> - 새 코드는 공통은 `useCommonCode.ts` 의 `COMMON_CODE_KEYS`, BGT 전용은 `useBgtCode.ts` 의 `BGT_CODE_QUERY_MAP` 에 등록한 뒤 사용한다.
> - 그리드 Enum 은 `useCode(...)` 의 `applyGridEnums` 를 `Events.onRenderFinish` 에서 호출하고, 서버 데이터 로드 전에 적용되지 않도록 하며 필요 시 `EnumStrictMode: 1` 로 미정의 값을 유지한다.

## 언제 쓰나
- 셀렉트박스/그리드 Enum 에 공통코드(그룹코드, 프로젝트별 리스트)를 채울 때.
- `useCode` 훅 + `Form.CodeSelect` 폼 컴포넌트 사용.

## 핵심 파일
- 통합 훅: `bgt-fe/src/hooks/pms/useCode.ts`
- 카테고리: `useCommonCode.ts`(COMMON_CODE_KEYS) · `useLaborCode.ts`(LABOR_CODE_KEYS) · `useBgtCode.ts`(BGT_CODE_QUERY_MAP)
- 폼: `bgt-fe/src/components/common/form/Form.tsx` (`Form.CodeSelect`)
- 전체 가이드: `bgt-fe/src/docs/ko/features/Code.md`

## 레시피

### 1. 코드가 이미 등록돼 있나 확인
- 공통(그룹코드): `COMMON_CODE_KEYS` 배열.
- BGT 전용 리스트: `BGT_CODE_QUERY_MAP` 객체.
- 없으면 추가(아래 2), 있으면 바로 사용(3).

### 2. 새 코드 추가
- 공통: `useCommonCode.ts` 의 `COMMON_CODE_KEYS` 에 서버 그룹코드 키 추가.
- BGT 전용: `useBgtCode.ts` 의 `BGT_CODE_QUERY_MAP` 에 `{ useQuery, select, params }` 항목 추가.

### 3. 폼에서 사용
```tsx
<Form.CodeSelect name="bizFldCd" code="BIZ_FLD_CD" isIncludeAll={false} defaultSelected={false} />
```

### 4. 그리드 Enum 에 적용
- `useCode('PARTNER')` 의 `applyGridEnums` 를 그리드 `Events.onRenderFinish` 에서 호출.
- Enum 컬럼은 초기값 `Enum: '|'`, `EnumKeys: '|'`, 필요 시 `EnumStrictMode: 1`.

## 코드 예제

### 폼 셀렉트 (계층형) — `features/Code.md` 발췌
```tsx
<Form.CodeSelect name="cstrnTypeCdLv1" code="CONSTRUCTION" params={{ level: '1' }} isIncludeAll={false} />
<Form.CodeSelect
  name="cstrnTypeCdLv2" code="CONSTRUCTION"
  parents={['cstrnTypeCdLv1']} params={{ level: '2', uprCd: '' }}
  isIncludeAll={false} isShowLoading={false}
/>
```

### 그리드 Enum 적용 — `features/Code.md:140`
```ts
const { applyGridEnums: applyPartnerEnums } = useCode('PARTNER');
const gridOptions = {
  Events: {
    onRenderFinish: (evtParams) => applyPartnerEnums(evtParams.sheet.id, 'partner'),
  },
};
```

### 실제 폼 사용 — `template/form/__components/FormBox.tsx:72`
```tsx
<Form.CodeSelect name="bizFldCd" width={FORM_ITEM_WIDTH} code="BIZ_FLD_CD" defaultSelected={false} isIncludeAll={false} />
```

## 흔한 실패와 가드
- **옵션 label 이 null 이면 페이지 크래시** — 드롭다운 내부(`Po`)가 `label.length` 접근 시 터진다. SP 가 코드명 컬럼을 빈 문자열 대신 null 로 주면 발생.
  - 가드: SP 결과 코드 컬럼 규약을 **`CODE` / `NAME` / `NAME_ENG`** 로 맞추고, `FormSelect` 의 label 가드를 거친다.
- **Enum 적용 전에 서버 데이터 로드** → 값이 빈 값으로 초기화됨. `EnumStrictMode: 1` 추가하면 정의 안 된 값도 유지.
- **그룹코드 키 오타** → 옵션 빈 배열. 등록 위치(`COMMON_CODE_KEYS`/`BGT_CODE_QUERY_MAP`) 확인.

## 검증 방법
```powershell
yarn build:local; npx tsc --noEmit
```

## 관련 문서
- [A-04 폼](../a-archetype/a-04-form.md) · [B-09 비동기 로드](./b-09-grid-async-load.md)
- 기능 상세: `bgt-fe/src/docs/ko/features/Code.md`
