using System;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public static class SettingsManager
    {
        private static readonly string settingsPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        public static AppSettings Current { get; private set; } = new AppSettings();

        public static void Load()
        {
            try
            {
                if (File.Exists(settingsPath))
                {
                    string json = File.ReadAllText(settingsPath);
                    Current = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();

                    if (Current.ServerPresets == null)
                        Current.ServerPresets = new System.Collections.Generic.List<ServerPreset>();
                }
                else
                {
                    Current = new AppSettings();
                    Save();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to load settings: " + ex.Message);
                Current = new AppSettings();
            }
        }

        public static void Save()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(Current, options);
                File.WriteAllText(settingsPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save settings: " + ex.Message);
            }
        }

        public static void ResetToDefaults()
        {
            Current = new AppSettings();
            Save();
        }
    }
}