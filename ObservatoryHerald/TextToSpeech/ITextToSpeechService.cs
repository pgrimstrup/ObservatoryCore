using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Herald.TextToSpeech
{
    public interface ITextToSpeechService
    {
        Task<FileInfo> GetTextToSpeechAsync(string ssml, string filename);
        Task<IEnumerable<Voice>> GetVoicesAsync();
    }
}
