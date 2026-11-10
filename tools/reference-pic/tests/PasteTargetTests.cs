using System;

namespace ReferencePic.Tests
{
    public static class PasteTargetTests
    {
        public static void Run()
        {
            T.Check(PasteTarget.UsesAltV("Code") && PasteTarget.UsesAltV("idea64") && PasteTarget.UsesAltV("powershell"), "VS Code·IntelliJ·PowerShell 창은 Alt+V");
            T.Check(PasteTarget.UsesAltV("CODE"), "프로세스명 대소문자 무시");
            T.Check(!PasteTarget.UsesAltV("warp") && !PasteTarget.UsesAltV("ms-teams") && !PasteTarget.UsesAltV(""), "Warp·메신저·알 수 없음은 Ctrl+V");

            int calls = 0;
            Func<bool> threeTrueThenFalse = delegate
            {
                calls++;
                return calls <= 3;
            };
            int slept1 = 0;
            Action<int> fakeSleep1 = delegate(int ms) { slept1 += ms; };
            T.Check(PasteTarget.WaitForKeysReleased(threeTrueThenFalse, 2000, fakeSleep1) == true,
                "WaitForKeysReleased 3번 true 후 false면 true");

            int slept2 = 0;
            Action<int> fakeSleep2 = delegate(int ms) { slept2 += ms; };
            Func<bool> alwaysTrue = delegate { return true; };
            bool timedOut = PasteTarget.WaitForKeysReleased(alwaysTrue, 2000, fakeSleep2);
            T.Check(timedOut == false && slept2 >= 2000, "WaitForKeysReleased 계속 true면 false, 누적 sleep>=2000");
        }
    }
}
