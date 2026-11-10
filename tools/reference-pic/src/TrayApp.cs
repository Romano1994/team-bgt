using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace ReferencePic
{
    public class TrayApp : ApplicationContext
    {
        private const int HotkeyId = 1;
        private const string BalloonTitle = "reference-pic";

        private readonly NotifyIcon trayIcon;
        private readonly HotkeyWindow hotkeyWindow;
        private readonly Toolbar toolbar = new Toolbar(); // 앱 수명 동안 하나만 유지: 오버레이를 새로 열어도 색·도형을 기억한다.

        private Overlay overlay;
        private IntPtr foregroundBeforeOverlay; // 붙일 대상: 단축키 직전 마지막으로 쓰던 창

        public bool OverlayOpen { get { return overlay != null; } }

        public TrayApp()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("종료", null, OnExit);

            trayIcon = new NotifyIcon();
            trayIcon.Icon = SystemIcons.Application;
            trayIcon.Text = BalloonTitle;
            trayIcon.ContextMenuStrip = menu;
            trayIcon.Visible = true;

            hotkeyWindow = new HotkeyWindow();
            hotkeyWindow.HotkeyPressed += OnHotkeyPressed;

            bool registered = Native.RegisterHotKey(
                hotkeyWindow.Handle, HotkeyId,
                Native.MOD_WIN | Native.MOD_SHIFT | Native.MOD_NOREPEAT, (uint)'D');

            if (!registered)
            {
                trayIcon.ShowBalloonTip(3000, BalloonTitle,
                    "Win+Shift+D 단축키를 등록하지 못했어요. 다른 프로그램이 쓰고 있을 수 있어요.",
                    ToolTipIcon.Warning);
            }
        }

        private void OnHotkeyPressed(object sender, EventArgs e)
        {
            if (overlay != null)
            {
                Send(); // 오버레이가 열려 있을 때 같은 단축키는 "보내기".
                return;
            }

            foregroundBeforeOverlay = Native.GetForegroundWindow();

            Rectangle bounds = Screen.FromPoint(Cursor.Position).Bounds;
            Bitmap screenshot = Capture.Monitor(bounds);
            float scale = Native.ScaleAt(Cursor.Position);

            overlay = new Overlay(bounds, screenshot, scale, toolbar);
            overlay.FormClosed += OnOverlayClosed;
            overlay.SendRequested += delegate { Send(); };
            overlay.Show();
            overlay.Activate();
        }

        // 합성 → 클립보드 → 수정키 뗄 때까지 대기 → 오버레이 닫기 → 직전 창 활성화 확인 → Ctrl+V(IDE·터미널은 Alt+V).
        // 그 창을 활성화하지 못하면(닫힘 등) Ctrl+V를 보내지 않는다(엉뚱한 창에 붙이지 않게).
        private void Send()
        {
            using (Bitmap img = overlay.RenderFinal())
            {
                if (!Capture.SetClipboardImage(img))
                {
                    trayIcon.ShowBalloonTip(3000, BalloonTitle, "클립보드에 넣지 못했어요.", ToolTipIcon.Warning);
                    return;
                }
            }

            // 오버레이가 떠 있는 동안 기다려야 키 업이 우리 창으로 와서 SetForegroundWindow 권한을 잃지 않는다.
            bool released = PasteTarget.WaitForKeysReleased(Native.AnyModifierHeld, 2000, Thread.Sleep);
            overlay.Close();

            if (released && foregroundBeforeOverlay != IntPtr.Zero && Native.Activate(foregroundBeforeOverlay))
            {
                Native.SendPaste(PasteTarget.UsesAltV(Native.ProcessNameOf(foregroundBeforeOverlay)));
            }
            else
            {
                trayIcon.ShowBalloonTip(3000, BalloonTitle, "Ctrl+V로 붙여 넣으세요", ToolTipIcon.Info);
            }
        }

        private void OnOverlayClosed(object sender, FormClosedEventArgs e)
        {
            overlay.FormClosed -= OnOverlayClosed;
            overlay = null;
        }

        private void OnExit(object sender, EventArgs e)
        {
            ExitThread();
        }

        protected override void ExitThreadCore()
        {
            if (overlay != null)
            {
                overlay.Close();
            }

            Native.UnregisterHotKey(hotkeyWindow.Handle, HotkeyId);
            hotkeyWindow.DestroyHandle();

            trayIcon.Visible = false;
            trayIcon.Dispose();

            base.ExitThreadCore();
        }

        // Application.ThreadException / AppDomain.UnhandledException가 호출한다: 상주는 유지, 오버레이만 닫는다.
        public void HandleFatalException(Exception ex)
        {
            if (overlay != null)
            {
                overlay.Close();
            }

            trayIcon.ShowBalloonTip(3000, BalloonTitle, "예상치 못한 오류가 발생해 오버레이를 닫았어요.", ToolTipIcon.Error);
        }

        // 메시지만 받는 숨은 창: WM_HOTKEY 수신용. 이 앱은 UI 스레드에서만 동작하므로 스레드 안전 처리는 하지 않는다.
        private sealed class HotkeyWindow : NativeWindow
        {
            public event EventHandler HotkeyPressed;

            public HotkeyWindow()
            {
                CreateHandle(new CreateParams());
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == Native.WM_HOTKEY && HotkeyPressed != null)
                {
                    HotkeyPressed(this, EventArgs.Empty);
                }

                base.WndProc(ref m);
            }
        }
    }
}
