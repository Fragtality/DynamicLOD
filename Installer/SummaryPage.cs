using CFIT.AppTools;
using CFIT.Installer.UI.Behavior;

namespace Installer
{
    public class SummaryPage : PageSummary
    {
        protected override void SetActions()
        {
            if (BaseWorker?.IsSuccess == true)
            {
                Window.ActionLeft = null;
            }
            else
            {
                Window.ActionLeft = (w) =>
                {
                    LogAction();
                };
            }

            Window.ActionRight = (w) =>
            {
                w.SetPage();
                if (BaseConfig.GetOption<bool>(Config.OptionOpenApp) == true && !BaseDefinition.IsRunning)
                    Sys.StartProcess(BaseConfig.ProductExePath, BaseConfig.ProductPath);
                w.Close();
            };
        }
    }
}