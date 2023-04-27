using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Bridge
{
    internal static class Emojis
    {
        /// <summary>
        /// Orange circle
        /// </summary>
        public static string Solar => Char.ConvertFromUtf32(0x1F7E1); // yellow circle 

        /// <summary>
        /// Black circle
        /// </summary>
        public static string BlackHole => Char.ConvertFromUtf32(0x26AB); // black circle

        /// <summary>
        /// White circle
        /// </summary>
        public static string WhiteDwarf => Char.ConvertFromUtf32(0x26AA); // white circle

        /// <summary>
        /// Earth
        /// </summary>
        public static string Earthlike => Char.ConvertFromUtf32(0x1F30D); // earth

        /// <summary>
        /// Blue circle
        /// </summary>
        public static string WaterWorld => Char.ConvertFromUtf32(0x1F535); // blue circle

        /// <summary>
        /// Purple circle
        /// </summary>
        public static string HighMetalContent => Char.ConvertFromUtf32(0x1F7E3);  // purple circle

        /// <summary>
        /// Orange circle
        /// </summary>
        public static string Ammonia => Char.ConvertFromUtf32(0x1F7E0); // orange circle 

        /// <summary>
        /// Brown circle
        /// </summary>
        public static string OtherBody => Char.ConvertFromUtf32(0x1F7E4); // brown circle

        /// <summary>
        /// Sunrise
        /// </summary>
        public static string Terraformable => Char.ConvertFromUtf32(0x1F305); // sunrise

        /// <summary>
        /// Blue globe
        /// </summary>
        public static string Mapped => Char.ConvertFromUtf32(0x1F310); // blue globe 

        /// <summary>
        /// Dollar sign
        /// </summary>
        public static string HighValue => Char.ConvertFromUtf32(0x1F4B2); // dollar sign

        /// <summary>
        /// Telescope
        /// </summary>
        public static string FirstDiscovery => Char.ConvertFromUtf32(0x1F52D); // telescope

        /// <summary>
        /// DNA
        /// </summary>
        public static string BioSignals => Char.ConvertFromUtf32(0x1F9EC); // dna 

        /// <summary>
        /// Rock
        /// </summary>
        public static string GeoSignals => Char.ConvertFromUtf32(0x1FAA8); // rock

    }
}
