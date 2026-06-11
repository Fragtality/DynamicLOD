using System.Collections.Generic;

namespace DynamicLOD.AppConfig
{
    public class SettingProfile
    {
        public virtual string Name { get; set; } = "default";
        public virtual List<string> SimObjectMatches { get; set; } = [];
        public virtual bool IsVR { get; set; } = false;
        public virtual int TlodReset { get; set; } = 100;
        public virtual int OlodReset { get; set; } = 100;
        public virtual SortedDictionary<int, int> TlodLevels { get; set; } = new()
        {
            {0, 100 },
            {500, 125 },
            {2000, 150 },
            {7500, 175 },
            {15000, 200 },
            {20000, 250 }
        };
        public virtual SortedDictionary<int, int> OlodLevels { get; set; } = new()
        {
            {0, 100 },
            {500, 125 },
            {5000, 150 },
            {10000, 175 },
        };

        public virtual void Copy(SettingProfile profile)
        {
            Name = profile.Name;
            IsVR = profile.IsVR;
            TlodReset = profile.TlodReset;
            OlodReset = profile.OlodReset;

            this.SimObjectMatches.Clear();
            foreach (var match in profile.SimObjectMatches)
                this.SimObjectMatches.Add(new(match.ToCharArray()));

            this.TlodLevels.Clear();
            foreach (var pair in profile.TlodLevels)
                this.TlodLevels.Add(pair.Key, pair.Value);

            this.OlodLevels.Clear();
            foreach (var pair in profile.OlodLevels)
                this.OlodLevels.Add(pair.Key, pair.Value);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
