using StarGazer.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarGazer.Botanist
{
    class BotanistSettings
    {
        [SettingDisplayName("Enable Sampler Status Overlay")]
        public bool OverlayEnabled { get; set; }
    }
}
