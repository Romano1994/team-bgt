---
paths:
  - "bgt-fe/src/**/*Atch*.{ts,tsx}"
  - "bgt-fe/src/**/*Uploader*.{ts,tsx}"
  - "bgt-fe/src/**/*FileAttach*.{ts,tsx}"
  - "bgt-fe/src/**/FileGridBox.tsx"
---

# B-10. 파일 첨부

> **강제 규칙 (MUST)** — 이 케이스로 작업할 때 반드시 지킨다.
> - 편집 가능 여부로 컴포넌트를 분기한다: 편집 가능 → `FileUploaderDext5`, 읽기 전용 → `AuthUploader(isEditable={false})`.
> - 업로더에 `key={uploadRefresh}` 를 주어 저장/조회 후 강제 리마운트한다(누락 시 내부 상태 미갱신).
> - 신규 레코드는 `atchFileGrId || ''`(빈 값)로 시작하고 grId 는 저장 시 연결한다(강제 요구 금지).
> - `atchPolicyId` 는 `isPrd` 로 운영/개발 값을 분기한다(혼용 금지).
> - 저장 직전 `onSaveTargetDetected` 로 업로드 대상을 감지해 본문 저장과 함께 커밋한다.

## 언제 쓰나
- 레코드에 파일을 업로드/조회/삭제하고, 저장 시 첨부 그룹과 본문을 함께 커밋해야 할 때.
- BGT 는 DEXT5 업로더(`FileUploaderDext5`) 기반.

## 참고 원본 (복사 기반)
```
cst/cst-fe/src/pages/insp/insp-rqdm/__components/DocumentForm.tsx   # cst 안정 — FileUploaderDext5 편집/읽기 분기
```
- 컴포넌트: `FileUploaderDext5`, `AuthUploader` (`@amxis/pms-com` / `@/components/common`)
- 훅: `useDext5Uploader` → `uploaderProps` (`ref`, `maxFileCount`, `onSaveTargetDetected`)
- BE: `bgt-be/.../wsf/file/controller/FileController.java`, `FileService`

## 핵심 개념
- **`atchFileGrId`** (첨부 그룹 ID): 레코드가 보관하는 파일 묶음 키. 신규면 비어 있고, 저장 시 업로더가 발급/연결한 grId 를 레코드에 저장.
- **편집 가능 여부**로 컴포넌트 분기: 편집 가능 → `FileUploaderDext5`, 읽기 전용 → `AuthUploader(isEditable={false})`.
- **`atchPolicyId`** 는 운영/개발 환경별로 다른 값 (`isPrd` 분기).
- 저장 직전 `onSaveTargetDetected` 로 업로드 대상 파일을 감지해 본문 저장과 함께 처리.

## 레시피

### 1. 업로더 훅 준비
- 상위에서 `useDext5Uploader(...)` 로 `uploaderProps` 를 만들고 `AtchFile` 에 전달.

### 2. 화면에 AtchFile 배치
- `prjCd`, `atchFileGrId`, `uploadRefresh`(key), `isFileEditable`, `uploaderProps` 전달.

### 3. 저장 연동
- 본문 저장과 첨부 grId 를 함께 커밋. 업로더의 `onSaveTargetDetected` 로 감지된 파일을 저장 시점에 반영.

### 4. 검증
- `yarn build:local` + `npx tsc --noEmit`.

## 코드 예제

### 파일 첨부 (편집/읽기 분기) — cst `insp/insp-rqdm/__components/DocumentForm.tsx:181`
```tsx
{isEditable ? (                              // cst 는 편집상태(editingPages)로 파생 — BGT 는 isFileEditable
  <FileUploaderDext5
    ref={documentAttachRef}
    initialGrId={field.value}                // 첨부 그룹 ID (atchFileGrId)
    atchPolicyId={'c50d757a-...'}            // 정책 ID
    tags={tags}
    onSaveTargetDetected={(hasChanges) => onChangeForm('atchFileChange', hasChanges)}
  />
) : (
  <MyFileGrid                                // cst 읽기 전용 뷰어 (BGT 는 AuthUploader isEditable={false})
    ref={documentAttachRefViewRef}
    atchFileGrId={field.value ?? ''}
  />
)}
```

### 환경별 정책 ID (BGT 표준 — cst insp-rqdm 은 단일 ID 하드코딩)
```tsx
// 운영/개발 정책 ID 가 다르면 isPrd 로 분기한다 (BGT 규칙).
const atchPolicyId = isPrd
  ? 'c2969c59-dd56-4a42-96cc-6e3900992635'   // 운영
  : '09b557be-4717-4314-abf5-12cde8a6c62e';  // 개발
```

## 흔한 실패와 가드
- **`key={uploadRefresh}` 누락** → 저장/조회 후 업로더 내부 상태가 갱신 안 됨. refresh key 로 강제 리마운트.
- **신규 레코드인데 grId 강제 요구** → 신규는 `atchFileGrId || ''` (빈 값) 로 시작, 저장 시 연결.
- **운영/개발 정책 ID 혼용** → `isPrd` 분기 유지.
- **엑셀 업로드(암호화 해제) 케이스는 별도** → AIP 가이드 참조: `bgt-fe/src/docs/ko/features/AIP.md`.

## 검증 방법
```powershell
yarn build:local; npx tsc --noEmit
```

## 관련 문서
- [A-03 팝업](../a-archetype/a-03-dialog-popup.md) · [B-11 권한](./b-11-auth.md)
- 기능 상세: `bgt-fe/src/docs/ko/features/AIP.md`
