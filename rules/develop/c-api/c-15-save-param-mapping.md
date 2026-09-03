---
paths:
  - "bgt-be/**/*.{java,xml}"
  - "bgt-be/docs/sp/*.txt"
---

# C-15. 저장 파라미터 매핑

> **강제 규칙 (MUST)** — 이 케이스로 작업할 때 반드시 지킨다.
> - XML CALL 인자 = 배포 SP 시그니처와 순서·개수·모드·타입 정확히 일치(임의 인자 추가 금지, 어긋나면 `ORA-06550`). SP 를 못 바꾸면 FE/VO 를 SP 에 맞춘다.
> - 이름이 아니라 코드로 저장한다(예: 발주기관 이름→코드). SP 에 없는 컬럼(예: 공고시간)은 저장 대상에서 제외.
> - 작성자/flagCd 는 `CommonDtoUtil.initBaseFields(requestVOs)` 가 채우고, resultCode/resultMsg OUT 은 `ProcedureUtil.checkResult` 로 검사한다.
> - FE 키가 바뀌면 저장 VO 의 필드명/XML `#{...}`(=SP 인자)은 그대로 두고 `@JsonProperty` 값만 신 FE 키로 갱신. `소문자1+대문자` 필드는 `@JsonProperty("동일명")` identity 핀. → C-19

## 언제 쓰나
- 저장 시 FE 가 보낸 값과 SP 인자를 매핑할 때. 특히 **이름→코드 변환**, **저장 불가 컬럼**, **SP 시그니처 일치**가 얽힐 때.

## 핵심 원칙
1. **XML CALL 인자 = 배포된 Oracle SP 시그니처와 정확히 일치.** 순서·개수·모드(IN/OUT)·타입 어긋나면 `ORA-06550`. 임의로 인자 추가 금지.
2. **이름이 아니라 코드로 저장.** 화면에 이름이 보여도 SP 에는 코드값을 보낸다. 매핑은 FE 또는 Service 에서 명시적으로.
3. **저장 불가/시스템 컬럼**은 보내지 않는다. (예: 일부 시간 컬럼이 SP 에 없으면 저장 대상에서 제외)
4. **작성자/flagCd 는 공통 유틸이 채운다** — `CommonDtoUtil.initBaseFields(requestVOs)`.

## 매핑 체크리스트 (저장 구현 시)
- [ ] FE 키 ↔ `@JsonProperty` ↔ VO 필드 ↔ XML `#{...}`(=SP 인자) 4계층이 일치하는가 → [C-19](./c-19-sp-cursor-alias-mapping.md)
- [ ] FE 그리드/폼 필드명 ↔ Request VO 필드명(카멜케이스) 일치하는가
- [ ] Request VO 필드 ↔ XML `#{...}` ↔ SP 인자 순서가 1:1 인가
- [ ] 이름 컬럼은 코드로 변환해서 보내는가 (예: 발주기관 이름 → 발주기관 코드)
- [ ] SP 에 없는 컬럼(예: 공고시간 등)을 빼고 보내는가
- [ ] flagCd / frstWrtrId / lastWrtrId 를 `initBaseFields` 가 채우는가
- [ ] resultCode/resultMsg OUT 을 `ProcedureUtil.checkResult` 로 검사하는가

## 코드 예제

### Controller — 저장 전 공통 필드 주입 — `desccd/controller/DescCdController.java:71`
```java
@PostMapping(value = "/desc-cd-cud", ...)
public ResponseEntity<...> saveDescCdCud(@Valid @RequestBody List<DescCdCudRequestVO> requestVOs) {
  CommonDtoUtil.initBaseFields(requestVOs);   // frstWrtrId/lastWrtrId/flagCd 주입
  ...
}
```

### 공통 유틸 — 세션 작성자 + state→flagCd — `bgt/util/CommonDtoUtil.java:60`
```java
public static <T extends BaseSavedDto> void initBaseFields(T dto, UserSessionVO session) {
  if (session != null) {
    dto.setFrstWrtrId(session.getUserId());
    dto.setLastWrtrId(session.getUserId());
  }
  // flagCd 가 비어 있으면 state(C/U/D) 값을 사용
  if (StringUtils.hasText(dto.getState()) && !StringUtils.hasText(dto.getFlagCd())) {
    dto.setFlagCd(dto.getState());
  }
}
```

### XML — SP 시그니처와 동일 순서 — `desccd/DescCd.xml:33`
```xml
{ CALL PKG_BGT_SC_DESC_CD_MNG.SP_BGT_DESC_CD_CUD(
    #{flagCd, mode=IN, jdbcType=VARCHAR},
    #{classCd, mode=IN, jdbcType=VARCHAR},
    -- ↓ 인자 하나라도 빠지거나 순서 바뀌면 ORA-06550
    #{frstWrtrId, mode=IN, jdbcType=VARCHAR},
    #{lastWrtrId, mode=IN, jdbcType=VARCHAR},
    #{resultCode, mode=OUT, jdbcType=VARCHAR},
    #{resultMsg,  mode=OUT, jdbcType=VARCHAR}
) }
```

## 흔한 실패와 가드 (실측 사례)
- **이름을 그대로 저장** → 코드 컬럼에 이름이 들어가 깨짐. 발주기관 등은 **이름→코드 매핑** 후 저장. (메모리: cntrct-bid 저장 파라미터)
- **공고시간 등 SP 에 없는 값 전송** → 무시되거나 오류. SP 에 인자가 없으면 저장 대상에서 제외(저장 불가 컬럼).
- **SP 에 맞춘다고 XML 에 인자 임의 추가** → `ORA-06550`. SP 를 바꿀 권한이 없으면 SP 시그니처에 FE/VO 를 맞춘다.
- **카멜케이스 불일치** → MyBatis 가 값을 못 채움(null). VO 필드명과 `#{...}` 일치.
- **FE 키가 바뀌었는데 `@JsonProperty` 미갱신** → 옛 키로 받아 SP 파라미터가 null 로 드롭. 저장 VO 의 **필드명/XML 은 그대로 두고 `@JsonProperty` 값만 신 FE 키로** 갱신. → [C-19](./c-19-sp-cursor-alias-mapping.md)
- **`소문자1+대문자` 필드(`mSupply`/`rShare`) 게터 망글링** → JSON 키 어긋남. `@JsonProperty("동일명")` identity 핀. → [C-19](./c-19-sp-cursor-alias-mapping.md)

## 검증 방법
```powershell
gradlew.bat test
```
> SP 호출 자체는 DB 연결이 필요. 시그니처 일치 여부는 배포된 SP 정의와 대조해 확인.

## 관련 문서
- [C-13 신규 API](./c-13-new-api.md) · [C-14 조회 SP vs CUD SP](./c-14-select-vs-cud.md) · [C-19 SP 커서 alias 정합](./c-19-sp-cursor-alias-mapping.md) · [B-07 그리드 저장](../b-feature/b-07-grid-cud-save.md)
