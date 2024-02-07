using System;
using System.Collections.Generic;
using System.Globalization;

namespace DynamicLOD
{
    public class ServiceModel
    {
        public static readonly int maxProfile = 3;
        private static readonly int BuildConfigVersion = 1;
        public int ConfigVersion { get; set; }
        public bool ServiceExited { get; set; } = false;
        public bool CancellationRequested { get; set; } = false;

        public bool IsSimRunning { get; set; } = false;
        public bool IsSessionRunning { get; set; } = false;

        public MemoryManager MemoryAccess { get; set; } = null;
        public int VerticalTrend { get; set; }
        public bool OnGround { get; set; } = true;
        public bool ForceEvaluation { get; set; } = false;

        public int SelectedProfile { get; set; } = 0;
        public bool ProfileSelectionChanged { get; set; } = false;
        public int LastProfile { get; set; } = 0;
        public bool[] ProfilesVR = new bool[maxProfile];
        public bool IsVrProfile { get { return ProfilesVR[SelectedProfile]; } }
        public bool IsVrModeActive { get { return MemoryAccess != null && MemoryAccess.IsVrModeActive(); } }
        public List<List<(float, float)>> PairsTLOD { get; set; }
        public int CurrentPairTLOD;
        public List<List<(float, float)>> PairsOLOD { get; set; }
        public int CurrentPairOLOD;
        public bool fpsMode { get; set; }
        public bool UseTargetFPS { get; set; }
        public int TargetFPS { get; set; }
        public int TargetFPSIndex { get; set; }
        public int ConstraintTicks { get; set; }
        public float DecreaseTLOD { get; set; }
        public float DecreaseOLOD { get; set; }
        public float MinLOD { get; set; }
        public float SimMinLOD { get; set; }
        public float DefaultTLOD { get; set; }
        public float DefaultOLOD { get; set; }

        public string LogLevel { get; set; }
        public static int MfLvarsPerFrame { get; set; }
        public bool WaitForConnect { get; set; }
        public bool OpenWindow { get; set; }
        public string SimBinary { get; set; }
        public string SimModule { get; set; }
        public long OffsetModuleBase { get; set; }
        public long OffsetPointerMain { get; set; }
        public long OffsetPointerTlod { get; set; }
        public long OffsetPointerTlodVr { get; set; }
        public long OffsetPointerOlod { get; set; }
        public long OffsetVrMode { get; set; }
        public bool AutoSwitchVr { get; set; }

        protected ConfigurationFile ConfigurationFile = new();

        public ServiceModel()
        {
            CurrentPairTLOD = 0;
            CurrentPairOLOD = 0;
            LoadConfiguration();
        }

