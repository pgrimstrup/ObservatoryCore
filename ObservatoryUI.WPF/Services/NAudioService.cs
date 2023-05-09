using System.IO;
using Microsoft.Extensions.Logging;
using NAudio.Ogg;
using NAudio.Wave;
using Observatory.Framework.Interfaces;

namespace ObservatoryUI.WPF.Services
{
    internal class NAudioService : IAudioPlayback, IDisposable
    {
        readonly IMainFormDispatcher _dispatcher;
        readonly ILogger _logger;

        int _volume;
        WaveOut? _outputDevice;
        IWaveProvider? _audioFile;

        public bool IsPlaying => _outputDevice != null && _outputDevice.PlaybackState == PlaybackState.Playing;

        public string? FileName { get; private set; }

        public NAudioService(IMainFormDispatcher dispatcher, ILogger<NAudioService> logger)
        {
            _dispatcher = dispatcher;
            _logger = logger;
        }

        public void Dispose()
        {
            _dispatcher.Run(() => {
                Player_PlaybackStopped(this, new StoppedEventArgs());
            });
        }

        private void Player_PlaybackStopped(object? sender, StoppedEventArgs e)
        {
            (_audioFile as IDisposable)?.Dispose();
            _audioFile = null;

            if (_outputDevice != null)
            {
                _outputDevice.PlaybackStopped -= Player_PlaybackStopped;
                _outputDevice.Dispose();
                _outputDevice = null;
            }

            if (e.Exception != null)
                _logger.LogError(e.Exception, $"While playing file {FileName}");

            FileName = null;
        }

        public Task PlayAsync(string filename)
        {
            _dispatcher.Run(() => {
                if (Path.GetExtension(filename).ToLower().StartsWith(".ogg"))
                    _audioFile = new OggFileReader(filename);
                else
                    _audioFile = new AudioFileReader(filename);

                _outputDevice = new WaveOut();
                _outputDevice.PlaybackStopped += Player_PlaybackStopped;
                _outputDevice.Init(_audioFile);
                _outputDevice.Volume = _volume / 100.0f;
                _outputDevice.Play();
            });

            FileName = filename;
            return Task.CompletedTask;
        }

        public Task SetVolume(int volume)
        {
            _volume = volume;
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            _dispatcher.Run(() => {
                _outputDevice?.Stop();
            });
            return Task.CompletedTask;
        }
    }
}
