using Google.Apis.Auth.OAuth2;
using Google.Apis.Requests;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Auth.OAuth2.Responses;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows.Controls;
using System.Reflection.Metadata.Ecma335;

namespace SombaOpu
{    
    internal static class SQLHelper
    {
        private static List<string> gid = new List<string>();
        private static List<string> spreadSheetsID = new List<string>();        
        public static void Initialize()
        {
            //Load JSON file AppConfig, containing Gids and SpreadSheetsIDs for each month, to use in the application
            var config = ConfigLoader.Load();
            gid = config.Gids;
            spreadSheetsID = config.SpreadSheetsIDs;

            //-- Initialize SQLite Database --
            string connectionString = "Data Source=WeightReader.db;Version=3;";
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();                
                // Step 1. Check if date already exists
                string checkSql = "SELECT COUNT(*) FROM Weight WHERE date = @date";
                using (var checkCmd = new SQLiteCommand(checkSql, conn))
                {
                    checkCmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd"));
                    long count = (long)checkCmd.ExecuteScalar();

                    if (count == 0)
                    {
                        // Step 2. Date not found, insert it
                        string insertSql = "INSERT INTO Weight (Date) VALUES (@date)";
                        using (var insertCmd = new SQLiteCommand(insertSql, conn))
                        {
                            insertCmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd"));
                            insertCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        public static void Add(Button selectedButton, decimal value)
        {
            string connectionString = "Data Source=WeightReader.db;Version=3;";
            SQLiteConnection conn = new SQLiteConnection(connectionString);
            conn.Open();

            string tray = selectedButton.Name.Replace("Button_", "");
            string sql = $"UPDATE Weight SET {tray} = @value WHERE Date = @date";
            var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@value", value);
            cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd"));
            cmd.ExecuteNonQuery();

            conn.Close();
        }

        public static void SendWeightToGoogleSheets()
        {
            // -- Check if the last update was yesterday, if not, send the last data to yesterday's sheet as well --
            DateTime yesterday = DateTime.Today.AddDays(-1);
            if (ConfigLoader.LoadDate() is DateTime lastUpdate)
            {
                if (lastUpdate.Date < yesterday)
                {
                    SendData(yesterday, lastUpdate.Date);
                }                
            }
            ConfigLoader.SaveDate(DateTime.Now);
            SendData(DateTime.Today, DateTime.Today);            
        }

        private static void SendData(DateTime date, DateTime database)
        {
            // -- Credential --
            var json = File.ReadAllText("Credential/erwin-355300-e1153ddcf7c5.json");
            var credential = CredentialFactory.FromJson<ServiceAccountCredential>(json).ToGoogleCredential().CreateScoped(SheetsService.Scope.Spreadsheets);
            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "SombaOpu",
            });
            
            string connectionString = "Data Source=WeightReader.db;Version=3;";            
            var cells1 = new List<string> { "C3", "C4", "C5", "C6", "C7", "C10", "C11", "C12", "C13", "C14", "C16", "C17", "C18", "C19", "C20", "C22", "C23", "C24", "C25", "C26", "C28", "C29", "C30", "C31", "C32" };
            var cells2 = new List<string> { "H3", "H4", "H5", "H6", "H7", "H10", "H11", "H12", "H13", "H14", "H16", "H17", "H18", "H19", "H20", "H22", "H23", "H24", "H25", "H26", "H28", "H29", "H30", "H31", "H32" };
            var lines = new List<string>();
            string today = date.Day.ToString();
            string tommorow = date.AddDays(1).Day.ToString();
            bool isLastDayOfTheMonth = date.AddDays(1).Day == 1;
            // -- Load today's data from SQLite --
            lines.Clear();
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT A1,A2,A3,A4,A5,B2,B3,B4,B5,B6,C1,C2,C3,C4,C5,D1,D2,D3,D4,D5,E1,E2,E3,E4,E5 FROM Weight WHERE date = @date";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@date", database.ToString("yyyy-MM-dd"));

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read()) return;

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            string col = reader.GetName(i);
                            if (reader.IsDBNull(i))
                            {
                                lines.Add(" ");
                            }
                            else
                            {
                                string line = Convert.ToDecimal(reader.GetValue(i)).ToString(CultureInfo.InvariantCulture);
                                lines.Add(line);
                            }
                        }
                    }
                }
                conn.Close();
            }

            //-- If it's the last day of the month, send data to both current and next month's sheets --
            //-- Current Month, Last Day Data --
            if (isLastDayOfTheMonth)
            {
                try
                {
                    var data = new List<ValueRange>();
                    for (int i = 0; i < lines.Count; i++)
                    {
                        data.Add(new ValueRange
                        {
                            Range = $"{today}!{cells2[i]}",
                            Values = new List<IList<object>> { new List<object> { lines[i] } }
                        });
                    }

                    var batchRequest = new BatchUpdateValuesRequest
                    {
                        Data = data,
                        ValueInputOption = "USER_ENTERED"
                    };

                    var request = service.Spreadsheets.Values.BatchUpdate(batchRequest, spreadSheetsID[date.Month]);
                    request.Execute();
                }
                catch
                {

                }

                //-- Next Month, 1st Day Data --
                try
                {
                    var data = new List<ValueRange>();
                    for (int i = 0; i < lines.Count; i++)
                    {
                        data.Add(new ValueRange
                        {
                            Range = $"{tommorow}!{cells1[i]}",
                            Values = new List<IList<object>> { new List<object> { lines[i] } }
                        });
                    }

                    var batchRequest = new BatchUpdateValuesRequest
                    {
                        Data = data,
                        ValueInputOption = "USER_ENTERED"
                    };

                    var request = service.Spreadsheets.Values.BatchUpdate(batchRequest, spreadSheetsID[date.Month + 1]);
                    request.Execute();
                }
                catch
                {

                }
            }

            // -- If it's not the last day of the month, only send data to current month's sheet --
            else if (!isLastDayOfTheMonth)
            {
                var data = new List<ValueRange>();
                try
                {
                    for (int i = 0; i < lines.Count; i++)
                    {
                        data.Add(new ValueRange
                        {
                            Range = $"{tommorow}!{cells1[i]}",
                            Values = new List<IList<object>> { new List<object> { lines[i] } }
                        });
                    }

                    var batchRequest = new BatchUpdateValuesRequest
                    {
                        Data = data,
                        ValueInputOption = "USER_ENTERED"
                    };

                    var request = service.Spreadsheets.Values.BatchUpdate(batchRequest, spreadSheetsID[date.Month]);
                    request.Execute();
                }
                catch
                {

                }
            }
        }      

        public static void OpenWebsite()
        {
            int _day = (DateTime.Today.Day);
            int _month = (DateTime.Today.Month);

            Process.Start(new ProcessStartInfo
            {
                FileName = $"https://docs.google.com/spreadsheets/d/{spreadSheetsID[_month]}/edit?gid={gid[_day]}#gid={gid[_day]}",
                UseShellExecute = true
            });
        }
    }
}
