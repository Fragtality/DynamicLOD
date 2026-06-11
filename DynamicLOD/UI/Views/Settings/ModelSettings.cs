using DynamicLOD.AppConfig;
using System.Collections.Generic;

namespace DynamicLOD.UI.Views.Settings
{
    public partial class ModelSettings(AppService appService) : ModelBase<Config>(appService.Config, appService)
    {
        protected override void InitializeModel()
        {

        }

        //UI
        public virtual bool OpenAppWindowOnStart { get => Source.OpenAppWindowOnStart; set => SetModelValue<bool>(value); }
        public virtual bool AppWindowRestorePosition { get => Source.AppWindowRestorePosition; set => SetModelValue<bool>(value); }

        //LOD Controller
        public virtual double FeetPerSecondThreshold { get => Source.FeetPerSecondThreshold; set => SetModelValue<double>(value); }
        public virtual int TrendAvgPoints { get => Source.TrendAvgPoints; set => SetModelValue<int>(value); }
        public virtual bool VrModeDetection { get => Source.VrModeDetection; set => SetModelValue<bool>(value); }
        public virtual SimVariant Force24Variant { get => Source.Force24Variant; set => SetModelValue<SimVariant>(value); }
        public virtual Dictionary<SimVariant, string> Force24VariantOptions { get; } = new()
        {
            { SimVariant.AUTO, "Auto. by Path" },
            { SimVariant.MSFS2024_STEAM, "MS2024 Steam" },
            { SimVariant.MSFS2024_MSSTORE, "MS2024 MS Store" },
        };
    }
}
