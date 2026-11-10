using System;
using System.Runtime.InteropServices;

namespace ReferencePic.Tests
{
    public static class NativeTests
    {
        public static void Run()
        {
            // 크기가 틀리면 SendInput이 아무 일도 안 하고 0을 돌려준다.
            int size = Marshal.SizeOf(typeof(Native.INPUT));
            T.Check(size == (IntPtr.Size == 8 ? 40 : 28), "INPUT 크기 x64 40 / x86 28 (got " + size + ", IntPtr " + IntPtr.Size + ")");
        }
    }
}
