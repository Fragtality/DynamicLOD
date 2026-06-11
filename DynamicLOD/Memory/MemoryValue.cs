using System;

namespace DynamicLOD.Memory
{
    public enum MemoryValueType
    {
        TLOD = 1,
        TLOD_VR = 2,
        OLOD = 3,
        OLOD_VR = 4,
    }

    public class MemoryValue
    {
        public virtual Type CastType { get; set; }
        public virtual long Address { get; set; }
        public virtual object Value { get; set; }
        public virtual MemoryInterface Interface { get; set; }

        public virtual dynamic Read()
        {
            if (CastType == typeof(float))
                return Interface.ReadMemory<float>(Address);
            else if (CastType == typeof(int))
                return Interface.ReadMemory<int>(Address);
            else
                return 0;
        }

        public virtual void Write(dynamic value)
        {
            if (CastType == typeof(float))
                Interface.WriteMemory<float>(Address, value);
            else if (CastType == typeof(int))
                Interface.WriteMemory<int>(Address, value);
        }
    }
}
