using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WinFormsApp1
{
    internal class WorkStationFunctions
    {
        private AdminForm parentForm;

        private TcpListener listener;
        private TcpListener fileListener;
        private int fileSubmittedCount = 0;
        private int WorkStationNum = 0;
        private Dictionary<string, Button> workstationButtons = new Dictionary<string, Button>();
                
        public WorkStationFunctions(AdminForm form)
        {
            parentForm = form;
        }

        public async void StartServer()
        {
            listener = new TcpListener(IPAddress.Any, 5000);
            listener.Start();

            parentForm.lblTotalWorkstations.Text = "0";
            int registeredCount = workstationButtons.Count;

            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();

                string pcId = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

                Button MainPcButton = null;

                if (workstationButtons.ContainsKey(pcId))
                {
                    (MainPcButton) = workstationButtons[pcId];

                    if (parentForm.InvokeRequired)
                    {
                        parentForm.Invoke(new Action(() =>
                        {
                            MainPcButton.BackColor = Color.LightGreen;
                            UpdateConnectedCount();
                        }));
                    }
                    else
                    {
                        MainPcButton.BackColor = Color.LightGreen;
                        UpdateConnectedCount();
                    }
                }
                else
                {
                    // First time seeing this PC — create buttons
                    if (parentForm.InvokeRequired)
                    {
                        parentForm.Invoke(new Action(() =>
                        {
                            var mainBtn = OnWorkStationConnected(pcId);
                            MainPcButton = mainBtn;
                        }));
                    }
                    else
                    {
                        var mainBtn = OnWorkStationConnected(pcId);
                        MainPcButton = mainBtn;
                    }

                    workstationButtons[pcId] = (MainPcButton);

                    if (parentForm.InvokeRequired)
                    {
                        parentForm.Invoke(new Action(() =>
                        {
                            parentForm.lblTotalWorkstations.Text = workstationButtons.Count.ToString();
                            UpdateConnectedCount();
                        }));
                    }
                    else
                    {
                        parentForm.lblTotalWorkstations.Text = workstationButtons.Count.ToString();
                        UpdateConnectedCount();
                    }


                }
                _ = MonitorDisconnected(client, MainPcButton);

            }
        }

        private Button OnWorkStationConnected(string clientIp)
        {
            int currentCount = int.Parse(parentForm.lblTotalWorkstations.Text);
            currentCount++;
            parentForm.lblTotalWorkstations.Text = currentCount.ToString();

            WorkStationNum++;

            Button MainPcButton = new Button();
            MainPcButton.Text = "PC " + WorkStationNum.ToString();
            MainPcButton.Image = Properties.Resources.material_symbols_light_computer_outline_rounded;
            MainPcButton.Height = 180;
            MainPcButton.Width = 131;
            MainPcButton.Margin = new Padding(5);
            MainPcButton.BackColor = Color.LightGreen;
            MainPcButton.Tag = clientIp;
            MainPcButton.Click += (s, e) => parentForm.WorkstationButton_Click(s, e);

            parentForm.MainWorkstationFLP.Controls.Add(MainPcButton);

            return MainPcButton;

        }

        private async Task MonitorDisconnected(TcpClient client, Button MainPcButton)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1];

            try
            {
                while (client.Connected)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, 1);
                    if (bytesRead == 0) break;
                }
            }
            catch { }
            finally
            {
                if (parentForm.InvokeRequired)
                {
                    parentForm.Invoke(new Action(() =>
                    {
                        MainPcButton.BackColor = Color.Red;
                        UpdateConnectedCount();

                        int count = int.Parse(parentForm.lblTotalWorkstations.Text);
                        if (count > 0) count--;
                        parentForm.lblTotalWorkstations.Text = count.ToString();
                    }));
                }
                else
                {
                    MainPcButton.BackColor = Color.Red;
                    UpdateConnectedCount();

                    int count = int.Parse(parentForm.lblTotalWorkstations.Text);
                    if (count > 0) count--;
                    parentForm.lblTotalWorkstations.Text = count.ToString();

                }
                client.Close();
            }
        }

        private void UpdateConnectedCount()
        {
            int connectedCount = workstationButtons.Values
            .Count(btn => btn.BackColor == Color.LightGreen);

            int disconnectedCount = workstationButtons.Count - connectedCount;

        }

        
    }
}
