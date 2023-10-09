using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;

namespace DynamicLOD
{
    public class ServiceModel
    {
        public bool ServiceExited { get; set; } = false;
        public bool CancellationRequested { get; set; } = false;

        public bool IsSimRunning { get; set; } = false;
        public bool IsSessionRunning { get; set; } = false;

        public MemoryManager MemoryAccess { get; set; } = null;
        public int VerticalTrend {  get; set; }

        public List<(float, float)> PairsTLOD { get; set; }
        public int CurrentPairTLOD;
        public List<(float, float)> PairsOLOD { get; set; }
        public int CurrentPairOLOD;
        public bool fpsMode;
        public bool UseTargetFPS { get; set; }
        public int TargetFPS { get; set; }
        public int TargetFPSIndex { get; set; }
        public int ConstraintTicks { get; set; }
        public float DecreaseTLOD { get; set; }
        public float DecreaseOLOD { get; set; }
        public float MinLOD { get; set; }

        public string LogLevel { get; set; }
        public static int MfLvarsPerFrame { get; set; }
        public bool WaitForConnect { get; set; }
        public bool OpenWindow { get; set; }
        public string SimBinary { get; set; }
        public string SimModule { get; set; }


        protected Configuration AppConfiguration;

        public ServiceModel()
        {
            CurrentPairTLOD = 0;
            CurrentPairOLOD = 0;
            LoadConfiguration();
        }

        protected void LoadConfiguration()
        {
            AppConfiguration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var settings = AppConfiguration.AppSettings.Settings;

            LogLevel = Convert.ToString(settings["logLevel"].Value) ?? "Debug";
            MfLvarsPerFrame = Convert.ToInt32(settings["mfLvarPerFrame"].Value);
            WaitForConnect = Convert.ToBoolean(settings["waitForConnect"].Value);
            OpenWindow = Convert.ToBoolean(settings["openWindow"].Value);
            SimBinary = Convert.ToString(settings["simBinary"].Value);
            SimModule = Convert.ToString(settings["simModule"].Value);
            UseTargetFPS = Convert.ToBoolean(settings["useTargetFPS"].Value);
            TargetFPS = Convert.ToInt32(settings["targetFPS"].Value);
            TargetFPSIndex = Convert.ToInt32(settings["targetFpsIndex"].Value);
            ConstraintTicks = Convert.ToInt32(settings["constraintTicks"].Value);
            DecreaseTLOD = Convert.ToSingle(settings["decreaseTlod"].Value, new RealInvariantFormat(settings["decreaseTlod"].Value));
            DecreaseOLOD = Convert.ToSingle(settings["decreaseOlod"].Value, new RealInvariantFormat(settings["decreaseOlod"].Value));
            MinLOD = Convert.ToSingle(settings["minLod"].Value, new RealInvariantFormat(settings["minLod"].Value));

            PairsTLOD = new();
            LoadPairs("tlodPairs", settings, PairsTLOD);
            PairsOLOD = new();
            LoadPairs("olodPairs", settings, PairsOLOD);
        }

        public static void LoadPairs(string key, KeyValueConfigurationCollection settings, List<(float, float)> pairsList)
        {
            string[] strPairs = Convert.ToString(settings[key].Value).Split('|');
            int alt;
            float lod;
            foreach (string pair in strPairs)
            {
                string[] parts = pair.Split(':');
                alt = Convert.ToInt32(parts[0]);
                lod = Convert.ToSingle(parts[1], new RealInvariantFormat(parts[1]));
                pairsList.Add((alt, lod));
            }
            SortTupleList(pairsList);
        }

        public static void SortTupleList(List<(float, float)> pairsList)
        {
            pairsList.Sort((x, y) => x.Item1.CompareTo(y.Item1));
        }

        public static string CreateLodString(List<(float, float)> pairsList)
        {
            string result = "";
            bool first = true;

            foreach (var pair in pairsList)
            {
                if (first)
                    first = false;
                else
                    result += "|";

                result += $"{Convert.ToString((int)pair.Item1)}:{Convert.ToString(pair.Item2, CultureInfo.InvariantCulture)}";
            }

            return result;
        }

        protected void SaveConfiguration()
        {
            AppConfiguration.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection(AppConfiguration.AppSettings.SectionInformation.Name);
        }

        public string GetSetting(string key, string defaultValue = "")
        {
            return AppConfiguration.AppSettings.Settings[key].Value ?? defaultValue;
        }

        public void SetSetting(string key, string value)
        {
            if (AppConfiguration.AppSettings.Settings[key] != null)
            {
                AppConfiguration.AppSettings.Settings[key].Value = value;
                SaveConfiguration();
                LoadConfiguration();
            }
        }

        public void SavePairs()
        {
            AppConfiguration.AppSettings.Settings["tlodPairs"].Value = CreateLodString(PairsTLOD);
            AppConfiguration.AppSettings.Settings["olodPairs"].Value = CreateLodString(PairsOLOD);
            SaveConfiguration();
            LoadConfiguration();
        }
    }
}