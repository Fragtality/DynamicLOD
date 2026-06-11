using CFIT.AppLogger;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DynamicLOD.Memory
{
    public class MemoryInterface
    {
        public const int PROCESS_VM_OPERATION = 0x0008;
        public const int PROCESS_VM_READ = 0x0010;
        public const int PROCESS_VM_WRITE = 0x0020;

        protected virtual Process Process { get; set; } = null;
        protected virtual IntPtr ProcHandle { get; set; } = IntPtr.Zero;
        public virtual bool IsAttached => ProcHandle != IntPtr.Zero;

        public virtual long GetModuleAddress(string Name)
        {
            try
            {
                if (Process != null)
                {
                    foreach (ProcessModule ProcMod in Process.Modules)
                    {
                        if (Name == ProcMod.ModuleName)
                            return (long)ProcMod.BaseAddress;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return -1;
        }

        public virtual bool Attach(string name)
        {
            try
            {
                if (Process.GetProcessesByName(name).Length > 0)
                {
                    Process = Process.GetProcessesByName(name)[0];
                    ProcHandle = NativeMethods.OpenProcess(PROCESS_VM_OPERATION | PROCESS_VM_READ | PROCESS_VM_WRITE, false, Process.Id);
                    return ProcHandle != IntPtr.Zero;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return false;
        }

        public virtual bool Detach()
        {
            try
            {
                bool result = true;
                if (ProcHandle != IntPtr.Zero)
                {
                    result = NativeMethods.CloseHandle(ProcHandle);
                    ProcHandle = IntPtr.Zero;
                }

                Process = null;
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return false;
        }

        public virtual void WriteMemory<T>(long Address, object Value)
        {
            try
            {
                var buffer = StructureToByteArray(Value);
                NativeMethods.NtWriteVirtualMemory(checked((int)ProcHandle), Address, buffer, buffer.Length, out _);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public virtual T ReadMemory<T>(long address) where T : struct
        {
            try
            {
                var ByteSize = Marshal.SizeOf<T>();

                var buffer = new byte[ByteSize];

                NativeMethods.NtReadVirtualMemory(checked((int)ProcHandle), address, buffer, buffer.Length, out _);

                return ByteArrayToStructure<T>(buffer);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return default;
        }

        private static T ByteArrayToStructure<T>(byte[] bytes) where T : struct
        {
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }

        private static byte[] StructureToByteArray(object obj)
        {
            var length = Marshal.SizeOf(obj);

            var array = new byte[length];

            var pointer = Marshal.AllocHGlobal(length);

            Marshal.StructureToPtr(obj, pointer, true);
            Marshal.Copy(pointer, array, 0, length);
            Marshal.FreeHGlobal(pointer);

            return array;
        }
    }

    internal static class NativeMethods
    {
        [DllImport("kernel32.dll")]
        internal static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);
        [DllImport("kernel32.dll")]
        internal static extern bool CloseHandle(IntPtr ProcessHandle);

        [DllImport("ntdll.dll")]
        internal static extern IntPtr NtWriteVirtualMemory(int ProcessHandle, long BaseAddress, byte[] Buffer, int NumberOfBytesToWrite, out int NumberOfBytesWritten);
        [DllImport("ntdll.dll")]
        internal static extern bool NtReadVirtualMemory(int ProcessHandle, long BaseAddress, byte[] Buffer, int NumberOfBytesToRead, out int NumberOfBytesRead);
    }
}
