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

        // =========================================================
        // STATIC hooks — survive even if the form is disposed
        // =========================================================
        private static LowLevelProc _kbProc;
        private static LowLevelProc _mouseProc;
        private static IntPtr _kbHook = IntPtr.Zero;
        private static IntPtr _mouseHook = IntPtr.Zero;
        private static readonly object _hookLock = new object();

        private System.Windows.Forms.Timer _failsafeTimer;

        public BroadcastViewerForm()
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;

            // Kill any leftover hooks from a previous instance
            ForceRemoveHooks();

            this.Load += (s, e) => InstallHooks();
            this.FormClosing += (s, e) => ForceRemoveHooks();
            this.FormClosed += (s, e) => ForceRemoveHooks();
            this.Disposed += (s, e) => ForceRemoveHooks();

            // Failsafe: auto-remove after 4 hours in case something goes wrong
            _failsafeTimer = new System.Windows.Forms.Timer { Interval = 4 * 60 * 60 * 1000 };
            _failsafeTimer.Tick += (s, e) => { _failsafeTimer.Stop(); ForceRemoveHooks(); };
        }

        public PictureBox GetPictureBox() => pictureBoxBroadcast;

        private void InstallHooks()
        {
            lock (_hookLock)
            {
                // Already installed? Skip.
                if (_kbHook != IntPtr.Zero && _mouseHook != IntPtr.Zero) return;

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

                    Console.WriteLine($"[BroadcastViewer] Hooks installed — KB: {_kbHook}, Mouse: {_mouseHook}");
                    _failsafeTimer.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[BroadcastViewer] Hook install failed: " + ex.Message);
                }
            }
        }

        // Public static so ANY code can force-release the hooks
        public static void ForceRemoveHooks()
        {
            lock (_hookLock)
            {
                try
                {
                    if (_kbHook != IntPtr.Zero)
                    {
                        UnhookWindowsHookEx(_kbHook);
                        Console.WriteLine("[BroadcastViewer] KB hook removed.");
                        _kbHook = IntPtr.Zero;
                    }

                    if (_mouseHook != IntPtr.Zero)
                    {
                        UnhookWindowsHookEx(_mouseHook);
                        Console.WriteLine("[BroadcastViewer] Mouse hook removed.");
                        _mouseHook = IntPtr.Zero;
                    }

                    _kbProc = null;
                    _mouseProc = null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[BroadcastViewer] ForceRemoveHooks error: " + ex.Message);
                }
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
                return (IntPtr)1;

            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { _failsafeTimer?.Stop(); } catch { }
                try { _failsafeTimer?.Dispose(); } catch { }
                _failsafeTimer = null;
            }
            ForceRemoveHooks();
            base.Dispose(disposing);
        }
    }
}