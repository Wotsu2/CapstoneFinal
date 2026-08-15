using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WinFormsApp1
{
    internal class ScreenSharing
    {
        private AdminForm parentForm;
        private Dictionary<string, PictureBox> screenViewers = new Dictionary<string, PictureBox>();
        private TcpListener screenListener;
        private PictureBox pictureBoxScreen;
        

        public ScreenSharing(AdminForm form)
        {
            parentForm = form;
        }
        public async void StartScreenListener()
        {
            screenListener = new TcpListener(IPAddress.Any, 5002);
            screenListener.Start();

            while (true)
            {
                TcpClient client = await screenListener.AcceptTcpClientAsync();
                _ = ReceiveScreenStream(client);
            }
        }

        private async Task ReceiveScreenStream(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            string clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

            try
            {
                while (client.Connected)
                {

                    byte[] lengthBuffer = new byte[4];
                    int read = await ReadExactAsync(stream, lengthBuffer, 4);
                    if (read == 0) break;

                    int imageLength = BitConverter.ToInt32(lengthBuffer, 0);
                    Console.WriteLine("Receiving frame: " + imageLength + " bytes from " + clientIp);
                    byte[] imageBuffer = new byte[imageLength];

                    int totalRead = await ReadExactAsync(stream, imageBuffer, imageLength);
                    if (totalRead == 0) break;

                    using (MemoryStream ms = new MemoryStream(imageBuffer))
                    {
                        Image frame = Image.FromStream(ms);

                        if (parentForm.InvokeRequired)
                            parentForm.Invoke(new Action(() => UpdateScreenViewer(clientIp, frame)));
                        else
                            UpdateScreenViewer(clientIp, frame);
                    }
                }
            }
            catch { }
            finally
            {
                client.Close();
            }
        }

        private async Task<int> ReadExactAsync(NetworkStream stream, byte[] buffer, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int bytesRead = await stream.ReadAsync(buffer, totalRead, count - totalRead);
                if (bytesRead == 0) return 0;
                totalRead += bytesRead;
            }
            return totalRead;
        }

        private void UpdateScreenViewer(string clientIp, Image frame)
        {
            if (screenViewers.ContainsKey(clientIp) && screenViewers[clientIp] != null)
            {
                PictureBox pb = screenViewers[clientIp];
                Image oldImage = pb.Image;
                pb.Image = frame;
                oldImage?.Dispose();
            }
            else
            {
                frame.Dispose();
            }
        }

        public void AddScreenViewer(string workstationId)
        {
            ScreenViewerForm viewer = new ScreenViewerForm(workstationId);

            screenViewers[workstationId] = viewer.GetPictureBox();

            viewer.FormClosed += (s, args) => screenViewers.Remove(workstationId);

            viewer.Show();
        }
    }
}
