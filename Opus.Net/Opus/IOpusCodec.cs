using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Concentus.Enums;

namespace NAudio.Ogg.Opus
{
    public interface IOpusCodec
    {
        void SetBitrate(int bitrate);
        void SetComplexity(int complexity);
        void SetPacketLoss(int loss);
        void SetApplication(OpusApplication application);
        void SetFrameSize(double frameSize);
        void SetVBRMode(bool vbr, bool constrained);
        byte[] Encode(AudioChunk input);
        AudioChunk Decode(byte[] input);
        CodecStatistics GetStatistics();
    }
}
