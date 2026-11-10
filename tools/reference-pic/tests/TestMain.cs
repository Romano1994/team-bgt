using System;
using System.Collections.Generic;
using System.Drawing;

namespace ReferencePic.Tests
{
    public static class T
    {
        private static int pass;
        private static int fail;

        public static int PassCount { get { return pass; } }
        public static int FailCount { get { return fail; } }

        public static void Check(bool ok, string what)
        {
            if (ok)
            {
                pass++;
                Console.WriteLine("PASS " + what);
            }
            else
            {
                fail++;
                Console.WriteLine("FAIL " + what);
            }
        }

        public static void Near(PointF a, PointF b, float tol, string what)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            double d = Math.Sqrt(dx * dx + dy * dy);
            Check(d <= tol, what + " (got " + a.X + "," + a.Y + " want " + b.X + "," + b.Y + " d=" + d.ToString("0.00") + ")");
        }
    }

    public static class TestMain
    {
        [STAThread] // TextEditorTests의 RichTextBox(OLE)
        public static int Main(string[] args)
        {
            GeometryTests.Run();
            ModelTests.Run();
            TextRunsTests.Run();
            RendererTests.Run();
            PasteTargetTests.Run();
            ToolbarTests.Run();
            TextEditorTests.Run();
            NativeTests.Run();
            OverlayTests.Run();

            Console.WriteLine();
            Console.WriteLine(T.PassCount + " passed, " + T.FailCount + " failed");
            return T.FailCount > 0 ? 1 : 0;
        }
    }
}
