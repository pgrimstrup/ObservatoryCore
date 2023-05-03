using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Herald.TextToSpeech
{
    public interface ITextToSpeechService
    {
        FileInfo GetTextToSpeech(string ssml, string filename);
        IEnumerable<IVoice> GetVoices();
    }
}
