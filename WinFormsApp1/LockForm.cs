using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public class LockScreenForm : Form
    {
        // ---- low-level keyboard hook (blocks Win key, Alt+Tab, Ctrl+Esc, etc.) ----
        private const int WH_KEYBOARD_LL = 13;
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private readonly LowLevelKeyboardProc _hookProc;   // keep a reference so GC doesn't collect it
        private IntPtr _hookId = IntPtr.Zero;

        private readonly System.Windows.Forms.Timer _keepOnTop = new System.Windows.Forms.Timer { Interval = 500 };
        private readonly System.Windows.Forms.Timer _failsafe;
        private bool _allowClose = false;

        public LockScreenForm(string message, int failsafeMinutes = 120)
        {
            _hookProc = HookCallback;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = SystemInformation.VirtualScreen;      // covers all monitors
            TopMost = true;
            ShowInTaskbar = false;
            ControlBox = false;
            BackColor = Color.FromArgb(20, 20, 24);

            var lbl = new Label
            {
                Dock = DockStyle.Fill,
                Text = "🔒\r\n\r\n" + message,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 28F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            Controls.Add(lbl);

            _keepOnTop.Tick += (s, e) =>
            {
                TopMost = true;
                BringToFront();
                Activate();
            };

            // Failsafe: never stay locked forever if the professor's app dies
            _failsafe = new System.Windows.Forms.Timer { Interval = Math.Max(1, failsafeMinutes) * 60 * 1000 };
            _failsafe.Tick += (s, e) => Unlock();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Activate();
            _keepOnTop.Start();
            _failsafe.Start();
            InstallHook();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_allowClose) e.Cancel = true;   // blocks Alt+F4
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            RemoveHook();
            _keepOnTop.Stop(); _keepOnTop.Dispose();
            _failsafe.Stop(); _failsafe.Dispose();
            base.OnFormClosed(e);
        }

        public void Unlock()
        {
            _allowClose = true;
            RemoveHook();
            if (!IsDisposed) Close();
        }

        private void InstallHook()
        {
            try
            {
                using (var p = Process.GetCurrentProcess())
                using (var m = p.MainModule)
                    _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc, GetModuleHandle(m.ModuleName), 0);
            }
            catch (Exception ex) { Console.WriteLine("[LockScreen] hook failed: " + ex.Message); }
        }

        private void RemoveHook()
        {
            if (_hookId != IntPtr.Zero) { UnhookWindowsHookEx(_hookId); _hookId = IntPtr.Zero; }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0) return (IntPtr)1;   // swallow every key while locked
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }


    }
}