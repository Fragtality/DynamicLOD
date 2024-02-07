using H.NotifyIcon;
using Serilog;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
namespace DynamicLOD
{
    public partial class App : Application
    {
        private ServiceModel Model;
        private ServiceController Controller;

        private TaskbarIcon notifyIcon;

        public static new App Current => Application.Current as App;
        public static string ConfigFile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\DynamicLOD\DynamicLOD.config";
        public static string AppDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\DynamicLOD\bin";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (Process.GetProcessesByName("DynamicLOD").Length > 1)
            {
                MessageBox.Show("DynamicLOD is already running!", "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }

            Directory.SetCurrentDirectory(AppDir);

            if (!File.Exists(ConfigFile))
            {
                ConfigFile = Directory.GetCurrentDirectory() + @"\DynamicLOD.config";
                if (!File.Exists(ConfigFile))
                {
                    MessageBox.Show("No Configuration File found! Closing ...", "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Shutdown();
                    return;
                }
            }

            Model = new();
            InitLog();
            InitSystray();

            Controller = new(Model);
            Task.Run(Controller.Run);

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += OnTick;
            timer.Start();

            MainWindow = new MainWindow(notifyIcon.DataContext as NotifyIconViewModel, Model);
            if (Model.OpenWindow)
                MainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (Model != null)
                Model.CancellationRequested = true;
            notifyIcon?.Dispose();
            base.OnExit(e);

            Logger.Log(LogLevel.Information, "App:OnExit", "DynamicLOD exiting ...");
        }

        protected void OnTick(object sender, EventArgs e)
        {
            if (Model.ServiceExited)
            {
                Current.Shutdown();
            }
        }

        protected void InitLog()
        {
            string logFilePath = @"..\log\" + Model.GetSetting("logFilePath", "Fenix2GSX.log");
            string logLevel = Model.GetSetting("logLevel", "Debug"); ;
            LoggerConfiguration loggerConfiguration = new LoggerConfiguration().WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 3,
                                                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message} {NewLine}{Exception}");
            if (logLevel == "Warning")
                loggerConfiguration.MinimumLevel.Warning();
            else if (logLevel == "Debug")
                loggerConfiguration.MinimumLevel.Debug();
            else if (logLevel == "Verbose")
                loggerConfiguration.MinimumLevel.Verbose();
            else
                loggerConfiguration.MinimumLevel.Information();
            Log.Logger = loggerConfiguration.CreateLogger();
            Log.Information($"-----------------------------------------------------------------------");
            Logger.Log(LogLevel.Information, "App:InitLog", $"DynamicLOD started! Log Level: {logLevel} Log File: {logFilePath}");
        }

        protected void InitSystray()
        {
            Logger.Log(LogLevel.Information, "App:InitSystray", $"Creating SysTray Icon ...");
            notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
            notifyIcon.Icon = GetIcon("icon.ico");
            notifyIcon.ForceCreate(false);
        }

        public static Icon GetIcon(string filename)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"DynamicLOD.{filename}");
            return new Icon(stream);
        }
    }
}
