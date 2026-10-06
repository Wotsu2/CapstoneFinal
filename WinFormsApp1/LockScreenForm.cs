using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WinFormsApp1
{
    // "Freeze" mode: the student can still SEE their screen,
    // but keyboard and mouse input is swallowed until unlocked.
    public class LockScreenForm : Form
    {
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

        private readonly LowLevelProc _kbProc;
        private readonly LowLevelProc _mouseProc;
        private IntPtr _kbHook = IntPtr.Zero;
        private IntPtr _mouseHook = IntPtr.Zero;

        private readonly System.Windows.Forms.Timer _keepOnTop = new System.Windows.Forms.Timer { Interval = 1000 };
        private readonly System.Windows.Forms.Timer _failsafe;
        private bool _allowClose = false;

        public LockScreenForm(string message, int failsafeMinutes = 120)
        {
            _kbProc = HookCallback;
            _mouseProc = HookCallback;

            // Small banner at top-center instead of a full-screen cover
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            ShowInTaskbar = false;
            ControlBox = false;
            BackColor = Color.FromArgb(150, 20, 20);
            Opacity = 0.92;
            Size = new Size(560, 56);

            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(wa.Left + (wa.Width - Width) / 2, wa.Top + 8);

            Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "🔒  " + message,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            });

            _keepOnTop.Tick += (s, e) => { TopMost = true; };

            // Failsafe so nobody stays frozen forever if the professor app dies
            _failsafe = new System.Windows.Forms.Timer { Interval = Math.Max(1, failsafeMinutes) * 60 * 1000 };
            _failsafe.Tick += (s, e) => Unlock();
        }

        // Show the banner without stealing focus
        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_NOACTIVATE = 0x08000000;
                const int WS_EX_TOOLWINDOW = 0x00000080;
                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _keepOnTop.Start();
            _failsafe.Start();
            InstallHooks();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_allowClose) e.Cancel = true;
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            RemoveHooks();
            _keepOnTop.Stop(); _keepOnTop.Dispose();
            _failsafe.Stop(); _failsafe.Dispose();
            base.OnFormClosed(e);
        }

        public void Unlock()
        {
            _allowClose = true;
            RemoveHooks();   // input works again immediately
            if (!IsDisposed) Close();
        }

        private void InstallHooks()
        {
            try
            {
                using (var p = Process.GetCurrentProcess())
                using (var m = p.MainModule)
                {
                    IntPtr h = GetModuleHandle(m.ModuleName);
                    _kbHook = SetWindowsHookEx(WH_KEYBOARD_LL, _kbProc, h, 0);
                    _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, h, 0);
                }
            }
            catch (Exception ex) { Console.WriteLine("[Freeze] hook failed: " + ex.Message); }
        }

        private void RemoveHooks()
        {
            if (_kbHook != IntPtr.Zero) { UnhookWindowsHookEx(_kbHook); _kbHook = IntPtr.Zero; }
            if (_mouseHook != IntPtr.Zero) { UnhookWindowsHookEx(_mouseHook); _mouseHook = IntPtr.Zero; }
        }

        // Swallow every key press and every mouse move/click/scroll
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0) return (IntPtr)1;
            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }
    }
}