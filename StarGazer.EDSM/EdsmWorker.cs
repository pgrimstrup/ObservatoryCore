using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using Observatory.Framework;
using Observatory.Framework.Files;
using Observatory.Framework.Files.Journal;
using Observatory.Framework.Interfaces;
using StarGazer.Framework.Interfaces;

namespace StarGazer.EDSM
{
    public class EdsmWorker : IStarGazerWorker
    {
        static string GetDiscardList = "https://www.edsm.net/api-journal-v1/discard";

        IStarGazerCore _core = null!;
        EdsmWorkerSettings _settings = new EdsmWorkerSettings();
        EdsmTransientState _state = new EdsmTransientState();
        ConcurrentQueue<EdsmPayload> _edsmQueue = new ConcurrentQueue<EdsmPayload>();

        public string Name => "EDSM";

        public string ShortName => "EDSM";

        public string Version => GetType().Assembly.GetName().Version?.ToString(3) ?? "";

        public PluginUI PluginUI => new PluginUI(PluginUI.UIType.None, null);

        public object Settings 
        {
            get => _settings;
            set => _settings = (value as EdsmWorkerSettings) ?? _settings;
        }


        public async Task JournalEventAsync<TJournal>(TJournal journal) where TJournal : JournalBase
        {
            _state.ProcessJournalEvent(journal);

            // Submit journal entry if it isn't on the ignore list
            if (_settings.EnableSubmissions && _settings.JournalDiscardList != null && _settings.JournalDiscardList.Length > 0)
            {
                if (_settings.JournalDiscardList.Contains(journal.Event))
                    return;

                var payload = _state.CreatePayload(journal);
                payload.CommanderName = _settings.CommanderName;
                payload.ApiKey = _settings.EdsmApiKey;

                _edsmQueue.Enqueue(payload);
                //var json = JsonSerializer.Serialize(payload);
            }

            await Task.CompletedTask;
        }

        public void Load(IObservatoryCore observatoryCore)
        {
            // Unused
        }

        public async Task LoadAsync(IStarGazerCore core)
        {
            // Get the most recent version of the ignore list
            _core = core;

            try
            {
                var ignores = await _core.HttpClient.GetFromJsonAsync<string[]>(GetDiscardList);
                if (ignores != null)
                    _settings.JournalDiscardList = ignores;
                else
                    _core.GetPluginErrorLogger(this).Invoke(null, "EDSM Journal Discard list is empty");
            }
            catch(Exception ex)
            {
                _core.GetPluginErrorLogger(this).Invoke(ex, $"Getting EDSM Journal Discard: {GetDiscardList}");
            }

            await Task.CompletedTask;
        }

        public Task LogMonitorStateChangedAsync(LogMonitorStateChangedEventArgs eventArgs)
        {
            // unused
            return Task.CompletedTask;
        }

        public Task StatusChangeAsync(Status status)
        {
            // unused
            return Task.CompletedTask;
        }

        public Task UnloadAsync()
        {
            // shutdown the worker thread
            return Task.CompletedTask;
        }
    }
}