using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicLOD.AppConfig;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using System.Windows.Threading;

namespace DynamicLOD.UI.Views.Monitor
{
    public partial class ModelMonitor(AppService source) : ModelBase<AppService>(source, source)
    {
        protected virtual DispatcherTimer UpdateTimer { get; set; }
        protected virtual bool ForceRefresh { get; set; } = false;
        protected static SolidColorBrush ColorValid { get; } = new(Colors.Green);
        protected static SolidColorBrush ColorInvalid { get; } = new(Colors.Red);
        public virtual ObservableCollection<string> MessageLog { get; } = [];
        protected virtual LodController LodController => AppService.Instance?.LodController;

        protected override void InitializeModel()
        {
            UpdateTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(AppService.Instance.Config.UiRefreshInterval),
            };
            UpdateTimer.Tick += OnUpdate;
        }

        public virtual void Start()
        {
            ForceRefresh = true;
            UpdateTimer.Start();
        }

        public virtual void Stop()
        {
            UpdateTimer?.Stop();
        }

        [RelayCommand]
        public virtual void LogDir()
        {
            try { Process.Start(new ProcessStartInfo(Path.Join(Config.Definition.ProductPath, Config.Definition.ProductLogPath)) { UseShellExecute = true }); } catch { }
        }

        protected virtual void UpdateBoolState(string propertyValue, string propertyColor, bool value, bool reverseColor = false)
        {
            try
            {
                if (string.IsNullOrEmpty(propertyValue) || (object)value == null)
                    return;

                if (this.GetPropertyValue<bool>(propertyValue) != value || ForceRefresh)
                {
                    this.SetPropertyValue<bool>(propertyValue, value);
                    UpdateColor(propertyColor, value, reverseColor);
                }
            }
            catch { }
        }

        protected virtual void UpdateColor(string propertyColor, bool state, bool reverseColor = false)
        {
            try
            {
                if (reverseColor)
                    this.SetPropertyValue<SolidColorBrush>(propertyColor, state ? ColorInvalid : ColorValid);
                else
                    this.SetPropertyValue<SolidColorBrush>(propertyColor, state ? ColorValid : ColorInvalid);
            }
            catch { }
        }

        protected virtual void UpdateState<T>(string propertyValue, T value)
        {
            try
            {
                if (string.IsNullOrEmpty(propertyValue) || (object)value == null)
                    return;

                if (!this.GetPropertyValue<T>(propertyValue)?.Equals(value) == true || ForceRefresh)
                    this.SetPropertyValue<T>(propertyValue, value);
            }
            catch { }
        }

        protected virtual void OnUpdate(object? sender, EventArgs e)
        {
            try { UpdateSim(); } catch (Exception ex) { Logger.LogException(ex); }
            try { UpdateApp(); } catch (Exception ex) { Logger.LogException(ex); }
            try { UpdateLog(); } catch { }
            ForceRefresh = false;
        }

        protected virtual void UpdateSim()
        {
            UpdateBoolState(nameof(SimRunning), nameof(SimRunningColor), SimConnectController.IsSimRunning);
            UpdateBoolState(nameof(SimConnected), nameof(SimConnectedColor), SimConnect.IsSimConnected);
            UpdateBoolState(nameof(SimSession), nameof(SimSessionColor), SimConnect.IsSessionRunning && !SimConnect.IsSessionStopped);

            UpdateBoolState(nameof(SimPaused), nameof(SimPausedColor), SimConnect.IsPaused, true);
            UpdateState<bool>(nameof(OnGround), LodController?.IsOnGround ?? true);
            UpdateState<long>(nameof(CameraState), SimConnect.CameraState);

            UpdateState<string>(nameof(SimVersion), SimConnect.SimVersionString);
        }

        [ObservableProperty]
        public partial bool OnGround { get; set; } = false;

        [ObservableProperty]
        public partial bool SimRunning { get; set; } = false;

        [ObservableProperty]
        public partial SolidColorBrush SimRunningColor { get; set; } = ColorInvalid;

        [ObservableProperty]
        public partial bool SimConnected { get; set; } = false;

        [ObservableProperty]
        public partial SolidColorBrush SimConnectedColor { get; set; } = ColorInvalid;

        [ObservableProperty]
        public partial bool SimSession { get; set; } = false;

        [ObservableProperty]
        public partial SolidColorBrush SimSessionColor { get; set; } = ColorInvalid;

        [ObservableProperty]
        public partial bool SimPaused { get; set; } = false;

        [ObservableProperty]
        public partial SolidColorBrush SimPausedColor { get; set; } = ColorInvalid;

        [ObservableProperty]
        public partial long CameraState { get; set; } = 0;

        [ObservableProperty]
        public partial string SimVersion { get; set; } = "";

        protected virtual void UpdateApp()
        {
            UpdateState<string>(nameof(Tlod), $"{LodController?.Tlod ?? 0} / {LodController?.TlodVr ?? 0}");
            UpdateState<string>(nameof(Olod), $"{LodController?.Olod ?? 0} / {LodController?.OlodVr ?? 0}");
            UpdateState<bool>(nameof(VrMode), AppService.Instance?.IsInVr == true);

            int alt = LodController?.IsOnGround == true ? 0 : (int)(LodController?.Altitude ?? 0.0);
            UpdateState<int>(nameof(Altitude), alt);
            UpdateState<int>(nameof(VerticalSpeed), (int)(LodController?.VerticalSpeed ?? 0.0));
            int trend = LodController?.VerticalTrend ?? 0;
            string text = LodController?.IsOnGround == true ? "On Ground" : "Cruise";
            if (trend > 0)
                text = "Climb";
            else if (trend < 0)
                text = "Descent";
            UpdateState<string>(nameof(VerticalTrend), text);

            UpdateBoolState(nameof(Frozen), nameof(FrozenColor), AppService.Instance?.IsFrozen ?? true, true);
            UpdateState<string>(nameof(AppProfile), SettingProfile?.Name ?? "");
        }

        [ObservableProperty]
        public partial string Tlod { get; set; } = "0 / 0";

        [ObservableProperty]
        public partial string Olod { get; set; } = "0 / 0";

        [ObservableProperty]
        public partial bool VrMode { get; set; } = false;

        [ObservableProperty]
        public partial int Altitude { get; set; } = 0;

        [ObservableProperty]
        public partial int VerticalSpeed { get; set; } = 0;

        [ObservableProperty]
        public partial string VerticalTrend { get; set; } = "None";

        [ObservableProperty]
        public partial bool Frozen { get; set; } = false;

        [ObservableProperty]
        public partial SolidColorBrush FrozenColor { get; set; } = ColorInvalid;

        [ObservableProperty]
        public partial string AppProfile { get; set; } = "";


        protected virtual void UpdateLog()
        {
            if (Logger.Messages.IsEmpty)
                NotifyPropertyChanged(nameof(MessageLog));

            while (!Logger.Messages.IsEmpty)
            {
                MessageLog.Add(Logger.Messages.Dequeue());
                if (MessageLog.Count > 12)
                    MessageLog.RemoveAt(0);
            }
        }
    }
}
