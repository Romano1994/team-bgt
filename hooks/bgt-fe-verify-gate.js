#!/usr/bin/env node
// bgt-fe-verifier 하드 발동 게이트 (Claude Code Stop 훅)
//
// 목적: 이번 턴에 대규모 bgt-fe 변경을 하고 턴을 끝내려 할 때, stop 을 차단하고
//       bgt-fe-verifier 서브에이전트를 호출하도록 지시(reason 주입)한다.
// 범위: git diff 가 아니라 transcript(JSONL)에서 "이번 턴의 진짜 사용자 메시지" 이후
//       Write/Edit/MultiEdit 이 건드린 파일만 본다 → "이번 턴 수정분"으로 한정
//       (이전 턴/세션 누적 편집·기존 미커밋 diff 무시).
// 주의: 훅은 서브에이전트를 "직접" 실행할 수 없다. stop 을 막고 메인 에이전트에게
//       "이 범위로 bgt-fe-verifier 를 돌려라"라고 지시하는 것이 하드 발동의 실체다.
// 무한루프 방지: (1) stop_hook_active 플래그, (2) 같은 파일집합이면 재발동 안 함.

const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

// "대규모"의 기준: 이 세션이 수정한 .ts/.tsx 파일 수. 조정하려면 이 값만 바꾼다.
const THRESHOLD = 6;

// bgt-fe/src 하위 .ts/.tsx 를 식별하는 경로 마커(편집 파일 자신의 절대경로에서 찾는다).
const MARKER = '/bgt-fe/src/';
const EDIT_TOOLS = new Set(['Write', 'Edit', 'MultiEdit']);

// 진짜 사용자 턴 시작 메시지인가? (tool_result·주입 meta 는 user 타입이라도 제외)
function isUserTurnStart(e) {
  if (!e || e.type !== 'user' || e.isMeta === true || e.isSidechain === true) return false;
  const c = e.message && e.message.content;
  if (typeof c === 'string') return true; // 사용자가 타이핑한 메시지
  if (Array.isArray(c)) return c.every((b) => !b || b.type !== 'tool_result'); // tool_result 없으면 사용자 발화
  return false;
}

// transcript(JSONL)에서 "이번 턴"이 편집한 bgt-fe/src 의 .ts/.tsx 파일을 수집한다.
// 턴 시작 = 마지막 진짜 사용자 메시지. 그 이후 엔트리의 편집만 센다.
// 반환값은 bgt-fe 기준 상대경로(예: 'src/pages/foo/index.tsx').
function collectTurnEdits(transcriptPath, bgtFe) {
  const entries = [];
  for (const line of fs.readFileSync(transcriptPath, 'utf8').split('\n')) {
    if (!line) continue;
    try {
      entries.push(JSON.parse(line));
    } catch {
      /* 깨진/부분 라인 스킵 */
    }
  }
  // 뒤에서부터 마지막 사용자 턴 시작을 찾는다(없으면 0 = 전체).
  let start = 0;
  for (let i = entries.length - 1; i >= 0; i--) {
    if (isUserTurnStart(entries[i])) {
      start = i + 1;
      break;
    }
  }
  const files = new Set();
  for (let i = start; i < entries.length; i++) {
    const content = entries[i] && entries[i].message && entries[i].message.content;
    if (!Array.isArray(content)) continue;
    for (const block of content) {
      if (!block || block.type !== 'tool_use' || !EDIT_TOOLS.has(block.name)) continue;
      const fp = block.input && block.input.file_path;
      if (typeof fp !== 'string') continue;
      const norm = fp.replace(/\\/g, '/');
      const j = norm.toLowerCase().indexOf(MARKER); // 대소문자 무시(Windows)
      if (j === -1) continue; // bgt-fe/src 밖 → 제외(bgt-be/com/cst 등 자동 배제)
      if (!/\.(ts|tsx)$/i.test(norm)) continue;
      const rel = 'src/' + norm.slice(j + MARKER.length);
      if (fs.existsSync(path.join(bgtFe, rel))) files.add(rel); // 턴 중 삭제된 파일 제외
    }
  }
  return [...files].sort();
}

