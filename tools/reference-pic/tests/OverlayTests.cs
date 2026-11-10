using System.Drawing;
using System.Windows.Forms;

namespace ReferencePic.Tests
{
    // 보내기·닫기는 같은 버튼 위에서 뗄 때만 실행(누른 채 Ctrl+V가 나가지 않게). 핸들 없는 폼이라 화면에 안 뜬다.
    public static class OverlayTests
    {
        public static void Run()
        {
            Toolbar tb = new Toolbar();
            int sends = 0;
            using (TestOverlay o = new TestOverlay(tb))
            {
                o.SendRequested += delegate { sends++; };
                Point send = Center(Find(tb, HitKind.Send));

                o.Down(send);
                T.Check(sends == 0, "보내기: 누르기만 하면 안 보냄");
                o.Up(send);
                T.Check(sends == 1, "보내기: 같은 버튼 위에서 떼면 보냄");

                o.Down(send);
                o.Up(new Point(5, 5));
                T.Check(sends == 1, "보내기: 버튼 밖에서 떼면 안 보냄");

                o.Down(send); // 이 누름의 떼기를 못 받음(캡처 잃음) → 캔버스 누름 → 버튼 위에서 떼기
                o.Down(new Point(5, 5));
                o.Up(send);
                T.Check(sends == 1, "보내기: 떼기 없이 다른 곳을 누르면 그 누름은 취소");

                Point close = Center(Find(tb, HitKind.Close));
                o.Down(close);
                T.Check(!o.IsDisposed, "닫기: 누르기만 하면 안 닫음");
                o.Up(close);
                T.Check(o.IsDisposed, "닫기: 같은 버튼 위에서 떼면 닫음");
            }

            // 범위 지정: 긋기 -> 오른쪽 변 늘리기 -> 오른쪽 위 X로 제거
            using (TestOverlay o = new TestOverlay(tb))
            {
                o.Down(Center(FindTool(tb, Tool.Crop)));
                T.Check(tb.Tool == Tool.Crop, "범위: 지우개 오른쪽 버튼 -> Crop 도구");

                o.Drag(new Point(300, 200), new Point(100, 100)); // 거꾸로 그어도 정규화
                using (Bitmap img = o.RenderFinal()) T.Check(img.Size == new Size(200, 100), "범위: 보낼 이미지는 지정 영역 크기");

                o.Drag(new Point(300, 150), new Point(400, 150));
                using (Bitmap img = o.RenderFinal()) T.Check(img.Size == new Size(300, 100), "범위: 오른쪽 변을 끌면 넓어짐");

                o.Drag(new Point(600, 400), new Point(700, 500));
                using (Bitmap img = o.RenderFinal()) T.Check(img.Size == new Size(300, 100), "범위: 영역 밖 드래그는 새로 긋지 않음");

                tb.Tool = Tool.Pen;
                o.Down(new Point(390, 85)); // X: 영역 (100,100)-(400,200)의 오른쪽 위 바깥 22px
                o.Up(new Point(390, 85));
                using (Bitmap img = o.RenderFinal()) T.Check(img.Size == new Size(1280, 720), "범위: X로 제거하면 화면 전체");
                T.Check(tb.Tool == Tool.Crop, "범위: X로 제거하면 바로 다시 그을 수 있게 Crop 도구");

                o.Down(new Point(100, 100));
                o.Hover(new Point(300, 100)); // 떼기 전(높이 0)에 보내기
                using (Bitmap img = o.RenderFinal()) T.Check(img.Size == new Size(1280, 720), "범위: 긋는 중 넓이 0이면 화면 전체");
                o.Up(new Point(300, 100));
            }
        }

        private static ToolbarItem FindTool(Toolbar tb, Tool tool)
        {
            foreach (ToolbarItem item in tb.Items)
            {
                if (item.Kind == HitKind.Tool && item.Tool == tool) return item;
            }
            return null;
        }

        private static ToolbarItem Find(Toolbar tb, HitKind kind)
        {
            foreach (ToolbarItem item in tb.Items)
            {
                if (item.Kind == kind) return item;
            }
            return null;
        }

        private static Point Center(ToolbarItem item)
        {
            return new Point(item.Rect.X + item.Rect.Width / 2, item.Rect.Y + item.Rect.Height / 2);
        }

        private sealed class TestOverlay : Overlay
        {
            public TestOverlay(Toolbar tb) : base(new Rectangle(0, 0, 1280, 720), new Bitmap(1280, 720), 1f, tb) { }

            public void Down(Point p) { OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, p.X, p.Y, 0)); }

            public void Up(Point p) { OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, p.X, p.Y, 0)); }

            public void Hover(Point p) { OnMouseMove(new MouseEventArgs(MouseButtons.Left, 0, p.X, p.Y, 0)); }

            public void Drag(Point from, Point to)
            {
                Down(from);
                Hover(to);
                Up(to);
            }
        }
    }
}
