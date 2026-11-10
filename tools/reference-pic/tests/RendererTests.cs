using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace ReferencePic.Tests
{
    public static class RendererTests
    {
        public static void Run()
        {
            // 빨간 색연필 (20,100)->(180,100), 그 위에 지우개 (100,60)->(100,140)
            Bitmap layer = new Bitmap(200, 200, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(layer))
            {
                DrawObj pen = new DrawObj
                {
                    Kind = Kind.Pen,
                    Color = Style.Colors[0],
                    Points = new List<PointF> { new PointF(20, 100), new PointF(180, 100) }
                };
                DrawObj erase = new DrawObj
                {
                    Kind = Kind.Erase,
                    Points = new List<PointF> { new PointF(100, 60), new PointF(100, 140) }
                };
                Renderer.DrawItems(g, new List<DrawObj> { pen, erase }, 1f);
            }

            T.Check(layer.GetPixel(100, 100).A == 0, "지우개 지나간 자리는 완전 투명");
            T.Check(ColorClose(layer.GetPixel(50, 100), Style.Colors[0]), "지우개 밖 색연필 자리는 불투명 #FF3B30");

            // 합성: 파란 배경 + 위 레이어
            Bitmap blue = new Bitmap(200, 200, PixelFormat.Format32bppArgb);
            using (Graphics gb = Graphics.FromImage(blue))
            {
                gb.Clear(Color.Blue);
            }
            Bitmap composed = Renderer.Compose(blue, layer, new Rectangle(0, 0, 200, 200));
            T.Check(ColorClose(composed.GetPixel(100, 100), Color.Blue), "합성: 지워진 자리는 배경색 그대로");
            T.Check(ColorClose(composed.GetPixel(50, 100), Style.Colors[0]), "합성: 색연필 자리는 레이어 색");

            Bitmap part = Renderer.Compose(blue, layer, new Rectangle(40, 90, 30, 20));
            T.Check(part.Size == new Size(30, 20), "영역 합성: 결과 크기 == 영역 크기");
            T.Check(ColorClose(part.GetPixel(10, 10), Style.Colors[0]), "영역 합성: (10,10) == 원본 (50,100) 색연필");

            // Hidden 항목은 그리지 않음
            Bitmap hiddenLayer = new Bitmap(200, 200, PixelFormat.Format32bppArgb);
            using (Graphics gh = Graphics.FromImage(hiddenLayer))
            {
                DrawObj hiddenRect = new DrawObj
                {
                    Kind = Kind.Rect,
                    Hidden = true,
                    Color = Style.Colors[0],
                    P1 = new PointF(10, 10),
                    P2 = new PointF(50, 50)
                };
                Renderer.DrawItems(gh, new List<DrawObj> { hiddenRect }, 1f);
            }
            T.Check(hiddenLayer.GetPixel(10, 30).A == 0, "Hidden 네모는 그리지 않음(테두리 픽셀 A==0)");

            // 화살표 P1(20,20) P2(180,20): (178,20) 불투명 (촉 채움)
            Bitmap arrowLayer = new Bitmap(200, 200, PixelFormat.Format32bppArgb);
            using (Graphics ga = Graphics.FromImage(arrowLayer))
            {
                DrawObj arrow = new DrawObj
                {
                    Kind = Kind.Arrow,
                    Color = Style.Colors[0],
                    P1 = new PointF(20, 20),
                    P2 = new PointF(180, 20)
                };
                Renderer.DrawItems(ga, new List<DrawObj> { arrow }, 1f);
            }
            T.Check(arrowLayer.GetPixel(178, 20).A == 255, "화살촉 채움 안쪽은 불투명");

            // MeasureLines(2줄) 높이 == 48 (scale 1), scale 2면 96
            List<List<Run>> twoLines = new List<List<Run>>
            {
                new List<Run> { MakeRun("hello", Color.Black) },
                new List<Run> { MakeRun("world", Color.Black) }
            };
            SizeF m1 = Renderer.MeasureLines(twoLines, 1f);
            T.Check(Math.Abs(m1.Height - 48f) < 0.01f, "MeasureLines 높이 scale 1 -> 48");
            SizeF m2 = Renderer.MeasureLines(twoLines, 2f);
            T.Check(Math.Abs(m2.Height - 96f) < 0.01f, "MeasureLines 높이 scale 2 -> 96");
        }

        private static Run MakeRun(string text, Color color)
        {
            Run r = new Run();
            r.Text = text;
            r.Color = color;
            return r;
        }

        private static bool ColorClose(Color a, Color b)
        {
            return a.A == 255 && b.A == 255
                && Math.Abs(a.R - b.R) <= 2
                && Math.Abs(a.G - b.G) <= 2
                && Math.Abs(a.B - b.B) <= 2;
        }
    }
}
