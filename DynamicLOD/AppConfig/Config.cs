using CFIT.AppFramework.AppConfig;
using CFIT.AppLogger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace DynamicLOD.AppConfig
{
    public enum SimVariant
    {
        AUTO = 0,
        MSFS2020_STEAM = 1,
        MSFS2020_MSSTORE = 2,
        MSFS2024_STEAM = 3,
        MSFS2024_MSSTORE = 4
    }

    public class Config : AppConfigBase<Definition>
    {
        public virtual bool OpenAppWindowOnStart { get; set; } = false;
        public virtual int UiRefreshInterval { get; set; } = 500;
        public virtual int TickInterval { get; set; } = 500;
        public virtual double FeetPerSecondThreshold { get; set; } = 8.0;
        public virtual int TrendAvgPoints { get; set; } = 5;
        public virtual int MinTlodValue { get; set; } = 50;
        public virtual int MaxTlodValue { get; set; } = 400;
        public virtual int MinOlodValue { get; set; } = 50;
        public virtual int MaxOlodValue { get; set; } = 200;
        public virtual bool VrModeDetection { get; set; } = true;
        public virtual SimVariant Force24Variant { get; set; } = SimVariant.AUTO;
        public virtual Dictionary<SimVariant, SimPointerDefinition> SimPointerDefinitions { get; set; } = new()
        {
            { SimVariant.MSFS2020_STEAM, new()
                {
                    SimVariant = SimVariant.MSFS2020_STEAM,
                    SimProcess = "FlightSimulator",
                    SimModule = "WwiseLibPCx64P.dll",
                    OffsetModuleBase = 0x004B2368,
                    OffsetPointer = 0x3D0,
                    OffsetPointerTlod = 0xC,
                    OffsetPointerOlod = 0x18,
                    OffsetPointerTlodVr = 0x114,
                    OffsetPointerOlodVr = 0x120,
                }
            },
            { SimVariant.MSFS2024_MSSTORE, new()
                {
                    SimVariant = SimVariant.MSFS2024_MSSTORE,
                    SimProcess = "FlightSimulator2024",
                    SimModule = "FlightSimulator2024.exe",
                    OffsetModuleBase = 0xA46D528,
                    OffsetPointer = 0x0,
                    OffsetPointerTlod = 0x47C,
                    OffsetPointerOlod = 0x490,
                    OffsetPointerTlodVr = 0x5C4,
                    OffsetPointerOlodVr = 0x5D8,
                }
            },
            { SimVariant.MSFS2024_STEAM, new()
                {
                    SimVariant = SimVariant.MSFS2024_STEAM,
                    SimProcess = "FlightSimulator2024",
                    SimModule = "FlightSimulator2024.exe",
                    OffsetModuleBase = 0x0A7A4508,
                    OffsetPointer = 0x0,
                    OffsetPointerTlod = 0x47C,
                    OffsetPointerOlod = 0x490,
                    OffsetPointerTlodVr = 0x5C4,
                    OffsetPointerOlodVr = 0x5D8,
                }
            }
        };

        public virtual List<SettingProfile> SettingProfiles { get; set; } = new()
        {
            { new SettingProfile() }
        };

        [JsonIgnore]
        public virtual bool ForceOpen { get; set; } = false;
        [JsonIgnore]
        public virtual bool InhibitSave { get; set; } = false;

        public override void SaveConfiguration()
        {
            if (InhibitSave)
                return;

            SaveConfiguration<Config>(this, ConfigFile);
            Logger.Debug($"Configuration saved");
        }

        protected override void InitConfiguration()
        {
            if (SettingProfiles.Count < 1)
            {
                SettingProfiles.Add(new());
                SaveConfiguration();
            }
        }

        protected override void UpdateConfiguration(int buildConfigVersion)
        {
            //if (ConfigVersion < 2 && buildConfigVersion >= 2)
            //{

            //}
        }

        public virtual void SortProfiles(bool save = true)
        {
            SettingProfiles.Sort((x, y) => x.Name.CompareTo(y.Name));
            if (save)
                SaveConfiguration();
        }

        public virtual bool ImportProfile(string json)
        {
            try
            {
                SettingProfile profile = null;
                try { profile = JsonSerializer.Deserialize<SettingProfile>(json); } catch { }
                if (profile == null)
                {
                    Logger.Warning($"SettingProfile json Data could not be parsed");
                    return false;
                }

                return ImportProfile(profile);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return false;
            }
        }

        public virtual bool ImportProfile(SettingProfile profile)
        {
            bool result = false;

            if (profile == null)
            {
                Logger.Warning($"SettingProfile is NULL");
                return result;
            }

            var query = SettingProfiles.Where(p => p.Name.Equals(profile.Name, StringComparison.InvariantCultureIgnoreCase));
            if (query.Any())
            {
                Logger.Debug($"The Profile '{profile.Name}' is already configured");
                if (MessageBox.Show($"The Profile '{profile.Name}' is already configured.\r\nDo you want to override it?", "Profile already exists", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return result;
                else
                {
                    query.First().Copy(profile);
                    SortProfiles();
                    AppService.Instance.NotifyProfileCollectionChanged();
                    Logger.Information($"Profile '{profile.Name}' overridden");
                    result = true;
                }
            }
            else
            {
                AppService.Instance.AddSettingProfile(profile);
                Logger.Information($"Profile '{profile.Name}' imported");
                result = true;
            }

            return result;
        }
    }
}
