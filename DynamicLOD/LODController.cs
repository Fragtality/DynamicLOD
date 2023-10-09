using System.Collections.Generic;
using System.Linq;

namespace DynamicLOD
{
    public class LODController
    {
        private MobiSimConnect SimConnect;
        private ServiceModel Model;

        private int[] verticalStats = new int[5];
        private int verticalIndex = 0;
        private int altAboveGnd = 0;
        private float tlod = 0;
        private float tlod_dec = 0;
        private float olod = 0;
        private float olod_dec = 0;
        public bool FirstStart { get; set; } = true;
        private int fpsModeTicks = 0;

        public LODController(ServiceModel model)
        {
            Model = model;

            SimConnect = IPCManager.SimConnect;
            SimConnect.SubscribeSimVar("VERTICAL SPEED", "feet per second");
            SimConnect.SubscribeSimVar("PLANE ALT ABOVE GROUND", "feet");
            SimConnect.SubscribeSimVar("SIM ON GROUND", "Bool");
            tlod = Model.MemoryAccess.GetTLOD();
            olod = Model.MemoryAccess.GetOLOD();
            Model.CurrentPairTLOD = 0;
            Model.CurrentPairOLOD = 0;
            Model.fpsMode = false;
        }

        public void RunTick()
        {
            UpdateVariables();
            if (FirstStart)
            {
                fpsModeTicks++;
                if (fpsModeTicks > 2)
                    FindPairs();
                return;
            }

            if (Model.UseTargetFPS)
            {
                if (SimConnect.GetAverageFPS() < Model.TargetFPS && (Model.CurrentPairTLOD >= Model.TargetFPSIndex || (Model.CurrentPairOLOD >= Model.TargetFPSIndex)))
                {
                    if (!Model.fpsMode)
                    {
                        Logger.Log(LogLevel.Information, "LODController:RunTick", $"FPS Constraint active");
                        Model.fpsMode = true;
                        tlod_dec = Model.DecreaseTLOD;
                        olod_dec = Model.DecreaseOLOD;
                    }
                }
                else if (Model.fpsMode && (Model.CurrentPairTLOD < Model.TargetFPSIndex || Model.CurrentPairOLOD < Model.TargetFPSIndex))
                {
                    ResetFPSMode();
                }
                else if (SimConnect.GetAverageFPS() > Model.TargetFPS && Model.fpsMode)
                {
                    fpsModeTicks++;
                    if (fpsModeTicks > Model.ConstraintTicks)
                        ResetFPSMode();
                }
            }
            else if (!Model.UseTargetFPS && Model.fpsMode)
                ResetFPSMode();

            EvaluateLodByHeight(ref Model.CurrentPairTLOD, Model.PairsTLOD);
            float newlod = Model.PairsTLOD[Model.CurrentPairTLOD].Item2 - tlod_dec;
            if (tlod != newlod && newlod >= Model.MinLOD)
            {
                Logger.Log(LogLevel.Information, "LODController:RunTick", $"Setting TLOD {newlod}");
                Model.MemoryAccess.SetTLOD(newlod);
            }

            EvaluateLodByHeight(ref Model.CurrentPairOLOD, Model.PairsOLOD);
            newlod = Model.PairsOLOD[Model.CurrentPairOLOD].Item2 - olod_dec;
            if (olod != newlod && newlod >= Model.MinLOD)
            {
                Logger.Log(LogLevel.Information, "LODController:RunTick", $"Setting OLOD {newlod}");
                Model.MemoryAccess.SetOLOD(newlod);
            }
        }

        private void ResetFPSMode()
        {
            Logger.Log(LogLevel.Information, "LODController:RunTick", $"FPS Constraint lifted");
            Model.fpsMode = false;
            fpsModeTicks = 0;
            tlod_dec = 0;
            olod_dec = 0;
        }

