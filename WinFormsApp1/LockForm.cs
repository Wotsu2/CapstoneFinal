using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public class LockForm : Form
    {
        private Panel panelHeader;
        private Label lblTitle;
        private Panel panelFooter;
        private Button btnLock;
        private Button btnUnlock;
        private CheckBox chkSelectAll;
        private FlowLayoutPanel flowLayoutPanelPCs;

        private List<CheckBox> pcToggles = new List<CheckBox>();
        private List<string> _onlinePCs;
        private int _commandPort;

        public LockForm(List<string> onlinePCs, int commandPort)
        {
            _onlinePCs = onlinePCs ?? new List<string>();
            _commandPort = commandPort;

            InitializeCustomUI();
            this.Load += (s, e) => LoadPCs();
        }

        private void InitializeCustomUI()
        {
            this.Text = "Remote Lock Control";
            this.Size = new Size(520, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(45, 45, 48);

            // ============ Header ============
            this.panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(30, 30, 30)
            };

            this.lblTitle = new Label
            {
                Text = "🔒 Lock Workstations",
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 15)
            };
            this.panelHeader.Controls.Add(this.lblTitle);

            // ============ Footer ============
            this.panelFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                BackColor = Color.FromArgb(30, 30, 30)
            };

            this.chkSelectAll = new CheckBox
            {
                Text = "Select All PCs",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(25, 30)
            };
            this.chkSelectAll.CheckedChanged += ChkSelectAll_CheckedChanged;

            this.btnUnlock = new Button
            {
                Text = "UNLOCK",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(40, 130, 200),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(110, 40),
                Location = new Point(230, 20),
                Cursor = Cursors.Hand
            };
            this.btnUnlock.FlatAppearance.BorderSize = 0;
            this.btnUnlock.Click += BtnUnlock_Click;

            this.btnLock = new Button
            {
                Text = "LOCK NOW",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(220, 53, 69),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(120, 40),
                Location = new Point(350, 20),
                Cursor = Cursors.Hand
            };
            this.btnLock.FlatAppearance.BorderSize = 0;
            this.btnLock.Click += BtnLock_Click;

            this.panelFooter.Controls.Add(this.chkSelectAll);
            this.panelFooter.Controls.Add(this.btnUnlock);
            this.panelFooter.Controls.Add(this.btnLock);

            // ============ PC List ============
            this.flowLayoutPanelPCs = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(15),
                BackColor = Color.FromArgb(45, 45, 48)
            };

            this.Controls.Add(this.flowLayoutPanelPCs);
            this.Controls.Add(this.panelFooter);
            this.Controls.Add(this.panelHeader);
        }

        private void LoadPCs()
        {
            if (_onlinePCs.Count == 0)
            {
                Label lblEmpty = new Label
                {
                    Text = "No online workstations found.",
                    Font = new Font("Segoe UI", 12F, FontStyle.Italic),
                    ForeColor = Color.Gray,
                    AutoSize = false,
                    Size = new Size(flowLayoutPanelPCs.Width - 40, 100),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                flowLayoutPanelPCs.Controls.Add(lblEmpty);
                return;
            }

            foreach (string pc in _onlinePCs)
            {
                CheckBox chk = new CheckBox
                {
                    Text = pc,
                    Appearance = Appearance.Button,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Width = 150,
                    Height = 55,
                    Margin = new Padding(10),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(60, 60, 60),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                chk.FlatAppearance.BorderSize = 0;
                chk.CheckedChanged += PcToggle_CheckedChanged;

                flowLayoutPanelPCs.Controls.Add(chk);
                pcToggles.Add(chk);
            }
        }

        private void PcToggle_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chk = (CheckBox)sender;
            chk.BackColor = chk.Checked
                ? Color.FromArgb(40, 167, 69)
                : Color.FromArgb(60, 60, 60);
        }

        private void ChkSelectAll_CheckedChanged(object sender, EventArgs e)
        {
            bool state = chkSelectAll.Checked;
            foreach (var chk in pcToggles)
                chk.Checked = state;
        }

        // =========================================================
        // LOCK
        // =========================================================
        private void BtnLock_Click(object sender, EventArgs e)
        {
            var ips = GetSelectedIPs();
            if (ips.Count == 0)
            {
                MessageBox.Show("Please select at least one PC.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int success = 0;
            foreach (string ip in ips)
                if (SendCommand(ip, "LOCK")) success++;

            MessageBox.Show($"LOCK sent to {success} of {ips.Count} PC(s).",
                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }

        // =========================================================
        // UNLOCK
        // =========================================================
        private void BtnUnlock_Click(object sender, EventArgs e)
        {
            var ips = GetSelectedIPs();
            if (ips.Count == 0)
            {
                MessageBox.Show("Please select at least one PC.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int success = 0;
            foreach (string ip in ips)
                if (SendCommand(ip, "UNLOCK")) success++;

            MessageBox.Show($"UNLOCK sent to {success} of {ips.Count} PC(s).",
                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }

        // =========================================================
        // HELPERS
        // =========================================================
        private List<string> GetSelectedIPs()
        {
            var ips = new List<string>();

            foreach (var chk in pcToggles)
            {
                if (!chk.Checked) continue;

                string ip = chk.Text;
                int start = chk.Text.LastIndexOf('(');
                int end = chk.Text.LastIndexOf(')');
                if (start >= 0 && end > start)
                    ip = chk.Text.Substring(start + 1, end - start - 1).Trim();

                ips.Add(ip);
            }

            return ips;
        }

        private bool SendCommand(string ip, string command)
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(ip, _commandPort);
                    if (!connectTask.Wait(3000))
                    {
                        System.Diagnostics.Debug.WriteLine($"[{command}] Timeout: {ip}");
                        return false;
                    }

                    using (NetworkStream stream = client.GetStream())
                    {
                        byte[] data = Encoding.UTF8.GetBytes(command);
                        stream.Write(data, 0, data.Length);
                        stream.Flush();
                    }
                }
                System.Diagnostics.Debug.WriteLine($"[{command}] Sent to {ip}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{command}] Failed {ip}: {ex.Message}");
                return false;
            }
        }
    }
}