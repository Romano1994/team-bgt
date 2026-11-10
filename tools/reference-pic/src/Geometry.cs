using System;
using System.Collections.Generic;
using System.Drawing;

namespace ReferencePic
{
    public static class Geo
    {
        public static RectangleF Box(DrawObj o, float scale)
        {
            if (o.Kind == Kind.Text)
            {
                float pad = Style.TextPad * scale;
                return new RectangleF(o.Origin.X - pad, o.Origin.Y - pad, o.TextSize.Width + pad * 2, o.TextSize.Height + pad * 2);
            }

            float x = Math.Min(o.P1.X, o.P2.X);
            float y = Math.Min(o.P1.Y, o.P2.Y);
            float w = Math.Abs(o.P2.X - o.P1.X);
            float h = Math.Abs(o.P2.Y - o.P1.Y);
            return new RectangleF(x, y, w, h);
        }

        // 동그라미는 120개 균등 각도 샘플, 네모·텍스트 박스는 둘레를 4px 간격(변마다 최소 2분할)으로 샘플.
        public static List<PointF> Outline(DrawObj o, float scale)
        {
            RectangleF b = Box(o, scale);
            List<PointF> pts = new List<PointF>();

            if (o.Kind == Kind.Ellipse)
            {
                for (int i = 0; i < 120; i++)
                {
                    double t = i / 120.0 * 2 * Math.PI;
                    pts.Add(new PointF(
                        b.X + b.Width / 2f * (1f + (float)Math.Cos(t)),
                        b.Y + b.Height / 2f * (1f + (float)Math.Sin(t))));
                }
                return pts;
            }

            int nw = Math.Max(2, (int)Math.Ceiling(b.Width / 4f));
            int nh = Math.Max(2, (int)Math.Ceiling(b.Height / 4f));

            for (int i = 0; i <= nw; i++)
            {
                float x = b.X + b.Width * i / nw;
                pts.Add(new PointF(x, b.Y));
                pts.Add(new PointF(x, b.Y + b.Height));
            }
            for (int i = 0; i <= nh; i++)
            {
                float y = b.Y + b.Height * i / nh;
                pts.Add(new PointF(b.X, y));
                pts.Add(new PointF(b.X + b.Width, y));
            }

            return pts;
        }

        public static PointF Nearest(IList<PointF> pts, PointF p)
        {
            PointF best = pts[0];
            double bestD = Dist2(best, p);
            for (int i = 1; i < pts.Count; i++)
            {
                double d = Dist2(pts[i], p);
                if (d < bestD)
                {
                    best = pts[i];
                    bestD = d;
                }
            }
            return best;
        }

        public static bool Inside(DrawObj o, PointF p, float scale)
        {
            RectangleF b = Box(o, scale);
            if (o.Kind == Kind.Ellipse)
            {
                float dx = (p.X - b.X - b.Width / 2f) / (b.Width / 2f);
                float dy = (p.Y - b.Y - b.Height / 2f) / (b.Height / 2f);
                return dx * dx + dy * dy <= 1f;
            }
            return p.X >= b.X && p.X <= b.X + b.Width && p.Y >= b.Y && p.Y <= b.Y + b.Height;
        }

        // 목록을 뒤에서부터 보며 Rect/Ellipse/Text이고 !Hidden이며 Inside || 최근접 테두리 거리 <= SnapDistance*scale인 첫 항목.
        public static DrawObj FindAnchor(IList<DrawObj> items, PointF p, float scale)
        {
            for (int i = items.Count - 1; i >= 0; i--)
            {
                DrawObj o = items[i];
                if (o.Hidden) continue;
                if (o.Kind != Kind.Rect && o.Kind != Kind.Ellipse && o.Kind != Kind.Text) continue;

                if (Inside(o, p, scale) || Dist(Nearest(Outline(o, scale), p), p) <= Style.SnapDistance * scale)
                {
                    return o;
                }
            }
            return null;
        }

        public static PointF SnapStart(IList<DrawObj> items, PointF p, float scale)
        {
            DrawObj a = FindAnchor(items, p, scale);
            if (a == null) return p;
            return Nearest(Outline(a, scale), p);
        }

        // r의 어느 변을 잡았는지: 1=왼 2=위 4=오른 8=아래 비트(모서리는 둘), 테두리에서 tol 넘게 안쪽이면 15(통째 이동), 바깥이면 0.
        public static int Edges(Rectangle r, Point p, float tol)
        {
            if (!RectangleF.Inflate(r, tol, tol).Contains(p)) return 0;
            int e = 0;
            if (Math.Abs(p.X - r.Left) <= tol) e |= 1;
            else if (Math.Abs(p.X - r.Right) <= tol) e |= 4;
            if (Math.Abs(p.Y - r.Top) <= tol) e |= 2;
            else if (Math.Abs(p.Y - r.Bottom) <= tol) e |= 8;
            return e == 0 ? 15 : e;
        }

        private static double Dist2(PointF a, PointF b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }

        private static double Dist(PointF a, PointF b)
        {
            return Math.Sqrt(Dist2(a, b));
        }
    }
}
