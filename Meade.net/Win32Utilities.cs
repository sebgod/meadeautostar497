using System;
using System.Runtime.InteropServices;
using System.Text;
// ReSharper disable UnusedMember.Global

namespace ASCOM.Meade.net
{
    internal static class Win32Utilities
    {
        //Win32 API calls necesary to raise an unowned processs main window

        [DllImport("user32.dll")]

        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        // ReSharper disable once UnusedMember.Local
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, Int32 nMaxCount);

        [DllImport("user32.dll")]
        // ReSharper disable once UnusedMember.Local
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, ref Int32 lpdwProcessId);

        [DllImport("User32.dll")]
        public static extern IntPtr GetParent(IntPtr hWnd);

        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Local
        private const int SW_HIDE = 0;
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once IdentifierTypo
        private const int SW_SHOWNORMAL = 1;
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Local
        private const int SW_NORMAL = 1;
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once IdentifierTypo
        private const int SW_SHOWMINIMIZED = 2;
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once IdentifierTypo
        private const int SW_SHOWMAXIMIZED = 3;
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Local
        private const int SW_MAXIMIZE = 3;
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once IdentifierTypo
        private const int SW_SHOWNOACTIVATE = 4;
        // ReSharper disable once InconsistentNaming
        private const int SW_SHOW = 5;
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Local
        private const int SW_MINIMIZE = 6;
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once IdentifierTypo
        private const int SW_SHOWMINNOACTIVE = 7;
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once IdentifierTypo
        private const int SW_SHOWNA = 8;
        // ReSharper disable once InconsistentNaming
        private const int SW_RESTORE = 9;
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once IdentifierTypo
        private const int SW_SHOWDEFAULT = 10;
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Local
        private const int SW_MAX = 10;

        // ReSharper disable once InconsistentNaming
        // ReSharper disable once IdentifierTypo
        private const uint SPI_GETFOREGROUNDLOCKTIMEOUT = 0x2000;
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once IdentifierTypo
        private const uint SPI_SETFOREGROUNDLOCKTIMEOUT = 0x2001;

        // ReSharper disable once InconsistentNaming
        // ReSharper disable once IdentifierTypo
        private const int SPIF_SENDCHANGE = 0x2;


        public static void BringWindowToFront(IntPtr hWnd)
        {
            if (IsIconic(hWnd))
                ShowWindowAsync(hWnd, SW_RESTORE);

            ShowWindowAsync(hWnd, SW_SHOW);

            SetForegroundWindow(hWnd);

            // Code from Karl E. Peterson, www.mvps.org/vb/sample.htm
            // Converted to Delphi by Ray Lischner
            // Published in The Delphi Magazine 55, page 16
            // Converted to C# by Kevin Gale
            IntPtr foregroundWindow = GetForegroundWindow();
            IntPtr dummy = IntPtr.Zero;

            uint foregroundThreadId = GetWindowThreadProcessId(foregroundWindow, dummy);

            uint thisThreadId = GetWindowThreadProcessId(hWnd, dummy);

            if (AttachThreadInput(thisThreadId, foregroundThreadId, true))
            {
                BringWindowToTop(hWnd); // IE 5.5 related hack
                SetForegroundWindow(hWnd);
                AttachThreadInput(thisThreadId, foregroundThreadId, false);
            }

            if (GetForegroundWindow() != hWnd)
            {
                // Code by Daniel P. Stasinski
                // Converted to C# by Kevin Gale
                IntPtr timeout = IntPtr.Zero;
                SystemParametersInfo(SPI_GETFOREGROUNDLOCKTIMEOUT, 0, timeout, 0);
                SystemParametersInfo(SPI_SETFOREGROUNDLOCKTIMEOUT, 0, dummy, SPIF_SENDCHANGE);
                BringWindowToTop(hWnd); // IE 5.5 related hack
                SetForegroundWindow(hWnd);
                SystemParametersInfo(SPI_SETFOREGROUNDLOCKTIMEOUT, 0, timeout, SPIF_SENDCHANGE);
            }
        }
    }
}
