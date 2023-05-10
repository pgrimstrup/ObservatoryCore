using Observatory.Framework;
using Observatory.Framework.Files;
using Observatory.Framework.Files.ParameterTypes;

namespace Observatory.Bridge.Events
{
    internal class StatusChangedEventHandler : BaseEventHandler, IJournalEventHandler<Status>
    {
        private bool HasChanged(StatusFlags flag, Status oldStatus, Status newStatus)
        {
            return (oldStatus.Flags & flag) != (newStatus.Flags & flag);
        }

        private bool HasChanged(StatusFlags2 flag, Status oldStatus, Status newStatus)
        {
            return (oldStatus.Flags2 & flag) != (newStatus.Flags2 & flag);
        }

        public void HandleEvent(Status newStatus)
        {
            var currentStatus = Bridge.Instance.CurrentStatus;
            if(currentStatus != null)
            {
                if (HasChanged(StatusFlags.Masslock, currentStatus, newStatus))
                    DoMasslock(newStatus);

                if (HasChanged(StatusFlags.LandingGear, currentStatus, newStatus))
                    DoLandingGear(newStatus);
            }

            // Copy the instance
            Bridge.Instance.CurrentStatus = newStatus.CopyAs<Status>();
        }

        private void DoMasslock(Status newstatus)
        {
            var log = new BridgeLog(newstatus);
            log.SpokenOnly();

            if (newstatus.Flags.HasFlag(StatusFlags.Masslock))
            {
                log.TitleSsml.Append("Flight Operations");
                log.DetailSsml.Append("Left mass lock, FSD available");
            }
            else
            {
                log.TitleSsml.Append("Flight Operations");
                log.DetailSsml.Append("Mass lock, FSD unavailable");
            }
            Bridge.Instance.LogEvent(log);
        }

        private void DoLandingGear(Status newstatus)
        {
            var log = new BridgeLog(newstatus);
            log.SpokenOnly();

            if (newstatus.Flags.HasFlag(StatusFlags.LandingGear))
            {
                log.TitleSsml.Append("Flight Operations");
                log.DetailSsml.Append("Landing gear down");
            }
            else
            {
                log.TitleSsml.Append("Flight Operations");
                log.DetailSsml.Append("Landing gear up");
            }
            Bridge.Instance.LogEvent(log);
        }

    }
}
