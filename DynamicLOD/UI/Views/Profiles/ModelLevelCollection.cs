using CFIT.AppFramework.UI.ViewModels;
using System.Collections.Generic;

namespace DynamicLOD.UI.Views.Profiles
{
    public class ModelLevelCollection(IDictionary<int, int> source) : ViewModelCollection<KeyValuePair<int, int>, ModelLevelItem>(source as ICollection<KeyValuePair<int, int>> ?? [], (i) => new(i))
    {
        public override ICollection<KeyValuePair<int, int>> Source => SourceDict as ICollection<KeyValuePair<int, int>> ?? [];
        protected virtual IDictionary<int, int> SourceDict { get; set; } = source;

        public override bool UpdateSource(KeyValuePair<int, int> oldItem, KeyValuePair<int, int> newItem)
        {
            SourceDict.Remove(oldItem.Key);
            SourceDict.Add(newItem.Key, newItem.Value);
            AppService.Instance.Config.SaveConfiguration();
            return true;
        }

        protected override void AddSource(KeyValuePair<int, int> item)
        {
            if (SourceDict.TryAdd(item.Key, item.Value))
                AppService.Instance.Config.SaveConfiguration();
        }

        protected override bool RemoveSource(KeyValuePair<int, int> item)
        {
            if (SourceDict.Remove(item.Key))
            {
                AppService.Instance.Config.SaveConfiguration();
                return true;
            }
            else
                return false;
        }

        public override bool Contains(KeyValuePair<int, int> item)
        {
            return SourceDict.ContainsKey(item.Key);
        }

        public virtual void ChangeSource(IDictionary<int, int> source)
        {
            SourceDict = source;
            NotifyCollectionChanged();
        }
    }
}
