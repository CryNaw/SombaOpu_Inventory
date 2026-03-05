using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace SombaOpu
{
    public static class ConfigLoader
    {
        private static readonly string AppConfigPath = "Credential/app-config.json";
        private static readonly string APIKeyConfigPath = "Credential/erwin-355300-e1153ddcf7c5.json";
        public static AppConfig Load()
        {            
            //Load 
            try
            {
                if (!File.Exists(AppConfigPath)) return new AppConfig();

                string jsonString = File.ReadAllText(AppConfigPath);
                return JsonSerializer.Deserialize<AppConfig>(jsonString) ?? new AppConfig();
            }
            catch (Exception ex)
            {
                // Log error or handle appropriately
                Debug.WriteLine($"Error loading config: {ex.Message}");                
                return new AppConfig();
            }
        }

        public static void SaveDate(DateTime dateToSave)
        {
            // This creates a path like: C:\Users\YourName\AppData\Roaming\SombaOpu\save.json
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SombaOpu");
            string filePath = Path.Combine(folderPath, "save.json");

            // Ensure the directory exists
            Directory.CreateDirectory(folderPath);

            var saveConfig = new SaveConfig { LastUpdated = dateToSave };
            string jsonString = JsonSerializer.Serialize(saveConfig, new JsonSerializerOptions { WriteIndented = true });

            // Write to file
            File.WriteAllText(filePath, jsonString);
        }

        public static DateTime? LoadDate()
        {
            // This Create a path like: C:\Users\YourName\AppData\Roaming\SombaOpu\save.json
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Config");
            string filePath = Path.Combine(folderPath, "save.json");

            // Ensure the directory exists
            Directory.CreateDirectory(folderPath);

            if (!File.Exists(filePath)) return null;

            try
            {
                string jsonString = File.ReadAllText(filePath);
                var progress = JsonSerializer.Deserialize<SaveConfig>(jsonString);
                return progress?.LastUpdated;
            }
            catch
            {
                return DateTime.Today; // Handle corrupted or empty files
            }
        }

        public static bool isAppConfig()
        {
            if (File.Exists(AppConfigPath))
            {                
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool isAPIKeyConfig()
        {
            if (File.Exists(APIKeyConfigPath))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
