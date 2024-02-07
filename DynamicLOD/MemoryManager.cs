using System;

namespace DynamicLOD
{
    public class MemoryManager
    {
        private ServiceModel Model;

        private long addrTLOD;
        private long addrOLOD;
        private long addrTLOD_VR;
        private long addrOLOD_VR;
        private long addrVrMode;

        public MemoryManager(ServiceModel model)
        {
            try
            {
                this.Model = model;

                MemoryInterface.Attach(Model.SimBinary);
                long moduleBase = MemoryInterface.GetModuleAddress(Model.SimModule);

                addrTLOD = MemoryInterface.ReadMemory<long>(moduleBase + Model.OffsetModuleBase) + Model.OffsetPointerMain;
                addrTLOD_VR = MemoryInterface.ReadMemory<long>(addrTLOD) + Model.OffsetPointerTlodVr;
                Logger.Log(LogLevel.Debug, "MemoryManager:MemoryManager", $"Address TLOD VR: 0x{addrTLOD_VR:X} / {addrTLOD_VR}");
                addrTLOD = MemoryInterface.ReadMemory<long>(addrTLOD) + Model.OffsetPointerTlod;
                Logger.Log(LogLevel.Debug, "MemoryManager:MemoryManager", $"Address TLOD: 0x{addrTLOD:X} / {addrTLOD}");
                addrOLOD_VR = addrTLOD_VR + Model.OffsetPointerOlod;
                Logger.Log(LogLevel.Debug, "MemoryManager:MemoryManager", $"Address OLOD VR: 0x{addrOLOD_VR:X} / {addrOLOD_VR}");
                addrOLOD = addrTLOD + Model.OffsetPointerOlod;
                Logger.Log(LogLevel.Debug, "MemoryManager:MemoryManager", $"Address OLOD: 0x{addrOLOD:X} / {addrOLOD}");

                moduleBase = MemoryInterface.GetModuleAddress(Model.SimBinary);
                addrVrMode = addrTLOD + Model.OffsetVrMode;
                Logger.Log(LogLevel.Debug, "MemoryManager:MemoryManager", $"Address VrMode: 0x{addrVrMode:X} / {addrVrMode}");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "MemoryManager:MemoryManager", $"Exception {ex}: {ex.Message}");
            }
        }

        public bool IsVrModeActive()
        {
            try
            {
                return MemoryInterface.ReadMemory<int>(addrVrMode) == 1;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "MemoryManager:IsVrModeActive", $"Exception {ex}: {ex.Message}");
            }

            return false;
        }

        public float GetTLOD_PC()
        {
            try
            {
                return (float)Math.Round(MemoryInterface.ReadMemory<float>(addrTLOD) * 100.0f);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "MemoryManager:GetTLOD", $"Exception {ex}: {ex.Message}");
            }

            return 0.0f;
        }

        public float GetTLOD()
        {
            if (IsVrModeActive())
                return GetTLOD_VR();
            else
                return GetTLOD_PC();
        }
        public float GetTLOD_VR()
        {
            try
            {
                return (float)Math.Round(MemoryInterface.ReadMemory<float>(addrTLOD_VR) * 100.0f);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "MemoryManager:GetTLOD_VR", $"Exception {ex}: {ex.Message}");
            }

            return 0.0f;
        }

        public float GetOLOD_PC()
        {
            try
            {
                return (float)Math.Round(MemoryInterface.ReadMemory<float>(addrOLOD) * 100.0f);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "MemoryManager:GetOLOD", $"Exception {ex}: {ex.Message}");
            }

            return 0.0f;
        }

        public float GetOLOD()
        {
            if (IsVrModeActive())
                return GetOLOD_VR();
            else
                return GetOLOD_PC();
        }

        public float GetOLOD_VR()
        {
            try
            {
                return (float)Math.Round(MemoryInterface.ReadMemory<float>(addrOLOD_VR) * 100.0f);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "MemoryManager:GetOLOD_VR", $"Exception {ex}: {ex.Message}");
            }

            return 0.0f;
        }

        public void SetTLOD(float value)
        {
            try
            {
                if (IsVrModeActive())
                    MemoryInterface.WriteMemory<float>(addrTLOD_VR, value / 100.0f);
                else
                    MemoryInterface.WriteMemory<float>(addrTLOD, value / 100.0f);
                
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "MemoryManager:SetTLOD", $"Exception {ex}: {ex.Message}");
            }
        }

        public void SetOLOD(float value)
        {
            try
            {
                if (IsVrModeActive())
                    MemoryInterface.WriteMemory<float>(addrOLOD_VR, value / 100.0f);
                else
                    MemoryInterface.WriteMemory<float>(addrOLOD, value / 100.0f);
                
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "MemoryManager:SetOLOD", $"Exception {ex}: {ex.Message}");
            }
        }
    }
}
