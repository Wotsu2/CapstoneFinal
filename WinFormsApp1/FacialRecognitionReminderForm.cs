using DevExpress.Pdf.Native;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

// For webcam
using AForge.Video;
using AForge.Video.DirectShow;

namespace WinFormsApp1
{
    public partial class FacialRecognitionReminderForm : Form
    {
        private int StudentId;
        private string AuthenticationPhoto;
        private string StudentUsername;

        // =========================================================
        // WEBCAM
        // =========================================================
        private FilterInfoCollection _videoDevices;
        private VideoCaptureDevice _videoSource;
        private Bitmap _capturedFrame;
        private bool _isCameraRunning = false;

        public FacialRecognitionReminderForm(int StudentID, string Username)
        {
            InitializeComponent();
            StudentId = StudentID;
            StudentUsername = Username;
            InitializeGetRemainingLimit();
            initializeCloseExitButton();

            // Wire up the Take Photo button (in case it wasn't wired in the designer)
            if (btnTakePhoto != null)
                btnTakePhoto.Click += btnTakePhoto_Click;
        }

        // =========================================================
        // REMAINING LIMIT
        // =========================================================
        private void InitializeGetRemainingLimit()
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT remaining_limit FROM user_credential WHERE username = @username";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", StudentUsername);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int remainingLimit = 0;
                                if (!reader.IsDBNull(reader.GetOrdinal("remaining_limit")))
                                    remainingLimit = reader.GetInt32("remaining_limit");

