using System;
using System.Runtime.InteropServices;

namespace EnhancedTrackBar
{
    static class WParam
    {
        public static IntPtr FromBool(bool value) => new IntPtr(value ? 1U : 0U);
    }
    static class LParam
    {
        public static (ushort lo, short hi) GetLoHi(IntPtr lparam)
        {
            return ((ushort)(lparam.ToInt64() & 0xFFFF), (short)((lparam.ToInt64() & 0xFFFF0000) >> 16));
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct NMHDR
    {
        public IntPtr hwndFrom;
        public UIntPtr idFrom;
        public uint code;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    enum GetWindowLongItemIndex
    {
        GWL_EXSTYLE = -20,
        GWL_STYLE = -16,
    }

    static class WindowMessages
    {
        public const int
            WM_NOTIFY = 0x004E,
            WM_HSCROLL = 0x114,
            WM_VSCROLL = 0x115,
            WM_MOUSEWHEEL = 0x020A,
            WM_MOUSEHWHEEL = 0x020E,
            WM_USER = 0x400,
            WM_REFLECT = WM_USER + 0x1C00,
            WM_REFLECT_NOTIFY = WM_REFLECT + WM_NOTIFY
            ;
    }

    static class NativeMethods

    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetWindowLong(HandleRef hWnd, GetWindowLongItemIndex nIndex);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int SetWindowLong(HandleRef hWnd, GetWindowLongItemIndex nIndex, int dwNewLog);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr GetWindowLongPtr(HandleRef hWnd, GetWindowLongItemIndex nIndex);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr SetWindowLongPtr(HandleRef hWnd, GetWindowLongItemIndex nIndex, IntPtr dwNewLog);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(HandleRef hWnd, int msg, [MarshalAs(UnmanagedType.SysUInt)] IntPtr wparam, IntPtr lparam);


        public static T GetLParam<T>(in this System.Windows.Forms.Message m) where T : struct
        {
            return Marshal.PtrToStructure<T>(m.LParam);
        }

    }
}
