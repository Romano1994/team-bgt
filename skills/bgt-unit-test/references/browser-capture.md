# 브라우저 실수행 · 캡쳐

Playwright MCP 로 호스트 셸 안에서 대상 화면을 띄워 이벤트를 수행하고 케이스별로 캡쳐한다. 포트·라우팅은 프로젝트 실행 문서를 따른다.

## ⚠ 뷰포트 에뮬레이션 함정 (오보 이력 있음)

- `browser_resize`/`setViewportSize` 로 1194 등을 주면 페이지에 **가짜 innerHeight 가 주입**되고, 그 설정이 **세션에 눌러붙어** 이후 캡쳐가 실제 창과 다른 높이로 찍힌다. (과거 1920×1080 에뮬로 "중앙정렬 완료" 오보 → 실제는 틀림.)
- **판별**: `outerHeight - innerHeight == 0` 이면 에뮬레이션(실제 창은 크롬 UI 만큼 ~89).
- **해제 불가**: `Emulation.clearDeviceMetricsOverride`(CDP)·`setViewportSize(null)` 모두 실패(Playwright 재적용).
- **대응**: 실제 창 콘텐츠 크기로 맞춘다 → **1920×991**(`Browser.getWindowBounds` = 1936×1096 maximized − 크롬 UI 89). 이후 캡쳐가 실제 화면과 동일.
- 캡쳐는 `browser_take_screenshot({scale:'css'})`, 확인 필요하면 `Read` 로 이미지 확인.

## 캡쳐 프레이밍 — 좌측 메뉴 트리 포함

- 모든 케이스 캡쳐는 **제일 좌측 메뉴 트리를 펼친(포함) 상태**로 찍는다.
- 메뉴 트리는 **좌상단 햄버거 버튼 클릭으로 toggle** 된다 → 접혀 있으면 먼저 클릭해 펼친 뒤 캡쳐. 한 번 펼치면 유지되므로 세션 시작 시 한 번만 확인하면 된다.
- 예외: **1194 해상도 케이스(공통08)** 는 반응형으로 트리가 접히는 게 정상 동작이므로 억지로 펼치지 말고 그 케이스 규칙(아래 §1194)을 따른다.

## 창만 캡쳐 (유출 금지)

- **전체화면·데스크톱 캡쳐 금지**(IDE·대화 내용 유출). Playwright viewport 캡쳐는 브라우저 페이지 영역만 담기므로 안전. PS+`GetWindowRect` 로 대상 창만 뜨는 방식도 가능하나, 백그라운드에선 `SetForegroundWindow` 가 안 먹으니 필요하면 사용자에게 창을 열어달라 요청.
- 사용자 Excel 캡쳐가 필요하면 "지금 열어둘게"를 받고 그 창만.

## 1194 해상도 확인

- 공통08(태블릿 1194) 케이스: 폭 1194 로 조정 후 확인. **이미 열린 팝업은 리사이즈에 재배치되지 않아 잘려 보인다** → **닫고 1194 상태에서 새로 열어** 판정(팝업이 화면폭 안에 배치·컬럼 잘림/라벨 2줄 없음, 넘치면 스크롤).
- 확인 후 **1920×991 로 복구**.

## IBSheet 직접 접근 (그리드/트리 상태 확인)

- `window.IBSheet['0'..'N']` 에 시트 인스턴스. `.getDataRows()` 로 행 데이터, `.id` 로 그리드 식별(예 `<화면약칭>-master-...`). 화면 여러 그리드 중 대상 찾기:
  ```js
  for (let i=0;i<16;i++){const s=window.IBSheet[String(i)]; if(s&&s.getDataRows&&s.id.includes('search-master')) ...}
  ```
- **트리 자식은 더블클릭 지연 로드**(onDblClick). `page.mouse.dblclick(x,y)` 로 펼치고 `getDataRows()` 로 `prjSct/Level/discRevNo` 확인.
- `prjSct` 는 **root=숫자 1, 하위=문자열 "2"/"3"/"4"** 로 섞여 온다(비교 시 주의).
- 포커스된 행은 오버레이(`IBFocusRowBackground`)가 클릭을 가로챈다 → 이미 선택된 행이면 재클릭 말고 바로 다음 동작.

## 좌표 클릭 · 임시 id · 합성이벤트 한계

- 입력란은 `getBoundingClientRect` 로 좌표 잡아 임시 `id` 부여 후 클릭/타이핑, **캡쳐 전 반드시 `removeAttribute('id')`**.
- **합성 이벤트로는 못 잡는 UI 로직 주의**(결함 오인). `onFocus`로 세팅되는 ref 를 조건으로 쓰는 코드(면적 M2↔평 자동환산 등)는 네이티브 setter+`input` 이벤트로는 발동 안 함 → **실제 click+type**(`page.mouse.click` + `keyboard.type`)으로 재확인 후 결함 판정.
- 한 `<tr>` 에 필드 2개면 행 단위 텍스트 매칭이 엉뚱한 input 을 덮어씀 → 셀/좌표/인덱스로 지정.
- 입력 제한 검증: 영문 타이핑 후 값 비어있나 + 초과자리 잘리나(`12345`→`12`)를 값으로 확인.

