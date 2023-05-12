using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Observatory.Bridge.Events
{
    internal class BaseEventHandler
    {
        static public readonly string[] ScoopableStars = { "K", "G", "B", "F", "O", "A", "M" };

        protected Random R = new Random();

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

            return "Body " + name.Substring(currentSystem.SystemName.Length).Trim();
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
