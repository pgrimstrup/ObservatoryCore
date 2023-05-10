using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework.Files;
using Observatory.Framework.Files.Journal;
using Observatory.Framework.Files.ParameterTypes;

namespace Observatory.Bridge
{
    internal class CurrentShipData
    {
        public string ShipType { get; set; } = "";
        public string ShipName { get; set; } = "";
        public string Commander { get; set; } = "";
        public long Credits { get; set; } 
        public double FuelCapacity { get; set; }
        public StatusFlags Status { get; set; }
        public StatusFlags2 Status2 { get; set; }
        public FuelType Fuel { get; private set; } = new FuelType();

        public void Assign(LoadGame load)
        {
            ShipType = String.IsNullOrWhiteSpace(load.Ship_Localised) ? load.Ship : load.Ship_Localised;
            ShipName = load.ShipName;
            Commander = load.Commander;
            Credits = load.Credits;
            FuelCapacity = load.FuelCapacity;
            Status = StatusFlags.MainShip;
            Fuel = new FuelType {
                FuelMain = load.FuelLevel,
                FuelReservoir = 0
            };
        }

        public void Assign(Status status)
        {
            if (status.Fuel != null)
                Fuel = new FuelType {
                    FuelMain = status.Fuel.FuelMain,
                    FuelReservoir = status.Fuel.FuelReservoir
                };
        }
    }
}
