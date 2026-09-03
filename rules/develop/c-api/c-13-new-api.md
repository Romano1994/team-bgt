---
paths:
  - "bgt-be/**/*.{java,xml}"
  - "bgt-be/docs/sp/*.txt"
---

# C-13. 신규 API(프로시저) 등록·호출

> **강제 규칙 (MUST)** — 이 케이스로 작업할 때 반드시 지킨다.
> - BE 는 `Model → Repository → XML → Service → Controller` 순서로 작성한다. 복사 기반은 `sc/stdcd/desccd/` 슬라이스.
> - XML 은 `statementType="CALLABLE"`, CALL 인자 순서·개수·모드(IN/OUT/CURSOR)를 배포 SP 시그니처와 100% 일치(임의 추가/누락 시 `ORA-06550`). → C-15
> - 저장 Controller 는 `@RequestBody List<...>` 처리 전 `CommonDtoUtil.initBaseFields(requestVOs)` 로 작성자/flagCd 를 주입한다.
> - 조회 CUD Service Tx 를 구분: 조회 `@Transactional(readOnly=true)`, CUD `@Transactional(rollbackFor=Exception.class)` + 행마다 `ProcedureUtil.checkResult`.
> - FE 호출은 `BusinessCode.BGT`(템플릿의 `CST` 아님). resultMap column ≠ SP alias, `소문자1+대문자` identity 핀 누락은 조용한 null. → C-19

## 언제 쓰나
- 새 화면이 호출할 API 를 만들 때. BGT 는 두 방식이 있다.
  1. **Low-Code (공통 API 관리)**: 단일 프로시저 호출만. 화면에서 등록. (`bgt-be/docs/Server.md` 1절)
  2. **Full-Stack (직접 구현)**: 로직 복잡/다중 테이블. Controller→Service→Repository→XML→SP 직접 작성.
- 이 문서는 **2. Full-Stack 방식**의 레시피다.

## 구현 순서 (BE)
> `Model → Repository → XML → Service → Controller` 순서로 작성하면 흐름 파악이 쉽다. (Server.md 권장)

복사 기반 슬라이스: `bgt-be/.../sc/stdcd/desccd/` (조회 + CUD 전부 있는 완결 예시)

### 1. Model VO
- Request/Response VO 를 카멜케이스 필드로 정의. `@Schema` 필수 (Swagger).
- 조회는 OUT 커서를 담을 `result` 필드(List)를 Request VO 에 두는 패턴(아래 예시).

### 2. Repository (`@Mapper`)
- XML 의 SQL id 와 1:1 매핑되는 인터페이스 메소드.

### 3. XML (MyBatis)
- `namespace` = Repository 전체 경로. `statementType="CALLABLE"` 로 `{ CALL PKG_...SP_...(...) }`.
- **인자 순서/개수/모드(IN/OUT/CURSOR)는 배포된 Oracle SP 시그니처와 정확히 일치.** 틀리면 `ORA-06550`.

### 4. Service
- 조회: `@Transactional(readOnly = true)` 권장. CUD: `@Transactional(rollbackFor = Exception.class)`.
- CUD 리스트는 루프 돌며 SP 호출 + `ProcedureUtil.checkResult(...)`.

### 5. Controller
- `@RequestMapping("/api/v1/bgt/{module}/{...}")`. `@Operation` 필수.
- 조회: `@GetMapping` + `@ModelAttribute`/`@ParameterObject`. 저장: `@PostMapping` + `@RequestBody List<...>`.
- 저장 전 `CommonDtoUtil.initBaseFields(requestVOs)` 로 세션 작성자/flagCd 채움.

### 6. FE 호출
- `BusinessCode.BGT` 사용. URL 은 Controller 매핑 경로. 함수명은 Controller 메소드명과 일치.

## 코드 예제

### XML — 조회(CURSOR OUT) + 저장(CUD) — `desccd/DescCd.xml:22`
```xml
<select id="selectDescCd" statementType="CALLABLE" parameterType="...DescCdSelRequestVO" resultMap="descCdSelResponseVO">
  <![CDATA[
  { CALL PKG_BGT_SC_DESC_CD_MNG.SP_BGT_DESC_CD_SEL(
      #{classCd, mode=IN, jdbcType=VARCHAR},
      #{useYn,   mode=IN, jdbcType=VARCHAR},
      #{result,  mode=OUT, jdbcType=CURSOR, javaType=java.sql.ResultSet, resultMap=descCdSelResponseVO}
  ) }
  ]]>
</select>

<update id="saveDescCdCud" statementType="CALLABLE" parameterType="...DescCdCudRequestVO">
  <![CDATA[
  { CALL PKG_BGT_SC_DESC_CD_MNG.SP_BGT_DESC_CD_CUD(
      #{flagCd, mode=IN, jdbcType=VARCHAR},
      #{classCd, mode=IN, jdbcType=VARCHAR},
      -- ... SP 시그니처와 정확히 동일한 순서/개수 ...
      #{lastWrtrId, mode=IN, jdbcType=VARCHAR},
      #{resultCode, mode=OUT, jdbcType=VARCHAR},
      #{resultMsg,  mode=OUT, jdbcType=VARCHAR}
  ) }
  ]]>
</update>
```

