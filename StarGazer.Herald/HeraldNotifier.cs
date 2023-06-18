using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Observatory.Framework;
using Observatory.Framework.Interfaces;
using StarGazer.Framework;
using StarGazer.Framework.Interfaces;
using StarGazer.Herald.TextToSpeech;

namespace StarGazer.Herald
{
    public class HeraldNotifier : IStarGazerNotifier
    {
        private IStarGazerCore _core;
        private ILogger _logger;
        private IAudioPlayback _player;
        private HeraldSettings _heraldSettings;
        private HeraldQueue _heraldQueue;
        private SpeechRequestManager _speech;

        static Lazy<HeraldSettings> _defaultSettings;
        Lazy<List<Voice>> _voices;
        Lazy<Dictionary<string, object>> _voiceRates;
        Lazy<Dictionary<string, object>> _voiceStyles;

        static HeraldNotifier()
        {
            _defaultSettings = new Lazy<HeraldSettings>(() => {
                return new HeraldSettings() {
                    SelectedVoice = "English (Australia) A, Female",
                    SelectedRate = 0,
                    SelectedPitch = 0,
                    SelectedStyle = "Standard",
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
        }

        private static HeraldSettings DefaultSettings => _defaultSettings.Value;

        public string Name => "Observatory Herald";

        public string ShortName => "Herald";

        public string Version => typeof(HeraldNotifier).Assembly.GetName().Version.ToString();

        public PluginUI PluginUI => new PluginUI(PluginUI.UIType.None, null);

        public NotificationRendering Filter { get; } = NotificationRendering.PluginNotifier;

        public string DefaultVoiceName => _voices.Value.FirstOrDefault(v => v.Description == _heraldSettings.SelectedVoice && v.Category == DefaultVoiceStyle)?.Name;
        public string DefaultVoiceStyle => (string)_voiceStyles.Value[_heraldSettings.SelectedStyle];
        public int DefaultVoiceVolume => _heraldSettings.Volume;

        public string DefaultVoiceRate => "1.00";
        public string DefaultVoicePitch => "0.00";

        public object Settings
        {
            get => _heraldSettings;
            set => _heraldSettings = (HeraldSettings)value;
        }

        public void Load(IObservatoryCore core)
        {
            Task.Run(() => LoadAsync((IStarGazerCore)core)).GetAwaiter().GetResult();
        }

        public async Task LoadAsync(IStarGazerCore core)
        {
            _core = core;

            var settings = _core.Services.GetRequiredService<IAppSettings>();
            _logger = _core.Services.GetRequiredService<ILogger<HeraldNotifier>>();
            _player = _core.Services.GetRequiredService<IAudioPlayback>();
            _speech = new SpeechRequestManager(
                settings,
                _heraldSettings,
                _core.HttpClient,
                Path.Combine(_core.PluginStorageFolder, "HeraldCache"),
                _logger,
                _player);

            _heraldQueue = new HeraldQueue(_speech, _logger, _player);
            await Task.CompletedTask;
        }

        public async Task UnloadAsync()
        {
            _heraldQueue.Cancel();
            await Task.CompletedTask;
        }

        public Task<Dictionary<string, object>> GetVoiceNamesAsync(object settings)
        {
            if (_voiceStyles.Value == null || _voices.Value == null)
                return null;

            var style = _voiceStyles.Value.First().Key;

            if (settings is HeraldSettings heraldSettings && !String.IsNullOrWhiteSpace(heraldSettings.SelectedStyle))
                style = heraldSettings.SelectedStyle;

            return Task.FromResult(_voices.Value
                .Where(v => v.Category == style)
                .OrderBy(v => v.Description)
                .ToDictionary(v => v.Description, v => (object)v.Name));
        }

        public Dictionary<string, object> GetVoiceStyles()
        {
            return _voiceStyles.Value;
        }

        public Dictionary<string, object> GetVoiceRates()
        {
            return _voiceRates.Value;
        }

        public Task TestVoiceSettings(object testSettings)
        {
            if (testSettings is HeraldSettings settings)
            {
                // The full speed range is just stupid, so we will limit the speed range
                // 0 to 50 -> 0.50 to 1.00
                // 51 to 100 -> 1.00 to 2.00
                double rate =  settings.SelectedRate;
                if (rate < 50)
                    rate = Math.Round(0.50 + rate / 100.0, 2);
                else if (rate > 50)
                    rate = 1 + Math.Round((rate - 50) / 50.0, 2);
                else
                    rate = 1;

                // 0 to 100 -> -20 to +20
                double pitch = Math.Round((settings.SelectedPitch - 50) / 2.5, 2);
                var style = (string)_voiceStyles.Value[settings.SelectedStyle];
                var voice = _voices.Value.FirstOrDefault(v => v.Description == settings.SelectedVoice && v.Category == style);

                Debug.WriteLine($"Testing Herald Voice settings using voice {voice?.Name} at Rate {rate}, Pitch {pitch}");

                var notificationEventArgs = new VoiceNotificationArgs {
                    Suppression = NotificationSuppression.Title,
                    Detail = $"This is a test of the Herald Voice Notifier, using the {settings.SelectedVoice} voice.",
                    VoiceName = voice?.Name,
                    VoiceRate = rate.ToString(),
                    VoicePitch = pitch.ToString(),
                    VoiceStyle = style,
                    VoiceVolume = settings.Volume
                };

                _heraldQueue.Enqueue(notificationEventArgs);
            }
            return Task.CompletedTask;
        }

        public async Task ClearVoiceCache(object _)
        {
            await _speech.ClearCache();
        }

        public void OnNotificationEvent(NotificationArgs args)
        {
            if (_heraldSettings.Enabled)
            {
                if (args is VoiceNotificationArgs voiceNotificationArgs)
                {
                    voiceNotificationArgs.VoiceName ??= DefaultVoiceName;
                    voiceNotificationArgs.VoiceStyle ??= DefaultVoiceStyle;
                    voiceNotificationArgs.VoiceVolume ??= DefaultVoiceVolume;
                    voiceNotificationArgs.VoiceRate ??= DefaultVoiceRate;
                    voiceNotificationArgs.VoicePitch ??= DefaultVoicePitch;
                    _heraldQueue.Enqueue(voiceNotificationArgs);
                }
                else
                {

                    _heraldQueue.Enqueue(new VoiceNotificationArgs {
                        Title = args.Title,
                        TitleSsml = args.TitleSsml,
                        Detail = args.Detail,
                        DetailSsml = args.DetailSsml,
                        Rendering = args.Rendering,
                        Suppression = args.Suppression,
                        VoiceName = DefaultVoiceName,
                        VoiceRate = DefaultVoiceRate,
                        VoiceStyle = DefaultVoiceStyle,
                        VoiceVolume = DefaultVoiceVolume
                    });
                }
            }
        }

        public async Task OnNotificationEventAsync(NotificationArgs args)
        {
            OnNotificationEvent(args);
            await Task.CompletedTask;
        }

        public async Task OnNotificationEventAsync(Guid id, NotificationArgs args)
        {
            if (_heraldSettings.Enabled)
            {
                if (args is VoiceNotificationArgs voiceNotificationArgs)
                {
                    voiceNotificationArgs.VoiceName ??= DefaultVoiceName;
                    voiceNotificationArgs.VoiceRate ??= DefaultVoiceRate;
                    voiceNotificationArgs.VoiceStyle ??= DefaultVoiceStyle;
                    voiceNotificationArgs.VoiceVolume ??= DefaultVoiceVolume;
                    voiceNotificationArgs.VoiceRate ??= DefaultVoiceRate;
                    voiceNotificationArgs.VoicePitch ??= DefaultVoicePitch;
                    _heraldQueue.UpdateNotification(id, voiceNotificationArgs);
                }
                else
                {

                    _heraldQueue.UpdateNotification(id, new VoiceNotificationArgs {
                        Title = args.Title,
                        TitleSsml = args.TitleSsml,
                        Detail = args.Detail,
                        DetailSsml = args.DetailSsml,
                        Rendering = args.Rendering,
                        Suppression = args.Suppression,
                        VoiceName = DefaultVoiceName,
                        VoiceRate = DefaultVoiceRate,
                        VoicePitch = DefaultVoicePitch,
                        VoiceStyle = DefaultVoiceStyle,
                        VoiceVolume = DefaultVoiceVolume
                    });
                }
            }
            await Task.CompletedTask;
        }

        public async Task OnNotificationCancelledAsync(Guid id)
        {
            _heraldQueue.CancelNotification(id);
            await Task.CompletedTask;
        }

    }
}
