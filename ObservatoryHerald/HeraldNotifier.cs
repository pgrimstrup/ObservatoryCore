using Microsoft.Extensions.DependencyInjection;
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
                SelectedRate = "1.0",
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
            var logger = _core.Services.GetRequiredService<ILogger<HeraldNotifier>>();
            var speechManager = new SpeechRequestManager(
                _heraldSettings,
                _core.HttpClient,
                _core.PluginStorageFolder,
                logger);

            _heraldQueue = new HeraldQueue(speechManager, logger);
            _heraldSettings.Test = () => {
                _heraldQueue.Enqueue(
                    new NotificationArgs() {
                        Title = "Herald voice testing",
                        Detail = $"This is {_heraldSettings.SelectedVoice.Split(" - ")[1]}, your Herald Vocalizer for spoken notifications."
                    },
                    _heraldSettings.SelectedVoice,
                    GetAzureStyleNameFromSetting(_heraldSettings.SelectedVoice),
                    _heraldSettings.SelectedRate,
                    _heraldSettings.Volume);
            };
            await Task.CompletedTask;
        }

        public async Task UnloadAsync()
        {
            _heraldQueue.Cancel();
            await Task.CompletedTask;
        }

        public Dictionary<string, object> GetVoices()
        {
            return null;
        }

        public Dictionary<string, object> GetVoiceRates()
        {
            return new Dictionary<string, object>
            {
                {"Slowest", "0.5"},
                {"Slower", "0.75"},
                {"Default", "1.0"},
                {"Faster", "1.25"},
                {"Fastest", "1.5"}
            };
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
                    _heraldSettings.SelectedVoice,
                    GetAzureStyleNameFromSetting(_heraldSettings.SelectedVoice),
                    _heraldSettings.SelectedRate,
                    _heraldSettings.Volume);

            await Task.CompletedTask;
        }

        public async Task OnNotificationEventAsync(Guid id, NotificationArgs notificationEventArgs)
        {

            await Task.CompletedTask;
        }

        public async Task OnNotificationCancelledAsync(Guid id)
        {

            await Task.CompletedTask;
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
