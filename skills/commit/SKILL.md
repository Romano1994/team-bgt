---
name: commit
description: BGT 프로젝트의 변경사항을 fe/be git 레포지토리에 커밋한다. 변경 diff를 분석해 컨벤션에 맞는 한국어 커밋 메세지를 작성하고, 민감 파일(.env/.properties 등)은 차단하며 문서(.md/.txt)는 포함 여부를 사용자에게 확인한다. 순서는 ponytail-review(필수) → develop pull → 커밋으로, 커밋 전 반드시 ponytail-review로 과잉 설계를 점검하고 develop 최신을 병합한다. Use when user says "커밋", "커밋해줘", "commit", "fe 커밋", "be 커밋", or asks to commit BGT fe/be changes.
---

# BGT Commit

## 대상 레포 (기본: 둘 다)

- 사용자가 레포를 명시하지 않으면 **fe(bgt-fe)와 be(bgt-be) 둘 다** 커밋한다.
- "fe"/"프론트"만 → bgt-fe만, "be"/"백엔드"만 → bgt-be만.
- 변경사항이 없는 레포는 건너뛴다(빈 커밋을 만들지 않음).
- 레포 경로: `C:\workspace\bgt\bgt-fe`, `C:\workspace\bgt\bgt-be`.

## Workflow

각 대상 레포에서 순서대로 처리한다:

1. `git -C <repo> status -s` 로 변경을 확인한다. 변경이 없으면 그 레포는 건너뛴다.
2. 관련 파일만 스테이징한다(`git -C <repo> add <paths>`). 무관한 파일을 임의로 섞지 않는다.
3. **가드 (스테이징 파일 직접 검사 · 번들 스크립트 호출 없음)** — `git -C <repo> diff --cached --name-only --diff-filter=ACM` 로 스테이징 목록을 얻어 아래 패턴으로 분류한다(정규식, 대소문자 무시):
   - **민감 파일**: `(^|/)\.env($|\.)` · `\.(properties|pem|key|p12|pfx|jks|keystore|pkcs12)$` · `(^|/)id_rsa$` — 하나라도 매칭되면 **즉시 중단**한다. 사용자에게 목록을 보고하고 `git restore --staged <file>` / `.gitignore` 추가를 안내한다. 절대 커밋하지 않는다.
   - **문서 파일**: `\.(md|txt)$` — 매칭이 있으면 **사용자에게 확인**한다(AskUserQuestion): "이 .md/.txt 파일을 커밋에 포함할까요?" 제외를 택하면 `git restore --staged <file>` 후 진행한다.
4. **Ponytail 리뷰 (필수 · 생략 불가)** — Skill 툴로 `ponytail:ponytail-review` 를 실행해 스테이징 diff(`git -C <repo> diff --cached`)의 과잉 설계를 점검한다. 이 단계는 반드시 실행하며 건너뛸 수 없다.
   - 결과가 `Lean already. Ship.` 이면 그대로 진행한다.
   - 지적 사항이 있으면 사용자에게 보고하고 AskUserQuestion으로 확인한다: "그대로 커밋 / 수정 후 커밋". 수정을 택하면 커밋을 중단한다.
5. **develop pull** — `git -C <repo> pull origin develop` 으로 현재 브랜치에 develop 최신 변경을 병합한다(커밋 전, 스테이징 미커밋 상태에서 수행).
   - 충돌이 나면 변경사항이 남지 않도록 소스를 적절히 수정해 충돌을 해소한 뒤 진행한다.
   - 단, 충돌·수정 규모가 크면 작업을 중단하고 사용자에게 보고한다.
6. diff(`git -C <repo> diff --cached`)를 읽고 아래 컨벤션으로 메세지를 작성한다. fe/be는 변경 내용이 다르므로 **레포별로 다른 메세지**를 쓴다.
7. `git -C <repo> commit -m "..."` 으로 커밋한다. **푸시는 하지 않는다**(사용자가 명시적으로 요청할 때만 push).
8. 각 레포의 커밋 해시와 메세지를 보고한다.

