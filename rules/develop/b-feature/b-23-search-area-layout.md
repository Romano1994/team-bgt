---
paths:
  - "bgt-fe/src/**/SearchBox.tsx"
  - "bgt-fe/src/**/*SearchBox.tsx"
---

# B-23. 조회조건(SearchArea) 레이아웃

> **강제 규칙 (MUST)** — 이 케이스로 작업할 때 반드시 지킨다.
> - 전 필드 `labelAlign="left"` + **열 단위로 동일한** `labelMinWidth`. 화면 전체 단일값 금지, 열마다 다른 값을 쓸 땐 상수명·주석에 근거를 남긴다. → §1
> - 조건을 전폭 묶음 `<Grid container item xs={12}>` 로 감싸지 않는다 — `conditions` 에 바로 둔다. → §2
> - 한 행의 `xs` 합계는 **12 이하**. 마지막 행이 미달해도 빈칸을 억지로 채우지 않는다(남는 폭은 버튼영역이 쓴다). → §2
> - 버튼영역은 DS `SearchArea` 가 그린다. **자리 예약 칸·버튼 띄우기 `sx` 금지.** → §4
> - 기간(`PeriodPicker`)만 `width="100%"`. 단일 `Form.DatePicker` 엔 금지(컨테이너만 늘어나 인풋 뒤 흰 칸이 생긴다).
> - 다중선택은 **태그형 `DropdownField multiple`**. 체크박스 바둑판 배열 금지.
> - 공용 컴포넌트가 있는 조건은 반드시 그것을 쓴다(`Form.ProjectInput` 등) — 복붙 + 고정폭 하드코딩 금지. → §5

## 언제 쓰나
- `SearchBox.tsx` 의 `conditions` 를 새로 짜거나 조건을 더하고 뺄 때.
- 조회조건 값이 잘리거나, 버튼이 한 줄을 더 먹거나, 같은 열 입력칸 시작선이 어긋날 때.

## 참고 원본 (복사 기반)
- `bgt-fe/src/pages/template/default/__components/SearchBox.tsx` — `sx`·예약 칸·묶음 없이 `conditions` 에 조건만 둔 기본형
- **룰북 원문**: `docs/pmx-uiux-audit/rules/rulebook/01-fe-standards_5-1-search-area.md` (§5-1~§5-1-3, 2026-09-11 판).
  이 문서와 어긋나면 원문이 이긴다.

## 레시피

### 1. 라벨 — `labelAlign="left"` + 열 단위 `labelMinWidth`

불변식은 둘뿐이다.

1. **전 필드 `labelAlign="left"`** — `Form.Input`/`Form.CodeSelect`/`Form.Checkbox`/커스텀 `FormItem` 전부.
2. **같은 열 안에서는 `labelMinWidth` 한 값.** 열이 다르면 값이 달라도 된다.

노리는 것은 *같은 열의 위·아래 입력칸 시작점이 맞는 것*이고, 그건 열 단위로 맞으면 달성된다.
화면 전체를 한 값으로 묶으면 짧은 열에 과한 여백이 생긴다. 열별로 다른 값을 쓸 땐 **상수명이나 주석에 근거**를 남긴다(`LABEL_MIN_WIDTH` / `LABEL_MIN_WIDTH_SHORT`).

**산정치(권고, ±1단 허용)** — 한글 1자 ≈ 14px, 슬래시·공백 ≈ 6px, 필수 `*` 있으면 `+8`.

| 라벨 길이 | ≤4자 | 5자 | 6자 | 7자 | 8자 | 9자+ |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `labelMinWidth` | `70` | `78` | `90` | `100` | `110` | `120` |

- 숫자는 **권고치**다. 강제되는 불변식은 "열 단위 단일값 + `labelAlign="left"`" 이므로 숫자만 보고 이탈로 판정하지 않는다.
- 라벨을 넓혀 입력칸이 실제 값을 다 못 보여주면 **그 열의 라벨을 줄인다**(라벨 폭 < 실제 입력 데이터 길이가 우선).
- 열 배치(`xs`)를 바꾸면 칸이 다른 열로 옮겨가므로 **열별 라벨 값도 다시 맞춘다.**

### 2. 배치 — 전폭 묶음 금지 + 한 행 12

```tsx
// ✗ 전폭 묶음 — 안쪽 열 합과 무관하게 12열을 통째로 먹어
//   뒤 조건과 버튼영역이 그 행의 남은 자리로 올라오지 못한다
<Grid container item xs={12}>
  <Form.Input xs={3} ... />
  <Form.CodeSelect xs={3} ... />
</Grid>

// ⭕ conditions 에 바로 둔다. 12를 넘기면 자동 줄바꿈된다
<>
  <Form.Input xs={3} ... />
  <Form.CodeSelect xs={3} ... />
</>
```

- 묶음이 `conditions` 의 유일한 부모면 **fragment(`<>…</>`)** 로 바꿔야 JSX 가 깨지지 않는다.
- **한 행 합계 12 이하.** 마지막 행이 미달하는 건 허용 — 빈칸을 억지로 채우지 않는다.
- **조건 개수로 `xs` 계열을 정하지 않는다.** 조건 수와 계열은 상관이 없다(전수 실측). 유형별로 §3 을 쓴다.
- 대/중/소 캐스케이드·체크박스 묶음 같은 복합 필드는 단일 라벨 `FormItem` 으로 감싸 2칸 이상 점유.

### 3. 조건 유형별 열수

| 조건 유형 | 열수 |
| --- | --- |
| 콤보 · 단일날짜 · 라디오 | **2열** |
| 키워드 입력 | 2열(라벨 3자↓) / **3열**(4자↑) |
| 기간(`PeriodPicker`) · 멀티칩 · 코드+명 팝업(담당자·협력사·품목군) | **3열** |
| 프로젝트(`Form.ProjectInput`) | **4열** |
| 3단 연동 콤보 | **줄이지 않는다** — 현행 유지 |