                                lblRemainingLimit.Text = $"*{remainingLimit} remaining";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("InitializeGetRemainingLimit error: " + ex.Message);
            }
        }

        private void initializeCloseExitButton()
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT remaining_limit FROM user_credential WHERE username = @username";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", StudentUsername);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int remainingLimit = 0;
                                if (!reader.IsDBNull(reader.GetOrdinal("remaining_limit")))
                                    remainingLimit = reader.GetInt32("remaining_limit");

                                if (remainingLimit <= 0)
                                {
                                    btnCloseForm.Enabled = false;
                                    btnSkipforNow.Enabled = false;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("initializeCloseExitButton error: " + ex.Message);
            }
        }

        // =========================================================
        // CLOSE / SKIP
        // =========================================================
        private void btnCloseForm_Click(object sender, EventArgs e) => DecrementLimitAndClose();
        private void btnSkipforNow_Click(object sender, EventArgs e) => DecrementLimitAndClose();

        private void DecrementLimitAndClose()
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "UPDATE user_credential SET remaining_limit = remaining_limit - 1 WHERE username = @username";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", StudentUsername);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            CustomMessageBox.Show("Remaining limit updated successfully.",
                                "Success", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                        }
                        else
                        {
                            CustomMessageBox.Show("No rows were updated. Please check the user ID.",
                                "Update Failed", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                        }
                    }
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("An error occurred while updating the remaining limit.\n\n" + ex.Message,
                    "Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
            }
        }

        // =========================================================
        // UPLOAD PHOTO (still works)
        // =========================================================
        private void btnUploadAuthenticationPhoto_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    AuthenticationPhoto = ofd.FileName;
                    picboxAuthenticationPhoto.Image = Image.FromFile(AuthenticationPhoto);
                    picboxAuthenticationPhoto.SizeMode = PictureBoxSizeMode.Zoom;
                    btnSubmitAuthenticationPhoto.Enabled = true;
                }
            }
        }

        // =========================================================
        // TAKE PHOTO — open webcam
        // =========================================================
        private void btnTakePhoto_Click(object sender, EventArgs e)
        {
            try
            {
                _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                if (_videoDevices.Count == 0)
                {
                    CustomMessageBox.Show("No webcam detected on this device.",
                        "Camera Not Found", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                    return;
                }

                using (var captureForm = new CameraCaptureForm(_videoDevices))
                {
                    if (captureForm.ShowDialog(this) == DialogResult.OK && captureForm.CapturedImage != null)
                    {
                        _capturedFrame?.Dispose();
                        _capturedFrame = new Bitmap(captureForm.CapturedImage);

                        picboxAuthenticationPhoto.Image?.Dispose();
                        picboxAuthenticationPhoto.Image = new Bitmap(_capturedFrame);
                        picboxAuthenticationPhoto.SizeMode = PictureBoxSizeMode.Zoom;

                        string tempFile = Path.Combine(Path.GetTempPath(),
                            $"auth_capture_{StudentId}_{DateTime.Now:yyyyMMddHHmmss}.jpg");
                        _capturedFrame.Save(tempFile, ImageFormat.Jpeg);

                        AuthenticationPhoto = tempFile;
                        btnSubmitAuthenticationPhoto.Enabled = true;

                        CustomMessageBox.Show("Photo captured successfully. You can now submit it.",
                            "Captured", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Error opening the camera:\n\n" + ex.Message,
                    "Camera Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
            }
        }

        // =========================================================
        // SUBMIT AUTHENTICATION PHOTO
        // =========================================================
        private async void btnSubmitAuthenticationPhoto_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(AuthenticationPhoto) || !File.Exists(AuthenticationPhoto))
            {
                CustomMessageBox.Show("Take or upload a photo first!",
                    "Validation", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                return;
            }

            try
            {
                byte[] imageBytes = await File.ReadAllBytesAsync(AuthenticationPhoto);

                string ext = Path.GetExtension(AuthenticationPhoto);
                if (string.IsNullOrEmpty(ext)) ext = ".jpg";
                string fileName = $"{StudentId}_{StudentUsername}_{DateTime.Now:yyyyMMddHHmmss}{ext}";

                string savedPath = await SendAuthenticationPhotoToAdmin(imageBytes, fileName);
                if (string.IsNullOrEmpty(savedPath))
                {
                    CustomMessageBox.Show("Failed to send authentication photo to server.",
                        "Send Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                    return;
                }

                Console.WriteLine("authPhoto savedPath = " + savedPath);

                string connStr = SettingsManager.Current.GetConnectionString();
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"UPDATE user_credential 
                                     SET authentication_photo = @path 
                                     WHERE username = @username";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@path", savedPath);
                        cmd.Parameters.AddWithValue("@username", StudentUsername);
                        cmd.ExecuteNonQuery();
                    }
                }

                CustomMessageBox.Show("Authentication photo sent to admin successfully.",
                    "Success", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Error sending authentication photo:\n\n" + ex.Message,
                    "Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
            }
        }

        // =========================================================
        // FORM CLOSING — release webcam
        // =========================================================
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                if (_videoSource != null && _videoSource.IsRunning)
                {
                    _videoSource.SignalToStop();
                    _videoSource.WaitForStop();
                    _videoSource = null;
                }
            }
            catch { }

            _capturedFrame?.Dispose();
            _capturedFrame = null;

            base.OnFormClosing(e);
        }

        // =========================================================
        // HELPERS
        // =========================================================
        private string CleanIp(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "localhost";

            string s = raw.Trim()
                          .Replace("(null)", "")
                          .Replace(" ", "")
                          .TrimStart('\\')
                          .TrimEnd('\\');

            int slash = s.IndexOf('\\');
            if (slash > 0) s = s.Substring(0, slash);

            return s;
        }

        private async Task<string> SendAuthenticationPhotoToAdmin(byte[] imageBytes, string fileName)
        {
            try
            {
                string adminIp = CleanIp(SettingsManager.Current.ServerIp);
                int adminPort = SettingsManager.Current.FileTransferPort;

                Console.WriteLine($"[AUTH SEND] {adminIp}:{adminPort}, file={fileName}, bytes={imageBytes.Length}");

                using (TcpClient client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(adminIp, adminPort);
                    var timeoutTask = Task.Delay(5000);
                    var completed = await Task.WhenAny(connectTask, timeoutTask);

                    if (completed == timeoutTask)
                    {
                        Console.WriteLine($"Admin ({adminIp}:{adminPort}) not reachable (timeout).");
                        return null;
                    }

                    await connectTask;

                    if (!client.Connected)
                    {
                        Console.WriteLine($"Admin ({adminIp}:{adminPort}) refused the connection.");
                        return null;
                    }

                    string returnedPath = null;

                    using (NetworkStream stream = client.GetStream())
                    using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
                    {
                        writer.Write("AUTH_PHOTO");
                        writer.Write(fileName);
                        writer.Write(imageBytes.Length);
                        writer.Write(imageBytes);
                        writer.Flush();

                        try
                        {
                            using (var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
                            {
                                returnedPath = reader.ReadString();
                                Console.WriteLine("[AUTH SEND] Server replied: " + returnedPath);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("[AUTH SEND] No reply from server: " + ex.Message);
                        }
                    }

                    return returnedPath;
                }
            }
            catch (SocketException sex)
            {
                Console.WriteLine($"Network error: {sex.SocketErrorCode} — {sex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("SendAuthenticationPhotoToAdmin error: " + ex.Message);
                return null;
            }
        }
    }

    // =========================================================
    // CAMERA CAPTURE FORM
    // =========================================================
    public class CameraCaptureForm : Form
    {
        private readonly FilterInfoCollection _devices;
        private VideoCaptureDevice _videoSource;
        private PictureBox _preview;
        private ComboBox _cameraSelector;
        private Button _btnCapture;
        private Button _btnRetake;
        private Button _btnUse;
        private Button _btnCancel;
        private Label _lblHint;

        private Bitmap _latestFrame;

        public Image CapturedImage { get; private set; }

        public CameraCaptureForm(FilterInfoCollection devices)
        {
            _devices = devices;
            BuildUi();
            StartCamera(0);
        }

        private void BuildUi()
        {
            Text = "Take Authentication Photo";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(700, 620);
            MinimumSize = new Size(600, 550);
            BackColor = Color.FromArgb(30, 30, 40);
            ForeColor = Color.White;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            // Header
            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.FromArgb(94, 14, 33)
            };
            Controls.Add(header);

            Label lblTitle = new Label
            {
                Text = "📸  Take a Selfie for Authentication",
                Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(20, 14)
            };
            header.Controls.Add(lblTitle);

            // Camera selector
            _cameraSelector = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F),
                Location = new Point(20, 70),
                Size = new Size(300, 28)
            };
            foreach (FilterInfo d in _devices)
                _cameraSelector.Items.Add(d.Name);
            if (_cameraSelector.Items.Count > 0)
                _cameraSelector.SelectedIndex = 0;
            _cameraSelector.SelectedIndexChanged += (s, e) => StartCamera(_cameraSelector.SelectedIndex);
            Controls.Add(_cameraSelector);

            _lblHint = new Label
            {
                Text = "Make sure your face is centered and well-lit.",
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.FromArgb(190, 190, 200),
                AutoSize = true,
                Location = new Point(340, 76)
            };
            Controls.Add(_lblHint);

            // Preview
            _preview = new PictureBox
            {
                Location = new Point(20, 110),
                Size = new Size(645, 400),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(_preview);

            // Capture
            _btnCapture = new Button
            {
                Text = "📷  Capture",
                Size = new Size(150, 44),
                Location = new Point(20, 525),
                BackColor = Color.FromArgb(94, 14, 33),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnCapture.FlatAppearance.BorderSize = 0;
            _btnCapture.Click += (s, e) => CaptureFrame();
            Controls.Add(_btnCapture);

            // Retake
            _btnRetake = new Button
            {
                Text = "↺  Retake",
                Size = new Size(130, 44),
                Location = new Point(180, 525),
                BackColor = Color.FromArgb(60, 60, 70),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            _btnRetake.FlatAppearance.BorderSize = 0;
            _btnRetake.Click += (s, e) => Retake();
            Controls.Add(_btnRetake);

            // Use Photo
            _btnUse = new Button
            {
                Text = "✓  Use Photo",
                Size = new Size(160, 44),
                Location = new Point(320, 525),
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            _btnUse.FlatAppearance.BorderSize = 0;
            _btnUse.Click += (s, e) =>
            {
                if (CapturedImage != null)
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }
            };
            Controls.Add(_btnUse);

            // Cancel
            _btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(110, 44),
                Location = new Point(490, 525),
                BackColor = Color.FromArgb(40, 40, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnCancel.FlatAppearance.BorderSize = 0;
            _btnCancel.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };
            Controls.Add(_btnCancel);
        }

        private void StartCamera(int index)
        {
            StopCamera();

            if (index < 0 || index >= _devices.Count) return;

            try
            {
                _videoSource = new VideoCaptureDevice(_devices[index].MonikerString);
                _videoSource.NewFrame += VideoSource_NewFrame;
                _videoSource.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot start camera:\n\n" + ex.Message,
                    "Camera Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StopCamera()
        {
            try
            {
                if (_videoSource != null)
                {
                    _videoSource.NewFrame -= VideoSource_NewFrame;
                    if (_videoSource.IsRunning)
                    {
                        _videoSource.SignalToStop();
                        _videoSource.WaitForStop();
                    }
                    _videoSource = null;
                }
            }
            catch { }
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                var frame = (Bitmap)eventArgs.Frame.Clone();

                if (_preview.IsHandleCreated && !_preview.IsDisposed)
                {
                    _preview.BeginInvoke(new Action(() =>
                    {
                        var old = _latestFrame;
                        _latestFrame = frame;
                        _preview.Image = frame;
                        old?.Dispose();
                    }));
                }
                else
                {
                    frame.Dispose();
                }
            }
            catch { }
        }

        private void CaptureFrame()
        {
            try
            {
                if (_videoSource == null || !_videoSource.IsRunning) return;

                var frame = _latestFrame != null ? (Bitmap)_latestFrame.Clone() : null;

                if (frame == null)
                {
                    MessageBox.Show("Could not capture frame. Try again.",
                        "Capture Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                StopCamera();

                CapturedImage?.Dispose();
                CapturedImage = frame;

                _preview.Image?.Dispose();
                _preview.Image = new Bitmap(frame);
                _preview.SizeMode = PictureBoxSizeMode.Zoom;

                _btnCapture.Enabled = false;
                _btnRetake.Enabled = true;
                _btnUse.Enabled = true;

                _lblHint.Text = "Photo captured! Click 'Use Photo' to keep it, or 'Retake' to try again.";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Capture error: " + ex.Message,
                    "Capture Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Retake()
        {
            CapturedImage?.Dispose();
            CapturedImage = null;

            _btnCapture.Enabled = true;
            _btnRetake.Enabled = false;
            _btnUse.Enabled = false;

            _lblHint.Text = "Make sure your face is centered and well-lit.";

            if (_cameraSelector.Items.Count > 0)
                StartCamera(_cameraSelector.SelectedIndex);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopCamera();
            base.OnFormClosing(e);
        }
    }
}