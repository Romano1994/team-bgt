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
    └── grill-me
```

설치 시 `superpowers`(claude-plugins-official), `ponytail`(ponytail 마켓)이 함께 설치된다.
ponytail은 서드파티 마켓이므로 팀 배포 시 `ponytail` 마켓플레이스 등록이 선행돼야 한다.

## 로컬 테스트

```
claude --plugin-dir ./team-bgt
```

## 팀 설치 (마켓플레이스 배포 후)

```
/plugin marketplace add <your-org>/team-bgt
/plugin install team-bgt@team-bgt
```
