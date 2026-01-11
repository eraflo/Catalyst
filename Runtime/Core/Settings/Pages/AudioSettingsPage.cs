using System.Collections.Generic;
using UnityEngine.Audio;

namespace Eraflo.Catalyst.Core.Settings
{
    public class AudioSettingsPage : ISettingsPage
    {
        public string Id => "Audio";
        public string DisplayName => "Audio";

        private AudioMixer _mixer;

        public void SetMixer(AudioMixer mixer) => _mixer = mixer;

        public IEnumerable<string> GetSettingKeys()
        {
            yield return "MasterVolume";
            yield return "MusicVolume";
            yield return "SFXVolume";
        }

        public void Apply(SettingsData data)
        {
            if (_mixer == null) return;

            SettingsUtils.ApplyVolume(_mixer, "MasterVol", data.MasterVolume);
            SettingsUtils.ApplyVolume(_mixer, "MusicVol", data.MusicVolume);
            SettingsUtils.ApplyVolume(_mixer, "SFXVol", data.SFXVolume);
        }
    }
}