### Controller — 조회/저장 — `desccd/controller/DescCdController.java:52`
```java
@Operation(summary = "내역코드 목록 조회")
@GetMapping(value = "/desc-cd-sel", produces = MediaType.APPLICATION_JSON_VALUE)
public ResponseEntity<CommonResponseVO<List<DescCdSelResponseVO>>> selectDescCd(
        @Valid @ParameterObject @ModelAttribute DescCdSelRequestVO requestVO) {
  List<DescCdSelResponseVO> response = descCdService.selectDescCd(requestVO);
  return new ResponseEntity<>(CommonResponseVO.<List<DescCdSelResponseVO>>builder()
          .successOrNot(CommonConstants.YES_FLAG).statusCode(StatusCodeConstants.SUCCESS)
          .data(response).build(), HttpStatus.OK);
}

@Operation(summary = "내역코드 저장 (추가/수정/삭제)")
@PostMapping(value = "/desc-cd-cud", produces = MediaType.APPLICATION_JSON_VALUE)
public ResponseEntity<CommonResponseVO<DescCdCudResponseVO>> saveDescCdCud(
        @Valid @RequestBody List<DescCdCudRequestVO> requestVOs) {
  CommonDtoUtil.initBaseFields(requestVOs);   // 세션 작성자/flagCd 주입
  DescCdCudResponseVO response = descCdService.saveDescCdCud(requestVOs);
  return new ResponseEntity<>(/* ... */, HttpStatus.OK);
}
```

### Service — 조회는 OUT 커서 반환 — `desccd/service/DescCdServiceImpl.java:37`
```java
public List<DescCdSelResponseVO> selectDescCd(DescCdSelRequestVO requestVO) {
  descCdRepository.selectDescCd(requestVO);  // SP가 requestVO.result 에 커서 채움
  return requestVO.getResult();
}
```

### FE 호출 — `BusinessCode.BGT`
```ts
const request: CommonRequest = {
  method: Method.GET,
  url: '/v1/bgt/sc/stdcd/desccd/desc-cd-sel',
  businessCode: BusinessCode.BGT,            // 템플릿의 CST 아님!
  queryParams: toQueryParams(params),
};
const response: CommonResponse<DescCdItem[]> = await callApi(request);
```

## 흔한 실패와 가드
- **SP 인자 임의 추가/누락/순서 변경** → `ORA-06550`. XML CALL 인자는 SP 시그니처와 100% 일치. → [C-15](./c-15-save-param-mapping.md)
- **`BusinessCode.CST`** 그대로 둠 → BGT 라우팅 안 됨. `BusinessCode.BGT`.
- **`@Operation`/`@Schema` 누락** → Swagger 노출/테스트 불가.
- **저장 시 `initBaseFields` 누락** → 작성자/flagCd 미설정으로 저장 실패.
- **resultMap column ≠ SP 커서 alias / VO 필드 ≠ alias camelCase** → 그 필드만 조용히 null. → [C-19](./c-19-sp-cursor-alias-mapping.md)
- **`소문자1+대문자` 필드(`mSupply`/`rShare` 등) identity 핀 누락** → 게터 망글링으로 JSON 키 어긋남. `@JsonProperty("동일명")`. → [C-19](./c-19-sp-cursor-alias-mapping.md)

## 검증 방법
```powershell
# bgt-be 에서
gradlew.bat test     # compileJava 시 spotlessApply 자동 실행
```
> DB 없이 통신만 검증하려면 standalone MockMvc 패턴 사용(메모리: bgt-be 테스트 실행 환경).
> 응답 JSON 키 정합(VO→JSON)은 직렬화 wire test 로 고정한다(`jsonPath` 어설션). → [C-19](./c-19-sp-cursor-alias-mapping.md)

## 관련 문서
- [C-14 조회 SP vs CUD SP](./c-14-select-vs-cud.md) · [C-15 저장 파라미터 매핑](./c-15-save-param-mapping.md) · [C-19 SP 커서 alias 정합](./c-19-sp-cursor-alias-mapping.md)
- 기본 가이드: `bgt-be/docs/Server.md`, `bgt-fe/src/docs/ko/development/API.md`
