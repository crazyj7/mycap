using System.Windows;
using System.Threading;

namespace MyCap
{
    public partial class App : System.Windows.Application
    {
        private static Mutex? _mutex;
        public static bool CreatedNew { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            const string appName = "MyCapApp";

            _mutex = new Mutex(true, appName, out bool createdNew);
            CreatedNew = createdNew;

            if (!CreatedNew)
            {
                // If another instance is already running, shut down this one
                System.Windows.MessageBox.Show("MyCap is already running.", "Instance Check", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            base.OnStartup(e);
            
            // Set ShutdownMode to OnExplicitShutdown so the application doesn't
            // terminate when the main window is closed
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Release the mutex
            try {
            _mutex?.ReleaseMutex();
            }
            catch(Exception ex)
            {
            }
            base.OnExit(e);
        }
    }
} 