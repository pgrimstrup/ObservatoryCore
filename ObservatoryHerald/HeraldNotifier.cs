using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Observatory.Framework;
using Observatory.Framework.Interfaces;
using Observatory.Herald.TextToSpeech;
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
        private List<Voice> _voices;

        public HeraldNotifier()
        {
            _heraldSettings = DefaultSettings;
        }

        private static HeraldSettings DefaultSettings
        {
            get => new HeraldSettings()
            {
                SelectedVoice = "English (Australia) A, Female",
                SelectedRate = "Default",
                SelectedStyle = "Standard",
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

        public async Task<Dictionary<string, object>> GetVoiceNamesAsync(object settings)
        {
            var styles = await GetVoiceStylesAsync();
            if (styles == null || _voices == null)
                return null;

            var style = styles.First().Key;

            if (settings is HeraldSettings heraldSettings && !String.IsNullOrWhiteSpace(heraldSettings.SelectedStyle))
                style = heraldSettings.SelectedStyle;

            return _voices
                .Where(v => v.Category == style)
                .OrderBy(v => v.Description)
                .ToDictionary(v => v.Description, v => (object)v.Name);
        }

        public async Task<Dictionary<string, object>> GetVoiceStylesAsync()
        {
            if (_voices == null)
                _voices = await _speech.GetVoices();

            if (_voices == null)
                return null;

            var result = new Dictionary<string, object>();
            foreach (var style in _voices.Select(v => v.Category).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(v => v))
                result.Add(style, style);

            return result;
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
