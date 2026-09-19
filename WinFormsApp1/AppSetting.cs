using System;
using System.Collections.Generic;
using System.Text;

namespace WinFormsApp1
{
    public class AppSettings
    {
        // Network
        public string ServerIp { get; set; } = "192.168.100.4";
        public int WorkstationPort { get; set; } = 5000;
        public int ScreenSharePort { get; set; } = 5001;
        public int BroadcastPort { get; set; } = 5002;
        public int FileTransferPort { get; set; } = 5003;
        public int CommandPort { get; set; } = 8888;

        // Database connection info
        public string DatabaseHost { get; set; } = "localhost";
        public int DatabasePort { get; set; } = 3306;
        public string DatabaseName { get; set; } = "cdsga_hub";
        public string DatabaseUser { get; set; } = "root";
        public string DatabasePassword { get; set; } = "";

        // File storage
        public string SaveFolder { get; set; } = @"C:\ReceivedFileFolder";

        // Performance
        public int ScreenShareInterval { get; set; } = 500;
        public int ThumbnailIntervalSeconds { get; set; } = 7;

        // Helper to build the connection string from the parts above
        public string GetConnectionString()
        {
            return $"Server={DatabaseHost};Port={DatabasePort};Database={DatabaseName};Uid={DatabaseUser};Pwd={DatabasePassword};";
        }
    }
}
