using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Herald.TextToSpeech
{
    public interface IVoice
    {
        string Language { get;  }
        string Name { get;  }
        string Gender { get; }
        string Description { get; }
    }
}
