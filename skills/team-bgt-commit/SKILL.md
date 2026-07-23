---
name: team-bgt-commit
description: Use when 개인 하네스 폴더(~/.claude/{skills,agents,commands,hooks})에서 개발·테스트한 아티팩트(스킬·에이전트·커맨드·훅)를 team-bgt 플러그인 레포에 발행(퍼블리시)할 때. 트리거 "/team-bgt-commit <이름>", "스킬/훅/에이전트/커맨드 발행/퍼블리시", "내 하네스 team-bgt에 올려/커밋", "하네스 PR". 이름 인자가 필수다. bgt-fe/bgt-be 소스 커밋(=commit 스킬)이 아니고, 플러그인 레포 전체를 main 에 직접 커밋하는 것도 아니다 — 아티팩트 단위로 브랜치를 파고 PR 을 만든다.
---

# team-bgt 하네스 발행 (Publish Artifact → PR)

## 무엇을 하나

개인 하네스 폴더(`~/.claude/`)에서 만든 아티팩트(**스킬·에이전트·커맨드·훅**)를 team-bgt 플러그인 레포(`C:\workspace\team-bgt`)의 해당 폴더로 옮겨, **이름 브랜치**에 커밋하고 origin 에 push 한 뒤 PR 생성용 compare URL 을 돌려준다.

- bgt-fe/bgt-be **소스** 커밋이면 이 스킬이 아니라 `commit` 스킬.
- 항상 브랜치 + PR. **main 에 직접 커밋/push 하지 않는다.**

## 아티팩트 타입 (개인 원본 → 플러그인 대상)

| 타입 | 개인 원본 | 플러그인 대상 | 형태 |
|------|-----------|---------------|------|
| skill | `~/.claude/skills/<name>/` | `skills/<name>/` | 폴더 |
| agent | `~/.claude/agents/<name>.md` | `agents/<name>.md` | 파일 |
| command | `~/.claude/commands/<name>.md` | `commands/<name>.md` | 파일 |
| hook | `~/.claude/settings.json` 의 `hooks` 엔트리가 가리키는 스크립트 | `hooks/<file>` + `hooks/hooks.json` 등록 | 파일 + json |

**타입은 인자로 주지 않아도 된다** — 위 4곳을 프로브해 자동 판별한다. 같은 이름이 여러 타입에 걸리면 AskUserQuestion 으로 어느 타입인지 확인한다. 명시하려면 `hook:<name>` 처럼 `타입:이름` 으로 줘도 된다.

## 핵심 규칙 (위반 시 멈춤)

