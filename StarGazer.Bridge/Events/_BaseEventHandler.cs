using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Observatory.Framework.Files.Journal;
using Observatory.Framework.Files.ParameterTypes;
using StarGazer.Framework;
using static System.Net.Mime.MediaTypeNames;

namespace StarGazer.Bridge.Events
{
    internal class BaseEventHandler
    {
        public static Regex CarrierNameRegex = new Regex("(.*)([A-Z0-9]{3}\\-[A-Z0-9]{3})$", RegexOptions.IgnoreCase);

        protected Random R = new Random();
        protected TimeSpan SpokenDestinationInterval = TimeSpan.FromSeconds(90);

        public static CurrentGameState GameState => Bridge.Instance.GameState;

        public string Stars(int count) => "star".CountAndPlural(count);
        public string Planets(int count) => "planet".CountAndPlural(count);
        public string Bodies(int count) => "body".CountAndPlural(count);
        public string NonBodies(int count) => "non-bodies".CountAndPlural(count);


        enum BodyType
        {
            Unknown,
            Null,
            Star,
            BinaryStar,
            Planet,
            BinaryPlanet,
            Moon,
            BinaryMoon,
            Ring,
            BeltCluster
        }

        class Body
        {
            Body? _parent;

            public int BodyId;
            public string? Name;
            public BodyType Type;
            public bool WasDiscovered;

            public Body? Parent
            {
                get => _parent;
                set => SetParent(value);
            }

            public List<Body> Children { get; } = new List<Body>();

            // Create a barycenter body
            public Body(int id, bool wasDiscovered)
            {
                BodyId = id;
                WasDiscovered = wasDiscovered;
                Type = BodyType.Null;
            }

            // Create a body based on the Detailed Scan 
            public Body(Scan scan)
            {
                BodyId = scan.BodyID;
                Name = scan.BodyName;
                WasDiscovered = scan.WasDiscovered;
                Type = scan.IsStar() ? BodyType.Star : (scan.IsBeltCluster() ? BodyType.BeltCluster : BodyType.Unknown);
            }


            public Body(Body? parent)
            {
                SetParent(parent);
            }


            public void SetParent(Body? parent)
            {
                if (parent != _parent)
                {
                    if (_parent != null)
                        _parent.Children.Remove(this);
                    _parent = parent;
                    if (_parent != null)
                    {
                        _parent.Children.Add(this);

                        // We can detect Stars and Binary-Stars early
                        if (_parent.Type == BodyType.Null && (Type == BodyType.Star || Type == BodyType.BinaryStar))
                            _parent.Type = BodyType.BinaryStar;
                    }
                }
            }

            // Bottom-up, checks children last
            public void FindMoons()
            {
                if (Parent != null && (Type == BodyType.Unknown || Type == BodyType.Null))
                {
                    // Initially flagged as planet (or null), we can only keep that status if our parent is a star/binary-star
                    if (Parent.Type == BodyType.Star || Parent.Type == BodyType.BinaryStar || Parent.Type == BodyType.BinaryPlanet)
                    {
                        if (Type == BodyType.Null)
                            Type = BodyType.BinaryPlanet;
                        else
                            Type = BodyType.Planet;
                    }
                    else
                    {
                        if (Type == BodyType.Null)
                            Type = BodyType.BinaryMoon;
                        else
                            Type = BodyType.Moon;
                    }
                }

                // Check all children last
                foreach (var child in Children)
                    child.FindMoons();
            }

            public override string ToString()
            {
                if (Parent == null)
                    return $"{BodyId}, {Type}: {Name}";
                return $"{BodyId}, {Type}: {Name} -> {Parent.BodyId}, {Parent.Type}: {Parent.Name}";
            }
        }

