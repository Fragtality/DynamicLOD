﻿using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace DynamicLOD
{
    public static class IPCManager
    {
        public static readonly int waitDuration = 30000;
        public static bool simWasStarted = false;

        public static MobiSimConnect SimConnect { get; set; } = null;

        public static bool WaitForSimulator(ServiceModel model)
        {
            bool simRunning = IsSimRunning();
            if (!simRunning && !simWasStarted && model.WaitForConnect)
            {
                do
                {
                    Logger.Log(LogLevel.Information, "IPCManager:WaitForSimulator", $"Simulator not started - waiting {waitDuration / 1000}s for Sim");
                    Thread.Sleep(waitDuration);
                }
                while (!IsSimRunning() && !model.CancellationRequested);

                Thread.Sleep(waitDuration);
                return true;
            }
            else if (simRunning)
            {
                Logger.Log(LogLevel.Information, "IPCManager:WaitForSimulator", $"Simulator started");
                return true;
            }
            else
            {
                Logger.Log(LogLevel.Error, "IPCManager:WaitForSimulator", $"Simulator not started - aborting");
                return false;
            }
        }

        public static bool IsProcessRunning(string name)
        {
            Process proc = Process.GetProcessesByName(name).FirstOrDefault();
            return proc != null && proc.ProcessName == name;
        }

        public static bool IsSimRunning()
        {
            bool result = IsProcessRunning("FlightSimulator");
            if (result)
                simWasStarted = true;
            return result;
        }
        
        public static bool WaitForConnection(ServiceModel model)
        {
            if (!IsSimRunning())
                return false;

            Thread.Sleep(waitDuration / 2);

            SimConnect = new MobiSimConnect();
            bool mobiRequested = SimConnect.Connect();

            if (!SimConnect.IsConnected && IsSimRunning())
            {
                do
                {
                    Logger.Log(LogLevel.Information, "IPCManager:WaitForConnection", $"Connection not established - waiting {waitDuration / 1000}s for Retry");
                    Thread.Sleep(waitDuration);
                    if (!mobiRequested)
                        mobiRequested = SimConnect.Connect();
                }
                while (!SimConnect.IsConnected && IsSimRunning() && !model.CancellationRequested);

                return SimConnect.IsConnected && IsSimRunning();
            }
            else
            {
                Logger.Log(LogLevel.Information, "IPCManager:WaitForConnection", $"SimConnect is opened");
                return true;
            }
        }
                
        public static bool WaitForSessionReady(ServiceModel model)
        {
            int waitDuration = 5000;
            SimConnect.SubscribeSimVar("CAMERA STATE", "Enum");
            SimConnect.SubscribeSimVar("PLANE IN PARKING STATE", "Bool");
            Thread.Sleep(250);
            bool isReady = IsCamReady();
            while (IsSimRunning() && !isReady && !model.CancellationRequested)
            {
                Logger.Log(LogLevel.Information, "IPCManager:WaitForSessionReady", $"Session not ready - waiting {waitDuration / 1000}s for Retry");
                Thread.Sleep(waitDuration);
                isReady = IsCamReady();
            }

            if (!isReady || !IsSimRunning())
            {
                Logger.Log(LogLevel.Error, "IPCManager:WaitForSessionReady", $"SimConnect or Simulator not available - aborting");
                return false;
            }

            return true;
        }

        public static bool IsCamReady()
        {
            return SimConnect.ReadSimVar("CAMERA STATE", "Enum") < 11;
        }

        public static void CloseSafe()
        {
            try
            {
                if (SimConnect != null)
                {
                    SimConnect.Disconnect();
                    SimConnect = null;
                }
            }
            catch { }
        }
    }
}
