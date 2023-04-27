using Microsoft.Extensions.Logging;
using Observatory.Framework;
using Observatory.Framework.Interfaces;
using System.Text.Json;

namespace Observatory.Herald
{
    public class HeraldNotifier : IObservatoryNotifier, IDisposable
    {
        public HeraldNotifier()
        {
            heraldSettings = DefaultSettings;
        }

        private static HeraldSettings DefaultSettings
        {
            get => new HeraldSettings()
            {
                SelectedVoice = "American - Christopher",
                SelectedRate = "Default",
                Volume = 75,
                Enabled = false,
                ApiEndpoint = "https://api.observatory.xjph.net/AzureVoice",
                CacheSize = 100
            };
        }

        public string Name => "Observatory Herald";

        public string ShortName => "Herald";

        public string Version => typeof(HeraldNotifier).Assembly.GetName().Version.ToString();

        public PluginUI PluginUI => new (PluginUI.UIType.None, null);

        public NotificationRendering Filter { get; } = NotificationRendering.PluginNotifier;

        public object Settings
        {
            get => heraldSettings;
            set
            {
                // Need to perform migration here, older
                // version settings object not fully compatible.
                var savedSettings = (HeraldSettings)value;
                if (string.IsNullOrWhiteSpace(savedSettings.SelectedRate))
                {
                    heraldSettings.SelectedVoice = savedSettings.SelectedVoice;
                    heraldSettings.Enabled = savedSettings.Enabled;
                }
                else
                {
                    heraldSettings = savedSettings;
                }
            }
        }

        public void Dispose()
        {
            heraldQueue.Cancel();
        }

        public void Load(IObservatoryCore core)
        {
            var logger = core.GetService<ILogger<HeraldNotifier>>();
            var speechManager = new SpeechRequestManager(
                heraldSettings, 
                core.GetService<HttpClient>(),
                core.PluginStorageFolder, 
                logger);

            heraldQueue = new HeraldQueue(speechManager, logger);
            heraldSettings.Test = TestVoice;
        }

        public void Unload()
        {

        }

        private void TestVoice()
        {
            heraldQueue.Enqueue(
                new NotificationArgs() 
                { 
                    Title = "Herald voice testing", 
                    Detail = $"This is {heraldSettings.SelectedVoice.Split(" - ")[1]}, your Herald Vocalizer for spoken notifications." 
                }, 
                GetAzureNameFromSetting(heraldSettings.SelectedVoice),
                GetAzureStyleNameFromSetting(heraldSettings.SelectedVoice),
                heraldSettings.Rate[heraldSettings.SelectedRate].ToString(),
                heraldSettings.Volume);
        }

        public void OnNotificationEvent(NotificationArgs notificationEventArgs)
        {
            if (heraldSettings.Enabled)
                heraldQueue.Enqueue(
                    notificationEventArgs, 
                    GetAzureNameFromSetting(heraldSettings.SelectedVoice),
                    GetAzureStyleNameFromSetting(heraldSettings.SelectedVoice),
                    heraldSettings.Rate[heraldSettings.SelectedRate].ToString(), 
                    heraldSettings.Volume);
        }

        public void OnNotificationCancelled(Guid id)
        {

        }

        private string GetAzureNameFromSetting(string settingName)
        {
            if (heraldSettings.Voices.TryGetValue(settingName, out var name))
                return name.ToString();
            return settingName;
        }

        private string GetAzureStyleNameFromSetting(string settingName)
        {
            string[] settingParts = settingName.Split(" - ");
            
            if (settingParts.Length == 3)
                return settingParts[2];
            else
                return string.Empty;
        }

        private HeraldSettings heraldSettings;
        private HeraldQueue heraldQueue;
    }
}
