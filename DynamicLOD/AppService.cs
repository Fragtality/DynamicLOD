using CFIT.AppFramework.Messages;
using CFIT.AppFramework.Services;
using CFIT.AppFramework.UI.ViewModels;
using CFIT.AppLogger;
using CFIT.SimConnectLib.Definitions;
using CFIT.SimConnectLib.SimResources;
using CFIT.SimConnectLib.SimVars;
using DynamicLOD.AppConfig;
using DynamicLOD.Memory;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DynamicLOD
{
    public enum AppResetRequest
    {
        None = 0,
        App = 1,
    }

    public class AppService : AppService<DynamicLOD, AppService, Config, Definition>, INotifyPropertyChanged
    {
        public const string Process2024Name = "FlightSimulator2024";
        public virtual CancellationTokenSource RequestTokenSource { get; protected set; }
        public virtual CancellationToken RequestToken { get; protected set; }
        public virtual MemoryManager MemoryManager { get; protected set; } = null;
        public virtual LodController LodController { get; protected set; } = null;
        public virtual bool IsMsfs2020 => SimService?.Manager?.GetSimVersion() == SimVersion.MSFS2020;
        public virtual bool IsMsfs2024 => SimService?.Manager?.GetSimVersion() == SimVersion.MSFS2024;
        public virtual string AircraftString => SimConnect?.AircraftString ?? "";
        public virtual bool IsProfileLoaded => SettingProfile != null;
        public virtual SettingProfile SettingProfile { get; protected set; } = null;
        public virtual string ProfileName => SettingProfile?.Name ?? "NULL";
        public virtual AppResetRequest ResetRequested { get; set; } = AppResetRequest.None;
        protected virtual bool IsSessionInitialized { get; set; } = false;
        public virtual bool IsFrozen { get; protected set; } = false;
        protected virtual bool LastVr { get; set; } = false;
        protected virtual DateTime NextGarbageCollection { get; set; } = DateTime.Now + TimeSpan.FromSeconds(300);
        protected virtual ISimResourceSubscription SubIsVr { get; set; }
        public virtual bool IsInVr => SubIsVr?.GetNumber() == 1;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action<SettingProfile> ProfileChanged;
        public event Action ProfileCollectionChanged;

        public AppService(Config config) : base(config)
        {
            RefreshToken();
        }

        protected virtual void RefreshToken()
        {
            RequestTokenSource = CancellationTokenSource.CreateLinkedTokenSource(DynamicLOD.Instance.Token);
            RequestToken = RequestTokenSource.Token;
        }

        protected override void CreateServiceControllers()
        {

        }

        protected override async Task DoInit()
        {
            await base.DoInit();
            SubIsVr = SimStore.AddVariable("E:IS IN VR", SimUnitType.Bool);
            MessageService.Subscribe<MsgSessionReady>(SessionInitialize);
            MessageService.Subscribe<MsgSessionEnded>(SessionCleanup);
        }

        protected override Task DoCleanup()
        {
            try
            {
                LodController?.Cleanup();
                LodController = null;
                MemoryManager?.Cleanup();
                MemoryManager = null;

                SimStore.Remove("E:IS IN VR");
                SubIsVr = null;
                MessageService.Unsubscribe<MsgSessionReady>(SessionInitialize);
                MessageService.Unsubscribe<MsgSessionEnded>(SessionCleanup);

                return base.DoCleanup();
            }
            catch { }

            return Task.CompletedTask;
        }

        protected virtual void DoGarbageCollect()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            NextGarbageCollection = DateTime.Now + TimeSpan.FromSeconds(300);
            Logger.Verbose("Garbage collected");
        }

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            ModelHelper.RunOnDispatcher(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
        }

        public virtual void NotifyProfileCollectionChanged()
        {
            if (ProfileCollectionChanged != null)
                ModelHelper.RunOnDispatcher(() => ProfileCollectionChanged?.Invoke());
        }

        public virtual void NotifyProfileChanged()
        {
            if (ProfileChanged != null)
                ModelHelper.RunOnDispatcher(() => ProfileChanged?.Invoke(SettingProfile));
        }

        protected virtual async Task SessionInitialize()
        {
            try
            {
                if (IsSessionInitialized)
                    return;

                Logger.Debug($"Refresh Token");
                RefreshToken();
                Logger.Information("Initializing Sim Session");
                var pointDef = Config.SimPointerDefinitions[SimVariant.MSFS2020_STEAM];
                if (IsMsfs2024)
                {
                    Process msfs2024 = Process.GetProcesses().Where(p => p.ProcessName.Equals(Process2024Name)).FirstOrDefault();
                    var last = msfs2024?.MainModule?.FileName?.Replace($"\\{Process2024Name}.exe", "")?.Split('\\')?.Last() ?? "";
                    if (last?.Contains("Limitless", StringComparison.InvariantCultureIgnoreCase) == true || Config.Force24Variant == SimVariant.MSFS2024_MSSTORE)
                        pointDef = Config.SimPointerDefinitions[SimVariant.MSFS2024_MSSTORE];
                    else if (last?.Contains("MSFS2024", StringComparison.InvariantCultureIgnoreCase) == true || Config.Force24Variant == SimVariant.MSFS2024_STEAM)
                        pointDef = Config.SimPointerDefinitions[SimVariant.MSFS2024_STEAM];
                    else
                        pointDef = Config.SimPointerDefinitions[SimVariant.MSFS2024_STEAM];
                }

                MemoryManager = new(pointDef);
                await Task.Delay(250);
                LodController = new();
                SetSettingProfile();

                IsSessionInitialized = true;
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }

            Logger.Debug("--- Session Init Done ---");
        }

        protected virtual async Task SessionCleanup()
        {
            try
            {
                if (!IsSessionInitialized)
                    return;

                Logger.Debug($"Cancel Request Token");
                RequestTokenSource.Cancel();

                LodController?.Cleanup();
                LodController = null;
                MemoryManager?.Cleanup();
                MemoryManager = null;
                IsFrozen = false;
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }

            IsSessionInitialized = false;
            Logger.Debug("--- Session Cleanup Done ---"); ;
        }

        protected override async Task StopServiceControllers()
        {
            await SessionCleanup();
            await base.StopServiceControllers();
        }

        protected override async Task MainLoop()
        {
            try
            {
                if (ResetRequested > AppResetRequest.None)
                {
                    Logger.Debug($"Reset was requested: {ResetRequested}");
                    await ResetApp();
                }
                else if (DateTime.Now >= NextGarbageCollection)
                    DoGarbageCollect();
                else if (IsSessionInitialized && LodController?.IsReady == true)
                {
                    if (LastVr != IsInVr && SettingProfile.IsVR != IsInVr)
                    {
                        Logger.Debug("VR Mode changed");
                        var profile = SearchSettingProfile();
                        if (profile != null && profile.Name != SettingProfile.Name)
                        {
                            SetSettingProfile(profile.Name);
                            await Task.Delay(250);
                            return;
                        }
                    }
                    LastVr = IsInVr;

                    await LodController.Run();
                }

                if (RunCondition())
                    await Task.Delay(Config.TickInterval, Token);
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                {
                    Logger.LogException(ex);
                    await ResetApp();
                }
            }
        }


        public virtual void ToggleFreeze()
        {
            IsFrozen = !IsFrozen;
        }

        protected virtual async Task ResetApp()
        {
            Logger.Information($"Restarting App Services ...");
            await SessionCleanup();
            await Task.Delay(2500, Token);
            await SessionInitialize();
            ResetRequested = AppResetRequest.None;
        }

        public virtual void SetSettingProfile()
        {
            SetSettingProfile(SearchSettingProfile()?.Name);
        }

        public virtual void SetSettingProfile(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                SettingProfile = Config.SettingProfiles.First(p => p.Name == name);
                LodController?.ProfileWasSet = true;
                Logger.Information($"Using Profile {SettingProfile} (VR: {SettingProfile.IsVR})");
                NotifyProfileChanged();
                NotifyPropertyChanged(nameof(ProfileName));
                NotifyPropertyChanged(nameof(SettingProfile));
            }
        }

        public virtual SettingProfile SearchSettingProfile()
        {
            SettingProfile profile = SearchSettingProfile(Config.SettingProfiles);
            if (profile == null)
            {
                Logger.Error($"No Profile matched - using First or Default");
                profile = Config.SettingProfiles.First() ?? new SettingProfile();
            }
            else
                Logger.Information($"Matched Setting Profile: {profile}");

            return profile;
        }

        protected virtual SettingProfile SearchSettingProfile(IEnumerable<SettingProfile> settingProfiles)
        {
            Logger.Debug($"Matching Profiles ...");
            SettingProfile result = null;
            foreach (var profile in settingProfiles)
            {
                foreach (var match in profile.SimObjectMatches)
                {
                    if (profile.IsVR == IsInVr)
                        continue;

                    if (AircraftString?.Contains(match, StringComparison.InvariantCultureIgnoreCase) == true)
                    {
                        result = profile;
                        break;
                    }
                }

                if (result != null)
                    break;
            }

            if (result == null && IsInVr)
            {
                Logger.Debug($"No Profile found for VR - search first VR Profile ...");
                foreach (var profile in settingProfiles)
                {
                    if (profile.IsVR)
                    {
                        result = profile;
                        break;
                    }
                }
            }

            return result;
        }

        public virtual void AddSettingProfile(SettingProfile settingProfile)
        {
            if (settingProfile == null)
                return;

            Config.SettingProfiles.Add(settingProfile);
            Config.SortProfiles();
            NotifyProfileCollectionChanged();
        }

        public virtual bool RemoveSettingProfile(SettingProfile settingProfile)
        {
            if (settingProfile == null || Config.SettingProfiles.Count <= 1)
                return false;

            if (Config.SettingProfiles.Any((p) => p.Name.Equals(settingProfile.Name)))
            {
                Config.SettingProfiles.RemoveAll((p) => p.Name.Equals(settingProfile.Name));
                Config.SaveConfiguration();
                NotifyProfileCollectionChanged();
                if (!Config.SettingProfiles.Any(p => p.Name == SettingProfile?.Name))
                    SetSettingProfile();
                return true;
            }
            else
                return false;
        }

        public virtual void RenameSettingProfile(SettingProfile settingProfile, string name)
        {
            if (settingProfile == null)
                return;

            bool isActive = settingProfile.Name == SettingProfile.Name;
            settingProfile.Name = name;
            Config.SortProfiles();
            NotifyProfileCollectionChanged();
            if (isActive)
                NotifyPropertyChanged(nameof(ProfileName));
        }

        public virtual void UpdateSettingProfile(SettingProfile settingProfile, SettingProfile newData)
        {
            bool isActive = settingProfile.Name == SettingProfile?.Name;
            settingProfile.Copy(newData);
            Config.SortProfiles();
            if (isActive)
                NotifyPropertyChanged(nameof(ProfileName));
        }
    }
}
