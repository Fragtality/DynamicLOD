using CFIT.AppLogger;
using CFIT.AppTools;
using DynamicLOD.AppConfig;
using System.Collections.Concurrent;

namespace DynamicLOD.Memory
{
    public class MemoryManager
    {
        public virtual Config Config => AppService.Instance.Config;
        public virtual SimPointerDefinition PointerDefinition { get; }
        public virtual ConcurrentDictionary<MemoryValueType, MemoryValue> MemoryValues { get; } = [];
        protected virtual MemoryInterface MemoryInterface { get; }
        public virtual bool IsAttached => MemoryInterface?.IsAttached == true;
        public virtual bool IsInitialized { get; protected set; } = false;

        public MemoryManager(SimPointerDefinition pointerDefinition)
        {
            PointerDefinition = pointerDefinition;
            MemoryInterface = new();
            Init();
        }

        protected virtual void Init()
        {
            long pointerBase = 0;
            MemoryInterface.Attach(PointerDefinition.SimProcess);
            long moduleBase = MemoryInterface.GetModuleAddress(PointerDefinition.SimModule);
            Logger.Debug($"Using Module Base {moduleBase:X12}");

            if (moduleBase != 0)
            {
                if (PointerDefinition.SimVariant == SimVariant.MSFS2020_STEAM || PointerDefinition.SimVariant == SimVariant.MSFS2020_MSSTORE)
                    pointerBase = Init2020(moduleBase);
                else if (PointerDefinition.SimVariant == SimVariant.MSFS2024_STEAM || PointerDefinition.SimVariant == SimVariant.MSFS2024_MSSTORE)
                    pointerBase = Init2024(moduleBase);
                else
                    Logger.Error($"Unknown Value for SimVariant: {PointerDefinition.SimVariant}");
            }
            else
                Logger.Error($"Module Base zero!");


            if (pointerBase != 0)
            {
                MemoryValues.Add(MemoryValueType.TLOD, new()
                {
                    CastType = typeof(float),
                    Address = pointerBase + PointerDefinition.OffsetPointerTlod,
                    Interface = MemoryInterface,
                });
                MemoryValues.Add(MemoryValueType.TLOD_VR, new()
                {
                    CastType = typeof(float),
                    Address = pointerBase + PointerDefinition.OffsetPointerTlodVr,
                    Interface = MemoryInterface,
                });
                MemoryValues.Add(MemoryValueType.OLOD, new()
                {
                    CastType = typeof(float),
                    Address = pointerBase + PointerDefinition.OffsetPointerOlod,
                    Interface = MemoryInterface,
                });
                MemoryValues.Add(MemoryValueType.OLOD_VR, new()
                {
                    CastType = typeof(float),
                    Address = pointerBase + PointerDefinition.OffsetPointerOlodVr,
                    Interface = MemoryInterface,
                });

                Logger.Debug($"Using TLOD Address {MemoryValues[MemoryValueType.TLOD].Address:X12}");
            }
            else
                Logger.Error($"Pointer Base zero!");

            IsInitialized = !MemoryValues.IsEmpty && MemoryValues[MemoryValueType.TLOD].Read() > 0.0f;
        }

        protected virtual long Init2020(long moduleBase)
        {
            long pointerBase = MemoryInterface.ReadMemory<long>(moduleBase + PointerDefinition.OffsetModuleBase) + PointerDefinition.OffsetPointer;
            pointerBase = MemoryInterface.ReadMemory<long>(pointerBase);
            Logger.Debug($"Using Pointer Base {pointerBase:X12}");

            return pointerBase;
        }

        protected virtual long Init2024(long moduleBase)
        {
            long pointerBase = MemoryInterface.ReadMemory<long>(moduleBase + PointerDefinition.OffsetModuleBase);
            Logger.Debug($"Using Pointer Base {pointerBase:X12}");

            return pointerBase;
        }

        public virtual void Cleanup()
        {
            MemoryValues.Clear();
            MemoryInterface.Detach();
            IsInitialized = false;
        }

        public virtual void WriteLod(MemoryValueType type, int value)
        {
            WriteLod(type, value / 100.0f);
        }

        public virtual void WriteLod(MemoryValueType type, float value)
        {
            if (MemoryValues.TryGetValue(type, out MemoryValue memoryValue))
            {
                Logger.Information($"Set LOD '{type}' to: {value}");
                memoryValue.Write(value);
            }
        }

        public virtual float ReadLod(MemoryValueType type)
        {
            if (MemoryValues.TryGetValue(type, out MemoryValue memoryValue))
                return memoryValue.Read();
            else
                return 0.0f;
        }

        public virtual float GetTlod()
        {
            MemoryValueType type = AppService.Instance.IsInVr ? MemoryValueType.TLOD_VR : MemoryValueType.TLOD;

            return ReadLod(type);
        }

        public virtual float GetOlod()
        {
            MemoryValueType type = AppService.Instance.IsInVr ? MemoryValueType.OLOD_VR : MemoryValueType.OLOD;

            return ReadLod(type);
        }
    }
}
