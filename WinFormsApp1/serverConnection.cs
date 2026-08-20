using MySqlX.XDevAPI;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace WinFormsApp1

{
    internal class serverConnection
    {
        private Nothing parentForm; // reference to the Form that owns the UI

        public serverConnection(Nothing form)
        {
            parentForm = form;
        }

        private void CreateFileButton(string fileName, string filePath)
        {
            if (parentForm.InvokeRequired)
            {
                parentForm.Invoke((MethodInvoker)delegate { CreateFileButton(fileName, filePath); });
                return;
            }

            Button btn = new Button();
            btn.Text = fileName;
            btn.Width = 200;
            btn.Height = 30;


            parentForm.flpActivities.Controls.Add(btn); // add to a FlowLayoutPanel on your form
        }
        private TcpListener listener;
        private bool isRunning;
        private string saveFolder = "ReceivedFiles"; // folder where files will be saved

        public serverConnection(int port = 5001)
        {
            listener = new TcpListener(IPAddress.Any, port);
            Directory.CreateDirectory(saveFolder); // ensure folder exists
        }

        public void Start()
        {
            listener.Start();
            isRunning = true;
            Console.WriteLine("Server started, waiting for files...");

            Task.Run(() => ListenForClients());
        }

        private async Task ListenForClients()
        {
            while (isRunning)
            {
                try
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    _ = HandleClientAsync(client); // handle each client without blocking others
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Listener error: " + ex.Message);
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    // Read in the SAME order the client wrote them
                    string fileName = reader.ReadString();
                    int fileLength = reader.ReadInt32();
                    byte[] fileBytes = reader.ReadBytes(fileLength);

                    string savePath = Path.Combine(saveFolder, fileName);
                    await File.WriteAllBytesAsync(savePath, fileBytes);

                    Console.WriteLine($"Received file: {fileName} ({fileLength} bytes)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error receiving file: " + ex.Message);
            }
        }

        public void Stop()
        {
            isRunning = false;
            listener.Stop();
        }
    }
}
