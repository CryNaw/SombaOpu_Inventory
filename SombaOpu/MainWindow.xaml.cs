using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Media;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SombaOpu
{
    /// <summary>
    /// Interaction logic for WeightReader.xaml
    /// </summary>
    public partial class MainWindow : Window
    {        
        private SerialPort serialPort = new SerialPort();

        private bool isSelected = false;
        private Button selectedButton = new Button();        

        List<decimal> logs = new List<decimal>();
        int maxSize = 20;

        List<Button> sequence = new List<Button>();
        private bool isConfirm = false;
        private bool isConnected = false;
        decimal currentValue = 0;

        SoundPlayer player_Switch = new SoundPlayer("Sound\\Switch.wav");
        SoundPlayer player_Confirm = new SoundPlayer("Sound\\Confirm.wav");

        DispatcherTimer timer = new DispatcherTimer();
        DateTime lastDataReceived = DateTime.Now;        

        public MainWindow()
        {
            InitializeComponent();            
            SQLHelper.Initialize();
            MouseCursorHelper.Initialized(this, Root);
            FullScreenHelper.EnableFullscreen(this);
            InitializeButtons();
            InitializeSequence();
            UpdateTextBlockChain();            
            
            // -- Button Disabled --
            foreach (var b in ButtonUniformGrid.Children.OfType<Button>()) b.IsEnabled = false;

            // -- Load Sounds --
            player_Switch.Load();
            player_Confirm.Load();

            // -- Timer for Auto Confirm --            
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private bool isProcessingTick = false; // Add this at the top of your class
        private bool hasEverConnected = false;
        private async void Timer_Tick(object? sender, EventArgs e)
        {
            // Prevent the timer from overlapping if a scan takes longer than 1 second
            if (isProcessingTick) return;
            isProcessingTick = true;

            try
            {
                if (isConnected)
                {
                    if ((DateTime.Now - lastDataReceived).TotalSeconds > 3)
                    {
                        serialPort.Close();
                        foreach (var b in ButtonUniformGrid.Children.OfType<Button>()) b.IsEnabled = false;
                        isConnected = false;
                        isProcessingTick = false; // Reset flag
                        return;
                    }
                }

                if (!isConnected)
                {
                    string? foundPort = null;
                    var ports = SerialPort.GetPortNames();

                    foreach (string portName in ports)
                    {                                                
                        try
                        {
                            using (SerialPort testPort = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One))
                            {
                                testPort.ReadTimeout = 500; // Increased slightly for stability
                                testPort.Open();

                                // Give the scale a moment to send a byte
                                await Task.Delay(100);

                                string incoming = testPort.ReadExisting();

                                if (!string.IsNullOrWhiteSpace(incoming) && incoming.Any(char.IsDigit))
                                {
                                    foundPort = portName;
                                    break;
                                }
                            }
                        }
                        catch
                        {
                            // Port busy or denied, skip to next
                        }
                    }

                    if (foundPort != null)
                    {
                        try
                        {
                            //-- Scale Found --
                            serialPort = new SerialPort(foundPort, 9600, Parity.None, 8, StopBits.One);
                            serialPort.DataReceived += SerialPort_DataReceived;
                            serialPort.Open();                            
                            foreach (var b in ButtonUniformGrid.Children.OfType<Button>()) b.IsEnabled = true;
                            Interface.Text = $"Connected to ({foundPort})";
                            hasEverConnected = true;
                            isConnected = true;
                            player_Switch.Play();
                            sequence[0].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        }
                        catch
                        {
                            Interface.Text = "Failed to connect to scale.";
                        }
                    }
                    else
                    {                        
                        Interface.Text = hasEverConnected ? "Scale Disconnected. Please Reconnect... ⏳ \n (Check Your USB Cable)" : "No scale detected. Retrying... ⏳ \n (Check Your USB Cable)";
                    }
                }

                // -- Auto Confirm Logic --
                if (isSelected && logs.Count >= maxSize && IsStable(logs))
                {
                    Confirm();
                }
                else if (!isSelected && isConfirm && logs.All(v => Math.Abs(v) <= 1))
                {
                    NextSequence();
                    isConfirm = false;
                }
            }
            finally
            {
                isProcessingTick = false; // Always release the lock
            }
        }

        private string _incomingBuffer = "";
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            lastDataReceived = DateTime.Now;                        
            string chunk = serialPort.ReadExisting();
            foreach (char c in chunk)
            {
                if (c == '\n' || c == '\r')
                {
                    if (_incomingBuffer.Length > 0)
                    {
                        string fullLine = _incomingBuffer;
                        _incomingBuffer = "";
                        Dispatcher.InvokeAsync(() =>
                        {
                            if (WeightDisplay.IsLoaded)
                            {
                                currentValue = decimal.Parse(ExtractNumber(fullLine).Replace(".", ","), System.Globalization.CultureInfo.GetCultureInfo("id-ID"));
                                if (isSelected) WeightDisplay.Text = currentValue + " g"; //Update display                            
                                AddLog(currentValue);
                            }
                        });
                    }
                }
                else
                {
                    _incomingBuffer += c;
                }
            }
        }
        
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Content is not Viewbox viewbox)
                return;

            if (viewbox.Child is not StackPanel panel)
                return;

            if (panel.Children[0] is not TextBlock nameTextBlock || panel.Children[1] is not TextBlock valueTextBlock)
                return;

            isSelected = true;
            selectedButton = btn;
            SelectedTray.Text = nameTextBlock.Text;

            // -- Play Sound --
            player_Switch.Play();

            // -- Update --
            UpdateTextBlockChain();            
            UpdateTextBlockTrayButtonColor();
        }        

        private void Button_Click_Cancel()
        {
            isSelected = false;
            selectedButton = new Button();                    

            SelectedTray.Text = "SombaOpu";
            WeightDisplay.Text = "💎";

            // -- Play Sound --
            player_Switch.Play();

            // -- Update --
            UpdateTextBlockChain();
            UpdateTextBlockTrayButtonColor();
        }

        List<string> TextChain = new List<string> { "B5", "A5", "C5", "B6", "E5", "D5", "A2", "A3", "A4", "B2", "B3", "B4", "A1", "C2", "C3", "C4", "C1", "E4", "E3", "E2", "E1", "D3", "D4", "D2", "D1" };
        private void UpdateTextBlockChain()
        {
            // -- Update Text Block Chain --
            TextBlock_Chain.Inlines.Clear();
            for (int i = 0; i < TextChain.Count; i++)
            {
                var run = new Run(TextChain[i]);
                if (sequence[i] == selectedButton)
                {
                    run.FontWeight = FontWeights.Bold;
                    run.FontSize = 24;
                }
                else
                {
                    run.FontSize = 12;
                }
                TextBlock_Chain.Inlines.Add(run);
                if (i + 1 < TextChain.Count) TextBlock_Chain.Inlines.Add(new Run(" - "));
            }
        }

        bool IsStable(IList<decimal> values)
        {
            if (values.Count < 20)
                return false;

            decimal max = values.Max();
            decimal min = values.Min();

            // Check stability and minimum weight threshold (10g)
            bool stableRange = (max - min) <= 0.2m;
            bool allAbove10g = values.All(v => v >= 10m);

            return stableRange && allAbove10g;
        }

        private void Confirm()
        {
            if (!isSelected) return;            

            isSelected = false;
            isConfirm = true;
                        
            WeightDisplay.Text = "Pick Up";

            SQLHelper.Add(selectedButton, currentValue);            
            player_Confirm.Play();                                    

            if (selectedButton is not Button btn || btn.Content is not Viewbox viewbox) return;
            if (viewbox.Child is not StackPanel panel) return;
            if (panel.Children[0] is not TextBlock nameTextBlock || panel.Children[1] is not TextBlock valueTextBlock) return;
            valueTextBlock.Text = $"{currentValue:0.00} g";
            nameTextBlock.Foreground = Brushes.Gray;
            valueTextBlock.Foreground = Brushes.Gray;
        }
       
             
        private Dictionary<string, float> LoadTodayValues()
        {
            var result = new Dictionary<string, float>();

            string today = DateTime.Now.ToString("yyyy-MM-dd");
            string connectionString = "Data Source=WeightReader.db;Version=3;";

            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();

                string sql = "SELECT * FROM Weight WHERE date = @date LIMIT 1";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@date", today);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            return result;   // no row for today

                        // Loop through all columns in the row
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            string col = reader.GetName(i);

                            if (col == "Id" || col == "Date")
                                continue;

                            if (reader.IsDBNull(i))
                            {
                                result[col] = 0;
                                continue;
                            }

                            // Safely convert to float regardless of SQL numeric type
                            float val = Convert.ToSingle(reader.GetValue(i), System.Globalization.CultureInfo.InvariantCulture);
                            result[col] = val;
                        }
                    }
                }

                conn.Close();
            }
            return result;
        }

        private void InitializeButtons()
        {
            var data = LoadTodayValues();
            foreach (var pair in data)
            {
                string tray = pair.Key;
                float value = pair.Value;
                if (FindName("Button_" + tray) is Button btn && btn.Content is Viewbox view && view.Child is Panel panel && panel.Children.Count > 1 && panel.Children[1] is TextBlock valueText)
                {
                    valueText.Text = $"{value:0.00} g";
                }
            }
        }

        private void InitializeSequence()
        {
            // -- Sequences List --
            sequence.Add(Button_B5); sequence.Add(Button_A5); sequence.Add(Button_C5); sequence.Add(Button_B6); sequence.Add(Button_E5); sequence.Add(Button_D5);
            sequence.Add(Button_A2); sequence.Add(Button_A3); sequence.Add(Button_A4);
            sequence.Add(Button_B2); sequence.Add(Button_B3); sequence.Add(Button_B4); sequence.Add(Button_A1);
            sequence.Add(Button_C2); sequence.Add(Button_C3); sequence.Add(Button_C4); sequence.Add(Button_C1);
            sequence.Add(Button_E4); sequence.Add(Button_E3); sequence.Add(Button_E2); sequence.Add(Button_E1);
            sequence.Add(Button_D3); sequence.Add(Button_D4); sequence.Add(Button_D2); sequence.Add(Button_D1);
        }     

        private void NextSequence()
        {
            for (int i = 0; i < sequence.Count; i++)
            {
                if (sequence[i] == selectedButton)
                {
                    if (i + 1 < sequence.Count)
                    {
                        // Click next button
                        sequence[i + 1].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));                      
                        return;
                    }
                    // If it's the last one                   
                    // Trigger the async method and move on
                    _ = Dispatcher.InvokeAsync(EndSequence);
                    return;
                }
            }
        }

        private async Task EndSequence()
        {
            // 1. Lockdown immediately - no turning back now!
            foreach (var b in ButtonUniformGrid.Children.OfType<Button>())
            {
                b.IsEnabled = false;
            }

            timer.Stop();
            timer.Tick -= Timer_Tick;
            SelectedTray.Text = "SombaOpu";
            WeightDisplay.Text = "💎";
            player_Switch.Play();

            // 2. The Persistent Internet Loop
            Interface.Text = "Checking connection... 🌐";
            await Task.Delay(1000); // The Benevolent Delay™

            while (!await CheckInternetConnection())
            {
                Interface.Text = "No Internet! ❌\nRetrying... Please check your Wi-Fi.";
                await Task.Delay(1000); // Wait 1 second before checking again
            }

            // 3. Internet is back! Start the data transfer            
            Task sendingTask = Task.Run(() => {
                SQLHelper.SendWeightToGoogleSheets();
            });

            // 4. The Countdown loop (runs while data sends)
            for (int i = 3; i > 0; i--)
            {
                Interface.Text = $"Sending Data to Google Sheets...\nClosing in {i}...";
                await Task.Delay(1000);
            }

            // 5. Finalize
            await sendingTask;

            Interface.Text = "Data Sent Successfully! ✅\nFinalizing...";
            await Task.Delay(1000); // The Benevolent Delay™

            SQLHelper.OpenWebsite();
            App.Current.Shutdown();
        }

        private async Task<bool> CheckInternetConnection()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(2);
                    // Using a "Head" request is even faster/cheaper than "Get"
                    var request = new HttpRequestMessage(HttpMethod.Head, "https://www.google.com");
                    using (var response = await client.SendAsync(request))
                    {
                        return response.IsSuccessStatusCode;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        string ExtractNumber(string input)
        {
            var match = Regex.Match(input, @"\d+(\.\d+)?");
            return match.Success ? match.Value : "";
        }

        void AddLog(decimal value)
        {
            logs.Add(value);
            if (logs.Count > maxSize)
                logs.RemoveAt(0);
        }

        private void UpdateTextBlockTrayButtonColor()
        {
            foreach (Button b in ButtonUniformGrid.Children)
            {
                if (b.Content is Viewbox v && v.Child is StackPanel p)
                {
                    var labelText = p.Children[0] as TextBlock;
                    var valueText = p.Children[1] as TextBlock;
                    bool hasValue = valueText != null && valueText.Text != "0,00 g";    

                    Brush textBrush = b == selectedButton ? Brushes.DarkGreen : hasValue ? Brushes.Gray : Brushes.Black;
                    if (labelText != null) labelText.Foreground = textBrush;
                    if (valueText != null) valueText.Foreground = textBrush;
                }
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Escape)            
            {
                Button_Click_Cancel();                
                e.Handled = true;
            }

            if (e.Key == Key.Space)
            {
                if (isSelected)
                {
                    Confirm();
                    e.Handled = true;
                    return;
                }

                if (!isSelected)
                {
                    sequence[0].RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); // -- Start First Sequence --
                    e.Handled = true;
                    return;
                }
                                
            }

            if(e.Key == Key.End)
            {
                // Trigger the async method and move on
                _ = Dispatcher.InvokeAsync(EndSequence);
                e.Handled = true;
            }            
        }

        // Cleanup on close to ensure process exits (stop timers, close serial, dispose players)
        protected override void OnClosing(CancelEventArgs e)
        {
            // Clean up hardware and timers
            timer?.Stop();

            if (serialPort != null)
            {
                serialPort.DataReceived -= SerialPort_DataReceived;
                if (serialPort.IsOpen) serialPort.Close();
                serialPort.Dispose();
            }

            player_Switch?.Dispose();
            player_Confirm?.Dispose();

            // Kill everything
            base.OnClosing(e);
            App.Current.Shutdown();
        }
    }
}
