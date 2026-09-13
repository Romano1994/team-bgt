---
paths:
  - "bgt-fe/src/**/*.tsx"
---

# 04. Components — 컴포넌트별 규격

> 정본: `docs/pmx-uiux-audit/rules/uiux-standard-rules.md` §버튼(p.64~66)·§Form 입력(p.67~78)·§Tab(p.85).
> 공통 UI는 `@amxis/design-system`으로 구현하고 아래 규격을 제약으로 얹는다.

## Button (p.64~67)

### Anatomy / Variation
- Container Type 3종: **Contained**(배경 채움) / **Outlined**(테두리) / **Unfilled**(텍스트만). 중요도에 따라 구분.
- Contents Type 3종: **Label Only**(Default) / **Icon+Label**(아이콘 **좌측** 기본, 경우에 따라 우측) / **Icon Only**.
- 중요도 **Primary(파란색) / Normal(짙은 남색·차콜)** × Size **Large/Medium/Small** × Style 3종 = 9조합.
- **Icon Only는 보편적 Action(삭제·즐겨찾기 등)에만** 쓰고, 시각적으로 안 보여도 **접근성 설명(aria) 필수**. 좌우 폭 고정 사이즈 적용.
- 특수 버튼: **AI** = 민트 Outlined + 스파클 / **카카오톡 발송** = 노랑 Contained + 말풍선.

### Placement — 모듈 버튼 (p.65)
- 데이터 컨테이너(그리드/입력폼)를 제어하는 버튼은 **대상 모듈 타이틀 우측 상단**. 불필요한 기능은 배치하지 않는다.
- 표준 순서(좌→우): **신규/행추가 > 삭제/행삭제 > 복사/행복사 > 위로 > 아래로 > 업로드 > 다운로드 > 새로고침.** 필요한 것만 쓰되 순서 유지.
- **표준 순서에 없는 모듈 전용 기능버튼은 주요 버튼 그룹의 좌측**에 배치.
- 모듈 단위 저장·확정 버튼은 그룹 **가장 우측**, 임시저장 등 연관 버튼은 그 **좌측**.
- 모듈 버튼 스타일은 **Normal · Outlined · Small**(저장/확정 버튼 제외).
- 예시(그리드): `기능버튼01 · 기능버튼02 | +행추가 · −행삭제 · ↑위로 · ↓아래로 · 업로드 · 다운로드 · 새로고침`

### Placement — 조회 영역 (p.66~67)
- 버튼은 **조회조건 우측**, 여러 줄이면 **마지막 행 우측**. 조회조건이 많아 같은 행 배치가 불가하면 **아래 별도 행**에 배치.
- 순서: **초기화 > 조회 > 기능버튼 > 저장.** 저장·연관 버튼은 조회 버튼 **우측**이며 **구분선(세로 divider)** 으로 분리.
- 조회 요건 없이 저장만 필요한 화면은 저장·연관 버튼만(저장이 그룹 맨 우측).
- 색상: **조회 = Normal Contained** / **저장 = Primary Contained** / **취소·임시저장 = Normal Outlined** / **새로고침 = 원형 아이콘 버튼(조회 바로 좌측)**.
- 버튼영역은 **DS `SearchArea` 가 직접 그린다.** 버튼 몫으로 빈 칸을 예약하거나 DS 내부를 겨냥한 `sx` 로 위치를 보정하지 않는다. → `develop/b-feature/b-23` §4

## Title Area
- **Page Title**: 선택 메뉴명과 일치. 길어도 **말줄임 금지**. 즐겨찾기(별) 버튼(불필요 시 제외).
- **Sub Title**: 좌측, 버튼은 우측 · **Small · Normal-Outlined**. 부연은 Helper Text(optional).

## Accordion
- 헤더 클릭 토글, `전체 펼치기/접기` 제공. Body 펼침 시 **사방 `12px` 여백**. **최대 3 Depth.**

## Avatar
- Single / Group. Content 기본 Icon(Text/Image 가능). Status + Count Badge 조합. Overflow는 **`+N`**, 1,000부터 **`999+`**.

## Checkbox & Radio (p.68)
- Checkbox = 다중, Radio = 단일. **옵션 5개 이하면 Dropdown 대신 Radio.**
- Selector·Label **둘 다 클릭으로 토글** 가능해야 한다.
- 전체선택 지원 시 부분선택 상태는 전체 체크박스가 **Indeterminate(가로선)**.
- 권한상 선택 불가한 Radio는 Unselected로 표기.

## Chip (p.69)
| 종류 | 규칙 |
| --- | --- |
| Input | 우측 삭제(X) Icon Button **필수** |
| Action | 클릭 시 상세정보 팝업 호출이 기본 Event |
| Filter | 체크박스와 기능 동일, 복잡 화면의 선택 시각화용 |
| Choice | Radio Group과 동일(단일선택), Label과 함께 Form Item 가능 |

## Datepicker (p.70~71)
- **Single Date** `YYYY.MM.DD` / **Date Range** `YYYY.MM.DD ~ YYYY.MM.DD`.
- 조회조건에서 `width="100%"` 는 **기간(`PeriodPicker`) 전용**. 단일 `Form.DatePicker` 에 주면 컨테이너만 늘어나 인풋 뒤에 빈 흰 칸이 생긴다.
- 캘린더 팝업 상단 `YYYY년 MM월` + `<`/`>` 네비게이션.
- Variation 3종(날짜 / 월 / 년도 선택) — **필드 표시 포맷은 항상 `YYYY.MM.DD` 유지.**

