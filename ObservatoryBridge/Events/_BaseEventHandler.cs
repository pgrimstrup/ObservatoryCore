using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Bridge.Events
{
    internal class BaseEventHandler
    {
        static public readonly string[] ScoopableStars = { "K", "G", "B", "F", "O", "A", "M" };


        public static string GetBodyName(string name)
        {
            var currentSystem = Bridge.Instance.CurrentSystem;
            if (currentSystem.SystemName == null || name.Length < currentSystem.SystemName.Length || !name.StartsWith(currentSystem.SystemName, StringComparison.OrdinalIgnoreCase))
                return name;

            // Single star system, primary star name is the same as the system name
            if (name.Equals(currentSystem.SystemName, StringComparison.OrdinalIgnoreCase))
                return "A";

            return name.Substring(currentSystem.SystemName.Length).Trim();
        }

    }
}
