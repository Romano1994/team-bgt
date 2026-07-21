---
name: uiux-standard-finder
description: 사용자가 지정한 UI 항목(컴포넌트/토큰/레이아웃/화면패턴, 예 "멀티항목 드롭다운")이 원본 UIUX 표준 PDF(PMX-UIX-AN-UI표준가이드-v1.0.pdf)의 몇 페이지(하단 footer 번호)·어느 섹션에 나오는지 아주 간략히(한 줄) 짚어 반환한다. Use when user runs "/uiux-standard-finder <UI 항목>" or asks "이 UI 항목 표준 어디 나와", "드롭다운/버튼/그리드/색상 몇 페이지", "UIUX 표준 문서 위치 찾아줘".
---

# UIUX Standard Finder

## 목적

인자로 받은 **UI 항목**(예: `멀티항목 드롭다운`)이 원본 UIUX 표준 PDF `PMX-UIX-AN-UI표준가이드-v1.0.pdf`의 **몇 페이지·어느 섹션**에 나오는지 **한 줄로** 짚어준다. 페이지는 **원본 하단 footer에 인쇄된 번호**를 그대로 쓴다. 규칙 내용·발췌는 반환하지 않는다(위치만).

## 동작 방식

아래 **위치 표**만 보고 답한다. 파일을 읽지 않는다 — 원본 PDF도 조회하지 않는다(사용자가 직접 열 대상이며 경로·크기와 무관). 인자를 표의 항목으로 **의미 매칭**한다(정확 일치 불필요).

- 예: `멀티항목 드롭다운`·`다중선택 콤보` → **Dropdown Field** / `날짜선택`·`달력` → **Datepicker** / `저장 토스트`·`알림바` → **Message Bar**
- 예: `Primary 색`·`간격`·`폰트`·`아이콘` → 토큰 / `팝업`·`모달` → **Dialogue** / `12컬럼`·`6:6` → **Contents Layout**

## 위치 표 (항목 → 원본 footer 페이지 · 섹션)

> 페이지 = **원본 하단 footer 인쇄 번호**. (PDF 뷰어의 물리 장수·목차 색인 페이지와 다를 수 있음 — 앞부분 표지/개정 페이지가 footer 번호에서 빠져 목차보다 작다.)

### 토큰·레이아웃 (Basic Environment)
| 항목 | footer | 섹션 |
| --- | --- | --- |
| 해상도 / Device·OS / 반응 | 4 | Basic Environment |
| Color / Primary #037AF2 / Gray / 명암비 | 7 | Color |
| Icon (24×24·1.5px) | 9 | Icon |
| Frame Layout (Header/Left/Contents) | 10 | Frame Layout |
| 폼(입력) 서식 | 14 | Frame Layout |
| 그리드 정렬 / 셀상태 | 15 | Frame Layout |
| Dialogue / 팝업 / 모달 | 16 | Dialogue Layout |
| Contents Layout / 12컬럼 / 분할비율(6:6·2:10·4:4:4·8:4) | 19 | Contents Layout |
| Spacing (세로 4/8/12·가로 24/12/8/4) | 21 | Spacing |
| Typography (Pretendard·13~14px·-3%) | 22 | Typography |

### 화면 패턴 (UI Pattern)
| 항목 | footer | 섹션 |
| --- | --- | --- |
| 페이지 패턴 개요(9종) | 24 | Page UI Pattern |
| Grid Main (조회+목록) | 28 | Grid Main |
| Grid Main(Edit) / 그리드 CRUD·행상태 | 34 | Grid Main(Edit) |
| Grid + Single Sub (n:1) | 36 | Grid Main + Single Sub |
| Grid + Grid Sub (n:n) | 40 | Grid Main + Grid Sub |
| Tree + Detail | 43 | Tree + Detail |
| Shuttle | 45 | Shuttle |
| Accordion List | 48 | Accordion List |
| Calendar | 51 | Calendar |
| Process | 55 | Process |
| Empty State | 57 | Empty State |

### 컴포넌트 (Component)
| 항목 | footer | 섹션 |
| --- | --- | --- |
| Title Area | 61 | Title Area |
| Accordion | 62 | Accordion |
| Avatar | 63 | Avatar |
| Button (배치·순서·스타일) | 64 | Button |
| Checkbox & Radio | 68 | Checkbox & Radio |
| Chip | 69 | Chip |
| Datepicker (날짜/달력) | 70 | Datepicker |
| Description | 73 | Description |
| Divider | 74 | Divider |
| **Dropdown Field** (드롭다운/콤보/다중선택) | 75 | Dropdown Field |
| Helper Text | 76 | Helper Text |
| Input Field (입력) | 77 | Input Field |
| Label | 78 | Label |
| Message Bar (토스트/알림바) | 79 | Message Bar |
| Process Tab | 81 | Process Tab |
| Progress Indicator | 82 | Progress Indicator |
| Slider | 83 | Slider |
| Switch | 84 | Switch |
| Tab | 85 | Tab |
| Tag | 86 | Tag |
| Tooltip | 87 | Tooltip |

## 출력 형식 (한 줄)

```
<항목> → 원본 <NN>p · <섹션명>
```

예: `멀티항목 드롭다운 → 원본 75p · Dropdown Field`

한 항목이 여러 페이지면 쉼표로(예 `15p, 34p`). 표에 없으면 한 줄로 "위치 표에 없음"만 알린다.

## 주의

- 표만 보고 답한다. **원본 PDF를 열지 않는다**(이미지 기반·대용량이라 조회 비효율, 경로도 불필요).
- 페이지 = **원본 하단 footer 인쇄 번호**를 그대로 반환한다. PDF 뷰어가 세는 물리 장수는 이보다 앞쪽 페이지 수만큼 클 수 있다.
- 읽기·보고만 하고 아무것도 수정하지 않는다.
