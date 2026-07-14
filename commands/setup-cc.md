---
description: 터미널에서 'cc'만 입력하면 claude 가 실행되도록 PowerShell 프로필에 별칭을 설치한다(멱등, 유저 scope).
allowed-tools: Bash(powershell:*)
---

팀원 PC의 PowerShell 프로필($PROFILE, Current User)에 `cc` → `claude` 별칭을 설치한다.
아래 PowerShell 스크립트를 **그대로 1회 실행**하라(문구 변경 금지). 멱등하므로 이미 설치돼 있으면 아무것도 하지 않는다.

```powershell
$line = 'function cc { claude @args }'
$dir  = Split-Path $PROFILE
if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
if ((Test-Path $PROFILE) -and (Select-String -Path $PROFILE -Pattern '^\s*function\s+cc\s' -Quiet)) {
  Write-Output "이미 설치됨: $PROFILE"
} else {
  Add-Content -Path $PROFILE -Value $line -Encoding utf8
  Write-Output "설치 완료: $PROFILE"
  Write-Output "새 PowerShell 창부터 'cc' 사용 가능. 현재 창에서 바로 쓰려면: . `$PROFILE"
}
```

실행 후:
- 결과 메시지(설치 완료 / 이미 설치됨)를 그대로 사용자에게 전달한다.
- Windows/PowerShell 전용이다. macOS/Linux 사용자면 설치하지 말고, 해당 셸(zsh/bash) 프로필에 `alias cc='claude'` 를 넣으라고 안내한다.
- 별칭 이름 `cc` 는 유닉스 C 컴파일러와 겹칠 수 있다. 실행 결과가 이상하면 다른 이름을 쓰라고 안내한다.
