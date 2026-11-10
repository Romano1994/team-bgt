using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;

namespace ReferencePic
{
    public static class Native
    {
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;
        public const uint MOD_NOREPEAT = 0x4000;
        public const int WM_HOTKEY = 0x0312;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        // SendInput용. 공용체에 가장 큰 MOUSEINPUT을 넣어야 크기가 x64 40 / x86 28바이트로 맞는다(키보드만 넣으면 x64 32).
        [StructLayout(LayoutKind.Sequential)]
        internal struct INPUT
        {
            public uint type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private const uint MONITOR_DEFAULTTONEAREST = 2;
        private const int MDT_EFFECTIVE_DPI = 0;
        private const int NI_COMPOSITIONSTR = 0x15;
        private const int CPS_COMPLETE = 0x1;
        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x2;
        private const ushort VK_CONTROL = 0x11;
        private const ushort VK_MENU = 0x12; // Alt
        private const ushort VK_V = 0x56;
        private const uint MAPVK_VK_TO_VSC = 0;

        // L/R Win, Shift, Ctrl, Alt
        private static readonly int[] Modifiers = { 0x5B, 0x5C, 0x10, 0x11, 0x12 };

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll", EntryPoint = "GetForegroundWindow")]
        private static extern IntPtr GetForegroundWindowNative();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", EntryPoint = "RegisterHotKey", SetLastError = true)]
        private static extern bool RegisterHotKeyNative(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", EntryPoint = "UnregisterHotKey")]
        private static extern bool UnregisterHotKeyNative(IntPtr hWnd, int id);

        [DllImport("user32.dll", EntryPoint = "MonitorFromPoint")]
        private static extern IntPtr MonitorFromPointNative(POINT pt, uint dwFlags);

        [DllImport("shcore.dll", EntryPoint = "GetDpiForMonitor")]
        private static extern int GetDpiForMonitorNative(IntPtr hMonitor, int dpiType, out uint dpiX, out uint dpiY);

        [DllImport("imm32.dll")]
        private static extern IntPtr ImmGetContext(IntPtr hWnd);

        [DllImport("imm32.dll")]
        private static extern bool ImmNotifyIME(IntPtr hIMC, int dwAction, int dwIndex, int dwValue);

        [DllImport("imm32.dll")]
        private static extern bool ImmReleaseContext(IntPtr hWnd, IntPtr hIMC);

        // 조합 중인 한글을 확정한다. 이 PC 실측: 호출이 끝났을 때 이미 조합이 끝나 있고 글자는 창 텍스트에 들어가 있다(뒤늦게 오는 메시지 없음).
        public static void CompleteComposition(IntPtr hwnd)
        {
            IntPtr himc = ImmGetContext(hwnd);
            if (himc == IntPtr.Zero) return;
            ImmNotifyIME(himc, NI_COMPOSITIONSTR, CPS_COMPLETE, 0);
            ImmReleaseContext(hwnd, himc);
        }

        public static IntPtr GetForegroundWindow()
        {
            return GetForegroundWindowNative();
        }

        public static bool AnyModifierHeld()
        {
            foreach (int vk in Modifiers)
            {
                if ((GetAsyncKeyState(vk) & 0x8000) != 0) return true;
            }
            return false;
        }

        // 포그라운드 전환은 비동기일 수 있어 최대 300ms 동안 실제로 바뀌었는지 확인한다.
        public static bool Activate(IntPtr hwnd)
        {
            SetForegroundWindow(hwnd);
            for (int waited = 0; GetForegroundWindowNative() != hwnd; waited += 20)
            {
                if (waited >= 300) return false;
                Thread.Sleep(20);
            }
            return true;
        }

        // Ctrl+V, altV면 Alt+V.
        public static void SendPaste(bool altV)
        {
            ushort mod = altV ? VK_MENU : VK_CONTROL;
            INPUT[] inputs =
            {
                Key(mod, 0), Key(VK_V, 0), Key(VK_V, KEYEVENTF_KEYUP), Key(mod, KEYEVENTF_KEYUP)
            };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        // 실측 약 3ms. 콘솔 창은 그 안의 셸(powershell 등) 이름이 나온다.
        public static string ProcessNameOf(IntPtr hwnd)
        {
            uint pid;
            GetWindowThreadProcessId(hwnd, out pid);
            try
            {
                return Process.GetProcessById((int)pid).ProcessName;
            }
            catch
            {
                return ""; // 그사이 프로세스가 끝남
            }
        }

        private static INPUT Key(ushort vk, uint flags)
        {
            INPUT input = new INPUT();
            input.type = INPUT_KEYBOARD;
            input.u.ki.wVk = vk;
            input.u.ki.wScan = (ushort)MapVirtualKey(vk, MAPVK_VK_TO_VSC); // Warp(winit) 등은 스캔코드를 읽는다.
            input.u.ki.dwFlags = flags;
            return input;
        }

        public static bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint vk)
        {
            return RegisterHotKeyNative(hWnd, id, modifiers, vk);
        }

        public static bool UnregisterHotKey(IntPtr hWnd, int id)
        {
            return UnregisterHotKeyNative(hWnd, id);
        }

        // 마우스 아래 모니터의 DPI 배율(96 DPI = 1.0f).
        public static float ScaleAt(Point point)
        {
            POINT p;
            p.X = point.X;
            p.Y = point.Y;
            IntPtr hMonitor = MonitorFromPointNative(p, MONITOR_DEFAULTTONEAREST);
            uint dpiX, dpiY;
            GetDpiForMonitorNative(hMonitor, MDT_EFFECTIVE_DPI, out dpiX, out dpiY);
            return dpiX / 96f;
        }
    }
}
