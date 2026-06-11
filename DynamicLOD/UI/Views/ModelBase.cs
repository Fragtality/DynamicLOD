
using CFIT.AppFramework.Messages;
using CFIT.AppFramework.UI.ViewModels;
using CFIT.SimConnectLib;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicLOD.AppConfig;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace DynamicLOD.UI.Views
{
    public abstract partial class ModelBase<TObject>(TObject source, AppService appService) : ViewModelBase<TObject>(source), IDisposable where TObject : class
    {
        public virtual AppService AppService { get; } = appService;
        public virtual Config Config => AppService.Config;
        public virtual SimConnectController SimConnectController => AppService.SimService.Controller;
        public virtual SimConnectManager SimConnect => AppService.SimConnect;
        public virtual SettingProfile SettingProfile => AppService.SettingProfile;
        private static bool _inhibitConfigSave = false;
        protected bool _isDisposed = false;
        public virtual bool InhibitConfigSave { get => _inhibitConfigSave; set => _inhibitConfigSave = value; }
        public virtual bool IsMsgSvcSubscribed { get; protected set; } = false;

        protected override void InitializeMemberBindings()
        {
            base.InitializeMemberBindings();
        }

        public virtual void InitializeMessageService()
        {
            AppService.MessageService.Subscribe<MsgSessionReady>(OnSessionReady);
            AppService.MessageService.Subscribe<MsgSessionEnded>(OnSessionEnded);
            IsMsgSvcSubscribed = true;
        }

        public virtual void SaveConfig()
        {
            if (!_inhibitConfigSave)
                Config.SaveConfiguration();
        }

        public virtual void SetModelValue<T>(T value, Func<T, ValidationContext, ValidationResult> validator = null, Action<T, T> callback = null, [CallerMemberName] string propertyName = null!)
        {
            SetSourceValue(value, validator, callback, propertyName);
            SaveConfig();
        }

        protected virtual Task OnSessionReady()
        {
            this.SetObservedProperty(nameof(IsSessionRunning), true);
            return Task.CompletedTask;
        }

        protected virtual Task OnSessionEnded()
        {
            this.SetObservedProperty(nameof(IsSessionRunning), false);
            return Task.CompletedTask;
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsSessionStopped))]
        public partial bool IsSessionRunning { get; set; } = false;

        public virtual bool IsSessionStopped => !IsSessionRunning;

        [RelayCommand]
        public virtual void SetProfile(SettingProfile settingProfile)
        {
            if (settingProfile == null)
                return;

            AppService.Instance.SetSettingProfile(settingProfile.Name);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_isDisposed && IsMsgSvcSubscribed)
            {
                AppService.MessageService.Unsubscribe<MsgSessionReady>(OnSessionReady);
                AppService.MessageService.Unsubscribe<MsgSessionEnded>(OnSessionEnded);
            }
            _isDisposed = true;
        }
    }
}
