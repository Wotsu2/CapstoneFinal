using System;
using System.Runtime.InteropServices;

namespace WinFormsApp1
{
    public static class NetworkShare
    {
        [StructLayout(LayoutKind.Sequential)]
        private class NETRESOURCE
        {
            public int dwScope = 0;
            public int dwType = 1; // RESOURCETYPE_DISK
            public int dwDisplayType = 0;
            public int dwUsage = 0;
            public string lpLocalName = null;
            public string lpRemoteName = null;
            public string lpComment = null;
            public string lpProvider = null;
        }

        [DllImport("mpr.dll")]
        private static extern int WNetAddConnection2(NETRESOURCE netResource,
            string password, string username, int flags);

        [DllImport("mpr.dll")]
        private static extern int WNetCancelConnection2(string name, int flags, bool force);

        /// <summary>
        /// Connects to a network share (UNC path). Returns true if success.
        /// </summary>
        public static bool ConnectToShare(string uncPath, string username, string password)
        {
            try
            {
                var nr = new NETRESOURCE
                {
                    dwType = 1,
                    lpRemoteName = uncPath
                };

                int result = WNetAddConnection2(nr, password, username, 0);
                return result == 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ConnectToShare error: " + ex.Message);
                return false;
            }
        }

        public static void Disconnect(string uncPath)
        {
            try
            {
                WNetCancelConnection2(uncPath, 0, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Disconnect error: " + ex.Message);
            }
        }
    }
}