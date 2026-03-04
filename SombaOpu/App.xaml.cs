using System.Configuration;
using System.Data;
using System.Windows;

namespace SombaOpu
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {        
        // Give your Mutex a unique name (e.g., use a GUID)
        private static Mutex _mutex = new Mutex();

        protected override void OnStartup(StartupEventArgs e)
        {
            const string appName = "SombaOpu-17112001";
            bool createdNew;

            // Try to create the mutex
            _mutex = new Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                // App is already running! Show a message and exit.
                MessageBox.Show("The application is already running.", "Instance Check");
                Application.Current.Shutdown();
                return;
            }

            if (!ConfigLoader.isAppConfig())
            {                
                // App cannot run without this file.                                
                MessageBox.Show("Configuration file not found. Please ensure 'app-config.json' exists in the 'Credential' folder.", "Config Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Release the mutex when the app closes
            if (_mutex != null)
            {
                _mutex.ReleaseMutex();
                _mutex.Dispose();
            }
            base.OnExit(e);
        }        
    }
}
