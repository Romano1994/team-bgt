---
name: bgt-unit-test-markup
description: >-
  Use when a screenshot needs explanatory markup drawn on it — 캡쳐에 클릭한 버튼·
  입력한 값·확인할 영역을 강조 표시할 때(빨간 박스·화살표·라벨). "캡쳐에 표시 좀 해줘",
  "어디 눌렀는지 표시해서 찍어줘", "설명용 화살표 넣어줘" 같은 요청, 그리고 단위테스트
  결과서·이슈 리포트·사용자 매뉴얼처럼 비개발자가 보는 문서에 넣을 캡쳐를 만들 때.
  Playwright MCP 로 찍는 브라우저 캡쳐 전제.
---

# 캡쳐 주석 (빨간 오버레이)

캡쳐만 보고 **무엇을 클릭했는지 / 어디에 무슨 값을 넣었는지** 알 수 있게 표시를 넣는다.

**이미지 후처리(sharp·System.Drawing·PIL 등)로 그리지 말 것.** 찍기 전에 **DOM 에 SVG 오버레이를 얹고 그대로 찍는다** — `scale:'css'` 캡쳐라 CSS px 좌표가 1:1 로 맞고, 좌표는 `getBoundingClientRect` 에서 그냥 나온다. 라이브러리도 재인코딩도 필요 없다.

**표시는 전부 빨강(`red`)** — 화면 팔레트(Primary `#037AF2`)와 안 겹쳐 가독성이 제일 좋다.

## ★ 라벨 문구 — 읽는 사람은 비개발자다

주석이 들어간 캡쳐는 **현업 담당자·감리·고객이 보는 배포 문서**에 실린다. 코드를 모르는 사람이 캡쳐만 보고 동작을 이해할 수 있어야 한다.

- **개발자만 아는 식별자를 라벨에 쓰지 말 것** — CSS 셀렉터(`#btnSearch`)·컴포넌트/파일명(`GridBox.tsx`)·필드 키(`cntrctAmt`, `flagCd='U'`)·프로시저명·API 경로·IBSheet 인덱스.
- 대신 **화면에 실제로 보이는 문구**(버튼 캡션·라벨명·입력한 값 원문)로 쓴다.
- 셀렉터는 `sel` 인자(코드)에만 쓰고 **`label` 에는 절대 넣지 않는다** — 코드에서 복붙하다 새는 게 가장 흔한 사고다.

| ✗ | ✓ |
|---|---|
| `#btnSearch click` | `① [조회] 클릭` |
| `prjNm = 'ZZ_TEST'` | `프로젝트명에 'ZZ_TEST' 입력` |
| `sheet[3] row 5 cellEdit` | `③ 계약금액 셀 수정: 1,000,000` |

- 번호를 붙일 땐 **그 문서의 항목 번호**(단위테스트면 케이스 순번)와 맞추고, 문구도 **그 항목과 같은 용어**로 쓴다. 문서와 캡쳐를 나란히 놓고 대조하게 된다.

## 헬퍼 주입 (세션당 1회, `browser_evaluate`)

```js
window.__ann = (specs) => {
  document.getElementById('__ann')?.remove();
  const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
  svg.id = '__ann';
  Object.assign(svg.style, { position:'fixed', left:'0', top:'0', width:'100%', height:'100%',
                             zIndex:2147483647, pointerEvents:'none' });
  const esc = t => String(t).replace(/[&<>]/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;'}[c]));
  const text = (x, y, t) => `<text x="${x}" y="${Math.max(y, 14)}" fill="red" font-size="14"
      font-weight="bold" paint-order="stroke" stroke="#fff" stroke-width="4">${esc(t)}</text>`;
  let h = `<defs><marker id="__ah" markerWidth="10" markerHeight="8" refX="10" refY="4"
      orient="auto"><path d="M0,0 L10,4 L0,8 z" fill="red"/></marker></defs>`;
  for (const s of specs) {
    let r = s.rect && { x:s.rect[0], y:s.rect[1], width:s.rect[2], height:s.rect[3] };
    if (s.sel) r = document.querySelector(s.sel)?.getBoundingClientRect();
    if (r) {
      h += `<rect x="${r.x-3}" y="${r.y-3}" width="${r.width+6}" height="${r.height+6}"
          fill="none" stroke="red" stroke-width="3" rx="3"/>`;
      if (s.label) h += text(r.x, r.y - 9, s.label);
    }
    if (s.arrow) {
      const [a, b, c, d] = s.arrow;
      h += `<line x1="${a}" y1="${b}" x2="${c}" y2="${d}" stroke="red" stroke-width="3"
          marker-end="url(#__ah)"/>`;
      if (s.label && !r) h += text(a, b - 6, s.label);
    }
  }
  svg.innerHTML = h;
  document.body.appendChild(svg);
};
```

## 사용 — 캡쳐 **직전** 호출, **직후** 제거

```js
window.__ann([
  { sel: '#btnSearch',  label: '① [조회] 클릭' },              // 요소 박스 + 라벨
  { sel: '#prjNm',      label: "프로젝트명에 'ZZ_TEST' 입력" },
  { rect: [820, 340, 160, 24], label: '② 계약금액 셀' },        // CSS 로 못 잡는 대상(IBSheet 셀 등)
  { arrow: [600, 300, 820, 344] },                             // [x1,y1,x2,y2] 화살표
]);
// ... screenshot ...
document.getElementById('__ann')?.remove();
```

## ⚠ 함정

- **캡쳐 후 제거 필수.** 안 지우면 다음 캡쳐에 그대로 남는다(임시 `id` 제거와 같은 자리에서 처리).
- **`pointerEvents:'none'` 을 빼지 말 것.** 오버레이가 클릭을 삼켜 이후 조작이 **무음 실패**한다.
- **사라지는 UI(toast 등)는 `browser_run_code_unsafe` 한 호출 안에서** `click → waitForFunction → __ann → screenshot → remove` 를 끝낸다. 주석 주입을 별도 호출로 쪼개면 그 MCP 왕복 동안 대상이 사라진다.
- **`fullPage:true` 로 찍지 말 것.** `position:fixed` 오버레이가 어긋난다. 뷰포트 캡쳐 유지.
- 리렌더로 지워질 수 있으니 `document.body` 에 붙이고 **캡쳐 직전에** 호출한다.
- 화면 최상단 요소는 라벨이 위로 잘린다 → `Math.max(y,14)` 로 내려 붙지만, 그래도 겹치면 `arrow` 로 아래쪽에서 가리킨다.

## 관련

- 단위테스트 결과서용 캡쳐의 프레이밍·뷰포트·toast 규칙은 `bgt-unit-test` 스킬 `references/browser-capture.md`.
