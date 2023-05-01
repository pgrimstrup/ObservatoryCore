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
        /// White sun
        /// </summary>
        public static string WhiteDwarf => Char.ConvertFromUtf32(0x1F323); 

        /// <summary>
        /// Earth
        /// </summary>
        public static string Earthlike => Char.ConvertFromUtf32(0x1F30D); // earth

        /// <summary>
        /// Blue circle
        /// </summary>
        public static string WaterWorld => Char.ConvertFromUtf32(0x1F535); // blue circle

        /// <summary>
        /// New Moon
        /// </summary>
        public static string HighMetalContent => Char.ConvertFromUtf32(0x1F311);  

        /// <summary>
        /// Orange circle
        /// </summary>
        public static string Ammonia => Char.ConvertFromUtf32(0x1F7E0); // orange circle 

        /// <summary>
        /// Ringed planet
        /// </summary>
        public static string GasGiant => Char.ConvertFromUtf32(0x1FA90); 

        /// <summary>
        /// White circle
        /// </summary>
        public static string IcyBody => Char.ConvertFromUtf32(0x26AA); // white circle

        /// <summary>
        /// Brown circle
        /// </summary>
        public static string OtherBody => Char.ConvertFromUtf32(0x1F7E4); // brown circle

        /// <summary>
        /// Rainbow
        /// </summary>
        public static string Terraformable => Char.ConvertFromUtf32(0x1F308); 

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
        /// Cactus
        /// </summary>
        public static string BioSignals => Char.ConvertFromUtf32(0x1F335);

        /// <summary>
        /// Rock
        /// </summary>
        public static string GeoSignals => Char.ConvertFromUtf32(0x1FAA8); // rock

        /// <summary>
        /// Satellite
        /// </summary>
        public static string Probe => Char.ConvertFromUtf32(0x1F6F0);

        public static string Approaching => Char.ConvertFromUtf32(0x1F6EC);

        /// <summary>
        /// Airplane arriving
        /// </summary>
        public static string Touchdown => Char.ConvertFromUtf32(0x1F6EC);

        /// <summary>
        /// Airplane departure
        /// </summary>
        public static string Liftoff => Char.ConvertFromUtf32(0x1F6EB);
        public static string Departing => Char.ConvertFromUtf32(0x1F6EB);

        /// <summary>
        /// Fuel pump
        /// </summary>
        public static string FuelScoop => Char.ConvertFromUtf32(0x26FD);


        /// <summary>
        /// Military medal
        /// </summary>
        public static string Promotion => Char.ConvertFromUtf32(0x1F396);
    }
}
