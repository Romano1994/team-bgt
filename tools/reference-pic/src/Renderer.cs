using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace ReferencePic
{
    public static class Renderer
    {
        public static void DrawItems(Graphics g, IList<DrawObj> items, float scale)
        {
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            foreach (DrawObj o in items)
            {
                if (o.Hidden) continue;

                if (o.Kind == Kind.Erase)
                {
                    // 부분 지우개: 투명 펜 + SourceCopy로 덮어써서 알파를 0으로 만든다(블렌딩이 아닌 교체).
                    g.CompositingMode = CompositingMode.SourceCopy;
                    g.SmoothingMode = SmoothingMode.None;
                }
                else
                {
                    g.CompositingMode = CompositingMode.SourceOver;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                }

                DrawOne(g, o, scale);
            }
        }

        // area(캡처 좌표) 부분만 잘라 합성한다.
        public static Bitmap Compose(Bitmap screenshot, Bitmap layer, Rectangle area)
        {
            Bitmap result = new Bitmap(area.Width, area.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.CompositingMode = CompositingMode.SourceOver;
                g.DrawImageUnscaled(screenshot, -area.X, -area.Y);
                g.DrawImageUnscaled(layer, -area.X, -area.Y);
            }
            return result;
        }

        public static Font TextFont(float scale)
        {
            return new Font("Malgun Gothic", Style.FontPx * scale, FontStyle.Bold, GraphicsUnit.Pixel);
        }

        public static SizeF MeasureLines(List<List<Run>> lines, float scale)
        {
            float height = lines.Count * Style.LineHeight * scale;
            float width = 0f;

            using (Bitmap tmp = new Bitmap(1, 1))
            using (Graphics g = Graphics.FromImage(tmp))
            using (Font font = TextFont(scale))
            {
                StringFormat fmt = StringFormat.GenericTypographic;
                foreach (List<Run> line in lines)
                {
                    float w = 0f;
                    foreach (Run r in line)
                    {
                        w += g.MeasureString(r.Text, font, PointF.Empty, fmt).Width;
                    }
                    if (w > width) width = w;
                }
            }

            return new SizeF(width, height);
        }

        private static void DrawOne(Graphics g, DrawObj o, float scale)
        {
            switch (o.Kind)
            {
                case Kind.Pen:
                    DrawPolyline(g, o.Points, o.Color, Style.PenWidth * scale);
                    break;
                case Kind.Erase:
                    DrawPolyline(g, o.Points, Color.Transparent, Style.EraserDiameter * scale);
                    break;
                case Kind.Rect:
                    DrawShapeStroke(g, o, scale, false);
                    break;
                case Kind.Ellipse:
                    DrawShapeStroke(g, o, scale, true);
                    break;
                case Kind.Line:
                    DrawLineOrArrow(g, o, scale, false);
                    break;
                case Kind.Arrow:
                    DrawLineOrArrow(g, o, scale, true);
                    break;
                case Kind.Text:
                    DrawText(g, o, scale);
                    break;
            }
        }

        private static void DrawPolyline(Graphics g, List<PointF> points, Color color, float width)
        {
            if (points == null || points.Count == 0) return;

            using (Pen pen = new Pen(color, width))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                if (points.Count == 1)
                {
                    PointF p = points[0];
                    g.DrawLine(pen, p.X, p.Y, p.X + 0.01f, p.Y);
                }
                else
                {
                    g.DrawLines(pen, points.ToArray());
                }
            }
        }

        private static void DrawShapeStroke(Graphics g, DrawObj o, float scale, bool ellipse)
        {
            RectangleF b = Geo.Box(o, scale);
            using (Pen pen = new Pen(o.Color, Style.ShapeWidth * scale))
            {
                pen.LineJoin = LineJoin.Round;
                if (ellipse)
                {
                    g.DrawEllipse(pen, b.X, b.Y, b.Width, b.Height);
                }
                else
                {
                    g.DrawRectangle(pen, b.X, b.Y, b.Width, b.Height);
                }
            }
        }

        private static void DrawLineOrArrow(Graphics g, DrawObj o, float scale, bool arrow)
        {
            float dx = o.P2.X - o.P1.X;
            float dy = o.P2.Y - o.P1.Y;
            double angle = Math.Atan2(dy, dx);
            float head = Style.ArrowHead * scale;
            float shortLen = arrow ? head * 0.7f : 0f;

            PointF lineEnd = new PointF(
                o.P2.X - shortLen * (float)Math.Cos(angle),
                o.P2.Y - shortLen * (float)Math.Sin(angle));

            using (Pen pen = new Pen(o.Color, Style.ShapeWidth * scale))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                g.DrawLine(pen, o.P1, lineEnd);
            }

            if (arrow)
            {
                PointF head1 = new PointF(
                    o.P2.X - head * (float)Math.Cos(angle - Style.ArrowHalfAngle),
                    o.P2.Y - head * (float)Math.Sin(angle - Style.ArrowHalfAngle));
                PointF head2 = new PointF(
                    o.P2.X - head * (float)Math.Cos(angle + Style.ArrowHalfAngle),
                    o.P2.Y - head * (float)Math.Sin(angle + Style.ArrowHalfAngle));

                using (Brush brush = new SolidBrush(o.Color))
                {
                    g.FillPolygon(brush, new PointF[] { o.P2, head1, head2 });
                }
            }
        }

        private static void DrawText(Graphics g, DrawObj o, float scale)
        {
            using (Font font = TextFont(scale))
            {
                StringFormat fmt = StringFormat.GenericTypographic;
                for (int i = 0; i < o.Lines.Count; i++)
                {
                    List<Run> line = o.Lines[i];
                    float x = o.Origin.X;
                    float y = o.Origin.Y + i * Style.LineHeight * scale;
                    foreach (Run r in line)
                    {
                        using (Brush brush = new SolidBrush(r.Color))
                        {
                            g.DrawString(r.Text, font, brush, x, y, fmt);
                        }
                        x += g.MeasureString(r.Text, font, PointF.Empty, fmt).Width;
                    }
                }
            }
        }
    }
}
