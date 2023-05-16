using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarGazer.Herald.TextToSpeech
{
    public class Voice 
    {
        public Voice()
        {
            Name = "";
            Description = "";
            Language = "en-US";
            Gender = "Male";
            Category = "Default";
            Style = "";
        }

        public Voice(string voiceName)
        {
            Name = voiceName;
            Description = voiceName;
            Language = "en-US";
            Gender = "Male";
            Category = "Default";
            Style = "";
        }

        public string Language { get; set; }

        public string Category { get; set; }

        public string Style { get; set; }

        public string Name { get; set; }

        public string Gender { get; set; }

        public string Description { get; set; }
    }
}
