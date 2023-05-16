using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Serialization;
using Syncfusion.Windows.Tools.Controls;

namespace StarGazer.UI.Services
{
    internal class DockingManagerStateLoader
    {
        // Customer DockingState loader which is more forgiving than the inbuilt one
        // 1. Ignore any DockingParams where the plugin is no longer loaded
        // 2. Ignore any DockItem that is not included in the DockingParams
        // 3. Try to dock matched DockItem and DockingParam. 
        internal static void LoadState(string fileName, ObservableCollection<DockItem> pluginViews, DockingManager dockManager)
        {
            var xs = new XmlSerializer(typeof(DockingParamsRoot));
            using var file = File.OpenRead(fileName);
            var root = xs.Deserialize(file) as DockingParamsRoot;

            if(root != null)
            {
                List<DockItem> items = pluginViews.ToList();
                foreach(var dockingParam in root.Params)
                {
                    if (dockingParam.State == DockState.Document)
                        continue;

                    var item = items.FirstOrDefault(i => i.Name == dockingParam.Name);
                    if(item != null)
                    {
                        // We have a DockingParams and a DockItem - attempt to move to where it used to be
                    }
                }
            }
        }
    }

    [XmlRoot("ArrayOfDockingParams")]
    public class DockingParamsRoot
    {
        [XmlElement("DockingParams")]
        public List<DockingParams> Params { get; set; } = new List<DockingParams>();
    }
}
