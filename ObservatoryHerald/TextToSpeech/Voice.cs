using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Herald.TextToSpeech
{
    internal class Voice : IVoice
    {
        public Voice()
        {
            Name = "";
            Description = "";
            Language = "en-US";
            Gender = "Male";

        }

        public Voice(string voiceName)
        {
            Name = voiceName;
            Description = voiceName;
            Language = "en-US";
            Gender = "Male";
        }

        public string Language { get; set; }

        public string Name { get; set; }

        public string Gender { get; set; }

        public string Description { get; set; }
    }
}
