using System.IO;
using System.Windows;
using Concentus.Oggfile;
using Concentus.Structs;
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
                var ext = Path.GetExtension(filename).ToLower();
                if (ext.StartsWith(".ogg") || ext.StartsWith(".opus"))
                    _audioFile = new OggFileReader(filename);
                else if (ext.StartsWith(".mp3"))
                    _audioFile = new MediaFoundationReader(filename);
                else if (ext.StartsWith(".wav"))
                    _audioFile = new WaveFileReader(filename);
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

        public string ConvertWavToOpus(string sourceFile)
        {
            var targetFile = Path.ChangeExtension(sourceFile, ".opus");
            using (var wavefile = new WaveFileReader(sourceFile))
            using (var opusfile = File.OpenWrite(targetFile))
            {
                OpusEncoder encoder = new OpusEncoder(wavefile.WaveFormat.SampleRate, wavefile.WaveFormat.Channels, Concentus.Enums.OpusApplication.OPUS_APPLICATION_AUDIO);
                OpusOggWriteStream writer = new OpusOggWriteStream(encoder, opusfile);

                var frame = wavefile.ReadNextSampleFrame();
                while (frame != null)
                {
                    writer.WriteSamples(frame, 0, frame.Length);
                    frame = wavefile.ReadNextSampleFrame();
                }

                writer.Finish();
            }
            return targetFile;
        }
    }
}
