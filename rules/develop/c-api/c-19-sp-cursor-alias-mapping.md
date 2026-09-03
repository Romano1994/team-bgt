---
paths:
  - "bgt-be/**/*.xml"
  - "bgt-be/**/model/*.java"
---

# C-19. SP 커서 alias ↔ VO ↔ FE 키 정합

> **강제 규칙 (MUST)** — 이 케이스로 작업할 때 반드시 지킨다.
> - 5계층(SP 커서 alias → resultMap column → VO 필드 → JSON 키 → FE 키)을 **한 이름(SP alias 의 camelCase)** 으로 통일. `@JsonProperty` 는 이름 변경 브리지 금지, identity 핀 전용.
> - 조회 resultMap `column` = SP 본문 SELECT 의 실제 alias 그대로(어긋나면 그 필드만 조용히 null). 커서 미반환 컬럼을 resultMap/VO 에 두는 phantom 필드 금지(화면값은 FE 도출).
> - 필드 2번째 글자가 대문자면(`mSupply`/`rShare`/`dContr` 등) `@JsonProperty("필드명동일")` 로 고정. 멀티 소문자 시작(`bizno`/`totAmt` 등)은 핀 불필요.
> - 저장 VO 필드명 = SP CALL `#{param}`(불변), `@JsonProperty` = FE 가 보내는 키. FE 키가 바뀌면 `@JsonProperty` 값만 갱신. → C-15
> - 검증 3종: 직렬화 wire test(VO→JSON), 역직렬화 test(FE키→SP파라미터), 라이브 raw fetch(resultMap 컬럼→VO 실증).

## 언제 쓰나
- SP(커서) 기반 화면에서 **조회값이 화면에 안 채워지거나(전부 null)**, **저장이 반영 안 될 때**.
- 증상은 에러가 아니라 "조용한 null"이다 → SP 커서 alias 와 BE VO / FE 키가 어긋난 경우를 의심한다.
- 특히 as-is(PMS/TEMS) 이식처럼 **배포된 SP 를 그대로 쓰는데 VO/화면 이름만 다른** 상황에서 반복된다. → [D-17](../d-workflow/d-17-as-is-migration.md)

## 핵심 원칙 — 5계층을 한 이름으로 정렬
조회/저장 한 값은 다섯 계층을 거친다. **하나의 이름으로 통일**한다(기준 = SP 커서 alias 의 camelCase).

```
[조회]  SP 커서 alias → resultMap column → VO 필드 → JSON 키 → FE 키
[저장]  FE 키 → JSON 키 → VO 필드 → XML #{param} → SP 인자
```

- **`@JsonProperty` 를 이름 변경 브리지로 쓰지 않는다.** 이름이 다르면 "조용한 불일치"의 원인이 된다. `@JsonProperty` 는 아래 게터 망글링 방지용 **identity 핀**으로만 쓴다.
- 임의 용어 신설 금지 — 기준은 배포 SP 커서 alias 다. (CLAUDE.md)

## 조회(응답) 정합
- resultMap `column` = **SP 본문 SELECT 의 실제 alias 그대로**, `property` = 그 alias 의 camelCase.
- alias 와 `column`/`property` 가 어긋나면 MyBatis 가 매핑하지 못해 **그 필드만 조용히 null**.
- **PK carve-out**: 커서마다 같은 값의 alias 가 달라도(예: 상세 general 커서는 `CNTR_YR`, 목록 커서는 `CTRT_YR`) **모듈 단일 PK명**(`ctrtYr`)으로 통일 매핑한다.
- **phantom 필드 금지**: SP 커서가 반환하지 않는 컬럼을 resultMap/VO 에 두면 항상 null 이다(예: 커서에 없는 `FILE_CAT_NM`). 화면 표시값이면 FE 에서 도출한다.

## Lombok/Jackson identity 핀 규칙
- 필드 2번째 글자가 **대문자**면(`mSupply`/`mVat`/`mDepo`/`mBond`/`rShare`/`dContr`) Lombok 게터(`getMSupply` 등) + Jackson 기본 네이밍이 어긋난 JSON 키를 만든다 → `@JsonProperty("필드명동일")` 로 **고정**한다.
- 멀티 소문자로 시작하면(`bizno`/`totAmt`/`curKnd`/`grnteSct`) 안전 — 핀 불필요.
- 이름을 **바꾸는** 게 아니라 **같게 고정**하는 것이다(브리지 ✗ / identity 핀 ✓).

## 저장(요청) 정합
- `*SaveRowVO` 의 **필드명 = SP CALL `#{param}`**(이 둘은 SP 시그니처에 묶여 불변). → [C-15](./c-15-save-param-mapping.md)
- `@JsonProperty` = **FE 가 보내는 키**.
- 따라서 **FE 키가 바뀌면 필드명/XML 은 그대로 두고 `@JsonProperty` 값만 갱신**한다.
- FE 가 안 보내는 필드의 `@JsonProperty` 는 동작에 영향 없으나, 통일 어휘(응답 VO 키)와 같게 맞춰 둔다.

## 코드 예제

### 응답 VO — identity 핀 (`cntrctctrt/model/CntrctCtrtConsorResponseVO.java`)
```java
// 소문자1+대문자 필드는 게터 망글링 방지로 동일명 핀 필수
@JsonProperty("rShare")  private Double rShare;
@JsonProperty("mSupply") private Long   mSupply;
@JsonProperty("mVat")    private Long   mVat;
// 멀티 소문자 시작은 핀 불필요(필드명 그대로 직렬화)
private String fkind;   // FKIND
private String grnteInstCd; // GRNTE_INST_CD
```

