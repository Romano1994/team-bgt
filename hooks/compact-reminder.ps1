# Stop hook: 컨텍스트 잔여량이 임계치 이하로 떨어지면 /compact 하라고 사용자에게 알림.
# stdin 으로 훅 입력 JSON(transcript_path, session_id)을 받아 transcript 를 읽고 직접 계산한다.
# ponytail: 훅은 컨텍스트 %를 직접 안 주므로 마지막 assistant usage 로 근사.
# 기준은 '모델의 실제 컨텍스트 창'이다. autoCompactWindow(별도 설정)는 그보다 먼저 걸리는 별개의 벽이라 여기 안 쓴다.

$ErrorActionPreference = 'SilentlyContinue'

# --- 설정 ---
# 모델별 창 크기 표는 두지 않는다. CLI 바이너리 안의 모델 레지스트리를 읽어 CLI 버전별로 캐시한다.
# /context 출력의 분모는 쓰면 안 된다 — min(모델 창, autoCompactWindow) 라서 autoCompactWindow 를
# 설정해 두면 모델과 무관하게 그 값이 나온다(실측: sonnet-5·fable-5-1·opus-5-5[1m] 전부 600k).
$FloorWindow                     = 200000    # 레지스트리에 없는 모델의 보수적 기본값
$OneM                            = 1000000
$CachePath                       = "$env:USERPROFILE\.claude\model-windows.json"
$RemindWhenRemainingPctAtOrBelow = 70        # 잔여 <= 70% (== 사용 >= 30%) 이면 알림. 사용 70%에서 알리려면 30.

$raw = [Console]::In.ReadToEnd()
if (-not $raw) { exit 0 }
$in  = $raw | ConvertFrom-Json
$tp  = $in.transcript_path
if (-not $tp -or -not (Test-Path -LiteralPath $tp)) { exit 0 }

# 꼬리 120줄에서 가장 최근 assistant 라인의 usage 를 정규식으로 추출.
# ponytail: PS5.1 ConvertFrom-Json 은 거대한 라인(agent_listing 등)에서 터지므로 숫자만 regex 로 뽑는다.
$used = $null
$cliVer = ''
$lines = @(Get-Content -LiteralPath $tp -Tail 120)
for ($i = $lines.Count - 1; $i -ge 0; $i--) {
  $ln = $lines[$i]
  if ($ln -notmatch '"type":"assistant"' -or $ln -notmatch '"input_tokens":') { continue }
  $it = if ($ln -match '"input_tokens":(\d+)')                { [int64]$Matches[1] } else { 0 }
  $cr = if ($ln -match '"cache_read_input_tokens":(\d+)')     { [int64]$Matches[1] } else { 0 }
  $cc = if ($ln -match '"cache_creation_input_tokens":(\d+)') { [int64]$Matches[1] } else { 0 }
  $ot = if ($ln -match '"output_tokens":(\d+)')              { [int64]$Matches[1] } else { 0 }
  $used = $it + $cr + $cc + $ot
  if ($ln -match '"version":"([^"]+)"') { $cliVer = $Matches[1] }   # 레지스트리는 CLI 버전따라 바뀐다
  break
}
if ($null -eq $used) { exit 0 }

# 실행중 모델 id. model 어태치먼트만 [1m] 접미사를 유지한다(message.model 은 잘린 claude-opus-5).
# 세션 앞쪽 + /model 전환 시점에 찍혀서 tail 로는 못 잡으므로 전체 스캔한다(24MB 기준 ~200ms).
$mm = [regex]::Matches((Get-Content -LiteralPath $tp -Raw), '"modelId":"([^"]+)"')
$modelId = if ($mm.Count) { $mm[$mm.Count - 1].Groups[1].Value } else { '' }

