---
paths:
  - "bgt-fe/src/**/*.tsx"
---

# 02. Layout & Frame — 프레임·12컬럼·다이얼로그·폼/그리드 서식

> 정본: `docs/pmx-uiux-audit/rules/uiux-standard-rules.md` §레이아웃 표준(p.10~20)·§부적합 사례(p.27).
> 화면 패턴(9종·Grid·마스터디테일·Process·Empty State)은 `03-screen-patterns.md`, 컴포넌트 규격은 `04-components.md`.

## 1. Frame 구조 (p.10~13)

| 구분 | 규칙 |
| --- | --- |
| 3영역 | Frame = ① Header Area + ② Left Area + ③ Contents Area |
| Header Area | 스크롤과 무관하게 **상단 고정**. 로고/GNB/현장 selector/Todo/프로필 |
| Left Area | 상단 탭(PMX/My Menu)+메뉴검색 **고정**, 그 아래 메뉴트리부터 스크롤 |
| Contents Area | 스크롤 시 영역 전체가 스크롤 범위 |
| MDI Tab | 스크롤 무관, Header 하단·Contents 상단에 **항상 고정** |
| 스크롤 원칙 | **가로 스크롤 금지**(원칙), 세로 스크롤만 컨텐츠양에 따라 허용 |
| 업무화면 필수요소 | 최상단 **화면 타이틀 + 업무담당자 배지**(이름·연락처·ⓘ) 필수. 타이틀 = 선택 메뉴명과 **일치** |
| 배치 원칙 | 기능은 작업순서대로 순차 배치, Contents는 중요도/순서에 따라 **상→하** |
| 버튼 위치 | 컨펌성 버튼(초기화/조회/저장)은 **조회영역 우측**, 그리드 CUD 버튼은 **그리드 우측 상단** |
| 최초 호출 | 지연 시 Progress Indicator, 기본은 **Empty State** 노출, 초기조회조건 있으면 결과 노출 상태로 호출 |
| 작업 실행 | 저장/삭제는 **Confirm 다이얼로그**로 재확인 → 완료 시 **Toast**. 가능하면 오류 필드 위치 표시 |

## 2. 입력폼 / 그리드 서식 (p.14~15)

- 텍스트 **좌측정렬**, 금액·수량 등 숫자 **우측정렬 + 천단위 콤마(,)**. 단위는 입력폼 **우측** 표기.
- 날짜 `YYYY.MM.DD`, 시간 `HH:mm`(24h), 날짜범위 `YYYY.MM.DD ~ YYYY.MM.DD`.
- 필수항목 = 라벨 앞 **빨간 `*`**(Required Dot). 라벨 없으면 Helper Text로 필수 안내.
- 유효성: **입력란 컬러 변경 + 하단 Helper Text**. 오류=빨강 테두리+금지 아이콘+빨간 텍스트 / 확인=초록 테두리+체크 아이콘+초록 텍스트.
- 중요정보(비밀번호·주민번호·계좌번호)는 입력·출력 **모두 `*` 마스킹**.

### 그리드 컬럼 정렬
| 유형 | 정렬 | 폭 |
| --- | --- | --- |
| 코드·날짜·성명·직급 | **중앙정렬** | 고정 |
| 서술형 텍스트·주소 | **좌측정렬** (+ 긴 텍스트는 말줄임) | 가변 |
| 금액·수량 | **우측정렬 + 콤마** | 가변 |

- 링크텍스트 = **파란색 + 밑줄**. 정렬 가능한 컬럼 헤더에만 **정렬 아이콘(↕)** 표시.
- 셀 상태: 편집가능 = **흰 배경 / 파란 텍스트**, Readonly = **회색 배경 / 검은 텍스트**.
- 그리드 헤더 바 좌측 = `서브타이틀 + 총 N 건`(N은 강조색) `+ Info Text(ⓘ, optional)`, 우측 = 액션 버튼 그룹.

## 3. Dialogue(팝업) Layout (p.16~18)

- 4영역 = **① Title / ② Function / ③ Contents / ④ Button Area** + **Dimmed Layer**.
- Title: 좌측 상단, **X 버튼은 메시지 없이 즉시 닫힘**.
- Function Area: 업로드/다운로드/불러오기 등 기능버튼(팝업 이탈 없이 사용).
- Scroll: **Title·Button Area 고정**, Function+Contents 합쳐서 세로 스크롤. **가로 스크롤 지양**.
- Button Area: **Alert = 확인 1개 / Confirm = 취소·확인 2개 / 조회성 팝업은 Button Area 미노출**. 중요 버튼일수록 **우측**. 동일 의미 버튼 중복 금지.
- 버튼명은 메시지와 의미 충돌 없게(예: "취소하시겠습니까?"에 '취소' 버튼명 지양).
- Type: **Alert**(Pre-submit/Success) · **Confirm**(Warning/Error/Info) · **Content Dialogue**(화면전환 없이 부가정보, 컨펌 불필요 시 Button Area 미노출, 대량정보 화면만 세로 스크롤).

## 4. Contents Area — 12 Column Grid (p.19~20)

- **12 컬럼**, Gutter **`24px` 고정**. 표준 분할 비율은 아래 4가지만 가이드에 명시(그 외 임의 분할은 근거 확인 필요).

| 분할 | 표준 패턴 | 구성 |
| --- | --- | --- |
| `6:6` | Grid Main + Grid Sub (n:n) | 좌 조회조건+메인그리드 / 우 서브그리드, 폭 동일 |
| `2:10` | Tree + Detail | 좌 트리(좁게) / 우 상세 그리드(넓게) |
| `4:4:4` | 3분할 위젯(카드형) | 대시보드·홈 |
| `8:4` | Grid Main + Single Sub (n:1) | 좌 메인그리드(넓게) / 우 단건 상세폼(좁게) |

- 분할 경계를 사용자가 조절해야 하면 **`@amxis/design-system` Splitter**로 구현: `SplitterLayout > SplitterPanelGroup(direction) > SplitterPanel(minSize/defaultSize) > SplitterItem`. 임의 div·CSS 리사이즈 하드코딩 금지.
  - 퍼블 샘플: Publish → UI Splitter(`/com/publish/ui-splitter/splitter`). 구현 레퍼런스: `pages/eb/ebgt-mgmt/ebgt-mgmt-frmt-unit/FrmtUnitTabContent.tsx`.

## 5. 부적합 사례 (MUST NOT, p.27)

1. UI 요소가 시선 흐름(**상→하, 좌→우**)을 역행 — 정보가 하단→상단으로 흐르거나 우측에서 흐름이 시작됨.
2. 화면 구성·배치 부적절로 **불필요한 스크롤** 유발(팝업·좁은 영역 내 그리드/폼의 과도한 스크롤 포함).
