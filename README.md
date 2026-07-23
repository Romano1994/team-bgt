# team-bgt

BGT 팀 공용 Claude Code 플러그인.

## 구조

```
team-bgt/
├── .claude-plugin/
│   └── plugin.json      # 매니페스트 + 의존성(superpowers, ponytail)
├── agents/              # 구현·검증 서브에이전트
├── commands/            # 슬래시 명령
│   └── setup-cc         # 터미널 'cc' → claude 별칭 설치
├── hooks/               # Stop 훅(fe 검증 게이트)
└── skills/              # 슬래시 스킬
    ├── cst-feature-finder
    ├── feature-finder
    ├── migrate-asis-to-bgt
    ├── migrate-feature-asis-to-bgt
    ├── pms-screen-finder
    ├── standard-verify
    ├── standard-refactor
    ├── uiux-standard-finder
    ├── uiux-guide
    ├── grill-me
    ├── commit                  # 커밋 가드는 인라인(plain git), 하드차단 훅은 skills/commit/hooks/pre-commit
    └── team-bgt-commit         # 플러그인 레포 자체 커밋/버전올려 push(fe/be 소스 커밋 아님)
```

### agents

| 에이전트 | 역할 |
|---|---|
| `bgt-fe-implementer` / `bgt-be-implementer` | fe/be 구현(Write/Edit). 무거운 읽기를 격리 컨텍스트에 가두고 메인엔 요약·API 계약만 반환 |
| `bgt-fe-verifier` / `bgt-be-verifier` | fe/be 변경을 표준·포맷·과잉설계 관점에서 검증(읽기 전용, 턴 종료 시) |
| `uiux-verifier` | `.claude/rules/UIUX/` 표준과 정적 대조(읽기 전용) |
| `codebase-locator` | 편집 전 수정 대상 위치를 확정하는 조회 전용 에이전트 |

### skills

| 스킬 | 설명 |
|---|---|
| `cst-feature-finder` | cst 프로젝트에서 특정 기능이 구현된 화면(파일·메뉴 path)을 찾음 |
| `feature-finder` | cst·bgt 양쪽에서 기능 구현 화면(경로·url·메뉴 path)을 함께 찾음 |
| `pms-screen-finder` | PMS 메뉴 path 또는 화면 코드로 로컬 화면 패키지 경로를 찾음 |
| `migrate-asis-to-bgt` | PMS/TEMS ASIS 화면을 UI·기능 1:1로 유지하며 BGT로 이관 |
| `migrate-feature-asis-to-bgt` | ASIS 화면의 특정 기능을 기존 BGT 화면에 기능 단위로 이식(migrate 후속) |
| `standard-verify` | 화면 단위로 fe/be/uiux verifier 3종을 돌려 `docs/verifier`에 간략 표준 검사 보고서 생성(읽기 전용) |
| `standard-refactor` | `standard-verify` 보고서의 위반 항목을 implementer에 위임해 수정(ID 지정 또는 전체, 재검증 없음) |
| `uiux-standard-finder` | UI 항목이 원본 UIUX 표준 PDF의 몇 페이지·어느 섹션에 나오는지 한 줄로 짚음(위치만) |
| `uiux-guide` | UI/UX 질문에 권장안을 답하되 근거는 원본 UIUX 표준 PDF의 footer 페이지로 인용(읽기 전용 자문) |
| `grill-me` | 계획·설계를 결정 트리 단위로 끝까지 캐물어 검증 |
| `commit` | fe/be 변경을 컨벤션 한국어 메세지로 커밋(민감파일 차단·문서확인·ponytail 리뷰) |
| `team-bgt-commit` | 플러그인 레포 자체를 커밋하고, 승인 시 origin/main pull→최신 version+patch→push(무관 매뉴얼 문서는 되물음) |

### commands

| 명령 | 설명 |
|---|---|
| `/setup-cc` | 터미널에서 `cc`만 입력하면 `claude`가 실행되도록 PowerShell 프로필($PROFILE, 유저 scope)에 별칭을 설치(멱등). 팀원이 1회 실행. Windows/PowerShell 전용. |

### hooks

`hooks/bgt-fe-verify-gate.js` — Stop 훅. 대규모 fe 변경이 있으면 응답 직전 fe 검증 게이트를 자동 실행한다.

`commit` 스킬은 민감파일 차단·문서확인을 SKILL.md 내 plain git으로 직접 수행한다(번들 스크립트 경로 의존 없음). 수동 커밋까지 하드 차단하려면 `skills/commit/hooks/pre-commit` 을 fe/be `.git/hooks` 에 설치한다(SKILL.md Setup 참고).

설치 시 `superpowers`(claude-plugins-official), `ponytail`(ponytail 마켓)이 함께 설치된다.
ponytail은 서드파티 마켓이므로 팀 배포 시 `ponytail` 마켓플레이스 등록이 선행돼야 한다.

## 로컬 테스트

```
claude --plugin-dir ./team-bgt
```

## 팀 설치 (마켓플레이스 배포 후)

```
/plugin marketplace add Romano1994/team-bgt
/plugin install team-bgt@team-bgt
```