> 현재 브랜치가 `develop`/`main`/`master` 면 커밋 전에 사용자에게 확인한다.
> PowerShell에서 멀티라인 메세지는 `-m` 을 여러 번 쓴다(여기 문서식 `@'...'@` 은 Bash 툴에서 깨진다).

## 커밋 메세지 컨벤션

fe/be 기존 커밋 내역 분석 기준(2026-06).

**형식**: `<type>: <한국어 제목>` — scope 괄호 없는 평문 prefix (`feat(x):` 형태 아님).

**type**:
- `feat:` 신규 화면·기능·모듈 추가 (예: `feat: 도급입찰 목록(메인) 화면 추가`)
- `fix:` 버그/매핑/컴파일 오류 수정 (예: `fix: 도급입찰 상세 PQ 마감시간 컬럼 매핑 수정`)
- refactor/docs/style 등 별도 type은 쓰지 않음 → 성격에 맞게 feat(추가형)/fix(수정형)로 매핑한다.

**제목**:
- 도메인 우선. 자주 쓰는 형태:
  - 메뉴 경로: `견적관리>견적요청관리>견적프로젝트 목록 ...`
  - 도메인+모듈명: `도급계약(cntrct-ctrt) 조회/저장 백엔드 구현`, `도급입찰(CntrctBid) 백엔드 모듈 추가`
  - 평이한 도메인 구절: `도급입찰 상세 ... 보완`
- 끝에 마침표 없음. 한국어 명사형 종결(추가/수정/구현/보완/작성 등).

**본문**(선택): 변경점이 여러 개면 `-` 불릿으로 요약한다.

## 가드 (민감/문서 파일)

- **민감 파일 차단**: `.env`/`.env.*`, `*.properties`, `*.pem/.key/.p12/.pfx/.jks/.keystore`, `id_rsa` 는 커밋 불가.
  - 런타임: 위 Workflow 3단계에서 스킬이 스테이징 목록을 직접 검사(plain git)해 민감 파일이 있으면 중단한다.
  - 추가 안전망: fe/be 각 `.git/hooks/pre-commit` 에 설치된 훅이 어떤 커밋이든(수동 포함) 하드 차단한다.
- **문서 확인**: `*.md`/`*.txt` 가 스테이징되면 포함 여부를 사용자에게 확인한다(훅은 비대화형이라 차단하지 않음 — 확인은 스킬이 담당).

## Setup (최초 1회 · 하드 차단 안전망)

fe/be 각 `.git/hooks/pre-commit` 에 훅을 설치하면 **수동 커밋 포함** 어떤 커밋이든 민감 파일을 하드 차단한다. 플러그인 설치 위치는 환경마다 달라 스크립트 경로에 의존하지 않는다 — 번들된 `hooks/pre-commit`(아래와 동일)의 내용을 그대로 fe/be 두 레포에 설치한다.

```sh
#!/bin/sh
# BGT commit guard — 스테이징된 민감 파일을 차단한다.
staged=$(git diff --cached --name-only --diff-filter=ACM)
[ -z "$staged" ] && exit 0
hits=$(printf '%s\n' "$staged" | grep -Ei '(^|/)\.env($|\.)|\.(properties|pem|key|p12|pfx|jks|keystore|pkcs12)$|(^|/)id_rsa$')
if [ -n "$hits" ]; then
  echo "✗ 커밋 차단: 민감 파일이 스테이징되었습니다." >&2
  printf '%s\n' "$hits" | sed 's/^/   - /' >&2
  exit 1
fi
exit 0
```

설치: 위 내용을 `bgt-fe\.git\hooks\pre-commit` 와 `bgt-be\.git\hooks\pre-commit` 로 쓰고 실행권한을 준다(`chmod +x`). 기존 pre-commit 이 있으면 `.bak` 백업 후 교체한다.
차단 패턴을 바꿀 때는 이 훅과 Workflow 3단계 인라인 패턴을 함께 맞춘다.
