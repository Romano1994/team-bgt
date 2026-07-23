---
name: team-bgt-commit
description: Use when 개인 스킬 폴더(~/.claude/skills)에서 개발·테스트한 스킬(들)을 team-bgt 플러그인 레포에 발행(퍼블리시)할 때. 트리거 "/team-bgt-commit <스킬명>", "스킬 발행/퍼블리시", "내 스킬 team-bgt에 올려/커밋", "스킬 PR". 스킬명 인자가 필수다. bgt-fe/bgt-be 소스 커밋(=commit 스킬)이 아니고, 플러그인 레포 전체를 main 에 직접 커밋하는 것도 아니다 — 스킬 단위로 브랜치를 파고 PR 을 만든다.
---

# team-bgt 스킬 발행 (Publish Skill → PR)

## 무엇을 하나

개인 스킬 폴더(`~/.claude/skills/<스킬명>/`)에서 만든 스킬(들)을 team-bgt 플러그인 레포(`C:\workspace\team-bgt`)의 `skills/` 로 옮겨, **스킬명 브랜치**에 커밋하고 origin 에 push 한 뒤 PR 생성용 compare URL 을 돌려준다.

- bgt-fe/bgt-be **소스** 커밋이면 이 스킬이 아니라 `commit` 스킬.
- 항상 브랜치 + PR. **main 에 직접 커밋/push 하지 않는다.**

## 핵심 규칙 (위반 시 멈춤)

- **스킬명 인자 필수.** 인자 없이 호출되면 진행하지 말고 사용자에게 발행할 스킬명을 묻는다.
- **다른 플러그인 스킬은 거부.** 개인 스킬 폴더에 없고 다른 플러그인 캐시에만 있는 이름이면 커밋을 거부하고 사용자에게 알린다.
- **원본 삭제는 push 성공 후에만.** 그전에 지우면 실패 시 유실.

## Workflow

### 0. 인자 확인 (필수)
호출 인자에서 스킬명을 읽는다. 공백/쉼표로 **여러 개**일 수 있다. 비어 있으면 AskUserQuestion 으로 "발행할 스킬명?" 을 받고, 끝까지 없으면 중단한다.

### 1. 스킬 검증 (소유권 + 문서) — 스킬명마다
1. **개인 스킬 확인**: `~/.claude/skills/<name>/SKILL.md` 가 있어야 원본으로 인정.
2. 없으면 **다른 플러그인 스킬인지** 확인:
   ```bash
   find "$HOME/.claude/plugins/cache" -maxdepth 6 -type d -name "<name>" | grep -v "/team-bgt/"
   ```
   - 결과가 있으면 → "`<name>` 은 `<plugin>` 플러그인 소유 스킬이라 team-bgt 로 발행할 수 없습니다" 로 **거부**.
   - 없으면 → "`<name>` 스킬을 개인 폴더에서 찾을 수 없습니다" 로 중단.
3. **문서 점검**: SKILL.md frontmatter 에 `name`, `description` 가 있는지 가볍게 확인. 이상하면 사용자에게 알리고 계속할지 묻는다.

### 2. team-bgt 디렉토리 찾기
`C:\workspace\team-bgt` 가 대상. `git -C <dir> config --get remote.origin.url` 이 `github.com/Romano1994/team-bgt` 인지 확인한다. 아니면 사용자에게 경로를 확인한다.

### 3. 브랜치 (스킬명 = 브랜치)
1. 최신 기준 확보: `git switch main` → `git pull --ff-only origin main`.
2. **브랜치명 = 대표 스킬명.** 스킬 1개면 그 이름, 여러 개면 대표(사용자가 지정한, 없으면 첫 번째) 스킬명.
3. `git switch -c <branch>` (이미 있으면 `git switch <branch>`).

### 4. 스킬 옮기기 (복사)
각 `<name>`: `~/.claude/skills/<name>/` 전체를 `team-bgt/skills/<name>/` 로 복사한다(기존 폴더 있으면 덮어씀 = 스킬 업데이트). **이 단계에서 원본은 아직 지우지 않는다.**

### 5. 버전업 + 커밋
1. **버전 판단** (팀원 관점 = "쓰는 사람이 뭐가 깨지나"):
   - **MAJOR** `x.0.0` — 기존 사용이 깨짐: 스킬/커맨드 삭제·이름변경, 트리거 변경으로 기존 호출 안 먹힘, 출력/동작 계약 변경으로 기존 워크플로가 깨짐, 의존 플러그인 강제 변경.
   - **MINOR** `1.x.0` — 하위호환 추가: 새 스킬·에이전트 추가, 새 기능, 트리거 확장 (`feat:`).
   - **PATCH** `1.0.x` — 동작 계약 불변인 고침·다듬기: 버그 픽스, 프롬프트 문구, 문서, 리팩터 (`fix:`).
   - 한 줄 룰: 머슬메모리 깨지면 MAJOR · 새로 생기면 MINOR · 조용히 나아지면 PATCH.
2. `.claude-plugin/plugin.json` 의 `version` 을 판단한 등급대로 올린다(예 MINOR: `1.0.3` → `1.1.0`, 올린 자리 아래는 0).
3. `git add skills/<name> [skills/<name2> ...] .claude-plugin/plugin.json` — 발행 대상 + version. `git add -A` 로 뭉뚱그리지 않는다.
4. 한국어 컨벤션 메세지: `feat: <스킬명> 스킬 추가` (여러 개면 나열, 기존 스킬 수정이면 `fix:`, MAJOR면 `feat!:` + 본문에 `BREAKING:` 로 깨지는 지점 명시). 끝에:
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
push 성공을 **확인한 뒤** 개인 스킬 원본 `~/.claude/skills/<name>/` 를 삭제한다(이동 완료). 스킬은 이제 team-bgt 레포에 있으며, PR 머지 후 `/plugin update team-bgt` 로 다시 로드된다고 사용자에게 알린다. push 실패 시 원본을 지우지 않는다.

## Red Flags — 멈추고 다시

- **인자 없이 진행** → 스킬명은 필수. 물어본다.
- **개인 폴더에 없는 이름을 발행** → 다른 플러그인 스킬이면 거부, 아니면 못 찾음으로 중단.
- **main 에 직접 커밋/push** → 항상 스킬명 브랜치 + PR.
- **커밋·push 전에 원본 삭제** → 실패 시 유실. 7단계는 push 성공 후에만.
- **`gh pr create` 시도** → gh 미설치. compare URL 로 안내.
- **`git add -A`** → 발행 대상 스킬 폴더만 add.

## 참고

- 플러그인 `version` 업은 5단계에서 등급 판단 후 `plugin.json` 을 올려 같은 커밋에 포함한다(위 규칙). 애매하면 낮은 등급으로 — PATCH < MINOR < MAJOR.
- 소유권 거부를 하드 게이트로 만들고 싶으면 PreToolUse 훅으로 승격 가능(현재는 워크플로 규칙으로 충분).
