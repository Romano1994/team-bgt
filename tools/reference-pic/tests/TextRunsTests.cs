using System.Collections.Generic;
using System.Drawing;

namespace ReferencePic.Tests
{
    public static class TextRunsTests
    {
        public static void Run()
        {
            List<List<Run>> a = TextRuns.Split("가나다\n라마바사", Colors(8, Color.Red));
            T.Check(a.Count == 2, "Split 개행으로 2줄");
            T.Check(a[0].Count == 1 && a[0][0].Text == "가나다" && a[0][0].Color.ToArgb() == Color.Red.ToArgb(),
                "Split 1번째 줄 내용/색");
            T.Check(a[1].Count == 1 && a[1][0].Text == "라마바사", "Split 2번째 줄 내용");

            List<Color> bc = new List<Color> { Color.Red, Color.Blue, Color.Blue };
            List<List<Run>> b = TextRuns.Split("abc", bc);
            T.Check(b.Count == 1, "Split 색만 다르면 1줄");
            T.Check(b[0].Count == 2
                && b[0][0].Text == "a" && b[0][0].Color.ToArgb() == Color.Red.ToArgb()
                && b[0][1].Text == "bc" && b[0][1].Color.ToArgb() == Color.Blue.ToArgb(),
                "Split 색 경계로 런 분리");

            List<List<Run>> c = TextRuns.Split("a\r\nb", Colors(4, Color.Black));
            T.Check(c.Count == 2, "Split \\r\\n은 한 줄바꿈으로 2줄");

            List<List<Run>> d = TextRuns.Split("a\n\nb", Colors(4, Color.Black));
            T.Check(d.Count == 3, "Split 가운데 빈 줄 포함 3줄");

            List<List<Run>> e = TextRuns.Split("a\n", Colors(2, Color.Black));
            T.Check(e.Count == 1, "Split 끝 빈 줄은 제거되어 1줄");

            T.Check(TextRuns.IsBlank(TextRuns.Split("  \n ", Colors(4, Color.Black))), "IsBlank 공백만 있으면 true");
            T.Check(!TextRuns.IsBlank(a), "IsBlank 내용이 있으면 false");
        }

        private static List<Color> Colors(int n, Color c)
        {
            List<Color> list = new List<Color>();
            for (int i = 0; i < n; i++) list.Add(c);
            return list;
        }
    }
}
