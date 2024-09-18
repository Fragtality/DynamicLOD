using System;
using System.Text.RegularExpressions;

namespace Installer
{
    public static class Parameters
    {
        public static readonly string fileName = "AppPackage.zip";

        public static readonly string appName = "DynamicLOD";
        public static readonly string appBinary = $"{appName}.exe";
        public static readonly string appDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\DynamicLOD";
        public static readonly string binDir = appDir + @"\bin";
        public static readonly string binPath = binDir + @"\DynamicLOD.exe";
        public static readonly string confFile = appDir + @"\DynamicLOD.config";

        public static readonly Regex netDesktop = new Regex(@"Microsoft.WindowsDesktop.App (\d+\.\d+\.\d+).+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly int netMajor = 8;
        public static readonly int netMinor = 0;
        public static readonly int netPatch = 8;
        public static readonly string netVersion = $"{netMajor}.{netMinor}.{netPatch}";
        public static readonly string netUrl = "https://download.visualstudio.microsoft.com/download/pr/907765b0-2bf8-494e-93aa-5ef9553c5d68/a9308dc010617e6716c0e6abd53b05ce/windowsdesktop-runtime-8.0.8-win-x64.exe";
        public static readonly string netUrlFile = "windowsdesktop-runtime-8.0.8-win-x64.exe";

        public static readonly string ipcRegPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\FSUIPC7";
        public static readonly string ipcRegInstallDirValue = "InstallDir";
        public static readonly string ipcRegValue = "DisplayVersion";
        public static readonly string ipcVersion = "7.4.0";

        public static readonly Regex wasmRegex = new Regex("^\\s*\"package_version\":\\s*\"([0-9\\.]+)\"\\s*,\\s*$", RegexOptions.Compiled);
        public static readonly string wasmMobiName = "mobiflight-event-module";
        public static readonly string wasmMobiVersion = "1.0.1";
        public static readonly string wasmUrl = "https://github.com/MobiFlight/MobiFlight-WASM-Module/releases/download/1.0.1/mobiflight-event-module.1.0.1.zip";
        public static readonly string wasmUrlFile = "mobiflight-event-module.1.0.1.zip";

        public static readonly string msConfigStore = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Packages\Microsoft.FlightSimulator_8wekyb3d8bbwe\LocalCache\UserCfg.opt";
        public static readonly string msConfigSteam = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Microsoft Flight Simulator\UserCfg.opt";
        public static readonly string msStringPackage = "InstalledPackagesPath ";
        public static readonly string msExeStore = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Packages\Microsoft.FlightSimulator_8wekyb3d8bbwe\LocalCache\EXE.xml";
        public static readonly string msExeSteam = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Microsoft Flight Simulator\EXE.xml";
    }
}
