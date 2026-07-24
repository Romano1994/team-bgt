# BE graceful 재기동 — Ctrl+C(SIGINT) 주입 정지 후 be-start.ps1 재기동 (application.yml 무수정)
# 출력(실패): NOT_OWNED(IDE) | NOT_OWNED(NO_CONSOLE) | STOP_TIMEOUT | be-start의 실패 출력
# 어떤 경로에서도 강제종료(Stop-Process -Force / taskkill /F) 하지 않는다 — ORA-18958 재발 방지(D-18 §1).
param(
  [int]$Port = 7078,
  [int]$StopTimeoutSec = 60
)
$ErrorActionPreference = 'Stop'
$startScript = Join-Path $PSScriptRoot 'be-start.ps1'

$conn = Get-NetTCPConnection -State Listen -LocalPort $Port -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $conn) {
  # 이미 내려가 있음 — 바로 기동
  & $startScript -Port $Port
  exit $LASTEXITCODE
}
$targetPid = $conn.OwningProcess

# 안전 검사 1: IDE 디버그 세션(인계 안 된 프로세스) 보호
$cl = (Get-CimInstance Win32_Process -Filter "ProcessId=$targetPid" -ErrorAction SilentlyContinue).CommandLine
if ($cl -and ($cl -match '-agentlib:jdwp' -or $cl -match 'debugger-agent\.jar')) {
  Write-Output "NOT_OWNED(IDE) pid=$targetPid"
  exit 1
}

# Ctrl+C(SIGINT) 주입 — 대상 콘솔에 attach 후 CTRL_C_EVENT 발생
if (-not ([System.Management.Automation.PSTypeName]'ConsoleCtrl').Type) {
  Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class ConsoleCtrl {
  [DllImport("kernel32.dll", SetLastError=true)] public static extern bool FreeConsole();
  [DllImport("kernel32.dll", SetLastError=true)] public static extern bool AttachConsole(uint pid);
  [DllImport("kernel32.dll", SetLastError=true)] public static extern bool SetConsoleCtrlHandler(IntPtr handler, bool add);
  [DllImport("kernel32.dll", SetLastError=true)] public static extern bool GenerateConsoleCtrlEvent(uint ctrlEvent, uint pgid);
}
'@
}

[ConsoleCtrl]::FreeConsole() | Out-Null
if (-not [ConsoleCtrl]::AttachConsole([uint32]$targetPid)) {
  # 안전 검사 2: 콘솔 없는 프로세스(서비스/IDE 파이프)면 개입 불가
  Write-Output "NOT_OWNED(NO_CONSOLE) pid=$targetPid"
  exit 1
}
[ConsoleCtrl]::SetConsoleCtrlHandler([IntPtr]::Zero, $true) | Out-Null  # 자기 자신은 Ctrl+C 무시
[ConsoleCtrl]::GenerateConsoleCtrlEvent(0, 0) | Out-Null                 # 0 = CTRL_C_EVENT (콘솔 그룹 전체)
[ConsoleCtrl]::FreeConsole() | Out-Null

# 포트 해제 대기 — 타임아웃 시에도 강제종료 폴백 없음(사람에게 넘김)
$deadline = (Get-Date).AddSeconds($StopTimeoutSec)
while ((Get-Date) -lt $deadline) {
  Start-Sleep -Seconds 2
  $c = Get-NetTCPConnection -State Listen -LocalPort $Port -ErrorAction SilentlyContinue | Select-Object -First 1
  if (-not $c) {
    & $startScript -Port $Port
    exit $LASTEXITCODE
  }
}
Write-Output "STOP_TIMEOUT pid=$targetPid"
exit 1
