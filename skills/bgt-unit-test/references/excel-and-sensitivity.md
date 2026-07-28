# Excel COM · 민감도 레이블 · 암호화

## PowerShell 5.1 한글 함정 (반복 발생)

- Write 툴이 만드는 **BOM 없는 UTF-8 .ps1 을 PS 5.1 이 ANSI 로 읽어 한글이 깨진다**. 파싱 에러뿐 아니라 **문자열 비교가 조용히 실패**해 엉뚱한 창을 캡쳐하거나 잘못된 시트를 건드린다.
- 대응: **스크립트 본문은 ASCII 전용**. 한글 데이터·경로는 **JSON(`-Encoding UTF8`)에서 로드**하고, 시트는 이름이 아니라 **인덱스(`Worksheets.Item(1/2/3)`)로 접근**.
- 백틱(`` ` ``)·`` `r?`n `` 같은 정규식은 PS 파서와 충돌하니 `[char]10`/`[char]13`·`.Replace()` 로 우회.

## 경로·프로세스 함정 (저장 직전 확인)

- ⚠ **경로에 `#` 이 있으면 bash 에서 주석으로 잘린다**(`D:\project\#docs` → `ls` 실패). **파일 조작은 전부 PowerShell + `-LiteralPath`**.
- ⚠ **`~$` 잠금파일 / 좀비 EXCEL 프로세스** — 대상 파일을 누가 열어두면 저장이 실패하거나 읽기전용으로 열린다. 저장 전 `Get-ChildItem <폴더> -Filter "~$*"` 와 `Get-Process EXCEL` 을 확인하고, 있으면 사용자에게 닫아달라고 요청한다.
- 예외로 중단되면 백그라운드 EXCEL 이 파일을 잡고 남는다 → 검증 스크립트는 `try/finally` 로 정리.

## Excel COM 읽기 (샘플·검증)

- 샘플 원본은 **읽기전용**으로 열기: `$xl.Workbooks.Open($p,$null,$true)`. **원본 수정 금지** — 항상 사본 작성.
- 검증은 dst 를 `snap.xlsx` 로 복사해서 검사. **사용자가 열어둔 원본을 열거나 Excel 을 Kill 하지 말 것**(미저장분 유실). Excel 이 점유 중이면 `FileShare::ReadWrite`.
- COM 객체는 `ReleaseComObject` 로 정리, `$wb.Close($false)`(저장 안 함).

### ⚠ `Visible`/`DisplayAlerts` 는 용도에 따라 정반대다 — 헷갈리면 저장이 실패한다

| 용도 | Visible | DisplayAlerts | Quit |
|---|---|---|---|
| **빌드(내용 채우기 → 사용자가 저장)** | **`$true`** | **`$true`** | **하지 않는다** (띄운 채 종료) |
| 검증(읽기 전용, 사본을 열어 확인) | `$false` | `$false` | `Quit()` + `ReleaseComObject` |

빌드에서 `$false` 를 쓰면 **민감도 레이블 창이 못 떠 저장이 조용히 실패**하고, `Quit()` 하면 사용자가 Ctrl+S 할 대상이 사라진다.

## 민감도 레이블 → 저장은 사용자가

- 조직 정책 `mandatory=true`(정책 파일 `%LOCALAPPDATA%\Microsoft\Office\CLP\policy.*.gz`, gzip XML). **레이블 없이 저장 불가**.
- `$xl.Visible=$false`면 레이블 창이 못 떠 **`Save()` 가 COM 오류로 실패하는데 "OK"처럼 보이고 파일은 그대로**다.
- 레이블 코드 지정(`SensitivityLabel.SetLabel`)은 **보안 분류기에 "컴플라이언스 우회"로 차단**됨(2회 시도 모두 실패).
- → **에이전트는 내용만 채우고 Excel 을 띄운 채 종료**, 사용자가 **Ctrl+S**.
  - ⚠ **사내한(Company)=암호화 레이블 → 저장 실패**. **개인한(Private) 으로 저장**해야 성공.
  - ⚠ **개인한(Private) 도 "특정인원 권한설정"을 붙이면 암호화**된다(레이블ID 동일한데 결과 다름). → **권한설정은 건너뛰라**고 안내.

## 암호화 판별 — ★ 매직바이트로 판정하지 말 것

**`D0 CF 11 E0` 는 이 환경의 정상 형식이다.** 개인한(Private) MIP 레이블이 OOXML 을 OLE 컨테이너로 감싸기 때문이고, **샘플 원본과 기존 산출물이 전부 D0CF** 다(실측 확인). "PK 가 아니면 실패" 로 판정하면 **정상 산출물을 매번 재저장시키는 오판**을 한다.

| 신호 | 의미 |
|---|---|
| `50 4B`(PK) | 레이블 없는 평범한 xlsx. 이 환경 산출물에선 오히려 드묾 |
| `D0 CF 11 E0` | **정상** (개인한 MIP 레이블 OLE 컨테이너) |
| COM Open 이 암호 프롬프트를 띄우거나 실패 | **진짜 실패** (password 암호화 = 권한설정을 붙인 경우) |

**실질 판정 기준 2가지**:
1. **Excel COM 오픈이 암호 프롬프트 없이 성공**하는가
2. COM 으로 **헤더(utId/utName/pgmId) · 케이스 수 · `ws2.Shapes.Count`(캡쳐 수) · 공통체크 Pass/N/A 집계**가 맞는가

⚠ D0CF 라 `ZipFile::OpenRead` 가 "End of Central Directory..." 로 실패한다 → **`sharedStrings`/`xl/media`/`vm=` 같은 zip 내부 검사는 애초에 수행 불가**하다. 형식 탓이지 결함이 아니니 검증 항목에서 빼고 위 2가지 COM 검사로 대체한다.

## 샘플 "셀에 배치" 이미지 제거

- 샘플 `테스트결과`(2번) 시트의 안내 이미지는 **Shape 이 아니라 "셀에 배치" 이미지**(셀 값에 `vm="N"` + `xl/richData/*`, drawing rels 없음).
- `Shapes.Delete()` 로 **안 지워진다**(shapes=0 인데 화면엔 보임). → **`Range("B2:B39").ClearContents()`** 로 셀 값을 비운다.
- 삽입은 `Shapes.AddPicture(path,$false,$true,left,top,-1,-1)` 후 `Height=372`, 행높이 378.
- ⚠ 잔존 확인은 `sheet2.xml` 의 `vm=` 카운트로 **못 한다**(D0CF 라 zip 열기 실패 — 위 §암호화 판별). → **COM 으로 `B2:B39` 값이 비어 있는지**로 확인한다.