> **전제 주의** — 이 조견표는 룰북이 **DS 0.0.88**(버튼영역이 조건 Grid 의 마지막 item)에서 1열 폭 131.8px(1617 창)로 산출한 값이다.
> **BGT 런타임은 호스트 com-fe 의 DS 0.0.86** 이고(`bgt-fe/yarn.lock`·`com/com-fe/node_modules` 모두 0.0.86 — `bgt-fe/package.json` 의 0.0.88 은 미반영), 0.0.86 `SearchArea` 는 버튼영역이 **조건 Grid 밖 flex 형제**라 조건 Grid 폭이 버튼 폭만큼 좁다.
> 열수는 출발점으로 쓰되 **1617 창 실측으로 확정**한다. 값이 잘리거나 여백이 과하면 조견표보다 실측이 우선이다.

### 4. 버튼영역 — 배선하지 않는다

DS `SearchArea` 가 `conditions` 뒤에 버튼영역을 직접 그린다. 화면에서 할 일이 없다.

- **자리 예약 칸 금지** — 버튼 몫으로 빈 `Grid item` 을 잡지 않는다. 버튼을 한 줄 더 밀어낸다.
- **버튼 띄우기 `sx` 금지** — DS 내부 구조를 겨냥한 `sx` 는 DS 버전이 오르면 해로운 코드가 된다.
- `buttonPosition="bottom"` 을 주면 버튼이 그리드 밖 아래 별도 영역으로 간다(기본값 = 조건 우측). 필요할 때만.
- 조회영역 버튼 순서·색상은 UIUX 표준을 따른다 → `.claude/rules/UIUX/04-components.md` §Placement — 조회 영역.

### 5. 셀을 넓혔으면 입력 컨트롤이 그 폭을 쓰는지 확인한다

**열수를 키워도 입력 컨트롤은 저절로 따라오지 않는다.** 셀만 넓어지고 컨트롤이 예전 폭에 머물면 값은 그대로 잘린다.

- 공용 컴포넌트를 안 쓰고 `SearchInput` 을 복붙한 뒤 `width: 90~100px` 을 하드코딩한 사례가 대표적이다 → **공용 컴포넌트로 교체**한다.
- 판정은 **"셀 폭"이 아니라 "실제 클리핑"** 으로 한다. 텍스트 필요폭이 상자보다 커도 `overflow: visible` 이면 그냥 보인다(라벨이 여기 해당해 2~5px 오탐이 대량 발생).
  `scrollWidth > clientWidth` **와** `overflow:hidden`/`text-overflow:ellipsis` 를 함께 요구한다.

### 6. 적용 제외

아래 두 곳은 **화면 폭을 다 쓰는 검색영역** 전제가 성립하지 않는다. §3 조견표를 대지 않는다(§1·§2 는 그대로 적용).

| 제외 대상 | 이유 |
| --- | --- |
| 팝업(`Dialog`) 내부 | size 400~1600 이라 1열 폭이 화면과 전혀 다르다 |
| 스플리터 패널 내부 | 패널 비율에 따라 1열이 30px 아래로도 떨어진다 |

### 7. 검증
```powershell
yarn build:local
npx tsc --noEmit
```
- 라벨·잘림은 빌드로 안 잡힌다. **1617 창에서 눈으로 확인**한다(정렬 = 같은 열 입력칸 왼쪽 끝, 잘림 = 값 끝이 실제로 가려졌거나 `…` 가 붙었을 때만).
- 조건을 고친 뒤 **런타임 행수가 늘지 않았는지** 확인한다. 소스에 없는 그리드 셀을 런타임에 추가로 렌더하는 화면이 있어 정적 계산이 빗나간다. 행이 늘었으면 되돌린다.

## 흔한 실패와 가드
- **전폭 묶음으로 감쌈** → 뒤 조건·버튼이 그 행에 못 올라와 행이 하나 더 생긴다. `conditions` 에 바로 두고, 유일 부모면 fragment 로.
- **화면 전체를 한 `labelMinWidth` 로 통일** → 짧은 열에 과한 여백, 긴 열엔 입력칸 잘림. **열 단위**로 잡는다.
- **`labelAlign` 미지정** → 라벨이 우측 정렬돼 같은 열 입력 시작선이 라벨 길이에 따라 흔들린다. 전 필드 `left`.
- **단일 `Form.DatePicker` 에 `width="100%"`** → 컨테이너만 늘어나 인풋 뒤에 흰 칸이 생긴다. `PeriodPicker` 에만.
- **다중선택을 체크박스 바둑판으로 배치** → 태그형 `DropdownField multiple` 로.
- **버튼 자리를 칸으로 예약** → 버튼이 한 줄 더 밀린다. DS 가 그리게 둔다.
- **`bgt-fe/src/docs/ko/features/SearchBox.md` 와 충돌** → 그 문서는 CST 실측 기반 **구 기준**(라벨 우측정렬·`labelSize` 우선·`labelMinWidth` 예외 사용)이다. 조회조건 정렬은 **이 문서와 룰북 원문이 이긴다.**

## 관련 문서
- [A-01 단일 그리드](../a-archetype/a-01-single-grid-crud.md) · [B-08 공통코드 드롭다운](./b-08-code-dropdown.md) · [B-22 zod 검증](./b-22-zod-validation.md)
- UIUX: `.claude/rules/UIUX/04-components.md`(조회영역 버튼·Dropdown·Datepicker) · `.claude/rules/UIUX/02-layout-and-frame.md`(12컬럼)
