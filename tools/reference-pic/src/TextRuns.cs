using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace ReferencePic
{
    public static class TextRuns
    {
        // charColors는 text와 같은 길이(\r·\n 자리 포함).
        public static List<List<Run>> Split(string text, IList<Color> charColors)
        {
            List<List<Run>> lines = new List<List<Run>>();
            List<KeyValuePair<char, Color>> current = new List<KeyValuePair<char, Color>>();

            int i = 0;
            while (i < text.Length)
            {
                char ch = text[i];
                if (ch == '\r' || ch == '\n')
                {
                    lines.Add(BuildLine(current));
                    current = new List<KeyValuePair<char, Color>>();
                    if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    {
                        i += 2;
                    }
                    else
                    {
                        i += 1;
                    }
                    continue;
                }

                current.Add(new KeyValuePair<char, Color>(ch, charColors[i]));
                i += 1;
            }
            lines.Add(BuildLine(current));

            // 끝 빈 줄 제거(줄바꿈 뒤에 아무 내용도 없을 때 생기는 마지막 빈 줄 한 개만).
            if (lines.Count > 1 && lines[lines.Count - 1].Count == 0)
            {
                lines.RemoveAt(lines.Count - 1);
            }

            return lines;
        }

        public static bool IsBlank(List<List<Run>> lines)
        {
            foreach (List<Run> line in lines)
            {
                foreach (Run r in line)
                {
                    foreach (char c in r.Text)
                    {
                        if (!char.IsWhiteSpace(c)) return false;
                    }
                }
            }
            return true;
        }

        private static List<Run> BuildLine(List<KeyValuePair<char, Color>> chars)
        {
            List<Run> runs = new List<Run>();
            if (chars.Count == 0) return runs;

            StringBuilder sb = new StringBuilder();
            Color color = chars[0].Value;
            sb.Append(chars[0].Key);

            for (int i = 1; i < chars.Count; i++)
            {
                if (chars[i].Value.ToArgb() == color.ToArgb())
                {
                    sb.Append(chars[i].Key);
                }
                else
                {
                    runs.Add(MakeRun(sb.ToString(), color));
                    sb = new StringBuilder();
                    sb.Append(chars[i].Key);
                    color = chars[i].Value;
                }
            }
            runs.Add(MakeRun(sb.ToString(), color));

            return runs;
        }

        private static Run MakeRun(string text, Color color)
        {
            Run r = new Run();
            r.Text = text;
            r.Color = color;
            return r;
        }
    }
}
