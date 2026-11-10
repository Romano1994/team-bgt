using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace ReferencePic
{
    public enum HitKind { None, Tool, ShapeMenu, ShapeItem, ColorMenu, ColorItem, Send, Close }

    public enum PopupKind { None, Shape, Color }

    public class ToolbarItem
    {
        public HitKind Kind;
        public Tool Tool;
        public Color Color;
        public Rectangle Rect;
    }

    // C형 툴바: Control이 아니라 상태+배치+그리기+히트테스트만 가진 평범한 클래스.
    // Overlay가 소유하며 마우스다운 때 Click()을 호출하고, 그 결과로 Send/Close를 처리한다.
    public class Toolbar
    {
        private const float ButtonSize = 34f;
        private const float Gap = 4f;
        private const float PadH = 8f;
        private const float PadV = 6f;
        private const float SepWidth = 1f;
        private const float SepHeight = 22f;
        private const float SepMargin = 4f;
        private const float PanelRadius = 12f;
        private const float ButtonRadius = 8f;
        private const float PopupRadius = 10f;
        private const float PopupGap = 12f;
        private const float PopupPad = 6f;
        private const float IconSize = 20f;
        private const float SendIconSize = 16f;
        private const float SendPad = 12f;
        private const float SendGap = 6f;

        private static readonly Tool[] Shapes = new Tool[] { Tool.Ellipse, Tool.Rect, Tool.Line, Tool.Arrow };
        private static readonly Color IconColor = Color.FromArgb(0xE8, 0xE8, 0xEA);
        private static readonly Color ActiveColor = Color.FromArgb(0x58, 0x65, 0xF2);
        private static readonly Color SendColor = Color.FromArgb(0xD9, 0x77, 0x57);
        private static readonly Color SepColor = Color.FromArgb(0x4A, 0x4C, 0x52);

        public Tool Tool = Tool.Pen;
        public Tool LastShape = Tool.Ellipse;
        public Color Color = Style.Colors[0];
        public PopupKind Popup = PopupKind.None;
        public Rectangle Bounds;
        public List<ToolbarItem> Items = new List<ToolbarItem>();

        private float scale = 1f;
        private ToolbarItem shapeMenuItem;
        private ToolbarItem colorMenuItem;
        private Rectangle sep1Rect;
        private Rectangle sep2Rect;
        private Rectangle popupPanelRect;

        public void Layout(Rectangle screen, float scale)
        {
            this.scale = scale;
            Popup = PopupKind.None;
            Items = new List<ToolbarItem>();

            float btn = ButtonSize * scale;
            float gap = Gap * scale;
            float padH = PadH * scale;
            float padV = PadV * scale;
            float sepW = SepWidth * scale;
            float sepMargin = SepMargin * scale;
            float sendW = MeasureSendWidth(scale);

            // 시안 CSS는 .tb{gap:4px}를 모든 형제 사이에 적용하고, 구분선 자체에 margin:0 4px를 더 얹는다.
            // 즉 구분선 양옆은 gap(4)+margin(4)=8px씩 떨어진다(구분선 아닌 보통 버튼 사이는 gap 4px만).
            float contentW =
                btn + gap +
                btn + gap +
                btn + gap +
                btn + gap +
                btn + gap + sepMargin +
                sepW + sepMargin + gap +
                btn + gap + sepMargin +
                sepW + sepMargin + gap +
                sendW + gap +
                btn;

            float totalWf = contentW + padH * 2f;
            float totalHf = btn + padV * 2f;

            int totalW = (int)Math.Round(totalWf);
            int totalH = (int)Math.Round(totalHf);

            int bottom = screen.Bottom - (int)Math.Round(Style.ToolbarBottom * scale);
            int top = bottom - totalH;
            double centerX = screen.Left + screen.Width / 2.0;
            int left = (int)Math.Round(centerX - totalW / 2.0);

            Bounds = new Rectangle(left, top, totalW, totalH);

            float x = Bounds.Left + padH;
            float y = Bounds.Top + padV;

            Items.Add(NewItem(HitKind.Tool, Tool.Pen, x, y, btn));
            x += btn + gap;

            shapeMenuItem = NewItem(HitKind.ShapeMenu, LastShape, x, y, btn);
            Items.Add(shapeMenuItem);
            x += btn + gap;

            Items.Add(NewItem(HitKind.Tool, Tool.Text, x, y, btn));
            x += btn + gap;

            Items.Add(NewItem(HitKind.Tool, Tool.Eraser, x, y, btn));
            x += btn + gap;

            Items.Add(NewItem(HitKind.Tool, Tool.Crop, x, y, btn));
            x += btn + gap + sepMargin;

            float sepY = y + (btn - SepHeight * scale) / 2f;
            sep1Rect = new Rectangle((int)Math.Round(x), (int)Math.Round(sepY), (int)Math.Round(sepW), (int)Math.Round(SepHeight * scale));
            x += sepW + sepMargin + gap;

            colorMenuItem = NewItem(HitKind.ColorMenu, Tool.Pen, x, y, btn);
            Items.Add(colorMenuItem);
            x += btn + gap + sepMargin;

            sep2Rect = new Rectangle((int)Math.Round(x), (int)Math.Round(sepY), (int)Math.Round(sepW), (int)Math.Round(SepHeight * scale));
            x += sepW + sepMargin + gap;

            Items.Add(NewItem(HitKind.Send, Tool.Pen, x, y, sendW, btn));
            x += sendW + gap;

            Items.Add(NewItem(HitKind.Close, Tool.Pen, x, y, btn));

            RebuildPopup();
        }

        public bool Contains(Point p)
        {
            if (Bounds.Contains(p)) return true;
            return Popup != PopupKind.None && popupPanelRect.Contains(p);
        }

        // 상태를 바꾼 뒤 눌린 항목을 반환한다(없으면 null). 바깥 클릭은 열려 있던 팝업만 닫는다.
        public ToolbarItem Click(Point p)
        {
            ToolbarItem hit = HitTest(p);
            if (hit == null)
            {
                if (Popup != PopupKind.None)
                {
                    Popup = PopupKind.None;
                    RebuildPopup();
                }
                return null;
            }

            switch (hit.Kind)
            {
                case HitKind.Tool:
                    Tool = hit.Tool;
                    Popup = PopupKind.None;
                    break;
                case HitKind.ShapeMenu:
                    Tool = LastShape;
                    Popup = Popup == PopupKind.Shape ? PopupKind.None : PopupKind.Shape;
                    break;
                case HitKind.ShapeItem:
                    Tool = hit.Tool;
                    LastShape = hit.Tool;
                    shapeMenuItem.Tool = hit.Tool;
                    Popup = PopupKind.None;
                    break;
                case HitKind.ColorMenu:
                    Popup = Popup == PopupKind.Color ? PopupKind.None : PopupKind.Color;
                    break;
                case HitKind.ColorItem:
                    Color = hit.Color;
                    Popup = PopupKind.None;
                    break;
                case HitKind.Send:
                case HitKind.Close:
                    Popup = PopupKind.None;
                    break;
            }

            RebuildPopup();
            return hit;
        }

        public void Paint(Graphics g)
        {
            SmoothingMode old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            PaintPanel(g, Bounds, PanelRadius * scale, 240);

            for (int i = 0; i < Items.Count; i++)
            {
                ToolbarItem item = Items[i];
                if (item.Kind == HitKind.ShapeItem || item.Kind == HitKind.ColorItem) continue;
                PaintBaseItem(g, item);
            }

            PaintSeparator(g, sep1Rect);
            PaintSeparator(g, sep2Rect);

            if (Popup != PopupKind.None)
            {
                PaintPanel(g, popupPanelRect, PopupRadius * scale, 247);
                for (int i = 0; i < Items.Count; i++)
                {
                    ToolbarItem item = Items[i];
                    if (item.Kind == HitKind.ShapeItem)
                    {
                        PaintIconButton(g, item.Rect, IconKey(item.Tool), item.Tool == Tool);
                    }
                    else if (item.Kind == HitKind.ColorItem)
                    {
                        PaintColorItem(g, item);
                    }
                }
            }

            g.SmoothingMode = old;
        }

        private ToolbarItem HitTest(Point p)
        {
            for (int i = Items.Count - 1; i >= 0; i--)
            {
                if (Items[i].Rect.Contains(p)) return Items[i];
            }
            return null;
        }

        private void RebuildPopup()
        {
            for (int i = Items.Count - 1; i >= 0; i--)
            {
                if (Items[i].Kind == HitKind.ShapeItem || Items[i].Kind == HitKind.ColorItem)
                {
                    Items.RemoveAt(i);
                }
            }

            if (Popup == PopupKind.Shape)
            {
                Rectangle[] rects = LayoutPopup(shapeMenuItem.Rect, Shapes.Length);
                for (int i = 0; i < Shapes.Length; i++)
                {
                    Items.Add(NewItem(HitKind.ShapeItem, Shapes[i], rects[i].X, rects[i].Y, rects[i].Width, rects[i].Height));
                }
            }
            else if (Popup == PopupKind.Color)
            {
                Rectangle[] rects = LayoutPopup(colorMenuItem.Rect, Style.Colors.Length);
                for (int i = 0; i < Style.Colors.Length; i++)
                {
                    ToolbarItem it = NewItem(HitKind.ColorItem, Tool.Pen, rects[i].X, rects[i].Y, rects[i].Width, rects[i].Height);
                    it.Color = Style.Colors[i];
                    Items.Add(it);
                }
            }
            else
            {
                popupPanelRect = Rectangle.Empty;
            }
        }

        // anchor(버튼) 위 12px에, 버튼 가로 중심으로 정렬된 count개 칸짜리 팝업 패널을 배치한다.
        private Rectangle[] LayoutPopup(Rectangle anchor, int count)
        {
            float btn = ButtonSize * scale;
            float gap = Gap * scale;
            float pad = PopupPad * scale;

            float width = count * btn + (count - 1) * gap + pad * 2f;
            float height = btn + pad * 2f;

            float centerX = anchor.X + anchor.Width / 2f;
            float bottom = anchor.Top - PopupGap * scale;
            float top = bottom - height;
            float left = centerX - width / 2f;

            popupPanelRect = new Rectangle(
                (int)Math.Round(left), (int)Math.Round(top),
                (int)Math.Round(width), (int)Math.Round(height));

            Rectangle[] rects = new Rectangle[count];
            float x = left + pad;
            float y = top + pad;
            for (int i = 0; i < count; i++)
            {
                rects[i] = new Rectangle((int)Math.Round(x), (int)Math.Round(y), (int)Math.Round(btn), (int)Math.Round(btn));
                x += btn + gap;
            }
            return rects;
        }

        private float MeasureSendWidth(float scale)
        {
            using (Bitmap tmp = new Bitmap(1, 1))
            using (Graphics g = Graphics.FromImage(tmp))
            using (Font font = SendFont(scale))
            {
                float textW = g.MeasureString("보내기", font, PointF.Empty, StringFormat.GenericTypographic).Width;
                return SendPad * scale * 2f + SendIconSize * scale + SendGap * scale + textW;
            }
        }

        private static Font SendFont(float scale)
        {
            return new Font("Malgun Gothic", 13f * scale, FontStyle.Bold, GraphicsUnit.Pixel);
        }

        private static ToolbarItem NewItem(HitKind kind, Tool tool, float x, float y, float size)
        {
            return NewItem(kind, tool, x, y, size, size);
        }

        private static ToolbarItem NewItem(HitKind kind, Tool tool, float x, float y, float w, float h)
        {
            ToolbarItem item = new ToolbarItem();
            item.Kind = kind;
            item.Tool = tool;
            item.Rect = new Rectangle(
                (int)Math.Round(x), (int)Math.Round(y),
                (int)Math.Round(w), (int)Math.Round(h));
            return item;
        }

        private void PaintBaseItem(Graphics g, ToolbarItem item)
        {
            switch (item.Kind)
            {
                case HitKind.Tool:
                    PaintIconButton(g, item.Rect, IconKey(item.Tool), item.Tool == Tool);
                    break;
                case HitKind.ShapeMenu:
                    PaintIconButton(g, item.Rect, IconKey(LastShape), Array.IndexOf(Shapes, Tool) >= 0);
                    PaintCaret(g, item.Rect);
                    break;
                case HitKind.ColorMenu:
                    PaintColorMenu(g, item);
                    break;
                case HitKind.Send:
                    PaintSend(g, item);
                    break;
                case HitKind.Close:
                    PaintIconButton(g, item.Rect, "close", false);
                    break;
            }
        }

        private void PaintIconButton(Graphics g, Rectangle rect, string key, bool active)
        {
            if (active)
            {
                using (GraphicsPath p = RoundedRect(rect, ButtonRadius * scale))
                using (Brush b = new SolidBrush(ActiveColor))
                {
                    g.FillPath(b, p);
                }
            }

            float iconSize = IconSize * scale;
            RectangleF box = new RectangleF(
                rect.X + (rect.Width - iconSize) / 2f,
                rect.Y + (rect.Height - iconSize) / 2f,
                iconSize, iconSize);
            DrawIcon(g, key, box, IconColor);
        }

        private void PaintCaret(Graphics g, Rectangle rect)
        {
            float s = 3f * scale;
            float cx = rect.Right - 5f * scale;
            float cy = rect.Bottom - 5f * scale;
            PointF[] tri = new PointF[]
            {
                new PointF(cx - s, cy + s * 0.6f),
                new PointF(cx + s, cy + s * 0.6f),
                new PointF(cx, cy - s * 0.6f)
            };
            using (Brush b = new SolidBrush(IconColor))
            {
                g.FillPolygon(b, tri);
            }
        }

        private void PaintColorMenu(Graphics g, ToolbarItem item)
        {
            if (Popup == PopupKind.Color)
            {
                using (GraphicsPath p = RoundedRect(item.Rect, ButtonRadius * scale))
                using (Brush b = new SolidBrush(ActiveColor))
                {
                    g.FillPath(b, p);
                }
            }

            float d = IconSize * scale;
            RectangleF dot = new RectangleF(
                item.Rect.X + (item.Rect.Width - d) / 2f,
                item.Rect.Y + (item.Rect.Height - d) / 2f,
                d, d);
            using (Brush b = new SolidBrush(Color))
            {
                g.FillEllipse(b, dot);
            }
            using (Pen ring = new Pen(System.Drawing.Color.White, 2f * scale))
            {
                g.DrawEllipse(ring, dot);
            }
        }

        private void PaintColorItem(Graphics g, ToolbarItem item)
        {
            float d = IconSize * scale;
            RectangleF dot = new RectangleF(
                item.Rect.X + (item.Rect.Width - d) / 2f,
                item.Rect.Y + (item.Rect.Height - d) / 2f,
                d, d);
            using (Brush b = new SolidBrush(item.Color))
            {
                g.FillEllipse(b, dot);
            }
            if (item.Color.ToArgb() == Color.ToArgb())
            {
                using (Pen ring = new Pen(System.Drawing.Color.White, 2f * scale))
                {
                    g.DrawEllipse(ring, dot);
                }
                RectangleF halo = RectangleF.Inflate(dot, 2f * scale, 2f * scale);
                using (Pen hring = new Pen(ActiveColor, 2f * scale))
                {
                    g.DrawEllipse(hring, halo);
                }
            }
        }

        private void PaintSend(Graphics g, ToolbarItem item)
        {
            using (GraphicsPath p = RoundedRect(item.Rect, ButtonRadius * scale))
            using (Brush b = new SolidBrush(SendColor))
            {
                g.FillPath(b, p);
            }

            float iconSize = SendIconSize * scale;
            float pad = SendPad * scale;
            float gap = SendGap * scale;
            RectangleF iconBox = new RectangleF(item.Rect.X + pad, item.Rect.Y + (item.Rect.Height - iconSize) / 2f, iconSize, iconSize);
            DrawIcon(g, "send", iconBox, System.Drawing.Color.White);

            using (Font font = SendFont(scale))
            using (Brush textBrush = new SolidBrush(System.Drawing.Color.White))
            {
                StringFormat fmt = new StringFormat(StringFormat.GenericTypographic);
                fmt.LineAlignment = StringAlignment.Center;
                float textLeft = iconBox.Right + gap;
                RectangleF textRect = new RectangleF(textLeft, item.Rect.Y, item.Rect.Right - textLeft - pad, item.Rect.Height);
                g.DrawString("보내기", font, textBrush, textRect, fmt);
            }
        }

        private static void PaintPanel(Graphics g, Rectangle rect, float radius, int alpha)
        {
            using (GraphicsPath p = RoundedRect(rect, radius))
            using (Brush b = new SolidBrush(Color.FromArgb(alpha, 32, 33, 36)))
            {
                g.FillPath(b, p);
            }
        }

        private static void PaintSeparator(Graphics g, Rectangle r)
        {
            using (Brush b = new SolidBrush(SepColor))
            {
                g.FillRectangle(b, r);
            }
        }

        private static GraphicsPath RoundedRect(Rectangle r, float radius)
        {
            return RoundedRect(new RectangleF(r.X, r.Y, r.Width, r.Height), radius);
        }

        private static GraphicsPath RoundedRect(RectangleF r, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2f;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;

            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static string IconKey(Tool t)
        {
            switch (t)
            {
                case Tool.Pen: return "pen";
                case Tool.Ellipse: return "ellipse";
                case Tool.Rect: return "rect";
                case Tool.Line: return "line";
                case Tool.Arrow: return "arrow";
                case Tool.Text: return "text";
                case Tool.Eraser: return "eraser";
                case Tool.Crop: return "crop";
                default: return "pen";
            }
        }

        // 시안(docs/prototype/toolbar-v3.html)의 ICON SVG path(24x24, stroke 2, round cap/join)를
        // GDI+로 옮긴 것. box는 목적지 사각형(정사각형)이고, 변환으로 0..24 좌표계를 그 안에 맞춘다.
        public static void DrawIcon(Graphics g, string key, RectangleF box, Color color)
        {
            GraphicsState state = g.Save();
            try
            {
                float s = box.Width / 24f;
                g.TranslateTransform(box.X, box.Y);
                g.ScaleTransform(s, s);

                using (Pen pen = new Pen(color, 2f))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    pen.LineJoin = LineJoin.Round;

                    switch (key)
                    {
                        case "pen":
                            g.DrawPolygon(pen, new PointF[] { new PointF(4, 20), new PointF(5, 16), new PointF(16, 5), new PointF(19, 8), new PointF(8, 19) });
                            g.DrawLine(pen, 14, 7, 17, 10);
                            break;
                        case "ellipse":
                            g.DrawEllipse(pen, 3.5f, 5.5f, 17f, 13f);
                            break;
                        case "rect":
                            g.DrawRectangle(pen, 4, 6, 16, 12);
                            break;
                        case "line":
                            g.DrawLine(pen, 5, 19, 19, 5);
                            break;
                        case "arrow":
                            g.DrawLine(pen, 5, 19, 19, 5);
                            g.DrawLines(pen, new PointF[] { new PointF(10, 5), new PointF(19, 5), new PointF(19, 14) });
                            break;
                        case "text":
                            g.DrawLines(pen, new PointF[] { new PointF(5, 6), new PointF(5, 5), new PointF(19, 5), new PointF(19, 6) });
                            g.DrawLine(pen, 12, 5, 12, 19);
                            g.DrawLine(pen, 9, 19, 15, 19);
                            break;
                        case "eraser":
                            g.DrawLine(pen, 7, 20, 20, 20);
                            g.DrawPolygon(pen, new PointF[] { new PointF(4.5f, 14.5f), new PointF(13.5f, 5.5f), new PointF(19.5f, 11.5f), new PointF(11f, 20f), new PointF(9, 20) });
                            g.DrawLine(pen, 9, 10, 15, 16);
                            break;
                        case "crop":
                            g.DrawLines(pen, new PointF[] { new PointF(6, 3), new PointF(6, 18), new PointF(21, 18) });
                            g.DrawLines(pen, new PointF[] { new PointF(3, 6), new PointF(18, 6), new PointF(18, 21) });
                            break;
                        case "send":
                            g.DrawPolygon(pen, new PointF[] { new PointF(4, 12), new PointF(20, 4), new PointF(14, 20), new PointF(11, 13) });
                            break;
                        case "close":
                            g.DrawLine(pen, 6, 6, 18, 18);
                            g.DrawLine(pen, 18, 6, 6, 18);
                            break;
                    }
                }
            }
            finally
            {
                g.Restore(state);
            }
        }
    }
}