- **이름 인자 필수.** 없이 호출되면 진행하지 말고 발행할 이름을 묻는다.
- **개인 폴더에 없는 이름은 거부.** 다른 플러그인 캐시에만 있으면 그 사실을 알리고 거부, 아무 데도 없으면 못 찾음으로 중단.
- **원본 삭제·등록 해제는 push 성공 후에만.** 그전에 지우면 실패 시 유실.
- **team-bgt 위치는 사용자에게 먼저 묻지 않는다** — 고정 경로 → 얕은 로컬 검색으로 직접 찾고, 그래도 안 나올 때만 묻는다. 단 `C:\` 전체 재귀 스캔은 금지(느림).

## Workflow

### 0. 인자 + 타입 판별 (필수)
호출 인자에서 이름을 읽는다. 공백/쉼표로 **여러 개**일 수 있다. 비어 있으면 AskUserQuestion 으로 "발행할 이름?" 을 받고, 끝까지 없으면 중단한다.
각 이름을 위 4개 개인 경로에 프로브해 타입을 판별한다. `타입:이름` 으로 명시됐으면 그 타입만 본다. 여러 타입에 걸리면 AskUserQuestion.

### 1. 검증 (소유권 + 존재) — 이름마다
1. **개인 원본 확인** (판별된 타입 기준):
   - skill: `~/.claude/skills/<name>/SKILL.md` 존재.
   - agent/command: `~/.claude/agents/<name>.md` · `~/.claude/commands/<name>.md` 존재. frontmatter 에 `name`/`description` 가볍게 확인.
   - hook: `~/.claude/settings.json` 의 `hooks` 에서 `<name>` 을 참조하는 엔트리를 찾아 **이벤트·matcher·command 경로**를 얻는다. 그 스크립트 파일이 실제로 존재하는지 확인.
2. 어느 개인 경로에도 없으면 **다른 플러그인 소유인지** 확인:
   ```bash
   find "$HOME/.claude/plugins/cache" -maxdepth 6 -name "<name>" | grep -v "/team-bgt/"
   ```
   - 결과가 있으면 → "`<name>` 은 `<plugin>` 플러그인 소유라 team-bgt 로 발행할 수 없습니다" 로 **거부**.
   - 없으면 → "`<name>` 을 개인 폴더에서 찾을 수 없습니다" 로 중단.

### 2. team-bgt 위치 찾기 (로컬에서 직접 · 못 찾을 때만 질문)
사용자에게 먼저 묻지 말고 로컬에서 직접 찾는다. origin 이 `github.com/Romano1994/team-bgt` 인 디렉토리가 정답이다.
1. **고정 경로 먼저**: `git -C "C:\workspace\team-bgt" config --get remote.origin.url` 로 origin 확인 — 맞으면 여기서 끝.
2. **없거나 안 맞으면 얕은 로컬 검색**으로 `team-bgt` 폴더를 찾고 origin 으로 검증한다:
   ```bash
   find /c/workspace -maxdepth 2 -type d -name team-bgt
   ```
   (필요하면 현재 repo/cwd 의 상위도 후보에 넣는다.) 나온 후보마다 origin 을 확인해 매칭되는 것을 쓴다.
3. **그래도 아무것도 안 나오면** → 그때 사용자에게 경로를 묻는다(AskUserQuestion).
- `C:\` 전체를 재귀 스캔하지는 않는다 — `-maxdepth` 얕은 검색으로 충분하고 빠르다.

### 3. 브랜치 (이름 = 브랜치)
1. 최신 기준 확보: `git switch main` → `git pull --ff-only origin main`.
2. **브랜치명 = 대표 아티팩트 이름.** 1개면 그 이름, 여러 개면 대표(사용자 지정, 없으면 첫 번째).
3. `git switch -c <branch>` (이미 있으면 `git switch <branch>`).

### 4. 아티팩트 옮기기 (복사) — 타입별
각 이름을 판별된 타입대로 대상에 복사한다(기존 있으면 덮어씀 = 업데이트). **이 단계에서 원본은 아직 지우지 않는다.**
- **skill**: `~/.claude/skills/<name>/` 폴더 전체를 `skills/<name>/` 로 복사.
- **agent/command**: `.md` 파일을 `agents/<name>.md` · `commands/<name>.md` 로 복사.
- **hook**: 스크립트 파일을 `hooks/<file>` 로 복사하고, **`hooks/hooks.json` 에 등록 엔트리를 추가/병합**한다.
  - `command` 경로는 개인 절대경로를 플러그인 루트 기준으로 바꾼다: `node "${CLAUDE_PLUGIN_ROOT}/hooks/<file>"`.
  - 이벤트(예 `Stop`, `PreToolUse`)와 matcher 는 개인 `settings.json` 에서 읽은 값을 유지한다.
  - 같은 이벤트가 이미 `hooks.json` 에 있으면 그 이벤트의 `hooks` 배열에 append(통째로 덮어쓰지 않는다).

### 5. 버전업 + 커밋
1. **버전 판단** (팀원 관점 = "쓰는 사람이 뭐가 깨지나"):
   - **MAJOR** `x.0.0` — 기존 사용이 깨짐: 아티팩트 삭제·이름변경, 트리거 변경으로 기존 호출 안 먹힘, 출력/동작 계약 변경, 훅 이벤트/차단 동작 변경으로 기존 워크플로가 깨짐.
   - **MINOR** `1.x.0` — 하위호환 추가: 새 스킬·에이전트·커맨드·훅 추가, 새 기능, 트리거 확장 (`feat:`).
   - **PATCH** `1.0.x` — 동작 계약 불변인 고침·다듬기: 버그 픽스, 프롬프트 문구, 문서, 리팩터 (`fix:`).
   - 한 줄 룰: 머슬메모리 깨지면 MAJOR · 새로 생기면 MINOR · 조용히 나아지면 PATCH.
2. `.claude-plugin/plugin.json` 의 `version` 을 판단한 등급대로 올린다(예 MINOR: `1.0.3` → `1.1.0`, 올린 자리 아래는 0).
3. `git add <대상 경로들> .claude-plugin/plugin.json` — 발행 대상 + version. hook 이면 `hooks/hooks.json` 도 add. `git add -A` 로 뭉뚱그리지 않는다.
4. 한국어 컨벤션 메세지: `feat: <이름> <타입> 추가` (예 `feat: bgt-fe-verify-gate 훅 추가`, `feat: codebase-locator 에이전트 추가`). 여러 개면 나열, 기존 수정이면 `fix:`, MAJOR면 `feat!:` + 본문에 `BREAKING:` 로 깨지는 지점 명시. 끝에:
   ```
   Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>
   ```
   (PowerShell 멀티라인은 `-m` 을 여러 번.)
5. `git commit`.

### 6. push + PR URL
1. `git push -u origin <branch>`.
2. compare URL 을 출력한다(gh 미설치 → 자동 PR 생성 불가, URL 로 클릭 한 번):
   ```
   https://github.com/Romano1994/team-bgt/compare/main...<branch>?expand=1
   ```

### 7. 원본 정리 (옮기기 마무리)
push 성공을 **확인한 뒤** 개인 원본을 제거해 이동을 마무리한다:
- skill: `~/.claude/skills/<name>/` 폴더 삭제. agent/command: `.md` 파일 삭제.
- hook: 개인 스크립트 파일 삭제 + `~/.claude/settings.json` 의 해당 `hooks` 엔트리 제거(그대로 두면 플러그인 훅과 **이중 실행**된다). 무엇을 지웠는지 정확히 보고한다.
아티팩트는 이제 team-bgt 레포에 있으며, PR 머지 후 `/plugin update team-bgt` 로 다시 로드된다고 사용자에게 알린다. **push 실패 시 아무것도 지우지 않는다.**

## Red Flags — 멈추고 다시

- **인자 없이 진행** → 이름은 필수. 물어본다.
- **개인 폴더에 없는 이름을 발행** → 다른 플러그인 소유면 거부, 아니면 못 찾음으로 중단.
- **못 찾자마자 사용자에게 묻기** 또는 반대로 **`C:\` 전체 재귀 스캔** → 둘 다 아님. 고정 경로 → 얕은 로컬 검색 → 그래도 없을 때만 질문.
- **훅 스크립트만 복사하고 `hooks/hooks.json` 등록 누락** → 훅이 안 돈다. 반드시 등록까지.
- **main 에 직접 커밋/push** → 항상 이름 브랜치 + PR.
- **커밋·push 전에 원본 삭제** → 실패 시 유실. 7단계는 push 성공 후에만.
- **`gh pr create` 시도** → gh 미설치. compare URL 로 안내.
- **`git add -A`** → 발행 대상만 add.

## 참고

- 플러그인 훅 등록 형식(`hooks/hooks.json`):
  ```json
  { "hooks": { "Stop": [ { "hooks": [ { "type": "command", "command": "node \"${CLAUDE_PLUGIN_ROOT}/hooks/<file>\"" } ] } ] } }
  ```
- 플러그인 `version` 업은 5단계에서 등급 판단 후 `plugin.json` 을 올려 같은 커밋에 포함한다. 애매하면 낮은 등급으로 — PATCH < MINOR < MAJOR.
- 소유권 거부를 하드 게이트로 만들고 싶으면 PreToolUse 훅으로 승격 가능(현재는 워크플로 규칙으로 충분).
