using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ReferencePic.Tests
{
    // 보이지 않는 폼에 편집기를 띄워 RichTextBox <-> Document 왕복을 확인한다(키·IME는 수동 확인).
    public static class TextEditorTests
    {
        public static void Run()
        {
            using (Form f = new Form())
            {
                f.AutoScaleMode = AutoScaleMode.None;
                IntPtr unused = f.Handle;
                Document doc = new Document();
                TextEditor ed = new TextEditor(f, 1f, doc);
                Color red = Style.Colors[0];
                Color blue = Style.Colors[4];

                ed.Open(new PointF(50, 60), red, null);
                RichTextBox box = (RichTextBox)f.Controls[0];
                box.SelectedText = "가나\n다라";
                Point first = f.PointToClient(box.PointToScreen(box.GetPositionFromCharIndex(0)));
                T.Check(first == new Point(50, 64), "첫 글자 자리 == Origin + 세로 보정 0.2em (got " + first + ")");
                T.Check(box.GetPositionFromCharIndex(3).Y - box.GetPositionFromCharIndex(0).Y == 24, "편집기 줄 간격 == LineHeight 24px");

                box.Select(1, 1);
                ed.ApplyColor(blue);
                T.Check(box.SelectionStart == 1 && box.SelectionLength == 1, "ApplyColor 후 선택 유지");

                ed.Commit();
                T.Check(!ed.IsOpen && f.Controls.Count == 0, "Commit 후 편집기 닫힘");
                T.Check(doc.Items.Count == 1 && Dump(doc.Items[0].Lines) == "가:FF3B30 나:007AFF / 다라:FF3B30", "Commit: 두 줄, 선택한 글자만 파랑");
                DrawObj t = doc.Items[0];
                T.Check(t.Origin == new PointF(50, 60) && !t.Hidden, "Commit: Origin 그대로, 보임");

                ed.Open(t.Origin, red, t);
                box = (RichTextBox)f.Controls[0];
                T.Check(t.Hidden && ed.Editing == t, "다시 편집: 원래 텍스트는 숨김");
                T.Check(box.Text == "가나\n다라" && box.GetPositionFromCharIndex(3).Y - box.GetPositionFromCharIndex(0).Y == 24, "다시 편집: 내용과 줄 간격 채움");
                ed.Cancel();
                T.Check(!t.Hidden && !ed.IsOpen, "Cancel: 원래 텍스트 다시 보임");

                ed.Open(t.Origin, red, t);
                ed.Commit();
                T.Check(doc.Items.Count == 1 && doc.Items[0] == t && Dump(t.Lines) == "가:FF3B30 나:007AFF / 다라:FF3B30", "다시 편집 확정: 같은 자리 같은 항목, 런별 색 유지");
            }
        }

        private static string Dump(List<List<Run>> lines)
        {
            List<string> parts = new List<string>();
            foreach (List<Run> line in lines)
            {
                List<string> runs = new List<string>();
                foreach (Run r in line) runs.Add(r.Text + ":" + (r.Color.ToArgb() & 0xFFFFFF).ToString("X6"));
                parts.Add(string.Join(" ", runs));
            }
            return string.Join(" / ", parts);
        }
    }
}
