using System.Collections.Generic;
using System.Drawing;

namespace ReferencePic
{
    public enum Tool { Pen, Ellipse, Rect, Line, Arrow, Text, Eraser, Crop }

    public enum Kind { Pen, Ellipse, Rect, Line, Arrow, Text, Erase }

    public class Run
    {
        public string Text;
        public Color Color;
    }

    public class DrawObj
    {
        public Kind Kind;
        public Color Color;
        public List<PointF> Points; // Pen, Erase
        public PointF P1, P2; // 도형, P1=시작점
        public PointF Origin; // Text 첫 줄 왼쪽 위
        public List<List<Run>> Lines;
        public SizeF TextSize;
        public bool Hidden;

        public DrawObj Clone()
        {
            DrawObj c = new DrawObj();
            c.Kind = Kind;
            c.Color = Color;
            c.P1 = P1;
            c.P2 = P2;
            c.Origin = Origin;
            c.TextSize = TextSize;
            c.Hidden = false;

            if (Points != null)
            {
                c.Points = new List<PointF>(Points);
            }

            if (Lines != null)
            {
                c.Lines = new List<List<Run>>();
                foreach (List<Run> line in Lines)
                {
                    List<Run> newLine = new List<Run>();
                    foreach (Run r in line)
                    {
                        Run nr = new Run();
                        nr.Text = r.Text;
                        nr.Color = r.Color;
                        newLine.Add(nr);
                    }
                    c.Lines.Add(newLine);
                }
            }

            return c;
        }
    }

    public class Document
    {
        private const int MaxHistory = 100;

        public readonly List<DrawObj> Items = new List<DrawObj>();
        private readonly List<List<DrawObj>> history = new List<List<DrawObj>>();

        public void Checkpoint()
        {
            List<DrawObj> snapshot = new List<DrawObj>();
            foreach (DrawObj o in Items)
            {
                snapshot.Add(o.Clone());
            }
            history.Add(snapshot);
            if (history.Count > MaxHistory)
            {
                history.RemoveAt(0);
            }
        }

        public bool Undo()
        {
            if (history.Count == 0) return false;

            List<DrawObj> snapshot = history[history.Count - 1];
            history.RemoveAt(history.Count - 1);
            Items.Clear();
            Items.AddRange(snapshot);
            return true;
        }

        public void DropLastCheckpoint()
        {
            if (history.Count > 0)
            {
                history.RemoveAt(history.Count - 1);
            }
        }

        public void Clear()
        {
            Items.Clear();
            history.Clear();
        }

        public void CommitText(DrawObj editing, PointF origin, Color color, List<List<Run>> lines, SizeF size)
        {
            if (TextRuns.IsBlank(lines))
            {
                if (editing != null)
                {
                    Checkpoint();
                    Items.Remove(editing);
                }
                return;
            }

            Checkpoint();
            if (editing != null)
            {
                editing.Lines = lines;
                editing.TextSize = size;
                editing.Hidden = false;
            }
            else
            {
                DrawObj t = new DrawObj();
                t.Kind = Kind.Text;
                t.Color = color;
                t.Origin = origin;
                t.Lines = lines;
                t.TextSize = size;
                t.Hidden = false;
                Items.Add(t);
            }
        }
    }

    public static class Style
    {
        public const float PenWidth = 4f;
        public const float ShapeWidth = 3f;
        public const float ArrowHead = 16f;
        public const float ArrowHalfAngle = 0.45f;
        public const float EraserDiameter = 24f;
        public const float SnapDistance = 18f;
        public const float FontPx = 18f;
        public const float LineHeight = 24f;
        public const float TextPad = 4f;
        public const float ToolbarBottom = 16f;

        public static readonly Color[] Colors = new Color[]
        {
            Color.FromArgb(0xFF, 0x3B, 0x30),
            Color.FromArgb(0xFF, 0x95, 0x00),
            Color.FromArgb(0xFF, 0xCC, 0x00),
            Color.FromArgb(0x34, 0xC7, 0x59),
            Color.FromArgb(0x00, 0x7A, 0xFF),
            Color.FromArgb(0x11, 0x11, 0x11)
        };
    }
}
