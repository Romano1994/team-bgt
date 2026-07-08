# team-bgt

BGT 팀 공용 Claude Code 플러그인.

## 구조

```
team-bgt/
├── .claude-plugin/
│   └── plugin.json      # 매니페스트 + 의존성(superpowers, ponytail)
└── skills/
    ├── cst-feature-finder
    ├── feature-finder
    ├── migrate-asis-to-bgt
    ├── migrate-feature-asis-to-bgt
    ├── pms-screen-finder
    ├── grill-me
    └── commit                  # 커밋 가드는 인라인(plain git), 하드차단 훅은 hooks/pre-commit
```

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
