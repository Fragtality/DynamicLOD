using CFIT.AppFramework;
using CFIT.AppLogger;
using DynamicLOD.AppConfig;
using DynamicLOD.UI;
using DynamicLOD.UI.NotifyIcon;
using System;

namespace DynamicLOD
{
    public class DynamicLOD(Type windowType) : SimApp<DynamicLOD, AppService, Config, Definition>(windowType, typeof(NotifyIconModelExt))
    {
        [STAThread]
        public static int Main(string[] args)
        {
            try
            {
                var app = new DynamicLOD(typeof(AppWindow));
                return app.Start(args);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return -1;
            }
        }

        protected override void InitAppWindow()
        {
            base.InitAppWindow();
            AppContext.SetSwitch("Switch.System.Windows.Controls.Grid.StarDefinitionsCanExceedAvailableSpace", true);
        }
    }
}
