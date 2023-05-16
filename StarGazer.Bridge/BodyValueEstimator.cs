using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarGazer.Bridge
{
    internal class BodyValueEstimator
    {
        public static int GetStarValue(string starType, bool firstDiscovery)
        {
            int baseValue = 1200;
            if (starType.IsNeutronStar())
                baseValue = 22628;

            if (starType.IsBlackHole())
                baseValue = 22628;

            if (starType.IsWhiteDwarf())
                baseValue = 14057;

            return firstDiscovery ? (int)(baseValue * 2.6) : baseValue;
        }

        public static int GetKValueForBody(string bodyType, bool terraformable) 
        {
            if (bodyType.IsMetalRich())
                return 21790;
            
            if (bodyType.IsAmmoniaWorld())
                return 9632;

            if (bodyType.IsGasGiant("I"))
                return 1656;

            if (bodyType.IsGasGiant("II"))
                return 9654;

            if (bodyType.IsHighMetalContent())
                return terraformable ? 100677 : 9654;

            if (bodyType.IsWaterWorld())
                return terraformable ? 116295 : 64831;

            if (bodyType.IsEarthlike())
                return terraformable ? 116295 : 64831;

            return terraformable ? 93328 : 300;
        }

        public static int GetBodyValue(int k, double mass, bool isFirstDiscoverer, bool isMapped, bool isFirstMapped, bool withEfficiencyBonus)
        {
            const double q = 0.56591828;
            double mappingMultiplier = 1;
            if (isMapped)
            {
                if (isFirstDiscoverer && isFirstMapped)
                {
                    mappingMultiplier = 3.699622554;
                }
                else if (isFirstMapped)
                {
                    mappingMultiplier = 8.0956;
                }
                else
                {
                    mappingMultiplier = 3.3333333333;
                }
            }
            double value = (k + k * q * Math.Pow(mass, 0.2)) * mappingMultiplier;
            if (isMapped)
            {
                value += ((value * 0.3) > 555) ? value * 0.3 : 555;
                if (withEfficiencyBonus)
                {
                    value *= 1.25;
                }
            }
            value = Math.Max(500, value);
            value *= (isFirstDiscoverer) ? 2.6 : 1;
            return (int)Math.Round(value);
        }
    }
}
