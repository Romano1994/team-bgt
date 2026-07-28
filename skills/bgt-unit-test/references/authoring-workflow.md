# 작성 절차 · 스크립트 패턴

화면 1건을 결과서로 만드는 전 과정. 스크래치패드(`.../scratchpad/`)에 데이터 JSON + ASCII 전용 `.ps1` 2개(build/verify)를 두고 재사용한다.

## 0. 인테이크 + 사전확인

받을 7항목과 착수 전 실측 4가지는 **SKILL.md §0** 참조. 요약하면:
- 받을 것: ID / 작성대상화면 / 1194 / 엑셀다운로드 / 저장경로·파일명 / 단위테스트명 / 도출방식
- 실측할 것: **경로 실존**(`Test-Path` — `00.` 뒤 공백 없음) · **동일 파일명 존재**(덮어쓰기 경고) · **ID↔화면↔명칭 3자 정합** · 재작성이면 **화면 변경 여부**(`git log`)
- 미수행 항목(브라우저별 등) 처리 방침(Pass/N/A)도 함께 확인.

## 1. 화면 코드 분석 → 케이스 후보

이벤트 기반이 원칙(SKILL.md 함정 1). 화면/팝업 `index.tsx`·`__components`에서 다음을 전수 추출:
- 버튼 클릭(조회/저장/신규/복사/닫기/X), 입력 변경, 행 선택, 드롭다운/라디오 변경, 체크박스, 팝업 오픈/닫기
- **검증 실패 분기**(각 alert/toast/confirm 취소) — 하나하나 케이스
- 상태별 동작 차이(readOnly 전환, 버튼 Visible/Enable), 입력 제한(정규식·maxLength), 자동채번·자동환산
- 해상도 1194(공통08), 필요 시 다운로드

## 2. 브라우저 실수행 + 캡쳐

- 프로젝트 실행 절차(호스트 셸 + remote)로 화면을 띄운다. **뷰포트는 실제 창 1920×991** 로 맞춘다(browser-capture.md).
- 이벤트를 하나씩 발생 → 결과 확인 → **케이스 지점마다 창 캡쳐**를 `bgt/cpNN.png`(또는 화면약칭)로 저장.
- 좌표 클릭·IBSheet 직접 접근·임시 `id` 부여 기법은 browser-capture.md.
- DB를 바꾸는 이벤트(저장/복사/상태전이)도 실제 수행하고, **잔존 테스트데이터를 진행 로그에 기록**.

## 3. 캡쳐 매핑 + 데이터 JSON

캡쳐를 케이스 번호에 맞춰 `utNN.png`로 복사(`imgPrefix="ut"`), 데이터 JSON 작성. `dst`는 최종 산출물 경로.

```json
{
  "sample": "D:\\project\\#docs\\00.단위테스트\\단위테스트 샘플.xlsx",
  "dst": "D:\\project\\#docs\\00.단위테스트\\00.단위테스트결과\\PMX-BGT-단위테스트결과서(UT_..._명)-v1.0.xlsx",
  "utId": "<단위테스트ID>",
  "utName": "<단위테스트명>",
  "pgmId": "<관련프로그램ID = UT_ 뗀 값>",
  "imgPrefix": "ut",
  "cases": [
    { "n": 1, "content": "이벤트/조작\n1) ...", "data": "입력값", "expect": "기대결과\n1) ...", "result": "Pass", "note": "비고" }
  ],
  "checks": [ { "row": 3, "v": "Pass" }, { "row": 4, "v": "N/A" }, ... { "row": 49, "v": "N/A" } ]
}
```

- `content`/`expect`/`note`는 셀 내 줄바꿈을 `\n`으로. 실사례 톤은 샘플 `테스트케이스` 시트의 PMX 예시(`PMX_CMM_UPD_W_0000010`)를 기준으로.
- `checks`는 3번 시트 R3~R49(47행) 전부. 화면에 해당 없는 항목은 N/A. 공통 항목 의미는 아래 §5.

## 4. 빌드 스크립트 (ASCII 전용, 한글은 JSON에서 로드)

`build_XX.ps1` — 샘플을 dst로 복사 후 3개 시트를 채우고 Excel을 **띄운 채로** 둔다(사용자 Ctrl+S). 시트는 **인덱스 접근**.

