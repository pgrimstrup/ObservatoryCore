using StarGazer.Framework;

namespace StarGazer.Herald.TextToSpeech
{
    public interface ITextToSpeechService
    {
        Task<bool> GetTextToSpeechAsync(VoiceNotificationArgs args, string ssml, string filename);
        Task<IEnumerable<Voice>> GetVoicesAsync();
    }
}