        public void CreateOrrery(out int starCount, out int planetCount, out int discoveredStarCount, out int discoveredPlanetCount)
        {
            starCount = 0;
            planetCount = 0;
            discoveredStarCount = 0;
            discoveredPlanetCount = 0;

            Dictionary<int, Body> system = new Dictionary<int, Body>();

            foreach (var scan in GameState.ScannedBodies.Values.OrderBy(s => s.BodyID))
            {
                var body = new Body(scan);
                system[scan.BodyID] = body;
                if (scan.Parent != null)
                {
                    foreach (var bc in scan.Parent)
                    {
                        // Make sure each parent exists, and assign the body to the appropriate parent-body
                        if (!system.TryGetValue(bc.Body, out var parentBody))
                        {
                            parentBody = new Body(bc.Body, false);
                            parentBody.Type = BodyType.Null;
                            system[bc.Body] = parentBody;
                        }

                        body.Parent = parentBody;
                        body = body.Parent;
                    }
                }

                // Do Rings as well here, if needed
                if (scan.Rings != null)
                {
                    for (int i = 1; i <= scan.Rings.Count; i++)
                    {
                        var ring = new Body(scan.BodyID + i, false);
                        ring.Name = scan.BodyName + " Ring " + ring.BodyId;
                        ring.Type = BodyType.Ring;
                        ring.Parent = system[scan.BodyID];
                        system[ring.BodyId] = ring;
                    }
                }
            }

            // Determine whether non-stars are planets or moons. Satellites around a Star or Binary-Star are planets,
            // satellites around a Planet or Binary-Planet are moons.
            system[0].FindMoons();

            starCount = system.Values.Count(b => b.Type == BodyType.Star);
            discoveredStarCount = system.Values.Count(b => b.Type == BodyType.Star && !b.WasDiscovered);
            planetCount = system.Values.Count(b => b.Type == BodyType.Planet);
            discoveredPlanetCount = system.Values.Count(b => b.Type == BodyType.Planet && !b.WasDiscovered);
        }

