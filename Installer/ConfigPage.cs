using CFIT.Installer.Product;
using CFIT.Installer.UI.Behavior;
using CFIT.Installer.UI.Config;

namespace Installer
{
    public class ConfigPage : PageConfig
    {
        public Config Config { get { return BaseConfig as Config; } }

        public override void CreateConfigItems()
        {
            string word = "Install";
            if (Config.Mode == SetupMode.UPDATE)
                word = "Update";

            ConfigItemHelper.CreateRadioAutoStart(Config, Items);
            if (Config.Mode == SetupMode.INSTALL)
                ConfigItemHelper.CreateCheckboxDesktopLink(Config, ConfigBase.OptionDesktopLink, Items);

            Items.Add(new ConfigItemCheckbox("Start App", $"Start DynamicLOD after {word}", Config.OptionOpenApp, Config));

            if (Config.Mode == SetupMode.UPDATE)
                Items.Add(new ConfigItemCheckbox("Reset Configuration", "Reset App Configuration to Default (only for Troubleshooting)", Config.OptionResetConfiguration, Config));
        }
    }
}
