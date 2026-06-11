using CFIT.AppTools;
using DynamicLOD.AppConfig;
using DynamicLOD.UI.Views.Monitor;
using DynamicLOD.UI.Views.Profiles;
using DynamicLOD.UI.Views.Settings;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Navigation;

namespace DynamicLOD.UI
{
    public interface IView
    {
        public void Start();
        public void Stop();
    }

    public partial class AppWindow : Window
    {
        public static UiIconLoader IconLoader { get; } = new(Assembly.GetExecutingAssembly(), IconLoadSource.Embedded, "DynamicLOD.UI.Icons.");
        protected virtual Button CurrentButton { get; set; } = null;
        protected virtual IView CurrentView { get; set; } = null;
        protected static SolidColorBrush BrushDefault { get; } = SystemColors.WindowFrameBrush;
        protected static SolidColorBrush BrushHighlight { get; } = SystemColors.HighlightBrush;
        protected static Thickness ThicknessDefault { get; } = new(1);
        protected static Thickness ThicknessHighlight { get; } = new(1.5);

        protected virtual IView ViewMonitor { get; } = new ViewMonitor();
        protected virtual IView ViewProfiles { get; } = new ViewProfiles();
        protected virtual IView ViewSettings { get; } = new ViewSettings();

        protected virtual Config Config => AppService.Instance?.Config;

        public AppWindow()
        {
            InitializeComponent();
            this.Loaded += OnWindowLoaded;
            this.IsVisibleChanged += OnVisibleChanged;

            ButtonMonitor.Click += (_, _) => SetView(ButtonMonitor, ViewMonitor);
            ButtonProfiles.Click += (_, _) => SetView(ButtonProfiles, ViewProfiles);
            ButtonSettings.Click += (_, _) => SetView(ButtonSettings, ViewSettings);

            DynamicLOD.Instance.NewVersion += (version, timestamp) => SetAppUpdateNotice("Version", $"{version.ToString(3)}-{timestamp}");
            DynamicLOD.Instance.NewBuild += (version, timestamp) => SetAppUpdateNotice("Build", $"{version.ToString(3)}-{timestamp}");
        }

        public virtual void SetAppUpdateNotice(string type, string version)
        {
            LabelVersionCheck.Inlines.Add($"New App {type} ");
            var run = new Run($"{version}");

            Hyperlink hyperlink = new(run)
            {
                NavigateUri = new Uri(AppService.Instance.Definition.ProductLatestUrl)
            };
            LabelVersionCheck.Inlines.Add(hyperlink);
            LabelVersionCheck.Inlines.Add(" available!");
            this.AddHandler(Hyperlink.RequestNavigateEvent, new RequestNavigateEventHandler(Nav.RequestNavigateHandler));
            PanelVersion.Visibility = Visibility.Visible;
            DynamicLOD.Instance.NotifyIcon.SetIconUpdate();
            this.Icon = DynamicLOD.Instance.NotifyIcon.Model.AppIcon.ToImageSource();
        }

        protected virtual void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (CurrentButton == null)
                SetView(ButtonMonitor, ViewMonitor);
        }

        protected virtual void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.Visibility != Visibility.Visible)
                CurrentView?.Stop();
            else
                CurrentView?.Start();
        }

        protected virtual void SetView(Button menuButton, IView viewControl)
        {
            CurrentView?.Stop();

            if (CurrentButton != null)
            {
                CurrentButton.IsHitTestVisible = true;
                CurrentButton.BorderBrush = BrushDefault;
                CurrentButton.BorderThickness = ThicknessDefault;
            }

            if (CurrentView != null)
                ViewControl.SizeChanged -= OnViewSizeChanged;

            CurrentButton = menuButton;
            CurrentButton.IsHitTestVisible = false;
            CurrentButton.BorderBrush = BrushHighlight;
            CurrentButton.BorderThickness = ThicknessHighlight;

            ViewControl.Content = viewControl;
            CurrentView = viewControl;
            ViewControl.SizeChanged += OnViewSizeChanged;
            viewControl.Start();
            InvalidateArrange();
            InvalidateMeasure();
            InvalidateVisual();
        }

        protected virtual void OnViewSizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                double height = Math.Max(ViewControl.ActualHeight + 96, 0);
                this.MinHeight = height;
                this.Height = height;
            }
            catch { }
        }
    }
}