        protected void LoadConfiguration()
        {
            ConfigurationFile.LoadConfiguration();

            LogLevel = Convert.ToString(ConfigurationFile.GetSetting("logLevel", "Debug"));
            MfLvarsPerFrame = Convert.ToInt32(ConfigurationFile.GetSetting("mfLvarPerFrame", "15"));
            ConfigVersion = Convert.ToInt32(ConfigurationFile.GetSetting("ConfigVersion", "1"));
            WaitForConnect = Convert.ToBoolean(ConfigurationFile.GetSetting("waitForConnect", "true"));
            OpenWindow = Convert.ToBoolean(ConfigurationFile.GetSetting("openWindow", "true"));
            SimBinary = Convert.ToString(ConfigurationFile.GetSetting("simBinary", "FlightSimulator"));
            SimModule = Convert.ToString(ConfigurationFile.GetSetting("simModule", "WwiseLibPCx64P.dll"));
            UseTargetFPS = Convert.ToBoolean(ConfigurationFile.GetSetting("useTargetFps", "true"));
            TargetFPS = Convert.ToInt32(ConfigurationFile.GetSetting("targetFps", "40"));
            TargetFPSIndex = Convert.ToInt32(ConfigurationFile.GetSetting("targetFpsIndex", "2"));
            ConstraintTicks = Convert.ToInt32(ConfigurationFile.GetSetting("constraintTicks", "60"));
            DecreaseTLOD = Convert.ToSingle(ConfigurationFile.GetSetting("decreaseTlod", "50"), new RealInvariantFormat(ConfigurationFile.GetSetting("decreaseTlod", "50")));
            DecreaseOLOD = Convert.ToSingle(ConfigurationFile.GetSetting("decreaseOlod", "50"), new RealInvariantFormat(ConfigurationFile.GetSetting("decreaseOlod", "50")));
            MinLOD = Convert.ToSingle(ConfigurationFile.GetSetting("minLod", "100"), new RealInvariantFormat(ConfigurationFile.GetSetting("minLod", "100")));
            DefaultTLOD = Convert.ToSingle(ConfigurationFile.GetSetting("defaultTlod", "200"), new RealInvariantFormat(ConfigurationFile.GetSetting("defaultTlod", "200")));
            DefaultOLOD = Convert.ToSingle(ConfigurationFile.GetSetting("defaultOlod", "200"), new RealInvariantFormat(ConfigurationFile.GetSetting("defaultOlod", "200")));
            OffsetModuleBase = Convert.ToInt64(ConfigurationFile.GetSetting("offsetModuleBase", "0x004B2368"), 16);
            OffsetPointerMain = Convert.ToInt64(ConfigurationFile.GetSetting("offsetPointerMain", "0x3D0"), 16);
            OffsetPointerTlod = Convert.ToInt64(ConfigurationFile.GetSetting("offsetPointerTlod", "0xC"), 16);
            OffsetPointerTlodVr = Convert.ToInt64(ConfigurationFile.GetSetting("offsetPointerTlodVr", "0x114"), 16);
            OffsetPointerOlod = Convert.ToInt64(ConfigurationFile.GetSetting("offsetPointerOlod", "0xC"), 16);
            OffsetVrMode = Convert.ToInt64(ConfigurationFile.GetSetting("offsetVrMode", "-0xC"), 16);
            SimMinLOD = Convert.ToSingle(ConfigurationFile.GetSetting("simMinLod", "10"), new RealInvariantFormat(ConfigurationFile.GetSetting("simMinLod", "10")));
            AutoSwitchVr = Convert.ToBoolean(ConfigurationFile.GetSetting("autoSwitchVr", "true"));

            SelectedProfile = Convert.ToInt32(ConfigurationFile.GetSetting("selectedProfile", "0"));
            PairsTLOD = new();
            PairsOLOD = new();
            ProfilesVR = new bool[maxProfile];

            for (int i = 0; i < maxProfile; i++)
            {
                ProfilesVR[i] = Convert.ToBoolean(ConfigurationFile.GetSetting($"isVr{i}", "false"));
                PairsTLOD.Add(LoadPairs(ConfigurationFile.GetSetting($"tlodPairs{i}", "0:100|1500:150|5000:200")));
                PairsOLOD.Add(LoadPairs(ConfigurationFile.GetSetting($"olodPairs{i}", "0:100|2500:150|7500:200")));
            }
            CurrentPairTLOD = 0;
            CurrentPairOLOD = 0;
            ForceEvaluation = true;


            if (ConfigVersion < BuildConfigVersion)
            {
                //CHANGE SETTINGS IF NEEDED, Example:

                SetSetting("ConfigVersion", Convert.ToString(BuildConfigVersion));
            }
        }

        public static List<(float, float)> LoadPairs(string settings)
        {
            List<(float, float)> pairsList = new();

            string[] strPairs = settings.Split('|');
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

            return pairsList;
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

        public string GetSetting(string key, string defaultValue = "")
        {
            return ConfigurationFile[key] ?? defaultValue;
        }

        public void SetSetting(string key, string value, bool noLoad = false)
        {
            ConfigurationFile[key] = value;
            if (!noLoad)
                LoadConfiguration();
        }

        public void SavePairs()
        {
            for (int i = 0; i < maxProfile; i++)
            {
                ConfigurationFile[$"isVr{i}"] = ProfilesVR[i].ToString().ToLower();
                ConfigurationFile[$"tlodPairs{i}"] = CreateLodString(PairsTLOD[i]);
                ConfigurationFile[$"olodPairs{i}"] = CreateLodString(PairsOLOD[i]);
            }
            LoadConfiguration();
        }
    }
}