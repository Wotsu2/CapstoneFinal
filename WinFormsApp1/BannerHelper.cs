using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace WinFormsApp1
{
    public static class BannerHelper
    {
        /// <summary>
        /// Returns  <SaveFolder>\Banners  — the folder that admin sets in Configuration.
        /// Example:  SaveFolder = C:\CDSGA_Hub\Users
        ///           Banners    = C:\CDSGA_Hub\Users\Banners
        /// </summary>
        public static string GetBannersFolder(bool createIfMissing = true)
        {
            try
            {
                string root = SettingsManager.Current.SaveFolder;

                if (string.IsNullOrWhiteSpace(root))
                {
                    Console.WriteLine("[BannerHelper] SaveFolder not configured yet.");
                    return "";
                }

                string banners = Path.Combine(root, "Banners");

                if (!Directory.Exists(banners) && createIfMissing)
                {
                    try { Directory.CreateDirectory(banners); }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[BannerHelper] Could not create: " + ex.Message);
                    }
                }

                return banners;
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetBannersFolder error: " + ex.Message);
                return "";
            }
        }

        /// <summary>
        /// Loads all images from the Banners folder, sorted by file name.
        /// Reads bytes into memory so files aren't locked.
        /// </summary>
        public static List<Image> LoadBannerImages()
        {
            List<Image> images = new List<Image>();

            string folder = GetBannersFolder(false);
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                return images;

            string[] patterns = { "*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif" };
            List<string> files = new List<string>();

            foreach (string pattern in patterns)
                files.AddRange(Directory.GetFiles(folder, pattern));

            files = files.OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();

            foreach (string file in files)
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(file);
                    using (var ms = new MemoryStream(bytes))
                    {
                        images.Add(Image.FromStream(ms));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"LoadBannerImages: failed {file}: {ex.Message}");
                }
            }

            return images;
        }
    }
}