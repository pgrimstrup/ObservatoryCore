using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace Observatory.Bridge.Events
{
    internal class BaseEventHandler
    {
        protected Random R = new Random();
        protected TimeSpan SpokenDestinationInterval = TimeSpan.FromSeconds(60);

        public static string GetBodyName(string name)
        {
            if (String.IsNullOrWhiteSpace(name))
                return name;

            var currentSystem = Bridge.Instance.CurrentSystem;
            if (currentSystem.SystemName == null || name.Length < currentSystem.SystemName.Length || !name.StartsWith(currentSystem.SystemName, StringComparison.OrdinalIgnoreCase))
                return name;

            // Single star system, primary star name is the same as the system name
            if (name.Equals(currentSystem.SystemName, StringComparison.OrdinalIgnoreCase))
                return "Star A";

            name = name.Substring(currentSystem.SystemName.Length).Trim();

            // Stars are named A..Z, and have a length of 1
            if (name.Length == 1 && Char.IsLetter(name[0]))
                return "Star " + name;

            return "Body " + name;
        }

        public string GetStarTypeName(string starType)
        {
            string name;

            switch (starType.ToLower())
            {
                case "b":
                    name = "Type-B blue-white giant star";
                    break;
                case "b_bluewhitesupergiant":
                    name = "Type-B blue-white supergiant star";
                    break;
                case "a":
                    name = "Type-A blue-white giant star";
                    break;
                case "a_bluewhitesupergiant":
                    name = "Type-A blue-white supergiant star";
                    break;
                case "f":
                    name = "Type-F white giant star";
                    break;
                case "f_whitesupergiant":
                    name = "Type-F white supergiant star";
                    break;
                case "g":
                    name = "Type-G yellow-white star";
                    break;
                case "g_whitesupergiant":
                    name = "Type-G white supergiant star";
                    break;
                case "k":
                    name = "Type-K yellow-orange star";
                    break;
                case "k_orangegiant":
                    name = "Type-K orange giant star";
                    break;
                case "m":
                    name = "Type-M red dwarf star";
                    break;
                case "m_redgiant":
                    name = "Type-M red giant star";
                    break;
                case "m_redsupergiant":
                    name = "Type-M red supergiant star";
                    break;
                case "aebe":
                    name = "Herbig AE/BE star";
                    break;
                case "w":
                case "wn":
                case "wnc":
                case "wc":
                case "wo":
                    name = "Wolf-Rayet star";
                    break;
                case "c":
                case "cs":
                case "cn":
                case "cj":
                case "ch":
                case "chd":
                    name = "Carbon star";
                    break;
                case "s":
                    name = "Type-S star";
                    break;
                case "ms":
                    name = "Type-MS star";
                    break;
                case "d":
                case "da":
                case "dab":
                case "dao":
                case "daz":
                case "dav":
                case "db":
                case "dbz":
                case "dbv":
                case "do":
                case "dov":
                case "dq":
                case "dc":
                case "dcv":
                case "dx":
                    name = "white dwarf";
                    break;
                case "n":
                    name = "neutron star";
                    break;
                case "h":
                    name = "black hole";
                    break;
                case "supermassiveblackhole":
                    name = "supermassive black hole";
                    break;
                case "x":
                    name = "exotic star";
                    break;
                case "rogueplanet":
                    name = "rogue planet";
                    break;
                case "tts":
                case "t":
                    name = "Type-T tauri star";
                    break;
                default:
                    if (starType.IsNeutronStar() || starType.IsBlackHole() || starType.IsWhiteDwarf())
                        return starType;

                    if (starType.Length == 1)
                        name = $"Type-{starType} star";
                    else
                        name = $"{starType} star";
                    break;
            }

            return name;
        }

        protected string ArticleFor(string text)
        {
            if (!String.IsNullOrWhiteSpace(text) && text.ToLower().IndexOfAny("aeiou".ToCharArray()) == 0)
                return "an";
            else
                return "a";
        }

        protected void LogInfo(string message)
        {
            Bridge.Instance.Core.GetPluginErrorLogger(Bridge.Instance).Invoke(null, message);
        }

        protected void LogError(Exception ex, string message)
        {
            Bridge.Instance.Core.GetPluginErrorLogger(Bridge.Instance).Invoke(ex, message);
        }

        protected void Speak(string text)
        {
            LogInfo(text);

            var log = new BridgeLog();
            log.SpokenOnly();
            log.DetailSsml.Append(text);
            Bridge.Instance.LogEvent(log);
        }
    }
}
