using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace NAudio.Ogg.Opus
{
    public class OpusSampleProvider_Option1 : ISampleProvider, IDisposable
    {
        private Stream _audioStream;
        private bool _closeOnDispose;
        private bool _disposed;
        private AudioChunk? _nextChunk = null;
        private MixingSampleProvider _mixer;
        private ConcurrentQueue<AudioChunk> _inputChunks = new ConcurrentQueue<AudioChunk>();

        private int _inCursor = 0;

        public WaveFormat WaveFormat { get; }

        public OpusSampleProvider_Option1(Stream audioStream, bool closeOnDispose, int sampleRate = 48000, int channels = 1)
        {
            _audioStream = audioStream;
            _closeOnDispose = closeOnDispose;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_closeOnDispose)
                {
                    _audioStream.Dispose();
                }
                _disposed = true;
            }
        }

        public int BufferSizeMs()
        {
            double length = 0;
            foreach (var chunk in _inputChunks.ToArray())
            {
                length += chunk.Length.TotalMilliseconds;
            }
            return (int)length;
        }

        public void QueueChunk(AudioChunk chunk)
        {
            short[] resampledData = ShortResampler.Resample(chunk.Data, chunk.SampleRate, WaveFormat.SampleRate);
            AudioChunk resampledChunk = new AudioChunk(resampledData, 44100);
            _inputChunks.Enqueue(resampledChunk);
        }

        public bool Finished
        {
            get
            {
                return false;
            }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            if (_nextChunk == null)
            {
                if(_inputChunks.TryDequeue(out var chunk))
                {
                    _nextChunk = chunk;
                }
                else
                {
                    // Serious buffer underrun. In this case, just return silence instead of stuttering
                    for (int c = 0; c < count && c + offset < buffer.Length; c++)
                    {
                        buffer[c + offset] = 0.0f;
                    }
                    return count;
                }
            }

            int samplesWritten = 0;
            short[] returnVal = new short[count];

            while (samplesWritten < count && _nextChunk != null)
            {
                int remainingInThisChunk = _nextChunk.DataLength - _inCursor;
                int remainingToWrite = (count - samplesWritten);
                int chunkSize = Math.Min(remainingInThisChunk, remainingToWrite);
                Array.Copy(_nextChunk.Data, _inCursor, returnVal, samplesWritten, chunkSize);
                _inCursor += chunkSize;
                samplesWritten += chunkSize;

                if (_inCursor >= _nextChunk.DataLength)
                {
                    _inCursor = 0;

                    if (_inputChunks.TryDequeue(out var chunk))
                    {
                        _nextChunk = chunk;
                    }
                    else
                    {
                        _nextChunk = null;
                        for (int c = 0; c < count && c + offset < buffer.Length; c++)
                        {
                            buffer[c + offset] = 0.0f;
                        }
                        return count;
                    }
                }
            }

            for (int c = 0; c < samplesWritten && c + offset < buffer.Length; c++)
            {
                buffer[c + offset] = ((float)returnVal[c]) / ((float)short.MaxValue);
            }

            return samplesWritten;
        }

    }
}
