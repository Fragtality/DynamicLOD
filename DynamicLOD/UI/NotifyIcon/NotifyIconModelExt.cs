using CFIT.AppFramework;
using CFIT.AppFramework.UI.NotifyIcon;
using CommunityToolkit.Mvvm.Input;

namespace DynamicLOD.UI.NotifyIcon
{
    public partial class NotifyIconModelExt(ISimApp simApp) : NotifyIconViewModel(simApp)
    {
        protected override void CreateItems()
        {
            base.CreateItems();
            Items.Add(new(null, null));
            Items.Add(new($"Toggle Freeze", ToggleFreezeCommand));
            Items.Add(new(null, null));
            Items.Add(new($"Restart {SimApp.DefinitionBase.ProductName}", RestartAppCommand));
        }

        [RelayCommand]
        public virtual void RestartApp()
        {
            try { (SimApp as DynamicLOD).AppService.ResetRequested = AppResetRequest.App; } catch { }
        }

        [RelayCommand]
        public virtual void ToggleFreeze()
        {
            try { (SimApp as DynamicLOD).AppService.ToggleFreeze(); } catch { }
        }
    }
}