# CLI 바이너리의 모델 레지스트리에서 first_party id -> 창 크기를 긁는다(8MB 청크, ~750ms).
# CLI 버전이 바뀔 때만 돌고 평소엔 캐시 조회뿐이다.
function Read-ModelWindows($exe) {
  $out = @{}
  $fs  = [IO.File]::OpenRead($exe)
  if (-not $fs) { return $out }
  $buf = New-Object byte[] (8MB)
  $tail = ''
  while (($r = $fs.Read($buf, 0, $buf.Length)) -gt 0) {
    $s = $tail + [Text.Encoding]::ASCII.GetString($buf, 0, $r)
    foreach ($m in [regex]::Matches($s, '(?s)first_party:"(claude-[a-z0-9-]+)".{0,400}?context:\{window:([0-9.e]+)')) {
      $out[$m.Groups[1].Value] = [int64][double]$m.Groups[2].Value
    }
    $tail = $s.Substring([Math]::Max(0, $s.Length - 800))   # 청크 경계에 걸친 엔트리 보존
  }
  $fs.Close()
  $out
}

$cache = if (Test-Path -LiteralPath $CachePath) { Get-Content -LiteralPath $CachePath -Raw | ConvertFrom-Json } else { $null }
if (-not $cache -or $cache.version -ne $cliVer) {
  # 훅이 CLAUDE_CODE_EXECPATH 없이 뜨는 경로가 있어 npm shim 으로도 찾는다.
  $exe = $env:CLAUDE_CODE_EXECPATH
  if (-not $exe) {
    $shim = (Get-Command claude -ErrorAction SilentlyContinue).Source
    if ($shim) { $exe = Join-Path (Split-Path $shim) 'node_modules\@anthropic-ai\claude-code\bin\claude.exe' }
  }
  if ($exe -and (Test-Path -LiteralPath $exe)) {
    $w = Read-ModelWindows $exe
    if ($w.Count) {
      $cache = [pscustomobject]@{ version = $cliVer; windows = $w }
      # ponytail: write 에 락 없음. 동시 세션이 겹쳐도 같은 값을 쓰므로 손해가 없다.
      $cache | ConvertTo-Json -Compress -Depth 5 | Set-Content -LiteralPath $CachePath -Encoding UTF8
    }
  }
}

# [1m] 은 접미사로 1M 을 여는 모델(sonnet-4-6 등)에만 의미가 있다. 이미 네이티브 1M 이면 그대로.
$base = $modelId -replace '\[1m\]$', ''
$win  = if ($base -and $cache.windows.$base) { [int64]$cache.windows.$base } else { $FloorWindow }
if ($modelId -match '\[1m\]' -and $win -lt $OneM) { $win = $OneM }
if ($used -gt $win) { $win = $OneM }   # 안전망: 레지스트리를 못 읽은 미지 모델도 여기서 풀린다

$remainingPct = [math]::Round((($win - $used) / $win) * 100)

# 임계치 위면 조용히 종료. 이하면 매 Stop 마다 반복 알림(dedup 없음) → /compact 로 잔여가 회복될 때까지 계속.
if ($remainingPct -gt $RemindWhenRemainingPctAtOrBelow) { exit 0 }

$usedK = [math]::Round($used / 1000)
$winK  = [math]::Round($win / 1000)
# ANSI 빨강으로 감쌈(esc=0x1b). Claude Code가 ANSI 미지원이면 esc/reset 두 부분만 지우고 🔴만 남긴다.
$esc = [char]27
$msg = "${esc}[31m🔴 컨텍스트 잔여 ${remainingPct}% (사용 ${usedK}K / ${winK}K). /compact 로 대화를 정리하세요.${esc}[0m"
$json = [ordered]@{ systemMessage = $msg } | ConvertTo-Json -Compress
# raw stdout 에 UTF-8 바이트 직접 쓰기 (PS 5.1 콘솔 인코딩 우회, BOM 없음)
$bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
$out = [Console]::OpenStandardOutput()
$out.Write($bytes, 0, $bytes.Length)
$out.Flush()
exit 0
