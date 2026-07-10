# team-bgt

BGT 팀 공용 Claude Code 플러그인.

## 구조

```
team-bgt/
├── .claude-plugin/
│   └── plugin.json      # 매니페스트 + 의존성(superpowers, ponytail)
├── agents/              # 구현·검증 서브에이전트
├── hooks/               # Stop 훅(fe 검증 게이트)
└── skills/              # 슬래시 스킬
    ├── cst-feature-finder
    ├── feature-finder
    ├── migrate-asis-to-bgt
    ├── migrate-feature-asis-to-bgt
    ├── pms-screen-finder
    ├── grill-me
    └── commit                  # 커밋 가드는 인라인(plain git), 하드차단 훅은 skills/commit/hooks/pre-commit
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
| `grill-me` | 계획·설계를 결정 트리 단위로 끝까지 캐물어 검증 |
| `commit` | fe/be 변경을 컨벤션 한국어 메세지로 커밋(민감파일 차단·문서확인·ponytail 리뷰) |

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
