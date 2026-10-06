using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class BroadcastViewerForm : Form
    {
        // =========================================================
        // LOW-LEVEL INPUT HOOKS — swallow keyboard + mouse
        // =========================================================
        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;

        private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        // Keep delegates as fields so GC doesn't collect them
        private LowLevelProc _kbProc;
        private LowLevelProc _mouseProc;
        private IntPtr _kbHook = IntPtr.Zero;
        private IntPtr _mouseHook = IntPtr.Zero;

        public BroadcastViewerForm()
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;

            // Install hooks as soon as the form loads
            this.Load += (s, e) => InstallHooks();
            this.FormClosing += (s, e) => RemoveHooks();
            this.FormClosed += (s, e) => RemoveHooks();
        }

        public PictureBox GetPictureBox() => pictureBoxBroadcast;

        private void InstallHooks()
        {
            try
            {
                _kbProc = HookCallback;
                _mouseProc = HookCallback;

                using (var p = Process.GetCurrentProcess())
                using (var m = p.MainModule)
                {
                    IntPtr h = GetModuleHandle(m.ModuleName);
                    _kbHook = SetWindowsHookEx(WH_KEYBOARD_LL, _kbProc, h, 0);
                    _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, h, 0);
                }

                Console.WriteLine($"[BroadcastViewer] KB hook: {_kbHook}, Mouse hook: {_mouseHook}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[BroadcastViewer] Hook install failed: " + ex.Message);
            }
        }

        private void RemoveHooks()
        {
            if (_kbHook != IntPtr.Zero) { UnhookWindowsHookEx(_kbHook); _kbHook = IntPtr.Zero; }
            if (_mouseHook != IntPtr.Zero) { UnhookWindowsHookEx(_mouseHook); _mouseHook = IntPtr.Zero; }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            // nCode >= 0 means it's a real input event -> swallow it
            if (nCode >= 0)
                return (IntPtr)1;

            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }
    }
}