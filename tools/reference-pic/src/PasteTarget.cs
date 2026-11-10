using System;

namespace ReferencePic
{
    // 붙일 대상은 단축키 직전 포그라운드 창 그대로다(TrayApp). 여기선 보낼 키와 보내기 전 수정키 대기를 정한다.
    public static class PasteTarget
    {
        // Claude Code(Windows)의 이미지 붙여넣기 키는 Alt+V. 이 앱들은 Ctrl+V를 가로채 글자만 붙이므로 Alt+V를 보낸다.
        // Warp는 Ctrl+V가 Claude Code까지 가므로(실측) 빠져 있고, 메신저 등 나머지는 Ctrl+V.
        private static readonly string[] AltVApps = { "Code", "idea64", "WindowsTerminal", "powershell", "pwsh", "cmd" };

        public static bool UsesAltV(string processName)
        {
            foreach (string a in AltVApps)
            {
                if (string.Equals(processName, a, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        public static bool WaitForKeysReleased(Func<bool> anyHeld, int timeoutMs, Action<int> sleep)
        {
            int elapsed = 0;
            while (anyHeld())
            {
                if (elapsed >= timeoutMs) return false;
                sleep(20);
                elapsed += 20;
            }
            return true;
        }
    }
}
