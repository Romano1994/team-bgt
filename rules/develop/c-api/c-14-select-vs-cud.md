---
paths:
  - "bgt-be/**/*.{java,xml}"
---

# C-14. 조회 SP vs CUD SP

> **강제 규칙 (MUST)** — 이 케이스로 작업할 때 반드시 지킨다.
> - 조회 OUT 은 `CURSOR`(ResultSet)→resultMap, `<select>` + `@GetMapping`/`@ModelAttribute`, Service 는 `requestVO.getResult()` 반환. 단건 값만 OUT 이면 VARCHAR.
> - CUD OUT 은 `resultCode`/`resultMsg`(VARCHAR), `<update>` + `@PostMapping`/`@RequestBody List<>`, 입력은 행마다 flagCd.
> - CUD Service 는 `@Transactional(rollbackFor=Exception.class)`(readOnly 금지)로 리스트 루프 + 행마다 `ProcedureUtil.checkResult(...)` 로 실패 시 예외→전체 롤백.
> - flagCd 는 Controller 에서 `CommonDtoUtil.initBaseFields(requestVOs)` 로 주입한다. → B-07

## 언제 쓰나
- BE 에서 SP 를 호출하는데, "조회용"과 "저장(CUD)용" 작성 패턴이 헷갈릴 때.

## 한눈 비교
| 구분 | 조회 (SELECT) | 저장 (CUD) |
| --- | --- | --- |
| XML 태그 | `<select>` | `<update>` (관례) |
| OUT | `CURSOR` (ResultSet) → resultMap | `resultCode` / `resultMsg` (VARCHAR) |
| 입력 | 검색 조건 단건 | `List<RequestVO>` (행마다 flagCd) |
| Service Tx | `@Transactional(readOnly = true)` | `@Transactional(rollbackFor = Exception.class)` |
| 반환 | `requestVO.getResult()` (커서 매핑 목록) | 결과 코드/메시지 |
| Controller | `@GetMapping` + `@ModelAttribute` | `@PostMapping` + `@RequestBody List<>` |
| 결과 검사 | 불필요 | 행마다 `ProcedureUtil.checkResult(...)` |

## 조회 SP 패턴

### XML — `desccd/DescCd.xml:22`
```xml
{ CALL PKG_BGT_SC_DESC_CD_MNG.SP_BGT_DESC_CD_SEL(
    #{classCd, mode=IN, jdbcType=VARCHAR},
    #{useYn,   mode=IN, jdbcType=VARCHAR},
    #{result,  mode=OUT, jdbcType=CURSOR, javaType=java.sql.ResultSet, resultMap=descCdSelResponseVO}
) }
```
- OUT 커서를 담는 `result` 필드는 **Request VO** 에 둔다. Service 는 `requestVO.getResult()` 반환.

## CUD SP 패턴

### Service — 리스트 루프 + 결과 검사 — `desccd/service/DescCdServiceImpl.java:47`
```java
@Transactional(rollbackFor = Exception.class)
public DescCdCudResponseVO saveDescCdCud(List<DescCdCudRequestVO> requestVOs) {
  for (DescCdCudRequestVO requestVO : requestVOs) {
    descCdRepository.saveDescCdCud(requestVO);                       // flagCd(C/U/D)로 SP 분기
    ProcedureUtil.checkResult(requestVO.getResultCode(), requestVO.getResultMsg()); // 실패 시 예외→롤백
  }
  // 마지막 결과를 응답으로
  DescCdCudResponseVO responseVO = new DescCdCudResponseVO();
  if (!requestVOs.isEmpty()) {
    DescCdCudRequestVO last = requestVOs.get(requestVOs.size() - 1);
    responseVO.setResultCode(last.getResultCode());
    responseVO.setResultMsg(last.getResultMsg());
  }
  return responseVO;
}
```

## 흔한 실패와 가드
- **CUD 를 readOnly 트랜잭션으로** → 저장 안 됨/예외. CUD 는 `@Transactional(rollbackFor = Exception.class)`.
- **루프 중간 실패를 무시** → 일부만 저장. 행마다 `ProcedureUtil.checkResult` 로 실패 시 예외 발생시켜 전체 롤백.
- **조회 OUT 을 VARCHAR 로** → 목록은 `CURSOR`. 단건 값만 OUT 이면 VARCHAR.
- **flagCd 미주입** → Controller 에서 `CommonDtoUtil.initBaseFields(requestVOs)` 로 채움. → [B-07](../b-feature/b-07-grid-cud-save.md)

## 검증 방법
```powershell
gradlew.bat test
```

## 관련 문서
- [C-13 신규 API](./c-13-new-api.md) · [C-15 저장 파라미터 매핑](./c-15-save-param-mapping.md) · [B-07 그리드 저장](../b-feature/b-07-grid-cud-save.md)
