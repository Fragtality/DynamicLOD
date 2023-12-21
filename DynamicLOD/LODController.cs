using System;
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

        private void UpdateVariables()
        {
            float vs = SimConnect.ReadSimVar("VERTICAL SPEED", "feet per second");
            Model.OnGround = SimConnect.ReadSimVar("SIM ON GROUND", "Bool") == 1.0f;
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
                    if (fpsModeTicks > Model.ConstraintTicks || Model.ForceEvaluation)
                        ResetFPSMode();
                }
            }
            else if (!Model.UseTargetFPS && Model.fpsMode)
                ResetFPSMode();

            EvaluateLodPairByHeight(ref Model.CurrentPairTLOD, Model.PairsTLOD[Model.SelectedProfile]);
            float newlod = EvaluateLodValue(Model.PairsTLOD[Model.SelectedProfile], Model.CurrentPairTLOD, tlod_dec);
            if (tlod != newlod)
            {
                Logger.Log(LogLevel.Information, "LODController:RunTick", $"Setting TLOD {newlod}");
                Model.MemoryAccess.SetTLOD(newlod);
            }

            EvaluateLodPairByHeight(ref Model.CurrentPairOLOD, Model.PairsOLOD[Model.SelectedProfile]);
            newlod = EvaluateLodValue(Model.PairsOLOD[Model.SelectedProfile], Model.CurrentPairOLOD, olod_dec);
            if (olod != newlod)
            {
                Logger.Log(LogLevel.Information, "LODController:RunTick", $"Setting OLOD {newlod}");
                Model.MemoryAccess.SetOLOD(newlod);
            }

            Model.ForceEvaluation = false;
        }

        private float EvaluateLodValue(List<(float, float)> pairs, int currentPair, float decrement)
        {
            if (Model.UseTargetFPS)
                return Math.Max(pairs[currentPair].Item2 - decrement, Math.Max(Model.MinLOD, Model.SimMinLOD));
            else
                return Math.Max(pairs[currentPair].Item2, Model.SimMinLOD);
        }

        private void ResetFPSMode()
        {
            Logger.Log(LogLevel.Information, "LODController:RunTick", $"FPS Constraint lifted");
            Model.fpsMode = false;
            fpsModeTicks = 0;
            tlod_dec = 0;
            olod_dec = 0;
        }

        private float EvaluateLodPairByHeight(ref int index, List<(float, float)> lodPairs)
        {
            float result = -1.0f;
            Logger.Log(LogLevel.Verbose, "LODController:EvaluateLodByHeight", $"VerticalAverage {VerticalAverage()}");
            if ((VerticalAverage() > 0 || Model.ForceEvaluation) && index + 1 < lodPairs.Count && altAboveGnd > lodPairs[index + 1].Item1)
            {
                index++;
                Logger.Log(LogLevel.Information, "LODController:EvaluateLodByHeight", $"Higher Pair found (altAboveGnd: {altAboveGnd} | index: {index} | lod: {lodPairs[index].Item2})");
                return lodPairs[index].Item2;
            }
            else if ((VerticalAverage() < 0 || Model.ForceEvaluation) && altAboveGnd < lodPairs[index].Item1 && index - 1 >= 0)
            {
                index--;
                Logger.Log(LogLevel.Information, "LODController:EvaluateLodByHeight", $"Lower Pair found (altAboveGnd: {altAboveGnd} | index: {index} | lod: {lodPairs[index].Item2})");
                return lodPairs[index].Item2;
            }

            return result;
        }

        public int VerticalAverage()
        {
            return verticalStats.Sum();
        }

        private void FindPairs()
        {
            Logger.Log(LogLevel.Information, "LODController:FindPairs", $"Finding Pairs (onGround: {Model.OnGround} | tlod: {tlod} | olod: {olod})");

            if (!Model.OnGround)
            {
                int result = 0;
                for (int i = 0; i < Model.PairsTLOD[Model.SelectedProfile].Count; i++)
                {
                    if (altAboveGnd > Model.PairsTLOD[Model.SelectedProfile][i].Item1)
                        result = i;
                }
                Model.CurrentPairTLOD = result;
                Logger.Log(LogLevel.Information, "LODController:FindPairs", $"TLOD Index {result}");
                if (tlod != Model.PairsTLOD[Model.SelectedProfile][result].Item2)
                {
                    Logger.Log(LogLevel.Information, "LODController:FindPairs", $"Setting TLOD {Model.PairsTLOD[Model.SelectedProfile][result].Item2}");
                    Model.MemoryAccess.SetTLOD(Model.PairsTLOD[Model.SelectedProfile][result].Item2);
                }

                result = 0;
                for (int i = 0; i < Model.PairsOLOD[Model.SelectedProfile].Count; i++)
                {
                    if (altAboveGnd > Model.PairsOLOD[Model.SelectedProfile][i].Item1)
                        result = i;
                }
                Model.CurrentPairOLOD = result;
                Logger.Log(LogLevel.Information, "LODController:FindPairs", $"OLOD Index {result}");
                if (olod != Model.PairsOLOD[Model.SelectedProfile][result].Item2)
                {
                    Logger.Log(LogLevel.Information, "LODController:FindPairs", $"Setting OLOD {Model.PairsOLOD[Model.SelectedProfile][result].Item2}");
                    Model.MemoryAccess.SetOLOD(Model.PairsOLOD[Model.SelectedProfile][result].Item2);
                }
            }
            else
            {
                int result = 0;
                Model.CurrentPairTLOD = result;
                if (tlod != Model.PairsTLOD[Model.SelectedProfile][result].Item2)
                {
                    Logger.Log(LogLevel.Information, "LODController:FindPairs", $"Setting TLOD {Model.PairsTLOD[Model.SelectedProfile][result].Item2}");
                    Model.MemoryAccess.SetTLOD(Model.PairsTLOD[Model.SelectedProfile][result].Item2);
                }
                Model.CurrentPairOLOD = result;
                if (olod != Model.PairsOLOD[Model.SelectedProfile][result].Item2)
                {
                    Logger.Log(LogLevel.Information, "LODController:FindPairs", $"Setting OLOD {Model.PairsOLOD[Model.SelectedProfile][result].Item2}");
                    Model.MemoryAccess.SetOLOD(Model.PairsOLOD[Model.SelectedProfile][result].Item2);
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