## Description (p.73)
- 구성: Title(optional) + List(Bullet/Numbered) + Button Area(optional: 자세히 알아보기/더보기).
- **장문 안내는 Helper Text가 아니라 Description**으로 별도 영역 배치.
- Type 4종: **Normal**(안내, 회색+info) / **Confirm**(확인, 초록+체크) / **Warning**(주의, 노랑+느낌표) / **Error**(경고·긴급, 빨강+금지).

## Divider (p.74)
- 가로형/세로형 × 적용위치 5단계(밝기·두께 순): List Item 구분(가장 얇음) < Form Table 내부 < Tab/Page 상하 콘텐츠 < Page 내 분할영역 < Process Tab 단계간(가장 굵음).

## Dropdown Field (p.75)
- **Placeholder 필수.**
- **Single**: 선택값 Text 1건 표시. **Multi**: 선택값 **Chip**으로 표시, Chip X·목록 해제로 제거, Focus 시 전체삭제 버튼(optional).
- 다중선택은 **태그형 `DropdownField multiple`** 로 구현한다. 체크박스를 바둑판처럼 늘어놓지 않는다.
- 옵션 5개 이하면 Radio 우선.

## Helper Text (p.76)
- **1줄 권장**(장문은 Description).
- Style 5종: Default(회색+info) / Error·Urgent(빨강+금지) / Warning(주황+느낌표) / Confirm(초록+체크) / Primary(파랑+info).
- 배치: Input 하단 **좌측정렬 일치**. 모듈 도움말은 서브타이틀 우측 → 하단 → 모듈 좌측하단 순. Validation 결과는 입력폼 하단.

## Input Field (p.77)
| Type | 포맷/규칙 |
| --- | --- |
| Text | 좌측정렬 / 우측정렬 2종 |
| Textarea | 좌측정렬, 글자수 제한 시 필드 내 **좌측 하단 `현재/제한`**(예 `2 / 500`) |
| Search | Text / Single Chip / Multi Chip 3종, Focus 시 전체삭제 버튼 |
| Date | `YYYY.MM.DD` 자동 포맷팅 |
| Date Range | `YYYY.MM.DD ~ YYYY.MM.DD` 자동 포맷팅 |
| Time | 오전·오후 구분 / 24시간 / 시·분 분리 **3방식 중 택1, 일관 적용** |

## Label
- Label Text + **Required Dot**(필수 시). Form Item 또는 Detail Table 헤더로 사용. 줄바꿈 시 텍스트·입력폼 상단 정렬.

## Message Bar / Toast
- **2줄 이내**, **4초 후 자동 소멸**. Message + Action(optional) + Close(optional).
- Type 4종: Normal / Confirm / Warning / Error.
- 위치: **화면 하단에서 `60px` 위, 좌우 중앙정렬.**

## Process Tab
- 상태: **진행완료 / 진행중 / 미진행(Stand by · Disabled)**. 좌→우 배치 + Separator. 넘치면 줄바꿈. 상세 동작은 `03-screen-patterns.md` §10.

## Progress Indicator
- **Determinate** = 완료 예측 가능 / **Indeterminate** = 불확실(기본 Infinite Circular).
- 전체 영향 시 화면 정중앙, 부분 영향 시 해당 부분 중앙. 중단 가능 업무는 일시정지/취소 제공.

## Slider / Switch
- Slider: **Default**(Pointer 값 표시) / **Discrete**(고정 스텝). Label과 함께 Form Item·Search Area에 사용.
- Switch: On/Off 즉시 반영. 기본 = Label + Switch 나란히. Label 내부 포함형은 의미가 짧고 명료할 때만.

## Tab (p.85)
- **최대 7개, 한 줄 배치.** 동일 레벨 콘텐츠 그룹 탐색용.
- Depth 2종: **Default Tab**(1D — 사각 테두리 박스, 활성 탭 하단 전체폭 파란 밑줄) / **Tab in Tab**(2D — 테두리 없는 텍스트형, 활성 항목 하단 좁은 파란 밑줄).
- **배치 순서: PageTitle → 조회조건(SearchArea) → Tab(1D) → [필요시] Tab(2D) → 콘텐츠 영역.**
- **Tab in Tab은 가급적 지양**(복잡도 상승). 3단 중첩은 가이드에 정의 없음 = 위반. 모바일은 복잡한 탭 구성 지양.

### Tab 위반 판별
1. 7개 초과 2. 여러 줄 배치 3. 3단 이상 중첩 4. 불필요한 Tab in Tab(소견) 5. 1D가 박스형+하단 전체 밑줄이 아님 6. 2D가 1D와 동일 스타일(구분 안 됨) 7. Tab이 PageTitle/조회조건보다 상단 8. 모바일에서 복잡한 탭 구성

## Tag
- 상태·구분값을 **성격별 Color**로 표현(긍정·완료 / 진행중 / 대기 / 승인대기 / 부정·긴급 / 종료·비활성 / 가능·Task). 그리드·목록의 보조 안내용.
- *(정보유형별 구체 Tag HEX는 원본에 이미지로만 존재 — 디자인시스템 Status 색과 매핑)*

## Tooltip
- **기본 Top**(요소를 가리면 Right/Bottom/Left). 너비는 글자수 가변.
- **Hover 노출**(벗어나면 사라짐), 클릭형은 바깥 클릭으로 닫힘. **화면 밖으로 벗어나지 않아야 한다.**
