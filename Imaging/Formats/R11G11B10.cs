using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AvaloniaToolbox.Core.Textures
{
    //Based on https://github.com/FlaxEngine/FlaxEngine/blob/49eeb7bf9a3dcb4bcf373fc5281d61ffcfab7b0a/Source/Engine/Core/Math/FloatR11G11B10.cs#L20
    public class R11G11B10 : ImageEncoder
    {
        public uint BitsPerPixel => 32;

        public override string ToString() => "R11G11B10_FLOAT";

        public byte[] Decode(byte[] data, uint width, uint height)
        {
            byte[] output = new byte[width * height * 4];

            var bitsPerPixel = (uint)(11 + 11 + 10);
            uint bytesPerPixel = (bitsPerPixel + 7) / 8;

            int dataIndex = 0;
            int pixelIndex = 0;
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

                    var rgb = Unpack(pixelData);

                    output[pixelIndex + 0] = (byte)(Math.Clamp(rgb.X, 0, 1) * 255);
                    output[pixelIndex + 1] = (byte)(Math.Clamp(rgb.Y, 0, 1) * 255);
                    output[pixelIndex + 2] = (byte)(Math.Clamp(rgb.Z, 0, 1) * 255);
                    output[pixelIndex + 3] = 255;

                    pixelIndex += 4;
                }
            }
            return output;
        }

        public byte[] Encode(byte[] data, uint width, uint height)
        {
            byte[] output = new byte[width * height * 4];

            var bitsPerPixel = (uint)(11 + 11 + 10);
            uint bytesPerPixel = (bitsPerPixel + 7) / 8;

            int dataIndex = 0;
            int pixelIndex = 0;
            for (uint y = 0; y < height; y++)
            {
                for (uint x = 0; x < width; x++)
                {
                    float r = data[pixelIndex + 0] / 255f;
                    float g = data[pixelIndex + 1] / 255f;
                    float b = data[pixelIndex + 2] / 255f;

                    var rgb = Pack(r, g, b);

                    output[dataIndex + 0] = (byte)(rgb & 0xFF);        
                    output[dataIndex + 1] = (byte)((rgb >> 8) & 0xFF); 
                    output[dataIndex + 2] = (byte)((rgb >> 16) & 0xFF); 
                    output[dataIndex + 3] = (byte)((rgb >> 24) & 0xFF);

                    pixelIndex += 4;
                    dataIndex += (int)bytesPerPixel;
                }
            }
            return output;
        }

        public uint CalculateSize(int width, int height)
        {
            return (uint)(width * height * 4u);
        }

        public unsafe Vector3 Unpack(uint rawValue)
        {
            int zeroExponent = -112;

            Packed packed = new Packed(rawValue);
            uint* result = stackalloc uint[4];
            uint exponent;

            // X Channel (6-bit mantissa)
            var mantissa = packed.xm;

            // INF or NAN
            if (packed.xe == 0x1f)
            {
                result[0] = 0x7f800000 | (packed.xm << 17);
            }
            else
            {
                // The value is normalized
                if (packed.xe != 0)
                {
                    exponent = packed.xe;
                }
                else if (mantissa != 0)
                {
                    // The value is denormalized
                    // Normalize the value in the resulting float
                    exponent = 1;

                    do
                    {
                        exponent--;
                        mantissa <<= 1;
                    } while ((mantissa & 0x40) == 0);

                    mantissa &= 0x3F;
                }
                else
                {
                    // The value is zero
                    exponent = *(uint*)&zeroExponent;
                }

                result[0] = ((exponent + 112) << 23) | (mantissa << 17);
            }

            // Y Channel (6-bit mantissa)
            mantissa = packed.ym;

            if (packed.ye == 0x1f)
            {
                // INF or NAN
                result[1] = 0x7f800000 | (packed.ym << 17);
            }
            else
            {
                if (packed.ye != 0)
                {
                    // The value is normalized
                    exponent = packed.ye;
                }
                else if (mantissa != 0)
                {
                    // The value is denormalized
                    // Normalize the value in the resulting float
                    exponent = 1;

                    do
                    {
                        exponent--;
                        mantissa <<= 1;
                    } while ((mantissa & 0x40) == 0);

                    mantissa &= 0x3F;
                }
                else
                {
                    // The value is zero
                    exponent = *(uint*)&zeroExponent;
                }

                result[1] = ((exponent + 112) << 23) | (mantissa << 17);
            }

            // Z Channel (5-bit mantissa)
            mantissa = packed.zm;

            if (packed.ze == 0x1f)
            {
                // INF or NAN
                result[2] = 0x7f800000 | (packed.zm << 17);
            }
            else
            {
                if (packed.ze != 0)
                {
                    // The value is normalized
                    exponent = packed.ze;
                }
                else if (mantissa != 0) // The value is denormalized
                {
                    // Normalize the value in the resulting float
                    exponent = 1;

                    do
                    {
                        exponent--;
                        mantissa <<= 1;
                    } while ((mantissa & 0x20) == 0);

                    mantissa &= 0x1F;
                }
                else
                {
                    // The value is zero
                    exponent = *(uint*)&zeroExponent;
                }

                result[2] = ((exponent + 112) << 23) | (mantissa << 18);
            }
            float* resultAsFloat = (float*)result;
            return new Vector3(resultAsFloat[0], resultAsFloat[1], resultAsFloat[2]);
        }

        private static unsafe uint Pack(float x, float y, float z)
        {
            uint* input = stackalloc uint[4];
            input[0] = *(uint*)&x;
            input[1] = *(uint*)&y;
            input[2] = *(uint*)&z;
            input[3] = 0;

            uint* output = stackalloc uint[3];

            // X & Y Channels (5-bit exponent, 6-bit mantissa)
            for (uint j = 0; j < 2; j++)
            {
                bool sign = (input[j] & 0x80000000) != 0;
                uint I = input[j] & 0x7FFFFFFF;

                if ((I & 0x7F800000) == 0x7F800000)
                {
                    // INF or NAN
                    output[j] = 0x7c0;
                    if ((I & 0x7FFFFF) != 0)
                    {
                        output[j] = 0x7c0 | (((I >> 17) | (I >> 11) | (I >> 6) | (I)) & 0x3f);
                    }
                    else if (sign)
                    {
                        // -INF is clamped to 0 since 3PK is positive only
                        output[j] = 0;
                    }
                }
                else if (sign)
                {
                    // 3PK is positive only, so clamp to zero
                    output[j] = 0;
                }
                else if (I > 0x477E0000U)
                {
                    // The number is too large to be represented as a float11, set to max
                    output[j] = 0x7BF;
                }
                else
                {
                    if (I < 0x38800000U)
                    {
                        // The number is too small to be represented as a normalized float11
                        // Convert it to a denormalized value.
                        uint shift = 113U - (I >> 23);
                        I = (0x800000U | (I & 0x7FFFFFU)) >> (int)shift;
                    }
                    else
                    {
                        // Rebias the exponent to represent the value as a normalized float11
                        I += 0xC8000000U;
                    }

                    output[j] = ((I + 0xFFFFU + ((I >> 17) & 1U)) >> 17) & 0x7ffU;
                }
            }

            // Z Channel (5-bit exponent, 5-bit mantissa)
            {
                bool sign = (input[2] & 0x80000000) != 0;
                uint I = input[2] & 0x7FFFFFFF;

                if ((I & 0x7F800000) == 0x7F800000)
                {
                    // INF or NAN
                    output[2] = 0x3e0;
                    if ((I & 0x7FFFFF) != 0)
                    {
                        output[2] = 0x3e0 | (((I >> 18) | (I >> 13) | (I >> 3) | (I)) & 0x1f);
                    }
                    else if (sign)
                    {
                        // -INF is clamped to 0 since 3PK is positive only
                        output[2] = 0;
                    }
                }
                else if (sign)
                {
                    // 3PK is positive only, so clamp to zero
                    output[2] = 0;
                }
                else if (I > 0x477C0000U)
                {
                    // The number is too large to be represented as a float10, set to max
                    output[2] = 0x3df;
                }
                else
                {
                    if (I < 0x38800000U)
                    {
                        // The number is too small to be represented as a normalized float10
                        // Convert it to a denormalized value.
                        uint shift = 113U - (I >> 23);
                        I = (0x800000U | (I & 0x7FFFFFU)) >> (int)shift;
                    }
                    else
                    {
                        // Rebias the exponent to represent the value as a normalized float10
                        I += 0xC8000000U;
                    }

                    output[2] = ((I + 0x1FFFFU + ((I >> 18) & 1U)) >> 18) & 0x3ffU;
                }
            }

            // Pack result
            return (output[0] & 0x7ff) | ((output[1] & 0x7ff) << 11) | ((output[2] & 0x3ff) << 22);
        }

        private struct Packed
        {
            public uint v;

            public uint xm; // x-mantissa
            public uint xe; // x-exponent
            public uint ym; // y-mantissa
            public uint ye; // y-exponent
            public uint zm; // z-mantissa
            public uint ze; // z-exponent

            public Packed(uint value)
            {
                v = value;
                xm = v & 0b111111;
                xe = (v >> 6) & 0b011111;
                ym = (v >> 11) & 0b111111;
                ye = (v >> 17) & 0b011111;
                zm = (v >> 22) & 0b011111;
                ze = (v >> 27) & 0b011111;
            }
        }
    }
}
