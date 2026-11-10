---
description: 화면 위에 그려 단축키(Win+Shift+D)로 직전 입력창에 바로 붙이는 트레이 앱 reference-pic 을 설치·갱신한다(멱등, Windows 전용). 인자 uninstall 이면 해제.
allowed-tools: Bash(powershell:*)
---

플러그인이 들고 있는 `tools/reference-pic/` 소스를 이 PC에서 빌드해 `%LOCALAPPDATA%\reference-pic\` 에 설치하고, 시작 프로그램에 등록해 로그인할 때마다 트레이에 자동 실행되게 한다.
이미 설치돼 있으면 종료 → 다시 빌드 → 교체(업데이트)한다. 빌드는 Windows 기본 내장 .NET Framework 컴파일러를 쓰므로 따로 설치할 것이 없다.

인자: `$ARGUMENTS` — 비어 있으면 설치, `uninstall` 이면 해제(프로세스 종료 + 시작 프로그램 바로가기·설치 폴더 삭제).

아래 PowerShell 스크립트를 **그대로 1회 실행**하라(문구 변경 금지). `$mode` 만 인자에 맞춰 `install` 또는 `uninstall` 로 둔다.

```powershell
$mode = 'install'   # 인자가 uninstall 이면 'uninstall'
$cmd  = Join-Path $env:USERPROFILE ".claude\plugins\marketplaces\team-bgt\tools\reference-pic\$mode.cmd"
if (-not (Test-Path $cmd)) {
  Write-Output "실패: $cmd 를 찾을 수 없습니다. team-bgt 플러그인을 업데이트한 뒤 다시 실행하세요."
  return
}
$out = & $cmd 2>&1
if ($LASTEXITCODE -eq 0) { $out | Select-Object -Last 1 }
else { Write-Output "실패(exit $LASTEXITCODE):"; $out | Where-Object { "$_" -notmatch '^PASS ' } }
```

실행 후:
- 결과 메시지를 그대로 사용자에게 전달한다. 설치 성공이면 "`Win+Shift+D` 로 그리기 시작, 다음 로그인부터 자동 실행" 을 덧붙인다.
- 플러그인 업데이트로 reference-pic 이 바뀌어도 자동 반영되지 않는다 — 이 명령을 다시 실행해야 새 버전이 깔린다.
- Windows 전용이다. macOS/Linux 사용자면 실행하지 말고 지원하지 않는다고 안내한다.
