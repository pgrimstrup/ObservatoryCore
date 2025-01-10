using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using NAudio.Ogg.Opus;
using NAudio.Vorbis;
using NAudio.Wave;

namespace NAudio.Ogg
{
    public class OggFileReader : IWaveProvider, IDisposable
    {
        ISampleProvider _sampleProvider;
        bool _disposed;
        float[] _floatBuffer = new float[0];

        public OggFileReader(string fileName)
            : this(File.OpenRead(fileName), true)
        {
        }

        public OggFileReader(Stream sourceStream, bool closeOnDispose = false)
        {
            // To maintain consistent semantics with v1.1, we don't expose the events and auto-advance / stream removal features of VorbisSampleProvider.
            // If one wishes to use those features, they should really use VorbisSampleProvider directly...
            if (sourceStream.CanSeek)
            {
                // Check Ogg header for stream encoding
                var bytes = new byte[8];
                sourceStream.Seek(0x1C, SeekOrigin.Begin);
                sourceStream.ReadExactly(bytes, 0, 8);
                sourceStream.Seek(0, SeekOrigin.Begin);

                if (Encoding.ASCII.GetString(bytes) == "OpusHead")
                {
                    // Appears to be an Ogg Opus
                    _sampleProvider = new OpusWaveProvider(sourceStream, closeOnDispose);
                    WaveFormat = _sampleProvider.WaveFormat;
                }
                else if (Encoding.ASCII.GetString(bytes, 1, 6) == "vorbis")
                {
                    // Appears to be an Ogg Vorbis
                    _sampleProvider = new VorbisSampleProvider(sourceStream, closeOnDispose);
                    WaveFormat = _sampleProvider.WaveFormat;
                }
            }

            if (_sampleProvider == null)
            {
                _sampleProvider = new VorbisSampleProvider(sourceStream, closeOnDispose);
                WaveFormat = _sampleProvider.WaveFormat;
            }
        }

        public WaveFormat WaveFormat { get; private set; } = null!;

        public void Dispose()
        {
            if(!_disposed)
            {
                (_sampleProvider as IDisposable)?.Dispose();
                _disposed = true;
            }
        }


        public int Read(byte[] buffer, int offset, int count)
        {
            if (count == 0 || _sampleProvider == null)
                return 0;

            int floatCount = count / sizeof(float);
            floatCount -= floatCount % WaveFormat.Channels;

            if (_floatBuffer.Length < floatCount)
                _floatBuffer = new float[floatCount];

            var bytesRead = _sampleProvider.Read(_floatBuffer, 0, floatCount) * sizeof(float);
            if (bytesRead == 0)
                return 0;

            Buffer.BlockCopy(_floatBuffer, 0, buffer, offset, bytesRead);
            return bytesRead;
        }
    }
}