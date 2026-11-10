using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ReferencePic
{
    public static class Capture
    {
        // bounds는 물리 픽셀(Per-Monitor V2). 1:1로 그대로 복사한다.
        public static Bitmap Monitor(Rectangle bounds)
        {
            Bitmap bmp = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
            }
            return bmp;
        }

        // Clipboard.SetImage와 같되 재시도만 100ms 간격 5회. copy=true라 플러시되므로 호출 후 img를 Dispose해도 된다.
        public static bool SetClipboardImage(Image img)
        {
            try
            {
                Clipboard.SetDataObject(new DataObject(DataFormats.Bitmap, img), true, 5, 100);
                return true;
            }
            catch (ExternalException)
            {
                return false;
            }
        }
    }
}
