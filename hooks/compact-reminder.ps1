# Stop hook: 컨텍스트 잔여량이 임계치 이하로 떨어지면 /compact 하라고 사용자에게 알림.
# stdin 으로 훅 입력 JSON(transcript_path, session_id)을 받아 transcript 를 읽고 직접 계산한다.
# ponytail: 훅은 컨텍스트 %를 직접 안 주므로 마지막 assistant usage 로 근사. 자동 compact 는 꺼져 있음.

$ErrorActionPreference = 'SilentlyContinue'

# --- 설정: 모델별 컨텍스트 창 크기 ---
# 키 = transcript 에 기록되는 model id. 마지막 assistant 라인의 model 로 창 크기를 고른다. 없으면 $DefaultWindow.
# 주의: [1m] 접미사는 transcript 에 안 남으므로(opus-4-8 200K/1M 이 같은 id) 아래서 사용량으로 보정한다.
$WindowByModel = @{
  'claude-opus-4-8'   = 1000000   # opus-4-8[1m]. 표준 200K 로만 쓰면 200000 로.
  'claude-sonnet-4-6' = 200000
  'claude-haiku-4-5'  = 200000
}
$DefaultWindow                   = 200000    # 표에 없는 model id 의 기본 창.
$RemindWhenRemainingPctAtOrBelow = 70        # 잔여 <= 70% (== 사용 >= 30%) 이면 알림. 사용 70%에서 알리려면 30.

$raw = [Console]::In.ReadToEnd()
if (-not $raw) { exit 0 }
$in  = $raw | ConvertFrom-Json
$tp  = $in.transcript_path
if (-not $tp -or -not (Test-Path -LiteralPath $tp)) { exit 0 }

# 꼬리 120줄에서 가장 최근 assistant 라인의 usage 를 정규식으로 추출.
# ponytail: PS5.1 ConvertFrom-Json 은 거대한 라인(agent_listing 등)에서 터지므로 숫자만 regex 로 뽑는다.
$used = $null
$model = $null
$lines = @(Get-Content -LiteralPath $tp -Tail 120)
for ($i = $lines.Count - 1; $i -ge 0; $i--) {
  $ln = $lines[$i]
  if ($ln -notmatch '"type":"assistant"' -or $ln -notmatch '"input_tokens":') { continue }
  $it = if ($ln -match '"input_tokens":(\d+)')                { [int64]$Matches[1] } else { 0 }
  $cr = if ($ln -match '"cache_read_input_tokens":(\d+)')     { [int64]$Matches[1] } else { 0 }
  $cc = if ($ln -match '"cache_creation_input_tokens":(\d+)') { [int64]$Matches[1] } else { 0 }
  $ot = if ($ln -match '"output_tokens":(\d+)')              { [int64]$Matches[1] } else { 0 }
  $used = $it + $cr + $cc + $ot
  if ($ln -match '"model":"([^"]+)"') { $model = $Matches[1] }   # 같은 라인의 메인 모델 id
  break
}
if ($null -eq $used) { exit 0 }

# 세션의 마지막 메인 모델로 창 크기 선택. 표에 없으면 기본값.
$ContextWindowTokens = if ($model -and $WindowByModel.ContainsKey($model)) { $WindowByModel[$model] } else { $DefaultWindow }
# [1m] 접미사가 transcript 에 없어 model id 로는 200K/1M 을 못 가른다.
# 보정: 이미 사용량이 표준 200K 를 넘었다면 창은 최소 1M 이다(200K 모델은 200K 초과 불가).
if ($used -gt 200000 -and $ContextWindowTokens -le 200000) { $ContextWindowTokens = 1000000 }

$remainingPct = [math]::Round((($ContextWindowTokens - $used) / $ContextWindowTokens) * 100)

# 임계치 위면 조용히 종료. 이하면 매 Stop 마다 반복 알림(dedup 없음) → /compact 로 잔여가 회복될 때까지 계속.
if ($remainingPct -gt $RemindWhenRemainingPctAtOrBelow) { exit 0 }

$usedK = [math]::Round($used / 1000)
$winK  = [math]::Round($ContextWindowTokens / 1000)
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
