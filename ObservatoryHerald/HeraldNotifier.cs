using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Observatory.Framework;
using Observatory.Framework.Interfaces;
using Observatory.Herald.TextToSpeech;

namespace Observatory.Herald
{
    public class HeraldNotifier : IObservatoryNotifierAsync
    {
        private IObservatoryCoreAsync _core;
        private ILogger _logger;
        private IAudioPlayback _player;
        private HeraldSettings _heraldSettings;
        private HeraldQueue _heraldQueue;
        private SpeechRequestManager _speech;

        static Lazy<HeraldSettings> _defaultSettings;
        Lazy<List<Voice>> _voices;
        Lazy<Dictionary<string, object>> _voiceRates;
        Lazy<Dictionary<string, object>> _voiceStyles;
        Lazy<Dictionary<string, object>> _audioEncodings;

        static HeraldNotifier()
        {
            _defaultSettings = new Lazy<HeraldSettings>(() => {
                return new HeraldSettings() {
                    SelectedVoice = "English (Australia) A, Female",
                    SelectedRate = "Default",
                    SelectedStyle = "Standard",
                    AudioEncoding = ".wav",
                    Volume = 75,
                    Enabled = false,
                    ApiEndpoint = GoogleCloud.ApiEndPoint,
                    CacheSize = 100
                };
            });
        }

        public HeraldNotifier()
        {
            _heraldSettings = DefaultSettings;
            _voices = new Lazy<List<Voice>>(() => {
                return Task.Run(() => _speech.GetVoices()).GetAwaiter().GetResult();
            });
            _voiceRates = new Lazy<Dictionary<string, object>>(() => {
                return new Dictionary<string, object>
                {
                    {"Slowest", "0.5"},
                    {"Slower", "0.75"},
                    {"Default", "1.0"},
                    {"Faster", "1.25"},
                    {"Fastest", "1.5"}
                };
            });
            _voiceStyles = new Lazy<Dictionary<string, object>>(() => {
                if (_voices.Value == null)
                    return null;

                var result = new Dictionary<string, object>();
                foreach (var style in _voices.Value.Select(v => v.Category).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(v => v))
                    result.Add(style, style);

                return result;
            });
            _audioEncodings = new Lazy<Dictionary<string, object>>(() => {
                return new Dictionary<string, object>
                {
                    {".wav", ".wav" },
                    {".mp3", ".mp3" },
                    {".ogg", ".ogg" }
                };
            });

        }

        private static HeraldSettings DefaultSettings => _defaultSettings.Value;

        public string Name => "Observatory Herald";

        public string ShortName => "Herald";

        public string Version => typeof(HeraldNotifier).Assembly.GetName().Version.ToString();

        public PluginUI PluginUI => new(PluginUI.UIType.None, null);

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
            _player = _core.Services.GetRequiredService<IAudioPlayback>();
            _speech = new SpeechRequestManager(
                _heraldSettings,
                _core.HttpClient,
                Path.Combine(_core.PluginStorageFolder, "HeraldCache"),
                _logger);

            _heraldQueue = new HeraldQueue(_speech, _logger, _player);
            await Task.CompletedTask;
        }

        public async Task UnloadAsync()
        {
            _heraldQueue.Cancel();
            await Task.CompletedTask;
        }

        public async Task<Dictionary<string, object>> GetVoiceNamesAsync(object settings)
        {
            if (_voiceStyles.Value == null || _voices.Value == null)
                return null;

            var style = _voiceStyles.Value.First().Key;

            if (settings is HeraldSettings heraldSettings && !String.IsNullOrWhiteSpace(heraldSettings.SelectedStyle))
                style = heraldSettings.SelectedStyle;

            return _voices.Value
                .Where(v => v.Category == style)
                .OrderBy(v => v.Description)
                .ToDictionary(v => v.Description, v => (object)v.Name);
        }

        public Dictionary<string, object> GetVoiceStyles()
        {
            return _voiceStyles.Value;
        }

        public Dictionary<string, object> GetVoiceRates()
        {
            return _voiceRates.Value;
        }

        public Dictionary<string, object> GetAudioEncodings()
        {
            return _audioEncodings.Value;
        }

        public async Task TestVoiceSettings(object testSettings)
        {
            if (testSettings is HeraldSettings settings)
            {
                var rate = (string)_voiceRates.Value[settings.SelectedRate];
                var style = (string)_voiceStyles.Value[settings.SelectedStyle];
                var voice = _voices.Value.FirstOrDefault(v => v.Description == settings.SelectedVoice && v.Category == style);
                var encoding = (string)_audioEncodings.Value[settings.AudioEncoding];

                Debug.WriteLine($"Testing Herald Voice settings using {voice.Name} at {rate} rate encoded as {encoding}");

                var notificationEventArgs = new NotificationArgs {
                    Suppression = NotificationSuppression.Title,
                    Detail = $"This is a test of the Herald Voice Notifier, using the {settings.SelectedVoice} voice.",
                    VoiceName = voice.Name,
                    VoiceRate = (string)rate,
                    VoiceStyle = (string)style,
                    VoiceVolume = settings.Volume,
                    AudioEncoding = (string)encoding
                };

                _heraldQueue.Enqueue(notificationEventArgs);
            }
        }

        public void OnNotificationEvent(NotificationArgs args)
        {
            if (_heraldSettings.Enabled)
            {
                args.VoiceName ??= _heraldSettings.SelectedVoice;
                args.VoiceRate ??= _heraldSettings.SelectedRate;
                args.VoiceStyle ??= _heraldSettings.SelectedStyle;
                args.VoiceVolume ??= _heraldSettings.Volume;
                args.AudioEncoding ??= _heraldSettings.AudioEncoding;

                _heraldQueue.Enqueue(args);
            }
        }

        public async Task OnNotificationEventAsync(NotificationArgs args)
        {
            OnNotificationEvent(args);
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

    }
}
