using CFIT.AppFramework.ResourceStores;
using CFIT.AppLogger;
using CFIT.SimConnectLib.SimResources;
using CFIT.SimConnectLib.SimVars;
using DynamicLOD.AppConfig;
using DynamicLOD.Memory;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DynamicLOD
{
    public class LodController
    {
        public virtual MemoryManager MemoryManager => AppService.Instance?.MemoryManager;
        public virtual Config Config => AppService.Instance?.Config;
        public virtual SettingProfile SettingProfile => AppService.Instance?.SettingProfile;
        public virtual bool IsReady => MemoryManager?.IsInitialized == true;
        protected virtual bool IsFrozen => AppService.Instance.IsFrozen;
        public virtual bool ProfileWasSet { get; set; } = false;
        protected virtual SimStore SimStore => AppService.Instance.SimStore;
        public virtual ISimResourceSubscription SubVerticalSpeed { get; protected set; }
        public virtual double VerticalSpeed { get; protected set; } = 0.0;
        public virtual ISimResourceSubscription SubAlt { get; protected set; }
        public virtual double Altitude { get; protected set; } = 0.0;
        public virtual ISimResourceSubscription SubAltCg { get; protected set; }
        public virtual double AltitudeCg { get; protected set; } = 0.0;
        public virtual ISimResourceSubscription SubOnGround { get; protected set; }
        public virtual bool IsOnGround { get; protected set; } = false;
        public virtual int Tlod { get; protected set; } = 0;
        public virtual int TlodVr { get; protected set; } = 0;
        public virtual int Olod { get; protected set; } = 0;
        public virtual int OlodVr { get; protected set; } = 0;
        public virtual int VerticalTrend { get; protected set; } = 0;
        public virtual int[] VerticalStats { get; protected set; }
        public virtual int VerticalIndex { get; protected set; } = 0;
        public virtual List<KeyValuePair<int, int>> TlodLevels { get; protected set; }
        public virtual int CurrentTlodLevel { get; protected set; } = 0;
        public virtual List<KeyValuePair<int, int>> OlodLevels { get; protected set; }
        public virtual int CurrentOlodLevel { get; protected set; } = 0;

        public LodController()
        {
            VerticalStats = new int[Config.TrendAvgPoints];
            SubVerticalSpeed = SimStore.AddVariable("VERTICAL SPEED", SimUnitType.FeetPerSecond);
            SubAlt = SimStore.AddVariable("PLANE ALT ABOVE GROUND", SimUnitType.Feet);
            SubAltCg = SimStore.AddVariable("PLANE ALT ABOVE GROUND MINUS CG", SimUnitType.Feet);
            SubOnGround = SimStore.AddVariable("SIM ON GROUND", SimUnitType.Bool);
        }

        public virtual void Cleanup()
        {
            if (MemoryManager.IsAttached && !AppService.Instance.SimConnect.QuitReceived)
            {
                MemoryManager.WriteLod(MemoryValueType.TLOD, SettingProfile.TlodReset);
                MemoryManager.WriteLod(MemoryValueType.TLOD_VR, SettingProfile.TlodReset);
                MemoryManager.WriteLod(MemoryValueType.OLOD, SettingProfile.OlodReset);
                MemoryManager.WriteLod(MemoryValueType.OLOD_VR, SettingProfile.OlodReset);

                SimStore.Remove("VERTICAL SPEED");
                SimStore.Remove("PLANE ALT ABOVE GROUND");
                SimStore.Remove("PLANE ALT ABOVE GROUND MINUS CG");
                SimStore.Remove("SIM ON GROUND");
            }
        }

        protected virtual void InitProfile()
        {
            TlodLevels = [.. SettingProfile.TlodLevels];
            CurrentTlodLevel = 0;
            OlodLevels = [.. SettingProfile.OlodLevels];
            CurrentOlodLevel = 0;
            CurrentTlodLevel = FindHighestLod(TlodLevels, "TLOD");
            CurrentOlodLevel = FindHighestLod(OlodLevels, "OLOD");
            UpdateLods();
        }

        public virtual async Task Run()
        {
            if (!MemoryManager.IsInitialized)
                return;

            await UpdateVariables();
            if (IsFrozen || Altitude == 0)
                return;

            if (ProfileWasSet)
            {
                ProfileWasSet = false;
                InitProfile();
            }
            else
            {
                CurrentTlodLevel = EvaluateLodByHeight(CurrentTlodLevel, TlodLevels, "TLOD");
                CurrentOlodLevel = EvaluateLodByHeight(CurrentOlodLevel, OlodLevels, "OLOD");
                UpdateLods();
            }
        }

        protected virtual void UpdateLods()
        {
            if (!CheckVariables())
            {
                Logger.Error($"Cannot Update LODs - Memory Value Check failed");
                return;
            }


            if (AppService.Instance.IsInVr)
            {
                if (Tlod != TlodLevels[CurrentTlodLevel].Value)
                    MemoryManager.WriteLod(MemoryValueType.TLOD_VR, TlodLevels[CurrentTlodLevel].Value);
                if (Olod != OlodLevels[CurrentOlodLevel].Value)
                    MemoryManager.WriteLod(MemoryValueType.OLOD_VR, OlodLevels[CurrentOlodLevel].Value);
            }
            else
            {
                if (Tlod != TlodLevels[CurrentTlodLevel].Value)
                    MemoryManager.WriteLod(MemoryValueType.TLOD, TlodLevels[CurrentTlodLevel].Value);
                if (Olod != OlodLevels[CurrentOlodLevel].Value)
                    MemoryManager.WriteLod(MemoryValueType.OLOD, OlodLevels[CurrentOlodLevel].Value);
            }
        }

        protected virtual bool CheckVariables()
        {
            return Tlod != 0 && TlodVr != 0 && Olod != 0 && OlodVr != 0 &&
                   Tlod < 1000 && TlodVr < 1000 && Olod < 1000 && OlodVr < 1000;
        }

        protected virtual async Task UpdateVariables()
        {
            Tlod = (int)(MemoryManager.ReadLod(MemoryValueType.TLOD) * 100.0f);
            TlodVr = (int)(MemoryManager.ReadLod(MemoryValueType.TLOD_VR) * 100.0f);
            Olod = (int)(MemoryManager.ReadLod(MemoryValueType.OLOD) * 100.0f);
            OlodVr = (int)(MemoryManager.ReadLod(MemoryValueType.OLOD_VR) * 100.0f);

            VerticalSpeed = SubVerticalSpeed?.GetNumber() ?? 0.0;
            Altitude = SubAlt?.GetNumber() ?? 0.0;
            AltitudeCg = SubAltCg?.GetNumber() ?? 0.0;
            if (Altitude == 0.0 && AltitudeCg != 0.0)
                Altitude = AltitudeCg;
            IsOnGround = SubOnGround?.GetNumber() != 0.0;

            if (VerticalSpeed >= Config.FeetPerSecondThreshold)
                VerticalStats[VerticalIndex] = 1;
            else if (VerticalSpeed <= Config.FeetPerSecondThreshold * -1.0)
                VerticalStats[VerticalIndex] = -1;
            else
                VerticalStats[VerticalIndex] = 0;

            VerticalIndex++;
            if (VerticalIndex >= VerticalStats.Length)
                VerticalIndex = 0;

            VerticalTrend = VerticalStats.Sum();
        }

        protected virtual int FindHighestLod(List<KeyValuePair<int, int>> lodLevels, string lodText)
        {
            int index = -1;
            foreach (var lodLevel in lodLevels)
            {
                index++;
                if (Altitude < lodLevel.Key)
                    return index;
            }

            Logger.Debug($"No highest {lodText} found");
            return index;
        }

        protected virtual int EvaluateLodByHeight(int index, List<KeyValuePair<int, int>> lodLevels, string lodText)
        {
            if (IsOnGround && index != 0)
            {
                index = 0;
                Logger.Information($"Lowest {lodText} Level not selected while on Ground (Altitude: {(int)Altitude} | IsOnGround: {IsOnGround} | index: {index} | lod: {lodLevels[index].Value})");
                return index;
            }
            else if (VerticalTrend > 0 && index + 1 < lodLevels.Count && Altitude > lodLevels[index + 1].Key)
            {
                index++;
                Logger.Information($"Higher {lodText} Level found (Altitude: {(int)Altitude} | index: {index} | lod: {lodLevels[index].Value})");
                return index;
            }
            else if (VerticalTrend < 0 && Altitude < lodLevels[index].Key && index - 1 >= 0)
            {
                index--;
                Logger.Information($"Lower {lodText} Level found (Altitude: {(int)Altitude} | index: {index} | lod: {lodLevels[index].Value})");
                return index;
            }
            else if (VerticalTrend == 0 && Altitude > lodLevels[^1].Key && index != lodLevels.Count - 1)
            {
                index = lodLevels.Count - 1;
                Logger.Information($"Highest {lodText} Level not selected while in Cruise (Altitude: {(int)Altitude} | index: {index} | lod: {lodLevels[index].Value})");
                return index;
            }

            return index;
        }
    }
}
