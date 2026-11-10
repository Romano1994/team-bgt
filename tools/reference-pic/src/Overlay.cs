using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace ReferencePic
{
    // 화면 고정 오버레이: 캡처된 화면을 그대로 보여주고(배경), 그 위에 그림 레이어·표시선·툴바를 얹는다.
    // Document/레이어는 오버레이마다 새로 만든다(닫으면 그림은 버려짐). Toolbar는 TrayApp이 앱 수명 동안 하나를 유지해 전달한다(색·도형 기억).
    public class Overlay : Form
    {
        private readonly Bitmap screenshot;
        private readonly Toolbar toolbar;
        private readonly Document doc = new Document();
        private readonly Bitmap layer;
        private readonly TextEditor editor;

        private DrawObj current;
        private DrawObj currentAnchor;
        private ToolbarItem pressed; // 눌린 보내기·닫기 버튼(떼기 전)
        private PointF? hoverPoint;

        // 보낼 영역(Empty면 화면 전체). 드래그 중엔 시작 때 영역·점에서 cropEdges가 가리키는 변만 움직인다.
        private Rectangle crop;
        private Rectangle cropStart;
        private Point cropFrom;
        private int cropEdges; // Geo.Edges 비트, 0=드래그 아님

        // 96 DPI 기준 상수에 곱할 배율(Style.*). 그리기 크기에 사용한다.
        public readonly float DpiScale;

        public event EventHandler SendRequested;

        public Overlay(Rectangle bounds, Bitmap screenshot, float scale, Toolbar toolbar)
        {
            this.screenshot = screenshot;
            this.toolbar = toolbar;
            DpiScale = scale;

            layer = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);

            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            Bounds = bounds;
            DoubleBuffered = true;
            ImeMode = ImeMode.Disable; // 그림 모드 키 입력이 한글 조합으로 안 들어가게.

            // 툴바는 overlay의 클라이언트 좌표(0,0 기준)로 가운데 배치한다(모니터의 절대 좌표가 아님).
            toolbar.Layout(new Rectangle(0, 0, bounds.Width, bounds.Height), scale);
            UpdateCursor(Point.Empty);

            editor = new TextEditor(this, scale, doc);
            editor.Closed += delegate { RedrawLayer(); Invalidate(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            // 그리기 순서: 캡처 -> 레이어 -> 표시선(이미지에 안 들어감) -> 툴바.
            g.DrawImageUnscaled(screenshot, 0, 0);
            g.DrawImageUnscaled(layer, 0, 0);

            PaintCrop(g);
            PaintIndicators(g);
            if (editor.IsOpen)
            {
                PaintTextFrame(g, editor.Bounds);
                editor.RedrawAfterParent(); // 안 하면 마우스만 움직여도 입력 중 글자가 사라진다.
            }

            toolbar.Paint(g);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            pressed = null; // 떼기를 못 받은 이전 누름(캡처 잃음)이 다음 획의 떼기를 가로채지 않게.
            cropEdges = 0;
            Point p = e.Location;

            if (toolbar.Contains(p))
            {
                ToolbarItem hit = toolbar.Click(p);
                if (hit != null && (hit.Kind == HitKind.Send || hit.Kind == HitKind.Close))
                {
                    pressed = hit; // 같은 버튼 위에서 뗄 때 실행: 누른 채 Ctrl+V가 나가면 떼는 클릭이 다른 창에 간다.
                    Invalidate();
                    return;
                }
                if (editor.IsOpen)
                {
                    // 툴바는 컨트롤이 아니라 폼에 그려지므로 클릭해도 편집기 포커스·선택이 그대로다.
                    if (toolbar.Tool != Tool.Text) editor.Commit();
                    else if (hit != null && hit.Kind == HitKind.ColorItem) editor.ApplyColor(hit.Color);
                }
                UpdateCursor(p);
                Invalidate();
                return;
            }

            if (toolbar.Popup != PopupKind.None)
            {
                // 바깥 클릭이므로 Click은 상태를 바꾸지 않고 열린 팝업만 닫는다.
                toolbar.Click(p);
            }

            if (editor.IsOpen)
            {
                editor.Commit(); // 편집기·툴바 밖 클릭은 확정만 한다.
                return;
            }

            if (!crop.IsEmpty && CropCloseRect().Contains(p))
            {
                crop = Rectangle.Empty;
                toolbar.Tool = Tool.Crop; // 지운 뒤 바로 다시 지정하게.
                UpdateCursor(p);
                Invalidate();
                return;
            }

            if (toolbar.Tool == Tool.Crop)
            {
                // 영역이 없으면 새로 긋고(오른쪽·아래 변이 따라옴), 있으면 잡은 변·모서리를 옮긴다.
                cropEdges = crop.IsEmpty ? 4 | 8 : Geo.Edges(crop, p, 6f * DpiScale);
                cropStart = crop.IsEmpty ? new Rectangle(p, Size.Empty) : crop;
                cropFrom = p;
                return;
            }

            if (toolbar.Tool == Tool.Text)
            {
                PointF at = new PointF(p.X, p.Y);
                DrawObj target = null;
                for (int i = doc.Items.Count - 1; i >= 0 && target == null; i--)
                {
                    DrawObj o = doc.Items[i];
                    if (o.Kind == Kind.Text && !o.Hidden && Geo.Inside(o, at, DpiScale)) target = o;
                }

                if (target != null)
                {
                    editor.Open(target.Origin, toolbar.Color, target);
                }
                else
                {
                    // 클릭한 점이 첫 줄의 세로 가운데에 오게.
                    editor.Open(new PointF(p.X, p.Y - (float)Math.Round(Style.LineHeight * DpiScale / 2f)), toolbar.Color, null);
                }
                RedrawLayer();
                Invalidate();
                return;
            }

            doc.Checkpoint();

            DrawObj obj = new DrawObj();
            obj.Color = toolbar.Color;
            PointF pf = new PointF(p.X, p.Y);

            switch (toolbar.Tool)
            {
                case Tool.Pen:
                    obj.Kind = Kind.Pen;
                    obj.Points = new List<PointF> { pf };
                    break;
                case Tool.Eraser:
                    obj.Kind = Kind.Erase;
                    obj.Points = new List<PointF> { pf };
                    break;
                case Tool.Ellipse:
                    obj.Kind = Kind.Ellipse;
                    obj.P1 = pf;
                    obj.P2 = pf;
                    break;
                case Tool.Rect:
                    obj.Kind = Kind.Rect;
                    obj.P1 = pf;
                    obj.P2 = pf;
                    break;
                case Tool.Line:
                case Tool.Arrow:
                    obj.Kind = toolbar.Tool == Tool.Line ? Kind.Line : Kind.Arrow;
                    obj.P1 = Geo.SnapStart(doc.Items, pf, DpiScale);
                    obj.P2 = pf;
                    currentAnchor = Geo.FindAnchor(doc.Items, pf, DpiScale);
                    break;
            }

            doc.Items.Add(obj);
            current = obj;

            RedrawLayer();
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            Point p = e.Location;
            hoverPoint = new PointF(p.X, p.Y);

            if (current != null)
            {
                if (current.Kind == Kind.Pen || current.Kind == Kind.Erase)
                {
                    current.Points.Add(hoverPoint.Value);
                }
                else
                {
                    current.P2 = hoverPoint.Value;
                }
                RedrawLayer();
            }
            else if (cropEdges != 0)
            {
                int dx = p.X - cropFrom.X;
                int dy = p.Y - cropFrom.Y;
                int l = cropStart.Left + ((cropEdges & 1) != 0 ? dx : 0);
                int t = cropStart.Top + ((cropEdges & 2) != 0 ? dy : 0);
                int r = cropStart.Right + ((cropEdges & 4) != 0 ? dx : 0);
                int b = cropStart.Bottom + ((cropEdges & 8) != 0 ? dy : 0);
                // 변이 맞은편을 넘어가도 뒤집힌 채로 따라오게 정규화하고, 화면 밖은 잘라낸다.
                crop = Rectangle.Intersect(
                    Rectangle.FromLTRB(Math.Min(l, r), Math.Min(t, b), Math.Max(l, r), Math.Max(t, b)),
                    new Rectangle(Point.Empty, layer.Size));
            }
            else
            {
                UpdateCursor(p);
            }

            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (pressed != null)
            {
                ToolbarItem item = pressed;
                pressed = null;
                if (!item.Rect.Contains(e.Location)) return;
                if (item.Kind == HitKind.Close)
                {
                    Close();
                    return;
                }
                EventHandler handler = SendRequested;
                if (handler != null) handler(this, EventArgs.Empty);
                return;
            }

            if (cropEdges != 0)
            {
                cropEdges = 0;
                if (crop.Width < 4 * DpiScale || crop.Height < 4 * DpiScale) crop = Rectangle.Empty; // 클릭만 한 것
                Invalidate();
                return;
            }

            if (current == null) return;

            bool isShapeOrLine = current.Kind == Kind.Ellipse || current.Kind == Kind.Rect
                || current.Kind == Kind.Line || current.Kind == Kind.Arrow;
            if (isShapeOrLine)
            {
                float dx = current.P2.X - current.P1.X;
                float dy = current.P2.Y - current.P1.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist < 4 * DpiScale)
                {
                    doc.Items.Remove(current);
                    doc.DropLastCheckpoint();
                }
            }

            current = null;
            currentAnchor = null;
            RedrawLayer();
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hoverPoint = null;
            Invalidate();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Z)
            {
                doc.Undo();
                current = null;
                currentAnchor = null;
                RedrawLayer();
                Invalidate();
                return;
            }

            if (e.KeyCode == Keys.Escape)
            {
                Close();
                return;
            }

            base.OnKeyDown(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);

            if (screenshot != null)
            {
                screenshot.Dispose();
            }
            if (layer != null)
            {
                layer.Dispose();
            }

            GC.Collect();
        }

        // 보낼 이미지 = 캡처 + 레이어(툴바·표시선 없음)의 지정 영역(없으면 전체). 편집 중 텍스트는 먼저 확정한다(IME 조합 포함). 호출자가 Dispose.
        public Bitmap RenderFinal()
        {
            editor.Commit();
            RedrawLayer();
            bool hasArea = crop.Width > 0 && crop.Height > 0; // 긋는 도중 단축키로 보내면 넓이 0일 수 있다.
            return Renderer.Compose(screenshot, layer, hasArea ? crop : new Rectangle(Point.Empty, screenshot.Size));
        }

        private void UpdateCursor(Point p)
        {
            int edges = toolbar.Tool == Tool.Crop && !crop.IsEmpty ? Geo.Edges(crop, p, 6f * DpiScale) : 0;
            if (!crop.IsEmpty && CropCloseRect().Contains(p)) Cursor = Cursors.Hand;
            else if (edges == 15) Cursor = Cursors.SizeAll;
            else if (edges == 1 || edges == 4) Cursor = Cursors.SizeWE;
            else if (edges == 2 || edges == 8) Cursor = Cursors.SizeNS;
            else if (edges == 3 || edges == 12) Cursor = Cursors.SizeNWSE;
            else if (edges != 0) Cursor = Cursors.SizeNESW;
            else Cursor = toolbar.Tool == Tool.Text ? Cursors.IBeam : Cursors.Cross;
        }

        // 영역 테두리 오른쪽 위 바깥(화면 위 끝이면 안쪽)의 X 버튼.
        private Rectangle CropCloseRect()
        {
            int s = (int)Math.Round(22 * DpiScale);
            return new Rectangle(crop.Right - s, Math.Max(0, crop.Top - s - (int)Math.Round(4 * DpiScale)), s, s);
        }

        // 보낼 영역: 흰 실선 위 검은 점선(어떤 화면 위에서도 보이게)과 X 버튼. 레이어(이미지)에는 안 들어간다.
        private void PaintCrop(Graphics g)
        {
            if (crop.IsEmpty) return;

            using (Pen under = new Pen(Color.White, 2f * DpiScale))
            using (Pen dash = new Pen(Color.Black, 2f * DpiScale))
            {
                dash.DashStyle = DashStyle.Dash;
                g.DrawRectangle(under, crop);
                g.DrawRectangle(dash, crop);
            }

            SmoothingMode old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle x = CropCloseRect();
            using (Brush b = new SolidBrush(Color.FromArgb(240, 32, 33, 36)))
            {
                g.FillEllipse(b, x);
            }
            Toolbar.DrawIcon(g, "close", RectangleF.Inflate(x, -x.Width / 5f, -x.Height / 5f), Color.White);
            g.SmoothingMode = old;
        }

        private void RedrawLayer()
        {
            using (Graphics g = Graphics.FromImage(layer))
            {
                g.Clear(Color.Transparent);
                Renderer.DrawItems(g, doc.Items, DpiScale);
            }
        }

        // 선·화살표 호버 시 앵커 테두리+최근접점, 드래그 중엔 앵커 테두리만, 지우개는 반지름 12 점선 원.
        // 레이어(이미지)에는 안 들어가고 화면에만 그린다.
        private void PaintIndicators(Graphics g)
        {
            if (hoverPoint == null) return;
            PointF hp = hoverPoint.Value;

            SmoothingMode old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if ((toolbar.Tool == Tool.Line || toolbar.Tool == Tool.Arrow) && current == null)
            {
                DrawObj anchor = Geo.FindAnchor(doc.Items, hp, DpiScale);
                if (anchor != null)
                {
                    PaintAnchorGlow(g, anchor);
                    PaintSnapDot(g, Geo.Nearest(Geo.Outline(anchor, DpiScale), hp));
                }
            }
            else if (current != null && currentAnchor != null)
            {
                PaintAnchorGlow(g, currentAnchor);
            }
            else if (toolbar.Tool == Tool.Eraser)
            {
                PaintEraserCircle(g, hp);
            }

            g.SmoothingMode = old;
        }

        private void PaintAnchorGlow(Graphics g, DrawObj anchor)
        {
            RectangleF box = Geo.Box(anchor, DpiScale);
            using (Pen pen = new Pen(Color.FromArgb(89, 0, 122, 255), 8f * DpiScale))
            {
                if (anchor.Kind == Kind.Ellipse)
                {
                    g.DrawEllipse(pen, box.X, box.Y, box.Width, box.Height);
                }
                else
                {
                    g.DrawRectangle(pen, box.X, box.Y, box.Width, box.Height);
                }
            }
        }

        private void PaintSnapDot(Graphics g, PointF p)
        {
            float r = 5f * DpiScale;
            using (Brush b = new SolidBrush(Color.FromArgb(0x00, 0x7A, 0xFF)))
            {
                g.FillEllipse(b, p.X - r, p.Y - r, r * 2, r * 2);
            }
        }

        // 편집 중인 텍스트 상자 테두리(지우개 원과 같은 회색 점선). 상자 안은 자식 창이라 바로 바깥에 그린다.
        private void PaintTextFrame(Graphics g, Rectangle box)
        {
            float w = 1f * DpiScale;
            RectangleF r = box;
            r.Inflate(w, w);
            using (Pen pen = new Pen(Color.Gray, w))
            {
                pen.DashStyle = DashStyle.Dash;
                g.DrawRectangle(pen, r.X, r.Y, r.Width, r.Height);
            }
        }

        private void PaintEraserCircle(Graphics g, PointF p)
        {
            float r = 12f * DpiScale;
            using (Pen pen = new Pen(Color.Gray, 1f * DpiScale))
            {
                pen.DashStyle = DashStyle.Dash;
                g.DrawEllipse(pen, p.X - r, p.Y - r, r * 2, r * 2);
            }
        }
    }
}
