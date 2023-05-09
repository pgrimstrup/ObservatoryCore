using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NAudio.Ogg.Opus
{
    internal static class PacketConvert
    {
        public static byte[] ShortsToBytes(this short[] srcArray)
        {
            return ShortsToBytes(srcArray, 0, srcArray.Length);
        }

        public static byte[] ShortsToBytes(this short[] srcArray, int srcOffset, int length)
        {
            byte[] processedValues = new byte[length * 2];
            ShortsToBytes(srcArray, srcOffset, processedValues, 0, length);
            return processedValues;
        }

        public static void ShortsToBytes(this short[] srcArray, int srcOffset,  byte[] destArray, int destOffset, int length)
        {
            for (int c = 0; c < length; c++)
            {
                destArray[(c * 2) + destOffset] = (byte)(srcArray[c + srcOffset] & 0xFF);
                destArray[(c * 2) + 1 + destOffset] = (byte)((srcArray[c + srcOffset] >> 8) & 0xFF);
            }
        }


        public static float[] ShortsToFloats(this short[] srcArray)
        {
            return ShortsToFloats(srcArray, 0, srcArray.Length);
        }

        public static float[] ShortsToFloats(this short[] srcArray, int srcOffset, int length)
        {
            float[] destArray = new float[length];
            ShortsToFloats(srcArray, srcOffset, destArray, 0, length);
            return destArray;
        }

        public static void ShortsToFloats(this short[] srcArray, int srcOffset, float[] destArray, int destOffset, int length)
        {
            for (int c = 0; c < length; c++)
            {
                destArray[c + destOffset] = srcArray[c + srcOffset] / (float)short.MaxValue;
            }
        }


        public static short[] BytesToShorts(this byte[] srcArray)
        {
            return BytesToShorts(srcArray, 0, srcArray.Length);
        }

        public static short[] BytesToShorts(this byte[] srcArray, int srcOffset, int length)
        {
            short[] destArray = new short[length / 2];
            BytesToShorts(srcArray, srcOffset, destArray, 0, length);
            return destArray;
        }

        public static void BytesToShorts(this byte[] srcArray, int srcOffset, short[] destArray, int destOffset, int length)
        {
            for (int c = 0; c < destArray.Length && c + srcOffset < srcArray.Length; c++)
            {
                int value = (srcArray[(c * 2) + srcOffset]) + (srcArray[(c * 2) + 1 + srcOffset] << 8);
                destArray[c + destOffset] = (short)value;
            }
        }
    }
}
