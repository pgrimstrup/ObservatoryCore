using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using StarGazer.Framework;

namespace StarGazer.EDSM
{
    public class EdsmWorkerSettings
    {
        [SettingDisplayName("EDSM Submissions Enabled")]
        public bool EnableSubmissions { get; set; }

        [SettingDisplayName("EDSM System Data Download Enabled")]
        public bool EnableSystemDataDownload { get; set; }

        [SettingDisplayName("EDSM Commander Name")]
        public string CommanderName { get; set; } = "";

        [SettingDisplayName("EDSM API Key")]
        public string EdsmApiKey { get; set; } = "";

        [SettingDisplayName("Last Event Date")]
        public string LastEventDateText
        {
            get => LastEventDate.ToString("d");
            set
            {
                if (DateTime.TryParse(value, out DateTime dt))
                    LastEventDate = dt;
            }
        }

        [SettingIgnore]
        public DateTime LastEventDate { get; set; }

        [SettingIgnore]
        public string[] JournalDiscardList { get; set; } = new string[0];
    }
}
