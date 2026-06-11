using System.Collections.Generic;

namespace DynamicLOD.UI.Views.Profiles
{
    public class ModelLevelItem(KeyValuePair<int, int> source)
    {
        public virtual int Altitude { get; set; } = source.Key;
        public virtual int LodValue { get; set; } = source.Value;

        public override string ToString()
        {
            return $"LOD {LodValue} @ {Altitude}ft";
        }
    }
}