```powershell
$ErrorActionPreference = "Continue"
$base = "<scratchpad>"; $img = "C:\Users\GS\project\workspace\bgt"
$cfg  = Get-Content -LiteralPath "$base\XX_data.json" -Raw -Encoding UTF8 | ConvertFrom-Json
$dst  = $cfg.dst
Copy-Item -LiteralPath $cfg.sample -Destination $dst -Force

$xl = New-Object -ComObject Excel.Application
$xl.Visible = $true; $xl.DisplayAlerts = $true
$wb = $xl.Workbooks.Open($dst)

# 1) 테스트케이스 시트
$ws = $wb.Worksheets.Item(1)
$ws.Cells.Item(1,2).Value2 = $cfg.utId
$ws.Cells.Item(1,5).Value2 = $cfg.utName
foreach ($c in $cfg.cases) {
  $r = 3 + [int]$c.n
  $ws.Cells.Item($r,2).Value2 = $c.content
  $ws.Cells.Item($r,3).Value2 = $c.data
  $ws.Cells.Item($r,4).Value2 = $c.expect
  $ws.Cells.Item($r,5).Value2 = $cfg.pgmId
  $ws.Cells.Item($r,6).Value2 = $c.result
  $ws.Cells.Item($r,7).Value2 = $c.note
}
$firstEmpty = 4 + $cfg.cases.Count
for ($r=$firstEmpty; $r -le 45; $r++) { for ($c2=2; $c2 -le 7; $c2++) { $ws.Cells.Item($r,$c2).Value2 = "" } }

# 3) 공통체크항목 시트 (D열)
$ws3 = $wb.Worksheets.Item(3)
foreach ($k in $cfg.checks) { $ws3.Cells.Item([int]$k.row,4).Value2 = $k.v }

# 2) 테스트결과 시트 - 샘플 "셀에 배치" 이미지 제거 후 캡쳐 삽입 (B열)
$ws2 = $wb.Worksheets.Item(2)
for ($i=$ws2.Shapes.Count; $i -ge 1; $i--) { $ws2.Shapes.Item($i).Delete() }
$ws2.Range("B2:B39").ClearContents()          # ← Shapes.Delete 로는 안 지워지는 셀배치 이미지
$ws2.Columns.Item(2).ColumnWidth = 120
foreach ($c in $cfg.cases) {
  $n = [int]$c.n
  $p = Join-Path $img ($cfg.imgPrefix + $n.ToString("00") + ".png")
  if (-not (Test-Path -LiteralPath $p)) { continue }
  $row = $n + 1
  $cell = $ws2.Cells.Item($row,2)
  $pic = $ws2.Shapes.AddPicture($p,$false,$true,$cell.Left+2,$cell.Top+2,-1,-1)
  $pic.LockAspectRatio = -1; $pic.Height = 372
  try { $ws2.Rows.Item($row).RowHeight = 378 } catch {}
}

$ws.Activate(); $ws.Range("A1").Select(); $xl.WindowState = -4137
[Runtime.InteropServices.Marshal]::ReleaseComObject($wb) | Out-Null
[Runtime.InteropServices.Marshal]::ReleaseComObject($xl) | Out-Null
Write-Output "Excel open. Ctrl+S, pick Private label, skip permission settings."
```

- 케이스 수가 다른 화면은 `-le 45`·`B2:B39` 상한만 케이스수에 맞게 확인(샘플은 순번 42까지).
- 실행 후 사용자에게 **"Ctrl+S → 개인한(Private) → 권한설정 건너뛰기"** 안내.

## 5. 검증 스크립트

`verify_XX.ps1` — dst 사본(`snap.xlsx`)을 떠서 검사(원본 열지 말 것, Excel 잠금 대비 `FileShare::ReadWrite`).

⚠ **매직바이트·zip 내부 검사는 쓰지 말 것.** 이 환경 산출물은 전부 `D0CF`(개인한 MIP 레이블 = 정상)라 "PK 아니면 실패" 는 오판이고, `ZipFile::OpenRead` 자체가 실패해 `sharedStrings`/`xl/media`/`vm=` 검사는 **수행 불가**하다. 상세는 excel-and-sensitivity.md §암호화 판별.

**COM 으로만 검증한다**:
- **오픈이 암호 프롬프트 없이 성공**하는가 (진짜 실패는 여기서 드러남)
- 헤더(utId·utName·pgmId) 일치, 샘플 ID(`PMX_CMM_UPD_W_0000010`) 잔존 없음
- 각 케이스 content·result 채워짐, 케이스 수 일치
- `ws2.Shapes.Count` = 캡쳐수, seq 커버리지, **`B2:B39` 값 잔존 0**(샘플 셀배치 이미지 제거 확인)
- 3번 시트 Pass/N/A 집계

검증 통과(= 엑셀 결과물 최종 산출) 후 **작업 기록 갱신 + 캡쳐에 쓰인 파일(`cpNN.png`·`utNN.png` 등)을 모두 삭제**. 엑셀에 이미지가 박힌 뒤이므로 원본 PNG 는 불필요하다.

## 6. 공통체크항목(3번 시트) 참고

R3~R49(47행), D열에 Pass/N/A. 화면 성격에 따라 다르지만 대체로:
- 조회 화면: 화면로드·필수표시·정렬·해상도·메시지·조회변경안내 계열 Pass, 배치/출력/입력Validation 다수 N/A.
- 편집(UPD): 위 + 입력제한(공통15)·자릿수(공통16)·필수미입력메시지(공통23)·저장Confirm/완료(유효성체크) Pass.
- 팝업 단순조회: 최소 Pass(화면로드·정렬·해상도·읽기전용 color) + 나머지 N/A.
- **이벤트 기반으로 짜면** 공통15/16/23·유효성체크가 실제 검증되어 N/A→Pass 로 바뀐다(실제 재작성 사례).
- 원문은 샘플 3번 시트를 Excel COM 으로 덤프해 그대로 인용(문구 변형 금지).
