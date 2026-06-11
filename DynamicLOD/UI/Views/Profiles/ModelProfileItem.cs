using DynamicLOD.AppConfig;
using System;

namespace DynamicLOD.UI.Views.Profiles
{
    public partial class ModelProfileItem(SettingProfile source) : ModelBase<SettingProfile>(source, AppService.Instance)
    {
        protected override void InitializeModel()
        {

        }

        public override string ToString()
        {
            return Source.ToString();
        }

        public virtual bool IsActive => AppService?.ProfileName?.Equals(Name, StringComparison.InvariantCultureIgnoreCase) == true;
        public virtual bool CanActivate => !IsActive;
        public virtual string Name { get => Source?.Name ?? ""; set { } }
        public virtual bool IsVR { get => Source?.IsVR ?? false; set { } }
        public virtual string TlodReset { get => (Source?.TlodReset ?? 100).ToString(); set => SetReset(value, nameof(SettingProfile.TlodReset)); }
        public virtual string OlodReset { get => (Source?.OlodReset ?? 100).ToString(); set => SetReset(value, nameof(SettingProfile.OlodReset)); }

        public virtual void SetFeature(bool? toggle, string propertyName)
        {
            SetModelValue<bool>(toggle == true, null, null, propertyName);
        }

        public virtual void SetReset(string value, string propertyName)
        {
            if (!int.TryParse(value, out int intVal))
                intVal = 100;

            SetModelValue<int>(intVal, null, null, propertyName);
        }
    }
}
