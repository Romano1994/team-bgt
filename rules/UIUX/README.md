# UIUX 표준 규칙 (BGT)

`bgt-fe` 화면·컴포넌트를 만들거나 고칠 때 **반드시 지켜야 하는** UI/UX 규칙 모음이다.

## 정본과 이 폴더의 관계

| | 역할 |
| --- | --- |
| **`docs/pmx-uiux-audit/rules/`** | **정본.** 완성 화면 점검(감사)용. 체크리스트 47항목(`uiux-checklist.csv`) · 표준가이드 182 세부규칙(`uiux-standard-rules.md`) · 가이드 목업 65장(`guide-pages/`) · 공통귀속 목록(`common-owned.json`) · **룰북 발췌 `rulebook/`**(조회조건 §5-1 · 팝업 D1~D14). 점검 절차는 `uiux-audit-screens` 스킬. |
| `docs/pmx-uiux-audit/rules/rulebook/` | 조회조건·팝업의 **최신 정본**(2026-09-11 / 2026-09-04 판). 182 세부규칙은 2026-07 추출본이라 두 영역에서 어긋나면 **룰북이 이긴다**. 구현 규칙은 `develop/b-feature/b-23`·`develop/a-archetype/a-03` §5~6 에 옮겨져 있다. |
| **`.claude/rules/UIUX/`** (이 폴더) | **코딩용 압축본.** 정본을 개발 중 강제 가능한 MUST/규격으로 줄인 것. |
| `docs/PMX-UI표준가이드_통합정리.md` | 서술형 전체본. 색상·아이콘·스페이싱·타이포(p.7~9, p.21~22)는 정본 audit 추출 범위 밖이라 **여기가 유일 근거**. |
| `docs/uiux_표준/PMX-UIX-AN-UI표준가이드-v1.0.pdf` | 원본 PDF(89p, 10p 단위 분할본 동봉). |

정본을 임의로 고치지 않는다. 이 폴더와 정본이 어긋나면 정본이 이긴다(단, 스페이싱은 아래 주의 참조).

## 파일 구성

| 파일 | 로드 시점 | 내용 |
| --- | --- | --- |
| `00-core.md` | **항상**(CLAUDE.md `@import`) | 게이트 16항목 + 정량 규칙 + MUST NOT |
| `01-foundation-tokens.md` | UI 작업 시 | 색상·타이포·스페이싱·아이콘·해상도(+위반 판별 10종) |
| `02-layout-and-frame.md` | 화면 설계 시 | Frame, 12컬럼 그리드, Dialogue, 폼·그리드 서식, 부적합 사례 |
| `03-screen-patterns.md` | 화면 패턴 선택 시 | 9패턴, Grid View/Edit, Analytical, Detail Dialog, 단건 폼, 마스터-디테일, Tree, Process, Empty State |
| `04-components.md` | 컴포넌트 구현 시 | Button(색상·배치), Form 컴포넌트, Tab, Tag, Tooltip 등 |

## 강제(enforcement) 방식

`.claude/rules/` 아래 `.md`는 Claude Code가 재귀적으로 자동 로드한다. 로드 시점은 상단 `paths:` frontmatter 유무로 갈린다.

- **`paths:` 없음 → 매 세션 상시 로드.** `00-core.md`가 여기 해당하며, 루트 `CLAUDE.md`의 `@import`가 이 의도를 고정한다.
- **`paths:` 있음 → 매칭 파일을 읽을 때만 로드.** `01~04`는 `bgt-fe/src/**/*.tsx` 스코프다. **`/compact` 후에는 사라지고**, 매칭 파일을 다시 읽어야 복귀한다 — compact를 넘겨 살아있어야 하는 규칙은 `00-core.md`에 둔다.

## 적용 원칙 (BGT 커스터마이즈)

- 원본은 PMX/GS건설 기준이다. **용어·도메인은 BGT로 바꾸고**, 정량 규칙(px·색상·정렬·순서)만 그대로 강제한다.
- 공통 UI는 `@amxis/design-system`, 그리드는 IBSheet로 구현한다. 위 규칙은 그 위에 얹는 **제약**이다. 디자인시스템이 동등 토큰을 제공하면 **토큰을 쓰고 임의 값 하드코딩을 금지**한다.
- 규칙과 기존 `bgt-fe` 패턴이 충돌하면 사용자에게 확인한다.

## 값 신뢰도 주의

- 원본이 이미지 기반이라 일부 색상 HEX·명암비 수치는 추출이 손상됐다. `(추출 불확실)` 표기 값은 **코드 반영 전 원본 PDF로 재확인**한다.
- **스페이싱 4/8/12/24px** — 정본 `uiux-standard-rules.md`는 "수치 없음"이라 적었지만 이는 footer/파일 페이지 오프셋 오판이다. 통합정리 p.21에 실재하므로 이 폴더 값이 맞다.
- **모듈 툴바 순서** — 정본 내부 상충(p.65 `신규>삭제>복사` vs p.36~43 `신규>복사>삭제`). **p.65 우선**으로 통일했다.