        private float EvaluateLodByHeight(ref int index, List<(float, float)> lodPairs)
        {
            float result = -1.0f;
            Logger.Log(LogLevel.Verbose, "LODController:EvaluateLodByHeight", $"VerticalAverage {VerticalAverage()}");
            if (VerticalAverage() > 0 && index + 1 < lodPairs.Count && altAboveGnd > lodPairs[index + 1].Item1)
            {
                index++;
                Logger.Log(LogLevel.Information, "LODController:EvaluateLodByHeight", $"Higher Pair found (altAboveGnd: {altAboveGnd} | index: {index} | lod: {lodPairs[index].Item2})");
                return lodPairs[index].Item2;
            }
            else if (VerticalAverage() < 0 && altAboveGnd < lodPairs[index].Item1 && index - 1 >= 0)
            {
                index--;
                Logger.Log(LogLevel.Information, "LODController:EvaluateLodByHeight", $"Lower Pair found (altAboveGnd: {altAboveGnd} | index: {index} | lod: {lodPairs[index].Item2})");
                return lodPairs[index].Item2;
            }

            return result;
        }

        private void UpdateVariables()
        {
            float vs = SimConnect.ReadSimVar("VERTICAL SPEED", "feet per second");
            if (vs >= 10.0f)
                verticalStats[verticalIndex] = 1;
            else if (vs <= -10.0f)
                verticalStats[verticalIndex] = -1;
            else
                verticalStats[verticalIndex] = 0;

            verticalIndex++;
            if (verticalIndex >= verticalStats.Length)
                verticalIndex = 0;

            Model.VerticalTrend = VerticalAverage();

            altAboveGnd = (int)SimConnect.ReadSimVar("PLANE ALT ABOVE GROUND", "feet");

            tlod = Model.MemoryAccess.GetTLOD();
            olod = Model.MemoryAccess.GetOLOD();
        }

        public int VerticalAverage()
        {
            return verticalStats.Sum();
        }

        private void FindPairs()
        {
            bool onGround = SimConnect.ReadSimVar("SIM ON GROUND", "Bool") == 1.0f;
            Logger.Log(LogLevel.Information, "LODController:FindPairs", $"Finding Pairs (onGround: {onGround} | tlod: {tlod} | olod: {olod})");

            if (!onGround)
            {
                int result = 0;
                for (int i = 0; i < Model.PairsTLOD.Count; i++)
                {
                    if (altAboveGnd > Model.PairsTLOD[i].Item1)
                        result = i;
                }
                Model.CurrentPairTLOD = result;
                Logger.Log(LogLevel.Information, "LODController:FindPairs", $"TLOD Index {result}");
                if (tlod != Model.PairsTLOD[result].Item2)
                {
                    Logger.Log(LogLevel.Information, "LODController:FindPairs", $"Setting TLOD {Model.PairsTLOD[result].Item2}");
                    Model.MemoryAccess.SetTLOD(Model.PairsTLOD[result].Item2);
                }

                result = 0;
                for (int i = 0; i < Model.PairsOLOD.Count; i++)
                {
                    if (altAboveGnd > Model.PairsOLOD[i].Item1)
                        result = i;
                }
                Model.CurrentPairOLOD = result;
                Logger.Log(LogLevel.Information, "LODController:FindPairs", $"OLOD Index {result}");
                if (olod != Model.PairsOLOD[result].Item2)
                {
                    Logger.Log(LogLevel.Information, "LODController:FindPairs", $"Setting OLOD {Model.PairsOLOD[result].Item2}");
                    Model.MemoryAccess.SetOLOD(Model.PairsOLOD[result].Item2);
                }
            }
            else
            {
                int result = 0;
                Model.CurrentPairTLOD = result;
                if (tlod != Model.PairsTLOD[result].Item2)
                {
                    Logger.Log(LogLevel.Information, "LODController:FindPairs", $"Setting TLOD {Model.PairsTLOD[result].Item2}");
                    Model.MemoryAccess.SetTLOD(Model.PairsTLOD[result].Item2);
                }
                Model.CurrentPairOLOD = result;
                if (olod != Model.PairsOLOD[result].Item2)
                {
                    Logger.Log(LogLevel.Information, "LODController:FindPairs", $"Setting OLOD {Model.PairsOLOD[result].Item2}");
                    Model.MemoryAccess.SetOLOD(Model.PairsOLOD[result].Item2);
                }
            }

            Model.fpsMode = false;
            fpsModeTicks = 0;
            tlod_dec = 0;
            olod_dec = 0;
            FirstStart = false;
        }
    }
}
