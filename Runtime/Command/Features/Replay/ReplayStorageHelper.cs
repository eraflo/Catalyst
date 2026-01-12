using System.Threading.Tasks;
using Eraflo.Catalyst.Core.Save;

namespace Eraflo.Catalyst.Command.Features.Replay
{
    /// <summary>
    /// Utility to easily save and load ReplayTracks using the Catalyst Save System.
    /// </summary>
    public static class ReplayStorageHelper
    {
        /// <summary>
        /// Saves a ReplayTrack to the persistent storage.
        /// </summary>
        public static async Task SaveTrack(ReplayTrack track, string fileName)
        {
            var saveManager = App.Get<SaveManager>();
            if (saveManager == null) return;

            byte[] data = saveManager.Serializer.Serialize(track);
            await saveManager.Storage.SaveAsync($"Replays/{fileName}", data);
        }

        /// <summary>
        /// Loads a ReplayTrack from persistent storage.
        /// </summary>
        public static async Task<ReplayTrack> LoadTrack(string fileName)
        {
            var saveManager = App.Get<SaveManager>();
            if (saveManager == null) return null;

            byte[] data = await saveManager.Storage.LoadAsync($"Replays/{fileName}");
            if (data == null || data.Length == 0) return null;

            var track = new ReplayTrack();
            saveManager.Serializer.Populate(data, track);
            return track;
        }
    }
}
