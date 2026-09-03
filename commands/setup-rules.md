---
description: 팀 표준 rules(개발·UIUX)를 현재 프로젝트의 .claude/rules/ 에 설치·갱신한다(멱등, 미러링).
allowed-tools: Bash(powershell:*)
---

플러그인이 들고 있는 팀 표준 rules 를 **현재 프로젝트**의 `.claude/rules/` 로 미러링한다.

## 왜 복사인가

`.claude/rules/**/*.md` 자동 주입은 **프로젝트 디렉터리 전용 빌트인**이다. 플러그인 폴더에 둔 rules 는 로더가 읽지 않는다.
프로젝트로 복사해야 원래 동작이 그대로 산다:

- `paths:` frontmatter **없음** → 세션 시작 시 상시 로드
- `paths:` **있음** → 매칭 파일을 도구가 건드릴 때 tool result 뒤에 **세션당 1회** 주입

아래 PowerShell 스크립트를 **그대로 1회 실행**하라(문구 변경 금지).

```powershell
$src = Join-Path $env:USERPROFILE ".claude\plugins\marketplaces\team-bgt\rules"
$dst = Join-Path (Get-Location) ".claude\rules"

if (-not (Test-Path $src)) {
  Write-Output "설치 실패: 플러그인 rules 를 찾을 수 없습니다 -> $src"
  Write-Output "team-bgt 플러그인 설치 상태를 확인하세요(plugin 명령)."
  return
}

$ver = "unknown"
$manifest = Join-Path $env:USERPROFILE ".claude\plugins\marketplaces\team-bgt\.claude-plugin\plugin.json"
if (Test-Path $manifest) { $ver = (Get-Content $manifest -Raw | ConvertFrom-Json).version }

# 기존 사본이 있으면 업데이트, 없으면 신규 설치 (동작은 동일 - 메시지만 구분)
$existed = (Test-Path (Join-Path $dst 'UIUX')) -or (Test-Path (Join-Path $dst 'develop'))
$mode = if ($existed) { "업데이트" } else { "설치" }

# 플러그인이 소유한 두 도메인만 미러링(/MIR 이 스테일 파일 제거). 그 밖 하위폴더는 건드리지 않는다.
$failed = $false
foreach ($domain in @('UIUX','develop')) {
  robocopy (Join-Path $src $domain) (Join-Path $dst $domain) /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
  if ($LASTEXITCODE -ge 8) { $failed = $true }
}
$global:LASTEXITCODE = 0   # robocopy 는 정상 복사에도 1을 반환한다

if ($failed) {
  Write-Output "$mode 실패: rules 복사 중 오류가 발생했습니다. $dst 쓰기 권한을 확인하세요."
} else {
  $n = (Get-ChildItem (Join-Path $dst 'UIUX'),(Join-Path $dst 'develop') -Recurse -Filter *.md | Measure-Object).Count
  Write-Output "$mode 완료: $dst  (rules $n 개 / team-bgt v$ver)"
  Write-Output "적용하려면 /clear 또는 Claude Code 재시작이 필요합니다 - 세션 도중 설치한 rules 는 그 세션에 주입되지 않습니다."
}
```

실행 후:

- 결과 메시지(설치/업데이트 완료 · 실패)를 **그대로** 사용자에게 전달한다. 프로젝트에 이미 사본이 있으면 자동으로 업데이트된다(별도 플래그 불필요).
- **반드시 재시작 안내를 덧붙인다**: 룰 인덱스는 세션 시작 시점에 잡히므로 지금 세션에서는 새 룰이 주입되지 않는다. `/clear` 또는 재시작 후에 적용된다.
- 프로젝트 루트가 아닌 곳에서 실행하면 엉뚱한 위치에 생긴다. 실행 전 현재 경로가 프로젝트 루트인지 확인한다.
- `.claude/rules/UIUX`·`.claude/rules/develop` 는 **미러링(통째 교체)** 된다 — 구 파일명 등 스테일 파일이 제거된다. 그 두 폴더에 프로젝트 고유 규칙을 넣어 뒀다면 사라지므로, 프로젝트 전용 규칙은 별도 하위폴더(예: `.claude/rules/local/`)에 둔다. 그 폴더는 건드리지 않는다.
- 플러그인을 업데이트한 뒤에는 이 커맨드를 **다시 실행**해야 프로젝트 사본이 최신이 된다.

## 설치되는 것

| 도메인 | 내용 |
|---|---|
| `rules/develop/` | 개발 표준 — `00-core`(상시) + 케이스 레시피 A(화면)/B(기능)/C(API)/D(워크플로) |
| `rules/UIUX/` | UI/UX 표준 — `00-core`(상시) + `01` 토큰 · `02` 레이아웃/프레임 · `03` 화면패턴 · `04` 컴포넌트 |
