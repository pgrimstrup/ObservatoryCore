using Observatory.Framework.Files.Journal;

namespace StarGazer.Bridge.Events
{
    internal class MusicEventHandler : BaseEventHandler, IJournalEventHandler<Music>
    {
        public void HandleEvent(Music journal)
        {
            LogInfo($"Music: {journal.MusicTrack}");

            string path = Path.Combine(Bridge.Instance.Core.PluginStorageFolder, "Zune", journal.MusicTrack);
            if (!Directory.Exists(path))
            { 
                Directory.CreateDirectory(path); 
            }
        }
    }
}
