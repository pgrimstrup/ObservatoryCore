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
        private IObservatoryCoreAsync _core;
        private ILogger _logger;
        private HeraldSettings _heraldSettings;
        private HeraldQueue _heraldQueue;
        private SpeechRequestManager _speech;

        public HeraldNotifier()
        {
            _heraldSettings = DefaultSettings;
        }

        private static HeraldSettings DefaultSettings
        {
            get => new HeraldSettings()
            {
                SelectedVoice = "",
                SelectedRate = "Default",
                Volume = 75,
                Enabled = false,
                ApiEndpoint = "",
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
            Task.Run(() => LoadAsync((IObservatoryCoreAsync)core)).GetAwaiter().GetResult();
        }

        public async Task LoadAsync(IObservatoryCoreAsync core)
        {
            _core = core;

            _logger = _core.Services.GetRequiredService<ILogger<HeraldNotifier>>();
            _speech = new SpeechRequestManager(
                _heraldSettings,
                _core.HttpClient,
                Path.Combine(_core.PluginStorageFolder, "HeraldCache"),
                _logger);

            _heraldQueue = new HeraldQueue(_speech, _logger);
            await Task.CompletedTask;
        }

        public async Task UnloadAsync()
        {
            _heraldQueue.Cancel();
            await Task.CompletedTask;
        }

        public async Task<Dictionary<string, object>> GetVoiceNamesAsync()
        {
            var voices = await _speech.GetVoices();
            return voices.ToDictionary(v => v.Description, v => (object)v.Name);
        }

        public async Task<Dictionary<string, object>> GetVoiceStylesAsync()
        {
            var voices = await _speech.GetVoices();
            return voices.ToDictionary(v => v.Description, v => (object)v.Name);
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

        public async Task TestVoiceSettings(object testSettings)
        {
            if(testSettings is HeraldSettings settings)
            {
                var notificationEventArgs = new NotificationArgs {
                    Suppression = NotificationSuppression.Title,
                    Detail = $"This is a test of the Herald Voice Notifier, using the {settings.SelectedVoice} voice.",
                    VoiceName = settings.SelectedVoice,
                    VoiceRate = settings.SelectedRate,
                    VoiceStyle = settings.SelectedStyle,
                    VoiceVolume = settings.Volume
                };

                await OnNotificationEventAsync(notificationEventArgs);
            }
        }

        public void OnNotificationEvent(NotificationArgs notificationEventArgs)
        {
            Task.Run(() => OnNotificationEventAsync(notificationEventArgs)).GetAwaiter().GetResult();
        }

        public async Task OnNotificationEventAsync(NotificationArgs args)
        {
            if (_heraldSettings.Enabled)
            {
                args.VoiceName ??= _heraldSettings.SelectedVoice;
                args.VoiceRate ??= _heraldSettings.SelectedRate;
                args.VoiceStyle ??= _heraldSettings.SelectedStyle;
                args.VoiceVolume ??= _heraldSettings.Volume;

                _heraldQueue.Enqueue(args);
            }

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
