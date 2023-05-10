using Observatory.Framework;
using Observatory.Framework.Files;
using Observatory.Framework.Files.ParameterTypes;

namespace Observatory.Bridge.Events
{
    internal class StatusChangedEventHandler : BaseEventHandler, IJournalEventHandler<Status>
    {
        private bool HasShipStatusChanged(StatusFlags flag, CurrentShipData ship, Status newStatus)
        {
            if(ship.Status.HasFlag(StatusFlags.MainShip) && newStatus.Flags.HasFlag(StatusFlags.MainShip))
                return (ship.Status & flag) != (newStatus.Flags & flag);
            return false;
        }

        private bool HasSrvStatusChanged(StatusFlags flag, CurrentShipData ship, Status newStatus)
        {
            if (ship.Status.HasFlag(StatusFlags.SRV) && newStatus.Flags.HasFlag(StatusFlags.SRV))
                return (ship.Status & flag) != (newStatus.Flags & flag);
            return false;
        }

        private bool HasOnFootStatusChanged(StatusFlags2 flag, Status oldStatus, Status newStatus)
        {
            if (oldStatus.Flags2.HasFlag(StatusFlags2.OnFoot) && newStatus.Flags2.HasFlag(StatusFlags2.OnFoot))
                return (oldStatus.Flags2 & flag) != (newStatus.Flags2 & flag);
            return false;
        }


        bool _fuelBelow25 = false;
        bool _fuelBelow10 = false;
        bool _fuelBelow5 = false;
        bool _fuelBelow1 = false;

        public void HandleEvent(Status newStatus)
        {
            CheckFuelLevel(newStatus);

            var ship = Bridge.Instance.CurrentShip;
            if (ship.Status == newStatus.Flags && ship.Status2 == newStatus.Flags2)
                return;

            if(ship.Status != newStatus.Flags)
                LogInfo($"  StatusFlags : {ship.Status} -> {newStatus.Flags}");
            if(ship.Status2 != newStatus.Flags2)
                LogInfo($"  StatusFlags2: {ship.Status2} -> {newStatus.Flags2}");

            if (HasShipStatusChanged(StatusFlags.Masslock, ship, newStatus))
                DoMasslock(newStatus);

            if (HasShipStatusChanged(StatusFlags.LandingGear, ship, newStatus))
                DoLandingGear(newStatus);

            // Copy the instance
            Bridge.Instance.CurrentShip.Status = newStatus.Flags;
            Bridge.Instance.CurrentShip.Status2 = newStatus.Flags2;
        }

        private void CheckFuelLevel(Status status)
        {
            var ship = Bridge.Instance.CurrentShip;
            if (ship == null )
                return;

            ship.Assign(status);
            if (ship.FuelCapacity == 0)
                return;

            BridgeLog? log = null;
            double fuelLevel = ship.Fuel.FuelMain / ship.FuelCapacity;
            if (fuelLevel <= 0.01)
            {
                if (!_fuelBelow1)
                {
                    log = new BridgeLog(status);
                    log.SpokenOnly()
                        .DetailSsml.AppendEmphasis("Commander", EmphasisType.Strong)
                        .Append("Fuel level is critical. Main Fuel tank is below 1 percent.");
                }
                _fuelBelow1 = true;
                _fuelBelow5 = true;
                _fuelBelow10 = true;
                _fuelBelow25 = true;
            }
            else if (fuelLevel <= 0.05)
            {
                if (!_fuelBelow5)
                {
                    log = new BridgeLog(status);
                    log.SpokenOnly()
                        .DetailSsml.AppendEmphasis("Commander", EmphasisType.Moderate)
                        .Append("Fuel level is dangerously low. Main Fuel tank is below 5 percent.");
                }
                _fuelBelow1 = false;
                _fuelBelow5 = true;
                _fuelBelow10 = true;
                _fuelBelow25 = true;
            }
            else if (fuelLevel <= 0.10)
            {
                if (!_fuelBelow10)
                {
                    log = new BridgeLog(status);
                    log.SpokenOnly()
                        .DetailSsml.AppendEmphasis("Commander", EmphasisType.Moderate)
                        .Append("Fuel level is very low. Main Fuel tank is below 10 percent.");
                }
                _fuelBelow1 = false;
                _fuelBelow5 = false;
                _fuelBelow10 = true;
                _fuelBelow25 = true;
            }
            else if (fuelLevel <= 0.25)
            {
                if (!_fuelBelow25)
                {
                    log = new BridgeLog(status);
                    log.SpokenOnly()
                        .DetailSsml.AppendEmphasis("Commander", EmphasisType.Moderate)
                        .Append("Fuel level is low. Main Fuel tank is below 25 percent.");
                }
                _fuelBelow1 = false;
                _fuelBelow5 = false;
                _fuelBelow10 = false;
                _fuelBelow25 = true;
            }
            else
            {
                _fuelBelow1 = false;
                _fuelBelow5 = false;
                _fuelBelow10 = false;
                _fuelBelow25 = false;
            }

            if (log != null)
                Bridge.Instance.LogEvent(log);
        }

        private void DoMasslock(Status newstatus)
        {
            var log = new BridgeLog(newstatus);
            log.SpokenOnly();

            if (newstatus.Flags.HasFlag(StatusFlags.Masslock))
            {
                log.TitleSsml.Append("Flight Operations");
                log.DetailSsml.Append("Mass lock, FSD unavailable");
            }
            else
            {
                log.TitleSsml.Append("Flight Operations");
                log.DetailSsml.Append("Left mass lock, FSD available");
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
                log.DetailSsml.Append("Deploying landing gear");
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
