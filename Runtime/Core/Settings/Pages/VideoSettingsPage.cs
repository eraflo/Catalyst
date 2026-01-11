using System.Collections.Generic;

namespace Eraflo.Catalyst.Core.Settings
{
    public class VideoSettingsPage : ISettingsPage
    {
        public string Id => "Video";
        public string DisplayName => "Video";

        public IEnumerable<string> GetSettingKeys()
        {
            yield return "ResolutionWidth";
            yield return "ResolutionHeight";
            yield return "Fullscreen";
            yield return "VSync";
            yield return "QualityLevel";
        }

        public void Apply(SettingsData data)
        {
            SettingsUtils.ApplyResolution(data.ResolutionWidth, data.ResolutionHeight, data.Fullscreen);
            SettingsUtils.ApplyQuality(data.QualityLevel);
            SettingsUtils.ApplyVSync(data.VSync);
        }
    }
}
