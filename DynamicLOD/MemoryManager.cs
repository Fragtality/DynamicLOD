using System;

namespace DynamicLOD
{
    public class MemoryManager
    {
        private ServiceModel Model;

        private long addrTLOD;
        private long addrOLOD;

        public MemoryManager(ServiceModel model)
        {
            try
            {
                this.Model = model;

                MemoryInterface.Attach(Model.SimBinary);
                long moduleBase = MemoryInterface.GetModuleAddress(Model.SimModule);

                addrTLOD = MemoryInterface.ReadMemory<long>(moduleBase + 0x004B23A8) + 0x3D0;
                addrTLOD = MemoryInterface.ReadMemory<long>(addrTLOD) + 0xC;
                addrOLOD = addrTLOD + 0xC;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "MemoryManager:MemoryManager", $"Exception {ex}: {ex.Message}");
            }
        }

        public float GetTLOD()
        {
            try
            {
                return MemoryInterface.ReadMemory<float>(addrTLOD);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "MemoryManager:GetTLOD", $"Exception {ex}: {ex.Message}");
            }

            return 0.0f;
        }

        public float GetOLOD()
        {
            try
            {
                return MemoryInterface.ReadMemory<float>(addrOLOD);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "MemoryManager:GetOLOD", $"Exception {ex}: {ex.Message}");
            }

            return 0.0f;
        }

        public void SetTLOD(float value)
        {
            try
            {
                MemoryInterface.WriteMemory<float>(addrTLOD, value);
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
                MemoryInterface.WriteMemory<float>(addrOLOD, value);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "MemoryManager:SetOLOD", $"Exception {ex}: {ex.Message}");
            }
        }
    }
}