### resultMap — column = SP alias 그대로 (`resources/sql/primary/core/cm/cntrctctrt/cntrct-ctrt.xml`)
```xml
<!-- general 커서 alias 는 CNTR_YR(목록 CTRT_YR 과 다름) → 모듈 단일 PK명 ctrtYr 로 통일 매핑 -->
<result column="CNTR_YR"       property="ctrtYr"/>
<result column="GRNTE_INST_CD" property="grnteInstCd"/>
<result column="SPLY_AMT"      property="splyAmt" typeHandler="...SafeLongTypeHandler"/>
```

### 저장 VO — 필드=SP파라미터 / @JsonProperty=FE키 (`cntrctctrt/model/CntrctCtrtConsorSaveRowVO.java`)
```java
@JsonProperty("fkind")  private String curKnd; // FE 키 fkind → SP #{curKnd}
@JsonProperty("rShare") private Double shrt;   // FE 키 rShare → SP #{shrt}
@JsonProperty("dutyFreeAmt") private Long txexAmt; // FE 키 dutyFreeAmt → SP #{txexAmt}
```

## 검증 방법 (DB 의존 최소화, 3종)

### 1) 직렬화 wire test — 응답 JSON 키 고정 (DB 불필요)
`bgt-be/src/test/java/com/amxis/wsf/CntrctCtrtWireTests.java` 패턴.
```java
mockMvc = MockMvcBuilders.standaloneSetup(new CntrctCtrtController(mock(CntrctCtrtService.class))).build();
when(service.selectCntrctCtrtDetail(any())).thenReturn(/* 신 키로 채운 VO */);
mockMvc.perform(get("/api/v1/bgt/cm/cntrct-ctrt/detail"))
       .andExpect(jsonPath("$.data.consor[0].rShare").value(50.0)); // VO→JSON 키 증명
```
> 이 테스트는 VO→JSON 키만 증명한다. resultMap(컬럼→VO)은 증명하지 못한다 → 3) 라이브로 확인.

### 2) 역직렬화 test — FE키→SP파라미터 필드 (DB 불필요)
`bgt-be/src/test/java/com/amxis/wsf/CntrctCtrtSaveDeserializeTests.java` 패턴.
```java
var om = new ObjectMapper().configure(DeserializationFeature.FAIL_ON_UNKNOWN_PROPERTIES, false); // Spring 과 동일
var vo = om.readValue("{\"consorList\":[{\"fkind\":\"USD\",\"rShare\":50.0}]}", CntrctCtrtSaveRequestVO.class);
assertEquals("USD", vo.getConsorList().get(0).getCurKnd());  // @JsonProperty 매핑 증명
```

### 3) 라이브 raw fetch — resultMap(컬럼→VO)까지 실증
UI 로그인 세션의 브라우저 콘솔/Playwright in-page fetch 로 `/bgt/api`(프록시 → :7078) 직접 호출.
```js
// 목록은 검색조건 필요: bizCd/statusCd 는 '%'(LIKE 전체)
const qs = new URLSearchParams({ bizCd:'%', statusCd:'%', hjCd:'', hjNm:'', startDt:'', endDt:'', ownership:'' });
const list = await (await fetch('/bgt/api/v1/bgt/cm/cntrct-ctrt/list?'+qs, {credentials:'include'})).json();
// 상세 PK 순회하며 각 커서 첫 행의 실제 키 확인
const r = list.data[0];
const dq = new URLSearchParams({ ctrtYr:r.ctrtYr, mrktSct:r.mrktSct, seq:r.seq, yrType:r.yrType, chgSeq:r.chgSeq, ognCtrtType:r.ognCtrtType });
const d = (await (await fetch('/bgt/api/v1/bgt/cm/cntrct-ctrt/detail?'+dq, {credentials:'include'})).json()).data;
console.log(Object.keys(d.consor[0]));  // 레거시 키가 남아있는지 확인
```
> 라이브 검증은 UI 가동 + 로그인 세션 의존(취약). 쓰기(저장) 검증은 분류기에 막힐 수 있다. → [D-18](../d-workflow/d-18-agent-loop-precautions.md)

## 흔한 실패와 가드
- **resultMap column ≠ SP alias** → 그 필드만 조용히 null. SP 본문 SELECT alias 와 1:1로 맞춘다.
- **identity 핀 누락**(`mSupply` 등) → `MSupply` 같은 망글 키로 직렬화되어 FE 가 못 읽음. `@JsonProperty("동일명")`.
- **저장 시 `@JsonProperty` 미갱신** → FE 키가 바뀌었는데 옛 키로 받아 SP 파라미터가 null 로 드롭. 필드/XML 은 그대로, `@JsonProperty`만 신 FE 키로.
- **phantom 필드 매핑**(커서 미반환 컬럼) → 항상 null. 화면 표시값은 FE 에서 도출.
- **이름 변경 브리지 남용**(`@JsonProperty("ownershipNm")` 류로 서로 다른 이름 연결) → 추적 불가한 불일치. 5계층을 한 이름으로 통일.

## 관련 문서
- [C-13 신규 API](./c-13-new-api.md) · [C-14 조회 SP vs CUD SP](./c-14-select-vs-cud.md) · [C-15 저장 파라미터 매핑](./c-15-save-param-mapping.md)
- [D-17 as-is 이식](../d-workflow/d-17-as-is-migration.md) · [D-18 에이전트 루프 주의사항](../d-workflow/d-18-agent-loop-precautions.md)
- 근거 메모리: `cntrct_ctrt_sp_alias_unified`, `lombok_jackson_identity_pin`, `cntrct_ctrt_detail_resultmap_alias`, `cntrct_ctrt_save_jackson_keys`
