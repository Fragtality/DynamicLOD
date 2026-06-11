using CFIT.Installer.LibFunc;
using CFIT.Installer.Product;
using System.IO;

namespace Installer
{
    public class Config : ConfigBase
    {
        public override string ProductName { get { return "DynamicLOD"; } }
        public override string ProductExePath { get { return Path.Combine(ProductPath, "bin", ProductExe); } }
        public virtual string InstallerExtractDir { get { return Path.Combine(ProductPath, "bin"); } }

        public static readonly string OptionOpenApp = "OpenApp";
        public static readonly string OptionResetConfiguration = "ResetConfiguration";

        //Worker: .NET
        public virtual bool NetRuntimeDesktop { get; set; } = true;
        public virtual string NetVersion { get; set; } = "10.0.9";
        public virtual bool CheckMajorEqual { get; set; } = true;
        public virtual string NetUrl { get; set; } = "https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/10.0.9/windowsdesktop-runtime-10.0.9-win-x64.exe";
        public virtual string NetInstaller { get; set; } = "windowsdesktop-runtime-10.0.9-win-x64.exe";

        public Config() : base()
        {
            SetOption(OptionOpenApp, false);
        }

        public override void CheckInstallerOptions()
        {
            base.CheckInstallerOptions();

            //ResetConfig
            SetOption(OptionResetConfiguration, false);

            if (FuncMsfs.IsRunning())
                SetOption(OptionOpenApp, true);
        }
    }
}
