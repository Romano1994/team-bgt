# BE(bgt-be) 기동 — 새 콘솔 창에서 gradlew.bat bootRun, 포트 리슨 폴링 + 로그 기반 fast-fail
# 출력: STARTED pid=<pid> | PORT_BUSY pid=<pid> | SPAWN_FAIL rv=<n>
#       | START_FAILED reason=<ORA-xxxxx|BUILD_FAILED> | START_TIMEOUT
# 스폰은 WMI(Win32_Process.Create) 사용 — 호출 셸(샌드박스)의 잡 오브젝트 밖에서 생성돼
# 셸 종료와 무관하게 살아남고, 콘솔 창이 데스크톱에 남아 사용자가 직접 Ctrl+C 가능.
# JAVA_HOME은 환경에 없으므로(실측) 기동 명령에 직접 주입한다.
# bootRun 출력을 로그 파일로 캡쳐해, 폴링 중 BUILD FAILED / ORA-xxxxx(DB 거부)를 즉시 감지한다
# — bootRun이 십수 초 만에 죽어도 300초 헛대기하던 문제(START_TIMEOUT 오진)를 없앤다.
param(
  [string]$BeDir = 'C:\workspace\bgt\bgt-be',
  [string]$JavaHome = 'C:\Users\GS\.jdks\corretto-19.0.2',
  [int]$Port = 7078,
  [int]$TimeoutSec = 300
)
$ErrorActionPreference = 'Stop'

$busy = Get-NetTCPConnection -State Listen -LocalPort $Port -ErrorAction SilentlyContinue | Select-Object -First 1
if ($busy) { Write-Output "PORT_BUSY pid=$($busy.OwningProcess)"; exit 1 }

# bootRun 콘솔 출력 캡쳐 로그(fast-fail 판정용). 매 기동마다 초기화해 이전 실행 로그 오판을 막는다.
$logPath = Join-Path $BeDir 'build\bootrun.log'
$logDir = Split-Path $logPath
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir -Force | Out-Null }
Set-Content -Path $logPath -Value '' -Encoding utf8

# cmd /k 로 콘솔 유지(사용자가 직접 Ctrl+C 가능) + gradlew 출력만 로그로 리다이렉트.
# 리다이렉트해도 콘솔 그룹은 유지되므로 be-restart.ps1 의 Ctrl+C(SIGINT) 주입은 그대로 동작한다.
$cmdLine = "cmd.exe /k title BGT-BE bootRun && set `"JAVA_HOME=$JavaHome`" && cd /d `"$BeDir`" && gradlew.bat bootRun > `"$logPath`" 2>&1"
$r = Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{ CommandLine = $cmdLine; CurrentDirectory = $BeDir }
if (-not $r -or $r.ReturnValue -ne 0) { Write-Output "SPAWN_FAIL rv=$($r.ReturnValue)"; exit 1 }

$deadline = (Get-Date).AddSeconds($TimeoutSec)
while ((Get-Date) -lt $deadline) {
  Start-Sleep -Seconds 3
  # 1) 성공 신호: 포트 리슨(앱이 완전히 떠 실제 LISTEN 상태일 때만 잡힘)
  $c = Get-NetTCPConnection -State Listen -LocalPort $Port -ErrorAction SilentlyContinue | Select-Object -First 1
  if ($c) { Write-Output "STARTED pid=$($c.OwningProcess)"; exit 0 }
  # 2) 실패 신호: 로그에서 DB 거부(ORA-) 또는 빌드/기동 실패 감지 → 즉시 반환(헛대기 금지)
  #    ORA-18958 등 DB 거부는 인프라 블로커(D-18 §3) — 재시도/강제종료 금지, BLOCKED 처리로 이어짐.
  $log = Get-Content $logPath -Tail 300 -ErrorAction SilentlyContinue | Out-String
  if ($log) {
    if ($log -match 'ORA-\d{4,5}') { Write-Output "START_FAILED reason=$($Matches[0])"; exit 1 }
    if ($log -match 'BUILD FAILED|APPLICATION FAILED TO START|Web server failed to start') {
      Write-Output 'START_FAILED reason=BUILD_FAILED'; exit 1
    }
  }
}
Write-Output 'START_TIMEOUT'
exit 1
