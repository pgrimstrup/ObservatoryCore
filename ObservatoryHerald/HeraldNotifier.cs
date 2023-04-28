using Microsoft.Extensions.Logging;
using Observatory.Framework;
using Observatory.Framework.Interfaces;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Observatory.Herald
{
    public class HeraldNotifier : IObservatoryNotifierAsync
    {
        IObservatoryCoreAsync _core;
        private HeraldSettings _heraldSettings;
        private HeraldQueue _heraldQueue;

        public HeraldNotifier()
        {
            _heraldSettings = DefaultSettings;
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
            get => _heraldSettings;
            set
            {
                // Need to perform migration here, older
                // version settings object not fully compatible.
                var savedSettings = (HeraldSettings)value;
                if (string.IsNullOrWhiteSpace(savedSettings.SelectedRate))
                {
                    _heraldSettings.SelectedVoice = savedSettings.SelectedVoice;
                    _heraldSettings.Enabled = savedSettings.Enabled;
                }
                else
                {
                    _heraldSettings = savedSettings;
                }
            }
        }

        public void Load(IObservatoryCore core)
        {
            Task.Run(() => LoadAsync(core as IObservatoryCoreAsync)).GetAwaiter().GetResult();
        }

        public async Task LoadAsync(IObservatoryCoreAsync core)
        {
            _core = core;
            var logger = _core.GetService<ILogger<HeraldNotifier>>();
            var speechManager = new SpeechRequestManager(
                _heraldSettings,
                _core.GetService<HttpClient>(),
                _core.PluginStorageFolder,
                logger);

            _heraldQueue = new HeraldQueue(speechManager, logger);
            _heraldSettings.Test = () => {
                _heraldQueue.Enqueue(
                    new NotificationArgs() {
                        Title = "Herald voice testing",
                        Detail = $"This is {_heraldSettings.SelectedVoice.Split(" - ")[1]}, your Herald Vocalizer for spoken notifications."
                    },
                    GetAzureNameFromSetting(_heraldSettings.SelectedVoice),
                    GetAzureStyleNameFromSetting(_heraldSettings.SelectedVoice),
                    _heraldSettings.Rate[_heraldSettings.SelectedRate].ToString(),
                    _heraldSettings.Volume);
            };
            await Task.CompletedTask;
        }

        public async Task UnloadAsync()
        {
            _heraldQueue.Cancel();
            await Task.CompletedTask;
        }

        private void TestVoice()
        {
        }

        public void OnNotificationEvent(NotificationArgs notificationEventArgs)
        {
            Task.Run(() => OnNotificationEventAsync(notificationEventArgs)).GetAwaiter().GetResult();
        }

        public async Task OnNotificationEventAsync(NotificationArgs notificationEventArgs)
        {
            if (_heraldSettings.Enabled)
                _heraldQueue.Enqueue(
                    notificationEventArgs,
                    GetAzureNameFromSetting(_heraldSettings.SelectedVoice),
                    GetAzureStyleNameFromSetting(_heraldSettings.SelectedVoice),
                    _heraldSettings.Rate[_heraldSettings.SelectedRate].ToString(),
                    _heraldSettings.Volume);

            await Task.CompletedTask;
        }

        private string GetAzureNameFromSetting(string settingName)
        {
            if (_heraldSettings.Voices.TryGetValue(settingName, out var name))
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

    }
}
