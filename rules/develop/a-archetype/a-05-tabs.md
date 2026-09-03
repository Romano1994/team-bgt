---
paths:
  - "bgt-fe/src/**/__tabs/**/*.{ts,tsx}"
---

# A-05. 탭 화면

> **강제 규칙 (MUST)** — 이 케이스로 작업할 때 반드시 지킨다.
> - 각 탭 객체는 `mnuUrl`(권한 체크용)을 반드시 가지며, 메뉴 관리·프로그램 관리에 동일 URL 을 등록한다(불일치 시 탭 권한 체크 실패). → [B-11](../b-feature/b-11-auth.md)
> - 페이지 props 의 `mnuId` 를 `AuthTabs` 로 그대로 전달한다(누락 금지).
> - 탭 내용은 한 컴포넌트에 몰지 말고 탭별 폴더(`__tabs/__first` 등)로 분리해 A-01 구조를 재사용한다.

## 언제 쓰나
- 한 화면에 여러 업무 영역을 탭으로 나누고, 탭마다 자체 검색/그리드를 갖는 경우.
- 탭별 권한 체크(메뉴 URL 기준)가 필요할 때.

## 참고 원본 (복사 기반)
```
bgt-fe/src/pages/template/tabs/
├── index.tsx                      # AuthTabs + tabs 배열
└── __tabs/
    ├── FirstTab.tsx               # 탭1 (자체 SearchBox/GridBox)
    ├── SecondTab.tsx
    └── __first/, __second/ (탭별 컴포넌트)
```

## 레시피

### 1. AuthTabs 사용 — `template/tabs/index.tsx:49`
- `@/components/auth` 의 `AuthTabs` 에 `mnuId`, `tabIndex`, `tabs`, `onChange` 전달.
- 각 탭 객체는 `content` 와 **`mnuUrl` (권한 체크용)** 을 가진다. `mnuUrl` 은 메뉴/프로그램 관리에 등록된 URL 과 일치해야 함.

### 2. 탭 간 데이터 전달
- 탭1 에서 선택한 조건을 부모 state 로 올리고 (`onTabChange`), 탭2 에 prop 으로 내려 연동.

### 3. 탭별 화면은 A-01 구조 재사용
- 각 탭 컴포넌트 내부는 단일 그리드/마스터-디테일 등 다른 케이스를 그대로 적용.

### 4. 검증
- `yarn build:local` + `npx tsc --noEmit`.

## 코드 예제

### 탭 정의 (mnuUrl 필수) — `template/tabs/index.tsx:25`
```tsx
const tabs = [
  {
    content: <FirstTab onTabChange={handleTabChange} />,
    // AuthTab 에서 URL로 권한 체크하므로 반드시 필요 (메뉴/프로그램 관리에도 등록)
    mnuUrl: `${MODULE_CODE}/template/tabs/first`,
  },
  {
    content: <SecondTab selectedCondition={selectedCondition} />,
    mnuUrl: `${MODULE_CODE}/template/tabs/second`,
    // disabled: !!selectedCondition, // 조건부 탭 활성/비활성
  },
];
```

### 탭 변경 + 조건 전달 — `template/tabs/index.tsx:41`
```tsx
function handleTabChange(index: number, selectedCondition?: SecondSearchForm) {
  setTabIndex(index);
  setSelectedCondition(selectedCondition);
}
```

## 흔한 실패와 가드
- **`mnuUrl` 누락/불일치** → 탭 권한 체크 실패. 메뉴 관리·프로그램 관리에 동일 URL 등록 필수.
- **탭 내용을 한 컴포넌트에 다 작성** → 탭별 폴더(`__tabs/__first` 등)로 분리해 A-01 구조 재사용.
- **`mnuId` 미전달** → 페이지 props 의 `mnuId` 를 `AuthTabs` 로 그대로 전달.

## 검증 방법
```powershell
yarn build:local; npx tsc --noEmit
```

## 관련 문서
- [B-11 권한 적용](../b-feature/b-11-auth.md) · [A-01 단일 그리드](./a-01-single-grid-crud.md)
- 기능 상세: `bgt-fe/src/docs/ko/features/Auth.md`