function main() {
  // 훅 입력(JSON)은 stdin 으로 온다.
  let input = {};
  try {
    input = JSON.parse(fs.readFileSync(0, 'utf8') || '{}');
  } catch {
    /* stdin 없음 → 빈 입력으로 진행 */
  }

  // 이미 stop 훅으로 재개된 상태면 재차단하지 않는다(1차 루프 가드).
  if (input.stop_hook_active) process.exit(0);

  // 훅 cwd 는 프로젝트 루트일 수도, bgt-fe 그 자체일 수도 있다(cwd 드리프트 대응).
  const cwd = input.cwd || process.cwd();
  let projectRoot = cwd;
  let bgtFe = path.join(cwd, 'bgt-fe');
  if (!fs.existsSync(bgtFe) && path.basename(cwd).toLowerCase() === 'bgt-fe') {
    bgtFe = cwd; // cwd 가 곧 bgt-fe
    projectRoot = path.dirname(cwd); // 진짜 루트는 그 부모(.claude 상태파일용)
  }
  if (!fs.existsSync(bgtFe)) process.exit(0);

  // 세션 로그가 없으면(구버전 등) 턴 스코프를 알 수 없으므로 발동하지 않는다.
  const transcriptPath = input.transcript_path;
  if (!transcriptPath || !fs.existsSync(transcriptPath)) process.exit(0);

  let files = [];
  try {
    files = collectTurnEdits(transcriptPath, bgtFe);
  } catch {
    process.exit(0); // 파싱 실패 → 세션 방해 금지
  }

  if (files.length < THRESHOLD) process.exit(0); // 소규모 → 자동 발동 안 함

  // 같은 파일집합에 재발동하지 않도록 집합 해시로 가드(2차 루프 가드).
  // 검증 지시 후 이어지는 stop 은 stop_hook_active(1차 가드)로 막힌다.
  const hash = crypto.createHash('sha1').update(files.join(',')).digest('hex');
  const stateFile = path.join(projectRoot, '.claude', '.bgt-fe-verify-state');
  let prev = '';
  try {
    prev = fs.readFileSync(stateFile, 'utf8').trim();
  } catch {
    /* 최초 실행 */
  }
  if (prev === hash) process.exit(0); // 동일 파일집합 → 이미 검증 지시함
  try {
    fs.writeFileSync(stateFile, hash);
  } catch {
    /* 상태 기록 실패해도 진행 */
  }

  // stop 차단 + 메인 에이전트에게 검증 지시.
  const list = files.map((f) => `bgt-fe/${f}`).join(', ');
  // UI 파일(.tsx)은 UIUX 표준 검증(uiux-verifier)도 함께 받는다. .ts 로직은 bgt-fe-verifier 만.
  const uiFiles = files.filter((f) => f.endsWith('.tsx')).map((f) => `bgt-fe/${f}`);

  let reason =
    `이번 턴에 대규모 bgt-fe 변경(${files.length}개 .ts/.tsx)이 감지되었습니다. ` +
    `Agent 툴로 subagent_type "bgt-fe-verifier" 를 호출해 아래 범위를 검증하세요(읽기 전용, 보고서만 출력): ` +
    `${list}. 위 파일 목록을 검사 범위로 그대로 전달하세요.`;
  if (uiFiles.length > 0) {
    reason +=
      ` 또한 UI 파일(.tsx)이 포함됐으므로 subagent_type "uiux-verifier" 도 호출해 아래 파일을 UIUX 표준 위반 관점에서 검증하세요` +
      `(읽기 전용, 표준 위반 경고만 — 직접 수정 금지): ${uiFiles.join(', ')}. ` +
      `각 파일이 속한 화면 슬라이스를 함께 읽어 화면 단위 규칙을 판정하세요.`;
  }

  process.stdout.write(JSON.stringify({ decision: 'block', reason }));
  process.exit(0);
}

main();