## ★ toast 캡쳐 — 일반 절차로는 절대 못 잡는다

toast 는 **`autoHideDuration: 4000`(4초, `@amxis/design-system` MessageBar)** 이지만 **MCP 도구 왕복(클릭 호출 → 스크린샷 호출)이 더 느려 빈 화면만 찍힌다.**

⚠ **confirm/alert 는 브라우저 네이티브가 아니라 React 모달**이다(`showConfirm` → CommonDialog). 자동으로 사라지지 않으므로 여유 있게 캡쳐할 수 있고, **`browser_handle_dialog` 가 아니라 일반 클릭**으로 다룬다.

⚠ **디바운스가 걸린 검증 toast** 는 입력 직후가 아니라 **디바운스 시간(예 500ms) 뒤**에 뜬다. 바로 캡쳐하면 빈 화면이다.

- **`browser_run_code_unsafe` 한 호출 안에서** `click → waitForFunction(toast 등장) → screenshot` 을 끝낸다.
- ⚠ **마우스를 다른 곳으로 옮기면 0.4~0.6초 안에 닫힌다** → `mouse.move(중립)` 금지.
- **★ toast 위로 마우스를 올리면 자동 닫힘 타이머가 멈춘다.** `waitForFunction`(등장) → `mouse.move(토스트 중앙)` → 로딩 오버레이 사라질 때까지 폴링 → 캡쳐 ⇒ **토스트 + 재조회 완료 화면을 한 장에** 담을 수 있다.
- **toast DOM = `div[role=alert]`**(MuiAlert). confirm/alert 모달은 **클래스가 고정이 아니므로**(`css-1phrvl4`/`css-1fwijtw`) **문구로 탐색**할 것.
- ⚠ **저장 완료 toast 는 프로시저가 느려(>30초) 기본 대기로는 못 잡는다** → `waitForFunction(..., {timeout:120000})`.
- ⚠ **무거운 팝업은 `page.screenshot` 기본 30초 타임아웃도 넘긴다**(무거운 팝업은 오픈에만 8초 이상 걸리는 사례 있음) → `{timeout: 60000}`.

## 검증 실패 분기(alert/toast) 캡쳐

- 이벤트 기반의 핵심. confirm/alert 텍스트를 DOM 에서 읽어 케이스 근거로. **취소 분기**도 별도 케이스(취소 시 미실행·팝업 유지·값 유지 확인).
- ✅ **메시지 앞 밑줄(`____`)은 결함이 아니다** . `____저장하시겠습니까?` 처럼 `getText('msg', ...)` 앞에 붙는 밑줄은 **그 문구가 다국어에 미등록**이라 나오는 표시일 뿐이다. **비고에 "결함 후보"로 적지 말 것.** 문구 자체는 그대로 인용해도 된다.

## DEXT5 파일첨부 자동화

- 숨겨진 `input[type=file]` 존재. **[파일추가] 클릭으로 file chooser modal 을 띄운 뒤 `browser_file_upload`**(modal 없이 `setInputFiles` 직접 호출은 실패).
- ⚠ Playwright MCP 는 **워크스페이스 밖 경로 거부**("outside allowed roots") → `bgt\.playwright-mcp\` 로 복사 후 업로드(끝나면 사본 삭제).
- 첨부 목록은 object/iframe 내부라 `innerText` 검색 불가 → 캡쳐로 확인.

## IBSheet 셀 편집 · 그리드 자동화

- **텍스트 셀** = 더블클릭 → `Ctrl+A` → 타이핑 → `Enter`. ⚠ **`Escape` 는 MUI Dialog 를 닫으므로 셀 편집 취소에 쓰지 말 것.**
- **Enum 셀** = 셀 클릭 → 우측 끝 재클릭으로 `IBEnumMenuBody` 오픈. **Bool 셀** = 클릭 후 **Space**.
- **헤더 버튼은 JS `.click()` 이 무효** → `page.mouse.click(좌표)`(스크롤마다 좌표 재계산). 값 세팅 대체는 `sheet.setValue(row,col,val,1)`.
- 팝업 재오픈 시 **IBSheet 인덱스가 바뀐다** → `id.includes('<그리드id 일부>')` 로 탐색.
- 행추가 위치는 그리드마다 다르다(맨 위/맨 아래) — 코드에서 `addGridFirstRow`/`addGridLastRow` 확인.
- 검색팝업이 **뒤늦게 열려 backdrop 이 클릭을 삼키는** 사고가 있으니 항상 존재 확인 후 닫을 것.

## DB 변경 이벤트 · 잔존 데이터

- 저장/복사/상태전이는 **DEV DB 에 실제 데이터를 생성**한다. 조회조건과 다른 명칭으로 만들면 재조회 LIKE 에 안 걸려 트리에 안 보일 수 있음 → **결함 오인 금지**, 새 명칭으로 재조회해 확인.
- ✅ **잔존 테스트데이터는 정리하지 않아도 된다**(개발서버 기준. 팀 정책을 한 번 확인할 것). 저장/복사/상태전이를 수행할 때 데이터 정리를 걱정하지 말 것. 다만 **무엇을 남겼는지는 진행 로그에 기록**해 다음 세션이 상태를 알게 한다.
