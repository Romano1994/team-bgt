using System;
using System.Threading;
using System.Windows.Forms;

namespace ReferencePic
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            bool createdNew;
            using (Mutex mutex = new Mutex(true, "Local\\reference-pic", out createdNew))
            {
                if (!createdNew)
                {
                    return;
                }

                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.EnableVisualStyles();

                // TrayApp 생성자(핫키 등록 등) 자체가 던질 수 있으므로, new TrayApp()보다 먼저 건다.
                TrayApp app = null;

                Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e)
                {
                    if (app != null) app.HandleFatalException(e.Exception);
                };
                AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
                {
                    if (app != null) app.HandleFatalException(e.ExceptionObject as Exception);
                };

                app = new TrayApp();
                Application.Run(app);
            }
        }
    }
}
