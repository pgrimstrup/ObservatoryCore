using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Concentus.Oggfile;
using Concentus.Structs;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace NAudio.Ogg.Opus
{
    internal class OpusWaveProvider : ISampleProvider, IDisposable
    {
        static int[] AllowedSampleRates = new[] { 8000, 12000, 16000, 24000, 48000 };
        static int[] AllowedChannels = new[] { 1, 2 };
        static object _sync = new object();

        private bool _closeOnDispose;
        private bool _disposed;
        private long _lastPagePos;
        private WaveFormat _waveFormat;
        private Stream _stream;
        private OpusOggReadStream _streamReader;
        private OpusDecoder _decoder;
        private BasicBufferShort _sampleBuffer;

        public WaveFormat WaveFormat => _waveFormat;

        public OpusWaveProvider(Stream stream, bool closeOnDispose = true, int outputSampleRate = 48000, int outputChannels = 1)
        {
            if (!AllowedSampleRates.Contains(outputSampleRate))
                throw new ArgumentOutOfRangeException(nameof(outputSampleRate));
            if (!AllowedChannels.Contains(outputChannels))
                throw new ArgumentOutOfRangeException(nameof(outputChannels));

            _decoder = new OpusDecoder(outputSampleRate, outputChannels);
            _stream = stream;
            _streamReader = new OpusOggReadStream(_decoder, _stream);
            _closeOnDispose = closeOnDispose;
            _waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(outputSampleRate, outputChannels);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_closeOnDispose)
                {
                    _sampleBuffer.Clear();
                    (_streamReader as IDisposable)?.Dispose();
                    (_stream as IDisposable)?.Dispose();
                    (_decoder as IDisposable)?.Dispose();
                }
                _disposed = true;
            }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            if (_disposed)
                throw new ObjectDisposedException("Cannot decode OGG Opus when the file has already been closed");

            if (count == 0)
            {
                for (int i = offset; i < count; i++)
                    buffer[i + offset] = 0;
                return 0;
            }

            lock (_sync)
            {
                if (_sampleBuffer == null || _sampleBuffer.Capacity < count * 2)
                {
                    // Create new buffer and copy all samples from the old buffer
                    var newBuffer = new BasicBufferShort(count * 2);
                    if (_sampleBuffer != null && _sampleBuffer.Available > 0)
                        newBuffer.Write(_sampleBuffer.Read(_sampleBuffer.Available));
                    _sampleBuffer = newBuffer;
                }

                // We need to provide exactly count number of samples unless we are end of stream
                while (_sampleBuffer.Available < count && _sampleBuffer.Available < _sampleBuffer.Capacity && _streamReader.HasNextPacket)
                {
                    var pagePos = _streamReader.PagePosition;

                    short[] packet = _streamReader.DecodeNextPacket();
                    if (packet == null)
                    {
                        if (_streamReader.LastError != null)
                            Debug.WriteLine(_streamReader.LastError);

                        break;
                    }

                    _sampleBuffer.Write(packet);
                    _lastPagePos = pagePos;
                }

                int samplesReturned = 0;
                if (count > 0)
                {
                    short[] samples = _sampleBuffer.Read(count);
                    samples.ShortsToFloats(0, buffer, offset, samples.Length);
                    samplesReturned = samples.Length;
                }

                // Zero out the remainder of the buffer to produce silence
                for (int i = samplesReturned; i < count; i++)
                    buffer[i + offset] = 0;

                return samplesReturned;
            }
        }
    }
}
