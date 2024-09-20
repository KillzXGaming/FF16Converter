using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AvaloniaToolbox.Core.Textures
{
    public class Rgba : ImageEncoder
    {
        public bool IsSRGB = false;
        public bool IsSigned = false;
        public bool IsLuminance = false;

        public string ChannelOrder = "RGBA";

        public uint BitsPerPixel { get; }
        public uint BlockWidth { get; } = 1;
        public uint BlockHeight { get; } = 1;
        public uint BlockDepth { get; } = 1;

        public byte R;
        public byte G;
        public byte B;
        public byte A;

        public Rgba(byte r, byte g = 0, byte b = 0, byte a = 0)
        {
            R = r; 
            G = g;
            B = b; 
            A = a;
            //total number of bits divided by 8 (total bits per byte)
            BitsPerPixel = (uint)(r + g + b + a) / 8;
        }

        public uint CalculateSize(int width, int height)
        {
            var bitsPerPixel = (uint)(R + G + B + A);
            uint bytesPerPixel = (bitsPerPixel + 7) / 8;

            return (uint)(width * height * bytesPerPixel);
        }

        public override string ToString()
        {
            string text = "";
            if (R != 0) text += $"R";
            if (G != 0) text += $"G";
            if (B != 0) text += $"B";
            if (A != 0) text += $"A";

            string value = "";
            if (R != 0) value += $"{R}";
            if (G != 0) value += $"{G}";
            if (B != 0) value += $"{B}";
            if (A != 0) value += $"{A}";

            string type = "_Unorm";
            if (IsSigned)    type = "_Snorm";
            else if (IsSRGB) type = "_Srgb";

            return $"{text}{value}{type}";
        }

        public byte[] Decode(byte[] data, uint width, uint height)
        {
            byte[] output = new byte[width * height * 4];

            var bitsPerPixel = (uint)(R + G + B + A);
            uint bytesPerPixel = (bitsPerPixel + 7) / 8;

            int pixelIndex = 0; // Index for output buffer
            int dataIndex = 0; // Index for input data

            for (uint y = 0; y < height; y++)
            {
                for (uint x = 0; x < width; x++)
                {
                    // Read raw pixel data from input
                    uint pixelData = 0;
                    for (int i = 0; i < bytesPerPixel; i++)
                    {
                        pixelData |= (uint)(data[dataIndex++] << (i * 8));
                    }

                    // Extract R, G, B, A components
                    byte r = ExtractChannel(pixelData, 0, R);
                    byte g = ExtractChannel(pixelData, R, G);
                    byte b = ExtractChannel(pixelData, R + G, B);
                    byte a = ExtractChannel(pixelData, R + G + B, A);

                    output[pixelIndex + 0] = 255;
                    output[pixelIndex + 1] = 255;
                    output[pixelIndex + 2] = 255;
                    output[pixelIndex + 3] = 255;

                    for (int i = 0; i < ChannelOrder.Length; i++)
                    {
                        switch (ChannelOrder[i])
                        {
                            case 'R': output[pixelIndex + i] = r; break;
                            case 'G': output[pixelIndex + i] = g; break;
                            case 'B': output[pixelIndex + i] = b; break;
                            case 'A': output[pixelIndex + i] = a; break;
                            default:  output[pixelIndex + i] = 255; break;
                        }
                    }
                    pixelIndex += 4;
                }
            }

            return output;
        }

        private byte ExtractChannel(uint pixelData, int bitOffset, int bitCount)
        {
            if (bitCount == 0) return 255; //bit unused

            if (bitCount > 8) //float and precise types
            {
                float value = 1.0f;
                if (bitCount == 32)
                {
                    // Decode from 32-bit float
                    uint floatBits = (pixelData >> bitOffset) & 0xFFFFFFFF;
                    value = BitConverter.ToSingle(BitConverter.GetBytes(floatBits), 0);
                }
                return (byte)(value * 255);
            }
            else //byte types
            {
                // Create a mask for the channel
                uint mask = (uint)((1 << bitCount) - 1);
                uint channelData = (pixelData >> bitOffset) & mask;

                if (IsSigned)
                {
                    // Handle signed conversion
                    int maxPositiveValue = (1 << (bitCount - 1)) - 1; // Max positive value for signed int
                    int signedValue = (int)channelData;

                    // If value is negative in 2's complement, sign-extend it
                    if (signedValue > maxPositiveValue)
                        signedValue -= (1 << bitCount); // Convert to negative range

                    // Normalize signed value to the range [0, 255]
                    return (byte)((signedValue + maxPositiveValue) * 255 / (2 * maxPositiveValue));
                }
                else
                {
                    // Normalize the extracted bits to the range [0, 255]
                    return (byte)((channelData * 255) / mask);
                }
            }
        }

        public byte[] Encode(byte[] data, uint width, uint height)
        {
            int bitsPerPixel = R + G + B + A;
            int bytesPerPixel = (bitsPerPixel + 7) / 8;

            byte[] output = new byte[width * height * bytesPerPixel];

            int pixelIndex = 0;  //input idx
            int dataIndex = 0; //output idx
            for (uint y = 0; y < height; y++)
            {
                for (uint x = 0; x < width; x++)
                {
                    // Read RGBA channels based on ChannelOrder
                    byte r = GetChannel(data, pixelIndex, 0);
                    byte g = GetChannel(data, pixelIndex, 1);
                    byte b = GetChannel(data, pixelIndex, 2);
                    byte a = GetChannel(data, pixelIndex, 3);

                    if (IsLuminance)
                    {
                        byte luminance = L4.CalculateLuminance(data, pixelIndex, 255);
                        r = luminance;
                        g = luminance;
                        b = luminance;
                        a = 255;
                    }

                    uint pixelData = 0;
                    pixelData |= (uint)(PackChannel(r, 0, R));
                    pixelData |= (uint)(PackChannel(g, R, G));
                    pixelData |= (uint)(PackChannel(b, R + G, B));
                    pixelData |= (uint)(PackChannel(a, R + G + B, A));

                    // Write pixel data to the encoded output
                    for (int i = 0; i < bytesPerPixel; i++)
                        output[dataIndex++] = (byte)((pixelData >> (i * 8)) & 0xFF);

                    pixelIndex += 4;
                }
            }
            return output;
        }

        private uint PackChannel(byte value, int bitOffset, int bitCount)
        {
            if (bitCount == 0) return 0; // No bits for this channel

            if (IsSigned)
            {
                int maxPositiveValue = (1 << (bitCount - 1)) - 1; // Max positive value for signed int
                int signedValue = (int)(value * (2 * maxPositiveValue) / 255) - maxPositiveValue;

                // Ensure signedValue is in the correct range
                signedValue = Math.Max(-maxPositiveValue - 1, Math.Min(maxPositiveValue, signedValue));

                // Pack signed value into unsigned bits
                return (uint)((signedValue < 0 ? (1 << bitCount) + signedValue : signedValue) << bitOffset);
            }
            else
            {
                // Normalize the channel value from [0, 255] to the channel bit range
                uint normalizedValue = (uint)(value * ((1 << bitCount) - 1) / 255);
                return normalizedValue << bitOffset;
            }
        }

        private byte GetChannel(byte[] input, int pixelIndex, int channelIndex)
        {
            switch (ChannelOrder[channelIndex])
            {
                case 'R': return input[pixelIndex + 0];
                case 'G': return input[pixelIndex + 1];
                case 'B': return input[pixelIndex + 2];
                case 'A': return input[pixelIndex + 3];
                default: return 255;
            }
        }
    }
}
