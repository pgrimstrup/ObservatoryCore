using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Security.Cryptography;
using Observatory.Framework;
using Observatory.Framework.Files;
using Observatory.Framework.Files.Journal;
using Observatory.Framework.Interfaces;

namespace StarGazer.Colonization
{
    public class ColonizationWorker : IObservatoryWorker
    {
        private IObservatoryCore _core = null!;
        private PluginUI _pluginUI = null!;
        private ObservableCollection<object> _gridData = new ();
        private ColonizationSettings _settings = new();

        public string Name => "StarGazer.Colonization";

        public string ShortName => "Colonization";

        public string Version => "1.0.0";

        public PluginUI PluginUI => _pluginUI;

        public object Settings 
        { 
            get => _settings;
            set { _settings = (ColonizationSettings)value; }
        }

        public void JournalEvent<TJournal>(TJournal journal) where TJournal : JournalBase
        {
            if(journal.Event == "CarrierLocation")
            {

            }
            if (journal.Event == "CarrierStats")
            {

            }
            if (journal.Event == "Cargo")
            {
                if (journal is Cargo cargo)
                {
                    Debug.WriteLine($"  >> Cargo: [{cargo.Count}] units in Cargo");
                }
                else if(journal is CargoFile cf)
                {
                    // Current ship's inventory
                    var items = "";
                    if(cf.Count > 0)
                        items = String.Join(", ", cf.Inventory.Select(i => $"{i.Count} x {i.Name_Localised ?? i.Name}"));
                    Debug.WriteLine($"  >> CargoFile: [{cf.Count}] " + items);
                }
            }
            if (journal.Event == "CargoTransfer" && journal is CargoTransfer ct)
            {
                foreach (var transfer in ct.Transfers)
                {
                    Debug.WriteLine($"  >> CargoTransfer: {transfer.Direction} {transfer.Count} x {transfer.Type_Localised ?? transfer.Type}");
                }
            }
            if(journal.Event == "Market" && journal is MarketFile mf)
            {

            }
            if(journal.Event == "MarketBuy" && journal is MarketBuy mb)
            {
                Debug.WriteLine($"  >> {mb.Event}: {mb.Count} of {mb.Type_Localised ?? mb.Type}");
            }
        }

        public void Load(IObservatoryCore observatoryCore)
        {
            ColonizationGridItem uiObject = new ColonizationGridItem { Commodity = "" };

            _gridData.Add(uiObject);
            _pluginUI = new PluginUI(_gridData);


            _core = observatoryCore;
        }
    }


}
