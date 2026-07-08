---
name: migrate-feature-asis-to-bgt
description: ASIS 화면(PMS/TEMS)의 특정 기능을 이미 존재하는 BGT TOBE 화면에 기능 단위로 이식. migrate-asis-to-bgt의 후속으로 사용하며, 화면은 이미 BGT에 존재하지만 특정 기능이 누락되거나 추가 수정이 필요한 경우에 사용한다. `bgt-be/docs/sp`의 배포 SP 문서를 확인해 SP와 컬럼명을 맞춰 실제 SP 호출로 구현한다. Use when user wants to port a specific feature (not a whole screen) from ASIS to an existing BGT screen, or mentions "기능 이식", "기능 마이그레이션", "기능 옮겨", "화면에 기능 추가".
---

# ASIS → BGT 기능 단위 마이그레이션

## 파라미터

| 파라미터 | 필수 | 설명 | 예시 |
|---------|------|------|------|
| `asis_path` | ✅ | ASIS 소스 경로 (PMS 또는 TEMS) | `PMS/03_MPOST/.../MPOST.OE.MPMO10100` |
| `feature_name` | ✅ | 이식할 기능명 (상세하게) | `발주형식 CheckSelectInput 다중 선택 팝업` |
| `bgt_url` | ✅ | BGT TOBE 화면 URL | `/cm/cntrct-bid/cntrct-bid-lst` |
| `bgt_feature_name` | ❌ | BGT에서 사용할 기능명 (미지정 시 `feature_name` 사용) | `발주형식 다중 선택` |

파라미터가 하나라도 없으면 작업 시작 전에 반드시 요청한다.

## 워크플로우

### Phase 1: ASIS 기능 분석

`asis_path`에서 `feature_name`에 해당하는 코드를 집중 탐색한다.

분석 항목:
- **UI 구조**: 해당 기능의 컴포넌트/컨트롤 구성, 팝업/모달 유무
- **데이터 흐름**: 표시되는 값의 종류, 코드-명칭 매핑, 그룹/카테고리 구분
- **상호작용**: 사용자 입력→화면 반응 흐름, 이벤트 핸들러 로직
- **연동 파라미터**: 어떤 값이 검색/저장 요청에 포함되는지

분석 후 **불명확한 부분**을 한 번에 묶어 사용자에게 질문한다. 답변을 받은 후에만 다음 단계로 진행한다.

### Phase 2: BGT 대상 화면 탐색

`bgt_url`로 BGT 파일 위치를 찾는다.

- URL 세그먼트 → `bgt-fe/src/pages/{경로}` 디렉터리로 매핑
- 해당 화면의 기존 구현을 파악한다 (컴포넌트 구조, 폼 타입, 상태 관리 방식)
- `feature_name` / `bgt_feature_name`에 해당하는 현재 구현 상태를 확인한다 (없음 / readOnly 텍스트 / 부분 구현 등)

### Phase 3: SP·컬럼명 확인 + API 계약 정의

`bgt-be/docs/sp`의 배포된 SP 패키지 문서에서 이 기능에 해당하는 SP를 찾아 데이터 연동을 설계한다.

**SP 매칭 원칙:**
- `bgt-be/docs/sp` 문서에서 기능에 매칭되는 SP를 찾아 CALL 시그니처(파라미터 순서·IN/OUT·커서)를 확정한다. 임의로 인자를 추가하지 않는다(불일치 시 ORA-06550).
- 화면에 표시·전송할 필드/코드 구조를 SP 문서의 커서 컬럼 alias(예: `CODE`/`NAME`/`NAME_ENG`)와 정확히 맞춘다 — alias가 다르면 매핑이 null이 된다.
- API URL은 BGT 규칙(`/v1/{L1}/{L2}/{L3}`)으로 정의한다.
- `bgt-be/docs/sp`에 매칭되는 SP 문서가 없으면 임의 구현하지 말고 사용자에게 확인한다.

### Phase 4: BGT 구현 (BE + FE)

