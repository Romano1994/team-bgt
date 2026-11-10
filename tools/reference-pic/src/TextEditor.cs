using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ReferencePic
{
    // 텍스트 도구 편집기: 열려 있는 동안만 parent 위에 RichTextBox를 띄우고, 확정하면 Document에 넣는다.
    public class TextEditor
    {
        private readonly Control parent;
        private readonly float scale;
        private readonly Document doc;

        private RichTextBox box;
        private PointF origin;
        private Color color;

        public DrawObj Editing { get; private set; }
        public bool IsOpen { get { return box != null; } }

        public event EventHandler Closed;

        public TextEditor(Control parent, float scale, Document doc)
        {
            this.parent = parent;
            this.scale = scale;
            this.doc = doc;
        }

        // existing이 있으면 그 텍스트를 숨기고 런별 색 그대로 채워 다시 편집한다.
        public void Open(PointF origin, Color color, DrawObj existing)
        {
            this.origin = origin;
            this.color = color;
            Editing = existing;

            Font font = Renderer.TextFont(scale);
            box = new ClearRichTextBox();
            box.Multiline = true;
            box.WordWrap = false;
            box.ScrollBars = RichTextBoxScrollBars.None;
            box.HideSelection = false;
            box.DetectUrls = false;
            box.BorderStyle = BorderStyle.None; // 점선 테두리는 Overlay가 Bounds 바깥에 그린다.
            box.ImeMode = ImeMode.On;
            box.ContentsResized += OnContentsResized;
            box.KeyDown += OnKeyDown;
            box.Disposed += delegate { font.Dispose(); };
            parent.Controls.Add(box);

            // 줄 간격을 Renderer의 LineHeight로 고정(RichEdit 기본은 맑은 고딕에서 1.8배쯤 벌어진다).
            // 글자 크기와 같은 pt 환산(SizeInPoints)을 써서 DPI와 상관없이 픽셀 비율이 맞는다. Enter로 생긴 줄도 이 서식을 물려받는다.
            int lineTwips = (int)Math.Round(font.SizeInPoints * 20f * Style.LineHeight / Style.FontPx);
            box.Rtf = @"{\rtf1\pard\sl-" + lineTwips + @"\slmult0\par}";
            box.Font = font;
            box.ForeColor = color;

            if (existing != null)
            {
                existing.Hidden = true;
                for (int i = 0; i < existing.Lines.Count; i++)
                {
                    if (i > 0) box.SelectedText = "\n";
                    foreach (Run r in existing.Lines[i])
                    {
                        box.SelectionColor = r.Color;
                        box.SelectedText = r.Text;
                    }
                }
            }
            box.Select(box.TextLength, 0);
            box.ClearUndo();

            // 첫 글자 자리(테두리·안쪽 여백 뒤)를 Origin에 맞춘다. 세로는 실측 보정: 줄 간격을 고정한 RichEdit은
            // 맑은 고딕 Bold를 Renderer(GDI+ GenericTypographic)보다 0.2em 위에 그린다(18/27/36px에서 4/5/7px).
            Point want = new Point((int)Math.Round(origin.X), (int)Math.Round(origin.Y + 0.2f * Style.FontPx * scale));
            Point at = parent.PointToClient(box.PointToScreen(box.GetPositionFromCharIndex(0)));
            box.Location = new Point(box.Left + want.X - at.X, box.Top + want.Y - at.Y);

            box.Focus();
            parent.Invalidate(); // 점선 테두리
        }

        public Rectangle Bounds { get { return box.Bounds; } }

        // 투명 상자 영역은 부모가 다시 그릴 때 함께 덮인다(WS_CLIPCHILDREN이 투명 자식은 안 뺌). 부모 그리기 뒤에 상자를 다시 그린다.
        public void RedrawAfterParent()
        {
            if (IsOpen) box.Invalidate();
        }

        // 선택이 있으면 그 부분만, 없으면 이후 입력 글자 색.
        public void ApplyColor(Color c)
        {
            if (IsOpen) box.SelectionColor = c;
        }

        public void Commit()
        {
            if (!IsOpen) return;

            // 캔버스 클릭·도구 전환·보내기는 키 이벤트 없이 오므로 조합 중인 글자를 먼저 끝내고 읽는다.
            Native.CompleteComposition(box.Handle);
            string text = box.Text; // 줄바꿈은 "\n" 한 글자라 Select 인덱스와 일치한다.
            List<Color> colors = new List<Color>(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                box.Select(i, 1);
                colors.Add(box.SelectionColor);
            }

            List<List<Run>> lines = TextRuns.Split(text, colors);
            doc.CommitText(Editing, origin, color, lines, Renderer.MeasureLines(lines, scale));
            End();
        }

        public void Cancel()
        {
            if (!IsOpen) return;
            if (Editing != null) Editing.Hidden = false;
            End();
        }

        private void End()
        {
            RichTextBox b = box;
            box = null;
            Editing = null;
            b.Dispose(); // parent.Controls에서도 빠진다.
            parent.Focus(); // 폼이 다시 키를 받게(폼의 ImeMode.Disable로 한글 모드에서도 Ctrl+Z).

            EventHandler handler = Closed;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        private void OnContentsResized(object sender, ContentsResizedEventArgs e)
        {
            // 오른쪽은 다음 글자 하나 들어갈 여유(크기 조정 전에 가로 스크롤돼 글자가 밀리지 않게), 아래는 안쪽 여백만큼.
            ((Control)sender).ClientSize = new Size(
                e.NewRectangle.Width + (int)Math.Ceiling(Style.LineHeight * scale),
                e.NewRectangle.Height + (int)Math.Ceiling(Style.TextPad * scale));
            parent.Invalidate(); // 점선 테두리가 커진 상자를 따라가게
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ProcessKey) return; // 한글 조합 중

            // 둘 다 미룬다: Enter는 조합 중이던 마지막 글자가 먼저 들어온 뒤 읽으려고, 둘 다 자기 키 처리 중에 상자를 없애지 않으려고.
            // Shift+Enter는 RichTextBox 기본 줄바꿈("\n")이 그대로 들어간다.
            if (e.KeyData == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                parent.BeginInvoke(new MethodInvoker(Commit));
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                parent.BeginInvoke(new MethodInvoker(Cancel));
            }
        }

        // WS_EX_TRANSPARENT: 배경을 칠하지 않아 아래 캡처 화면이 비친다(RichEdit20W 실측: 지우거나 커져도 잔상 없음, 클릭은 상자가 받음).
        private sealed class ClearRichTextBox : RichTextBox
        {
            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams cp = base.CreateParams;
                    cp.ExStyle |= 0x20;
                    return cp;
                }
            }
        }
    }
}
