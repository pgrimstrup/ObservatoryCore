using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Herald.TextToSpeech
{
    public interface ITextToSpeech
    {
        string Text { get; set; }
        string Ssml { get; set; }

        float Rate { get; set; }

    }
}