BE는 `bgt-be/docs/Server.md`를, FE는 `bgt-fe/src/docs/ko/Intro.md` 및 하위 문서를 먼저 읽는다.

**BE** (Phase 3에서 매칭한 SP가 있을 때):
- Model → Repository → Service → Controller 순서로 구현한다.
- XML `CALL` 구문은 `bgt-be/docs/sp`의 배포 SP 시그니처와 정확히 일치시키고, resultMap/Model 컬럼명은 SP 커서 컬럼 alias와 맞춘다.

**FE:**
- 기존 BGT 화면 패턴을 최우선으로 따른다 (`bgt-fe` → `cst` 순으로 참고)
- 같은 화면에 유사 기능이 있으면 그것을 템플릿으로 사용한다 (예: 사업구분 → 발주형식)
- `callApi` 유틸로 Phase 3에서 정의한 API를 호출한다. 응답 필드명은 SP 컬럼 alias와 일치시킨다
- ASIS에서 해당 기능이 검색 파라미터에 포함된다면: 폼 값 타입·스키마·핸들러·onSearch 전달 구조도 함께 수정한다
- ASIS 기능 추가/삭제 금지. 기존 BGT 기능에 영향을 주지 않는다

수정 범위:
- (BE) Model/Repository/Service/Controller, MyBatis XML — 매칭 SP 연동
- `_components/SearchBox.tsx` (또는 해당 컴포넌트) — UI 교체 및 상태 추가
- `_utils/__type.ts` — 필요 시 타입 추가

### Phase 5: 구현 검증 (docs/develop 표준 + 기능 동등성)

구현 완료 후, 이 검증만 전담하는 **별도의 검증 에이전트**를 새로 생성해 아래 두 축을 검증한다. (구현 작업과 분리된 독립 에이전트가 점검하며, 표준 위반·결함을 발견하면 직접 수정한다.)

**1) 개발 표준 검증 (docs/develop 기준)**

`docs/develop/README.md` 목차를 진입점으로, 이식한 기능에 해당하는 케이스 문서를 매칭하고 그 문서의 표준(참고 원본·레시피·"흔한 실패와 가드"·검증 방법)을 기준으로 점검한다.
- 기능 유형에 맞는 `b-feature`(그리드 CUD 저장, 코드 드롭다운, 비동기 로드, 파일 첨부, 권한, 모달) 또는 화면 구조에 맞는 `a-archetype` 문서를 매칭한다.
- 해당 케이스의 "흔한 실패와 가드"를 체크리스트로 삼아 위반 여부를 점검한다. (예: 코드 드롭다운 null label 가드, 조회 조건 변경 시 그리드 초기화, 그리드 저장 시 삭제행 포함 수집 등)

**2) 기능 동등성 / 사이드 이펙트 검증**
- [ ] SP CALL 시그니처·응답 컬럼 매핑이 `bgt-be/docs/sp` 문서와 일치하는가 (커서 alias ↔ 필드)
- [ ] 조회 결과가 ASIS 화면과 동일한 항목·명칭·구조로 표시되는가
- [ ] 팝업/선택 UI가 ASIS와 동일하게 동작하는가 (단일/다중 선택, 전체 선택 등)
- [ ] 선택값이 폼 상태에 올바르게 반영되는가
- [ ] 기존 다른 기능에 사이드 이펙트가 없는가
- [ ] 빌드/타입 검증을 통과하는가 (`yarn build:local` + `npx tsc --noEmit`, 필요 시 `gradlew.bat test`)

**검증 실패 시 재구현 루프**

위 두 축(개발 표준·기능 동등성)의 검증 중 **하나라도 통과하지 못하면 Phase 4(BGT FE 구현)로 돌아가 원인을 수정·재구현한 뒤 Phase 5를 다시 수행**한다. **모든 항목을 통과할 때까지 이 `구현 → 검증` 루프를 계속 반복**한다.

모든 검증을 통과한 뒤에 변경 내용을 사용자에게 요약 보고한다.
