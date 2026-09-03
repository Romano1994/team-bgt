---
paths:
  - "bgt-fe/src/**/__types.ts"
  - "bgt-fe/src/**/__type.ts"
---

# B-22. zod 검증 표준 (검색/폼 스키마)

> **강제 규칙 (MUST)** — 이 케이스로 작업할 때 반드시 지킨다.
> - 검증 스키마는 **`@/utils`(=`src/utils/zod.ts`)의 헬퍼**로 만든다. 공통 케이스(`필수/선택` × `문자/숫자/Y·N/날짜/패턴/다국어`)에 raw `z.string()`/`z.number()`를 직접 쓰지 않는다.
> - 헬퍼로 못 거는 **추가 제약만** raw `z` 체이닝 허용(예: `z.string().max(50).optional()`). 그 외는 헬퍼.
> - 스키마는 **팩토리 함수**로 만든다: `createSearchSchema(getText)` / `createDataSchema(getText)`. 에러 메시지는 **`getText('msg', MSG_*)`**(i18n 상수) — 하드코딩 문자열 지양.
> - 타입은 스키마에서 **파생**한다: `type SearchForm = z.infer<ReturnType<typeof createSearchSchema>>`. 별도 수기 타입 금지.
> - RHF 기본값은 **`createDefaultValues(schema)`** 로 생성한다(수기 defaultValues 금지). 폼 저장은 `handleSubmit` 으로 검증 통과 시에만. → A-04
> - 파일 위치: 스키마/타입은 화면의 `__utils/__types.ts`.

## 언제 쓰나
- 검색 조건(SearchForm) 또는 폼 입력(DataForm)에 **필수값·형식 검증**을 걸 때. 거의 모든 조회/폼 화면.

## 참고 원본 (복사 기반)
```
bgt-fe/src/utils/zod.ts                              # 헬퍼 패밀리(원천)
bgt-fe/src/pages/template/default/__utils/__types.ts # createSearchSchema 예
bgt-fe/src/pages/template/form/__utils/__types.ts    # createSearchSchema + createDataSchema 예
```
> 기본 가이드: `bgt-fe/src/docs/ko/features/Form.md`, `development/Front.md`, `general/Library.md`.

## 헬퍼 패밀리 (`src/utils/zod.ts`)

| 필수 | 선택 | 용도 |
| --- | --- | --- |
| `requiredString(msg)` | `optionalString()` | 문자 |
| `requiredNumber(msg)` | `optionalNumber()` | 숫자(문자→숫자 변환 포함) |
| `requiredStringNumber(msg)` | `optionalStringNumber()` | 문자\|숫자(제한적) |
| `requiredYn(msg)` | `optionalYn()` | `'Y'`/`'N'` enum |
| `requiredDate(fmt, msg, fmtMsg?)` | `optionalDate(fmt, fmtMsg)` | 날짜(dayjs strict), `validDateFormat(msg, fmt)` |
| `requiredPattern(re, msg, patMsg?)` | `optionalPattern(re, patMsg)` | 정규식 |
| `requiredLangCode(msg?)` | `optionalLangCode()` | 다국어 코드 |
| — | `createDefaultValues(schema)` | 스키마 → RHF 기본값 자동 생성 |

> 설계 규칙: `optional*` 헬퍼는 `preprocess` 로 `''`/`null` → `undefined` 정규화 후 optional. 그래서 빈 입력이 서버에 `''` 로 새지 않는다.

## 레시피

### 1. 검색 스키마 (`__utils/__types.ts`)
- `createSearchSchema(getText)` 가 `z.object({...})` 반환. 필드마다 헬퍼로 required/optional 지정.
- `type SearchForm = z.infer<ReturnType<typeof createSearchSchema>>`.

### 2. 폼 스키마 (폼 화면)
- `createDataSchema(getText)` 로 동일 패턴. `useForm({ resolver: zodResolver(dataSchema), defaultValues: createDefaultValues(dataSchema) })`. → A-04

### 3. 검증
- `npx tsc --noEmit` (스키마↔타입 파생 확인) + `yarn build:local`.

## 코드 예제

### 검색 스키마 — `template/default/__utils/__types.ts:7`
```ts
import { optionalLangCode, optionalString, requiredString } from '@/utils';
import z from 'zod';

export const createSearchSchema = (getText: ReturnType<typeof useText>['getText']) =>
  z.object({
    langCd: optionalLangCode(),
    prjCd: requiredString(getText('msg', MSG_PROJECT_REQUIRED)),
    prjNm: optionalString(),
    keyword: z.string().max(50).optional(),   // 헬퍼로 못 거는 제약만 raw z 허용
  });

export type SearchForm = z.infer<ReturnType<typeof createSearchSchema>>;
```

### 날짜 필수 + 폼 스키마 — `template/form/__utils/__types.ts`
```ts
searchYmd: requiredDate('YYYYMMDD',
  getText('msg', '조회일자를 선택해주세요.'),
  getText('msg', MSG_INVALID_DATE_FORMAT)),
// ...
export const createDataSchema = (getText) => z.object({
  bizApprNm: requiredString(getText('msg', '사업승인명을 입력해주세요.')),
  bizCnfYmd: requiredDate('YYYYMMDD', getText('msg', '...'), getText('msg', MSG_INVALID_DATE_FORMAT)),
  pmEmpNo:   optionalString(),
});
```

### 기본값 자동 생성 — A-04
```ts
const dataForm = useForm<DataForm>({
  resolver: zodResolver(dataSchema),
  defaultValues: createDefaultValues(dataSchema),  // 수기 defaultValues 금지
});
```

## 흔한 실패와 가드
- **raw `z.string()` 남발** → 빈 문자열이 `''` 로 서버에 전송/에러 메시지 불일치. 공통 케이스는 헬퍼(`requiredString`/`optionalString` …) 사용.
- **에러 메시지 하드코딩** → 다국어 누락. `getText('msg', MSG_*)`.
- **타입을 수기로 별도 선언** → 스키마와 어긋남. `z.infer<ReturnType<typeof create*Schema>>` 로 파생.
- **`defaultValues` 수기 작성** → 필드 누락 시 uncontrolled 경고. `createDefaultValues(schema)`.
- **날짜를 `requiredString` 으로만 검증** → 형식 오류 통과. `requiredDate(fmt, msg, fmtMsg)` 로 dayjs strict 형식 체크.
- **Y/N 을 `requiredString`** → 임의 문자 허용. `requiredYn` 으로 enum 강제.

## 검증 방법
```powershell
# bgt-fe 에서
npx tsc --noEmit
yarn build:local
```

## 관련 문서
- [A-04 폼 입력 화면](../a-archetype/a-04-form.md) · [A-01 단일 그리드](../a-archetype/a-01-single-grid-crud.md) · [D-16 신규 화면 체크리스트](../d-workflow/d-16-new-screen-checklist.md)
- 헬퍼 원천: `bgt-fe/src/utils/zod.ts` · 기능 상세: `bgt-fe/src/docs/ko/features/Form.md`
