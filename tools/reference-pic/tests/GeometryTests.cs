using System.Collections.Generic;
using System.Drawing;

namespace ReferencePic.Tests
{
    public static class GeometryTests
    {
        public static void Run()
        {
            // Edges: 영역 (100,100,200,100), tol 6
            Rectangle area = new Rectangle(100, 100, 200, 100);
            T.Check(Geo.Edges(area, new Point(103, 150), 6f) == 1, "Edges 왼쪽 변");
            T.Check(Geo.Edges(area, new Point(305, 150), 6f) == 4, "Edges 오른쪽 변(바깥 5px)");
            T.Check(Geo.Edges(area, new Point(200, 98), 6f) == 2, "Edges 위 변");
            T.Check(Geo.Edges(area, new Point(298, 202), 6f) == (4 | 8), "Edges 오른쪽 아래 모서리");
            T.Check(Geo.Edges(area, new Point(200, 150), 6f) == 15, "Edges 안쪽 -> 15(이동)");
            T.Check(Geo.Edges(area, new Point(200, 210), 6f) == 0, "Edges 바깥 -> 0");

            // 네모 P1(100,100) P2(300,200)
            DrawObj rect = new DrawObj { Kind = Kind.Rect, P1 = new PointF(100, 100), P2 = new PointF(300, 200) };
            List<DrawObj> rectList = new List<DrawObj> { rect };

            T.Near(Geo.SnapStart(rectList, new PointF(152, 90), 1f), new PointF(152, 100), 2.1f,
                "SnapStart 위 테두리 10px 바깥");

            T.Near(Geo.SnapStart(rectList, new PointF(200, 120), 1f), new PointF(200, 100), 2.1f,
                "SnapStart 안쪽 -> 가장 가까운 위 테두리");

            PointF outside = Geo.SnapStart(rectList, new PointF(152, 70), 1f);
            T.Check(outside.X == 152f && outside.Y == 70f, "SnapStart 30px 바깥 -> 그대로");

            T.Near(Geo.SnapStart(rectList, new PointF(152, 70), 2f), new PointF(152, 100), 2.1f,
                "SnapStart scale 2 스냅거리 36");

            // 동그라미 P1(0,0) P2(200,100)
            DrawObj ellipse = new DrawObj { Kind = Kind.Ellipse, P1 = new PointF(0, 0), P2 = new PointF(200, 100) };
            List<DrawObj> ellipseList = new List<DrawObj> { ellipse };
            T.Near(Geo.SnapStart(ellipseList, new PointF(100, -10), 1f), new PointF(100, 0), 3f,
                "SnapStart 동그라미 위쪽");

            // 겹친 네모 두 개 -> 목록 뒤(위)에 있는 것이 앵커
            DrawObj rectA = new DrawObj { Kind = Kind.Rect, P1 = new PointF(0, 0), P2 = new PointF(100, 100) };
            DrawObj rectB = new DrawObj { Kind = Kind.Rect, P1 = new PointF(50, 50), P2 = new PointF(150, 150) };
            List<DrawObj> overlap = new List<DrawObj> { rectA, rectB };
            T.Check(Geo.FindAnchor(overlap, new PointF(75, 75), 1f) == rectB,
                "FindAnchor 겹친 네모 -> 목록 뒤(위)쪽이 앵커");

            // 텍스트 Origin(50,50) TextSize(100,24) -> Box (46,46,108,32)
            DrawObj text = new DrawObj { Kind = Kind.Text, Origin = new PointF(50, 50), TextSize = new SizeF(100, 24) };
            RectangleF box = Geo.Box(text, 1f);
            T.Check(box.X == 46f && box.Y == 46f && box.Width == 108f && box.Height == 32f,
                "텍스트 Box 계산");

            List<DrawObj> textList = new List<DrawObj> { text };
            T.Near(Geo.SnapStart(textList, new PointF(60, 40), 1f), new PointF(60, 46), 2.1f,
                "SnapStart 텍스트 박스");

            // 색연필 획은 앵커가 아님
            DrawObj pen = new DrawObj { Kind = Kind.Pen, Points = new List<PointF> { new PointF(10, 10), new PointF(20, 20) } };
            List<DrawObj> penList = new List<DrawObj> { pen };
            T.Check(Geo.FindAnchor(penList, new PointF(15, 15), 1f) == null,
                "FindAnchor 색연필은 앵커 아님");

            // Hidden 텍스트는 앵커가 아님 (박스 안의 점이라도)
            DrawObj hiddenText = new DrawObj { Kind = Kind.Text, Origin = new PointF(50, 50), TextSize = new SizeF(100, 24), Hidden = true };
            List<DrawObj> hiddenList = new List<DrawObj> { hiddenText };
            T.Check(Geo.FindAnchor(hiddenList, new PointF(100, 60), 1f) == null,
                "FindAnchor Hidden 텍스트는 앵커 아님");
        }
    }
}
