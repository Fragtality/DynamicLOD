using CFIT.AppFramework.UI.ViewModels;
using DynamicLOD.AppConfig;
using System.Collections.Generic;

namespace DynamicLOD.UI.Views.Profiles
{
    public partial class ModelMatchingCollection(SettingProfile profile) : ViewModelCollection<string, string>(profile?.SimObjectMatches ?? [], (i) => i, (p) => !string.IsNullOrWhiteSpace(p))
    {
        public virtual SettingProfile Profile { get; protected set; } = profile;
        public override ICollection<string> Source => Profile?.SimObjectMatches ?? [];

        protected override void InitializeMemberBindings()
        {
            base.InitializeMemberBindings();
        }

        public override bool UpdateSource(string oldItem, string newItem)
        {
            if (base.UpdateSource(oldItem, newItem))
            {
                AppService.Instance.Config.SaveConfiguration();
                return true;
            }
            else
                return false;
        }

        protected override void AddSource(string item)
        {
            base.AddSource(item);
            AppService.Instance.Config.SaveConfiguration();
        }

        protected override bool RemoveSource(string item)
        {
            if (base.RemoveSource(item))
            {
                AppService.Instance.Config.SaveConfiguration();
                return true;
            }
            else
                return false;
        }

        public virtual void ChangeProfile(SettingProfile profile)
        {
            Profile = profile;
            NotifyCollectionChanged();
        }
    }
}
