$hook  = Join-Path $PSScriptRoot 'compact-reminder.ps1'
$dir   = Join-Path $env:TEMP 'cr-test'
$cache = "$env:USERPROFILE\.claude\model-windows.json"
New-Item -ItemType Directory -Force -Path $dir | Out-Null
Remove-Item -Force -LiteralPath $cache -ErrorAction SilentlyContinue   # cold start: force a binary extraction

function New-Transcript($model, $used) {
  $lines = @()
  $lines += @{ type='attachment'; attachment=@{ type='model'; identity=@{ modelId=$model } } } | ConvertTo-Json -Compress -Depth 9
  $lines += @{ type='assistant'; version='9.9.9'; message=@{ model=$model; usage=@{
      input_tokens=0; cache_read_input_tokens=$used; cache_creation_input_tokens=0; output_tokens=0 } } } |
      ConvertTo-Json -Compress -Depth 9
  $p = Join-Path $dir 't.jsonl'
  Set-Content -LiteralPath $p -Value ($lines -join "`n") -Encoding UTF8
  $p
}

$fails = 0
function Assert($name, $got, $want) {
  $ok = if ($got -like "*$want*") { 'PASS' } else { $script:fails++; 'FAIL' }
  '{0}  {1,-52} -> {2}' -f $ok, $name, $got
}
function Hook($model, $used) {
  $out = (@{ transcript_path=(New-Transcript $model $used); session_id='x'; cwd=$PWD.Path } | ConvertTo-Json -Compress) |
    powershell -NoProfile -ExecutionPolicy Bypass -File $hook
  if ($out) { ([regex]::Match($out, '[\d]+K / [\d]+K')).Value } else { '(silent)' }
}

# registry: claude-opus-5 / claude-sonnet-5 = 1e6 native, claude-haiku-4-5-* / claude-sonnet-4-6 = 200000
# 1st case is the one that proves the registry was read: the 200K floor would warn here, 1M stays silent.
Assert 'native 1M, 100K used -> silent (floor would warn)' (Hook 'claude-opus-5'              100000) '(silent)'
Assert 'native 1M, 400K used'                              (Hook 'claude-sonnet-5'            400000) '/ 1000K'
Assert '200K model, 80K used'                              (Hook 'claude-haiku-4-5-20251001'   80000) '/ 200K'
Assert '200K model + [1m] suffix, 400K used'               (Hook 'claude-sonnet-4-6[1m]'      400000) '/ 1000K'
Assert 'unknown model, 80K -> 200K floor'                  (Hook 'claude-zz-test'              80000) '/ 200K'
Assert 'unknown model, 400K -> 1M safety net'              (Hook 'claude-zz-test'             400000) '/ 1000K'

$c = Get-Content -LiteralPath $cache -Raw | ConvertFrom-Json
Assert 'cache written, keyed by cli version'               "version=$($c.version)"                     'version=9.9.9'
Assert 'cache holds registry windows'                      "opus-5=$($c.windows.'claude-opus-5') haiku=$($c.windows.'claude-haiku-4-5-20251001')" 'opus-5=1000000 haiku=200000'

Remove-Item -Recurse -Force -LiteralPath $dir
if ($fails) { "`n$fails FAILED"; exit 1 } else { "`nall passed"; exit 0 }