        public static string GetBodyName(string name)
        {
            if (String.IsNullOrWhiteSpace(name))
                return name;

            if (GameState.CurrentSystem.StarSystem == null || name.Length < GameState.CurrentSystem.StarSystem.Length || !name.StartsWith(GameState.CurrentSystem.StarSystem, StringComparison.OrdinalIgnoreCase))
                return name;

            // Single star system, primary star name is the same as the system name
            if (name.Equals(GameState.CurrentSystem.StarSystem, StringComparison.OrdinalIgnoreCase))
                return "Star A";

            name = name.Substring(GameState.CurrentSystem.StarSystem.Length).Trim();

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
                case "l":
                    name = "Type-L brown dwarf star";
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

        public void SendScanComplete(JournalBase journal)
        {
            CreateOrrery(out int starCount, out int planetCount, out int discoveredStars, out int discoveredPlanets);

            void AddStarsAndPlanets(BridgeLog log, bool isDiscovery)
            {
                int stars = isDiscovery ? discoveredStars : starCount;
                int planets = isDiscovery ? discoveredPlanets : planetCount;

                if (stars > 0)
                    log.DetailSsml.Append(Stars(stars));
                if (stars > 0 && planets > 0)
                    log.DetailSsml.Append("and");
                if (planets > 0)
                {
                    if (isDiscovery && discoveredPlanets == 1 && planetCount == 1)
                        log.DetailSsml.Append("the only planet");
                    else if (isDiscovery && discoveredPlanets == planetCount)
                        log.DetailSsml.Append("all " + Planets(planetCount));
                    else
                        log.DetailSsml.Append(Planets(planets));
                }
            }

            // First the text log.
            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Science Station");
            log.DetailSsml.Append($"System Scan Complete.");

            if (discoveredStars == 0 && discoveredPlanets == 0)
            {
                log.DetailSsml.Append("This system of");
                AddStarsAndPlanets(log, false);
                log.DetailSsml.Append("has been previously discovered.");

            }
            else if (discoveredStars == starCount && discoveredPlanets == planetCount)
            {
                log.DetailSsml.Append($"We are the first to discover this system consisting of");
                AddStarsAndPlanets(log, false);
                log.DetailSsml.Append(".");
            }
            else
            {
                log.DetailSsml.Append("We are the first to discover");
                AddStarsAndPlanets(log, true);
                log.DetailSsml.Append("in this system of");
                AddStarsAndPlanets(log, false);
                log.DetailSsml.Append(".");
            }

            log.Send();

        }

        protected BridgeLog? FindLogEntry(string eventName, string title)
        {
            return Bridge.Instance.Logs.FirstOrDefault(log => log.EventName == eventName && log.Title == title);
        }

        protected void UpdateSignals(BridgeLog? scanEntry, string bodyName, IEnumerable<Signal> signals)
        {
            if (scanEntry == null || String.IsNullOrEmpty(bodyName))
                return;

            int totalCount = signals.Sum(s => s.Count);
            int bioCount = signals
                .Where(s => s.Type_Localised.StartsWith("Bio", StringComparison.OrdinalIgnoreCase))
                .Sum(s => s.Count);
            int geoCount = signals
                .Where(s => s.Type_Localised.StartsWith("Geo", StringComparison.OrdinalIgnoreCase))
                .Sum(s => s.Count);
            int otherCount = totalCount - bioCount - geoCount;

            List<string> signalText = new List<string>();
            if (bioCount > 0)
                signalText.Add(Emojis.BioSignals + bioCount.ToString());
            if (geoCount > 0)
                signalText.Add(Emojis.GeoSignals + geoCount.ToString());
            if (otherCount > 0)
                signalText.Add(Emojis.OtherSignals + otherCount.ToString());

            scanEntry.Signals = String.Join(" ", signalText);
        }

        protected double DistanceBetween((double x, double y, double z) start, (double x, double y, double z) end)
        {
            if (start.x == 0 && start.y == 0 && start.z == 0)
                return 0;

            double dx = Math.Abs(start.x - end.x);
            double dy = Math.Abs(start.y - end.y);
            double dz = Math.Abs(start.z - end.z);

            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        protected void AppendRemainingJumps(BridgeLog log, bool newCourse)
        {
            if (GameState.JumpDestination.RemainingJumpsInRoute < 1)
                return;

            string planType = newCourse ? "new" : "current";
                 
            if (GameState.JumpDestination.RemainingJumpsInRoute == 1)
            {
                if (newCourse)
                {
                    log.DetailSsml.AppendSsml($"This is the only jump in the {planType} flight plan.");
                    GameState.RemainingJumpsInRouteTimeToSpeak = DateTime.Now.Add(SpokenDestinationInterval * 2);
                }
                else if (GameState.RemainingJumpsInRouteTimeToSpeak < DateTime.Now)
                {
                    log.DetailSsml.AppendSsml($"This is the final jump in the {planType} flight plan.");
                    GameState.RemainingJumpsInRouteTimeToSpeak = DateTime.Now.Add(SpokenDestinationInterval * 2);
                }
            }
            else if (GameState.JumpDestination.RemainingJumpsInRoute > 1 && GameState.RemainingJumpsInRouteTimeToSpeak < DateTime.Now)
            {
                // Every jump below 5, or every multiple of 5
                if (GameState.JumpDestination.RemainingJumpsInRoute < 5 || (GameState.JumpDestination.RemainingJumpsInRoute % 5) == 0)
                {
                    int remain = GameState.JumpDestination.RemainingJumpsInRoute;
                    log.DetailSsml.AppendSsml($"There are {"jump".CountAndPlural(remain)} remaining in the {planType} flight plan.");
                    GameState.RemainingJumpsInRouteTimeToSpeak = DateTime.Now.Add(SpokenDestinationInterval * 2);
                }
            }
            else if (newCourse && GameState.JumpDestination.RemainingJumpsInRoute > 0)
            {
                int remain = GameState.JumpDestination.RemainingJumpsInRoute;
                log.DetailSsml.AppendSsml($"There are {"jump".CountAndPlural(remain)} in the {planType} flight plan.");
                GameState.RemainingJumpsInRouteTimeToSpeak = DateTime.Now.Add(SpokenDestinationInterval * 2);
            }
        }

        protected void AppendHazardousStarWarning(BridgeLog log, string starClass)
        {
            if (starClass.IsNeutronStar() || starClass.IsWhiteDwarf() || starClass.IsBlackHole())
            {
                // Spoken only
                log.DetailSsml
                    .AppendSsml($"<emphasis level=\"moderate\">Commander</emphasis>")
                    .AppendSsml("that is a hazardous star type.")
                    .AppendSsml($"<emphasis level=\"moderate\">Caution is advised</emphasis>")
                    .AppendSsml("on exiting jump");
            }

        }

        protected bool TryGetStationName(string name, out string stationName)
        {
            stationName = name;

            var match = CarrierNameRegex.Match(name);
            if (match.Success)
            {
                // We have a registration, so lookup the name from the signals data that we have in this system
                if(String.IsNullOrWhiteSpace(match.Groups[1].Value))
                {
                    if (GameState.Carriers.TryGetValue(match.Groups[2].Value, out var found))
                        stationName = found;
                    else
                        stationName = name;
                }
                else
                {
                    // Use the carrier name part only
                    stationName = match.Groups[1].Value.Trim();
                    GameState.Carriers[match.Groups[2].Value] = stationName;
                }
            }

            return true;
        }

    }
}
