using System;
using System.Collections.Generic;
using System.Drawing;

namespace ReferencePic.Tests
{
    public static class ToolbarTests
    {
        public static void Run()
        {
            Toolbar tb = new Toolbar();
            tb.Layout(new Rectangle(0, 0, 1920, 1080), 1f);

            T.Check(tb.Bounds.Bottom == 1080 - 16, "Bounds.Bottom == screen.Bottom - 16");

            int centerX = tb.Bounds.X + tb.Bounds.Width / 2;
            T.Check(Math.Abs(centerX - 960) <= 1, "Bounds 가로 중심이 960 근처(<=1px)");

            HitKind[] expected = new HitKind[]
            {
                HitKind.Tool, HitKind.ShapeMenu, HitKind.Tool, HitKind.Tool, HitKind.Tool,
                HitKind.ColorMenu, HitKind.Send, HitKind.Close
            };
            T.Check(tb.Items.Count == expected.Length, "초기 Items 개수 8");
            bool orderOk = tb.Items.Count == expected.Length;
            for (int i = 0; orderOk && i < expected.Length; i++)
            {
                if (tb.Items[i].Kind != expected[i]) orderOk = false;
            }
            T.Check(orderOk, "Items 순서: Tool(Pen), ShapeMenu, Tool(Text), Tool(Eraser), Tool(Crop), ColorMenu, Send, Close");
            T.Check(tb.Items[0].Tool == Tool.Pen, "Items[0]은 Pen 도구");
            T.Check(tb.Items[2].Tool == Tool.Text, "Items[2]는 Text 도구");
            T.Check(tb.Items[3].Tool == Tool.Eraser, "Items[3]은 Eraser 도구");
            T.Check(tb.Items[4].Tool == Tool.Crop, "Items[4]는 Crop 도구(지우개 오른쪽)");

            ToolbarItem shapeMenu = Find(tb.Items, HitKind.ShapeMenu);
            tb.Click(Center(shapeMenu.Rect));
            T.Check(tb.Tool == Tool.Ellipse, "ShapeMenu 클릭 -> Tool==LastShape(Ellipse)");
            T.Check(tb.Popup == PopupKind.Shape, "ShapeMenu 클릭 -> Popup==Shape");
            List<ToolbarItem> shapeItems = FindAll(tb.Items, HitKind.ShapeItem);
            T.Check(shapeItems.Count == 4, "Popup==Shape -> ShapeItem 4개");

            tb.Click(Center(shapeItems[1].Rect));
            T.Check(tb.Tool == Tool.Rect, "ShapeItem[1](네모) 클릭 -> Tool==Rect");
            T.Check(tb.LastShape == Tool.Rect, "ShapeItem[1] 클릭 -> LastShape==Rect");
            T.Check(tb.Popup == PopupKind.None, "ShapeItem 클릭 후 Popup==None");

            ToolbarItem colorMenu = Find(tb.Items, HitKind.ColorMenu);
            tb.Click(Center(colorMenu.Rect));
            T.Check(tb.Popup == PopupKind.Color, "ColorMenu 클릭 -> Popup==Color");
            List<ToolbarItem> colorItems = FindAll(tb.Items, HitKind.ColorItem);
            T.Check(colorItems.Count == 6, "Popup==Color -> ColorItem 6개");

            tb.Click(Center(colorItems[4].Rect));
            T.Check(tb.Color.ToArgb() == Color.FromArgb(0x00, 0x7A, 0xFF).ToArgb(), "ColorItem[4] 클릭 -> Color==#007AFF");
            T.Check(tb.Popup == PopupKind.None, "ColorItem 클릭 후 Popup==None");

            ToolbarItem textBtn = tb.Items[2];
            tb.Click(Center(textBtn.Rect));
            T.Check(tb.Tool == Tool.Text, "텍스트 버튼 클릭 -> Tool==Text");
            T.Check(tb.Popup == PopupKind.None, "텍스트 버튼 클릭 -> Popup==None");

            ToolbarItem sendBtn = Find(tb.Items, HitKind.Send);
            ToolbarItem sendClick = tb.Click(Center(sendBtn.Rect));
            T.Check(sendClick != null && sendClick.Kind == HitKind.Send, "Send 중심 클릭 -> 반환 Kind==Send");

            Toolbar tb2 = new Toolbar();
            tb2.Layout(new Rectangle(0, 0, 1920, 1080), 2f);
            T.Check(tb2.Bounds.Height == tb.Bounds.Height * 2, "scale 2 -> Bounds.Height가 scale 1의 2배");
        }

        private static Point Center(Rectangle r)
        {
            return new Point(r.X + r.Width / 2, r.Y + r.Height / 2);
        }

        private static ToolbarItem Find(List<ToolbarItem> items, HitKind kind)
        {
            foreach (ToolbarItem i in items)
            {
                if (i.Kind == kind) return i;
            }
            return null;
        }

        private static List<ToolbarItem> FindAll(List<ToolbarItem> items, HitKind kind)
        {
            List<ToolbarItem> result = new List<ToolbarItem>();
            foreach (ToolbarItem i in items)
            {
                if (i.Kind == kind) result.Add(i);
            }
            return result;
        }
    }
}
