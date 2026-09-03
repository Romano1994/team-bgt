# D-17. as-is(PMS/TEMS) 기능 이식

> **강제 규칙 (MUST)** — 이 케이스로 작업할 때 반드시 지킨다.
> - PMS/TEMS 는 **읽기 전용 참조**로만 다룬다 — 명시 요청 없이 수정하지 않고, 소스를 그대로 붙여넣지 않으며 동작 명세만 추출해 BGT 스택(FE=React `bgt-fe`, BE=Java `bgt-be`)으로 재구현한다.
> - 추출한 화면 구조를 D-16 유형 표(A-01~A-06)에 매핑한 뒤 해당 템플릿을 복사해 BGT `Form.*`/IBSheet/`@amxis/design-system` 패턴으로 옮긴다.
> - SP 를 재사용해도 XML CALL 인자는 **BGT 에 배포된 SP 시그니처** 기준으로 일치시킨다. → C-15
> - resultMap `column`/VO 필드/FE 키를 배포 SP 커서 alias 기준으로 통일한다(어긋나면 조회 전부 null·저장 드롭, 조용히 실패 — as-is 에서 가장 자주 터짐). → C-19
> - as-is 도메인 용어는 BGT 용어로 치환하고, `bgt-fe`/`bgt-be` 밖 디렉터리 수정이 필요하면 먼저 사용자에게 확인한다.

## 언제 쓰나
- 기존 시스템(PMS, TEMS)의 화면/기능을 BGT 로 가져올 때.

## 원칙 (CLAUDE.md)
- **UI/동작은 as-is 와 최대한 비슷하게 유지.**
- **소스는 BGT 대상 스택으로 재구현.** FE = `bgt-fe` (React), BE = `bgt-be` (Java).
- 사용자가 명시적으로 요청하지 않는 한 **PMS/TEMS 를 직접 수정하지 않는다.** (읽기 전용 참조)
- 가져온 코드의 도메인 용어는 BGT 용어로 치환. 임의 용어 신설 금지.

## 레시피

### 1. as-is 분석 (참조만)
- PMS/TEMS 의 해당 화면을 읽어 **UI 구성, 입력/검증 규칙, 조회·저장 동작, 호출 SP/쿼리**를 파악.
- 그대로 복사 금지 — 동작 명세만 추출.

### 2. BGT 케이스로 매핑
- 추출한 화면 구조를 [D-16](./d-16-new-screen-checklist.md) 의 유형 표에 맞춰 BGT 아키타입 선택 (A-01~A-06).

### 3. FE 재구현
- 해당 템플릿 복사 후 as-is 의 컬럼/조건/검증을 BGT `Form.*`/IBSheet 패턴으로 옮긴다.
- as-is 의 그리드 라이브러리/위젯은 BGT 의 IBSheet/`@amxis/design-system` 으로 치환.

### 4. BE 재구현
- as-is 의 SP/쿼리를 BGT BE 슬라이스로 재구성. → [C-13](../c-api/c-13-new-api.md)
- **SP 를 재사용할 경우에도 XML CALL 인자는 BGT 에 배포된 SP 시그니처 기준**으로 일치시킨다. → [C-15](../c-api/c-15-save-param-mapping.md)
- **SP 커서 alias 정합**: resultMap `column`/VO 필드/FE 키를 배포 SP 커서 alias 기준으로 한 이름으로 맞춘다(어긋나면 조회 전부 null·저장 드롭, 에러 없이 조용히 실패). as-is 는 이름만 다른 경우가 많아 가장 자주 터진다. → [C-19](../c-api/c-19-sp-cursor-alias-mapping.md)

### 5. 동등성 확인
- as-is 와 동일 입력에 동일 결과가 나오는지 비교(가능하면). UI 레이아웃/필드 라벨이 유사한지 확인.

### 6. 검증
```powershell
yarn build:local; npx tsc --noEmit
gradlew.bat test
```

## 흔한 실패와 가드
- **as-is 소스를 그대로 붙여넣음** → 스택/패턴 불일치. 동작만 가져오고 BGT 패턴으로 재작성.
- **PMS/TEMS 파일 수정** → 금지. 명시 요청 없으면 참조만.
- **as-is 용어 그대로 사용** → BGT 도메인 용어로 치환.
- **이식 중 bgt-fe/bgt-be 밖 디렉터리 수정 필요** → 먼저 사용자에게 확인. (CLAUDE.md)

## 관련 문서
- [D-16 신규 화면 체크리스트](./d-16-new-screen-checklist.md) · [C-13 신규 API](../c-api/c-13-new-api.md) · [C-15 저장 파라미터 매핑](../c-api/c-15-save-param-mapping.md) · [C-19 SP 커서 alias 정합](../c-api/c-19-sp-cursor-alias-mapping.md)
