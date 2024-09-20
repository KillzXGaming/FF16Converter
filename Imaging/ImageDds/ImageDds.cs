using BCnEncoder.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AvaloniaToolbox.Core
{
    public class ImageDds
    {
        public static bool CanUse()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return false;

            return File.Exists("image_dds.dll");
        }

        public static unsafe byte[] Decode(byte[] input, uint width, uint height, ImageFormat format)
        {          
            //pad out the image data to the expected aligned width/height
            input = ImageToMultiple4(input, (int)width, (int)height);

            width  = (uint)(((int)width  + 3) & ~3);  // Align width to the next multiple of 4
            height = (uint)(((int)height + 3) & ~3); // Align height to the next multiple of 4

            IntPtr decodedDataPtr = IntPtr.Zero;
            ulong decodedLen;

            bool isSingle = format == ImageFormat.BC1RgbaUnorm || format == ImageFormat.BC4RUnorm;
            var bitsPerPixel = isSingle ? 8u : 16u;

            try
            {
                fixed (byte* src = &input[0])
                {
                    var result = ImageDdsNative.decode_bytes_x64(
                        width,
                        height,
                        src,
                        (ulong)input.Length,
                        (uint)format,
                        out decodedDataPtr,
                        out decodedLen
                    );

                    if ((SurfaceErrorCode)result != SurfaceErrorCode.Success)
                        throw new Exception($"[image_dds] {(SurfaceErrorCode)result}.");

                    byte[] decodedData = new byte[decodedLen];
                    Marshal.Copy(decodedDataPtr, decodedData, 0, (int)decodedLen);
                    return decodedData;
                }
            }
            finally
            {
                if (decodedDataPtr != IntPtr.Zero)
                    ImageDdsNative.free_decoded_data(decodedDataPtr);
            }
        }

        public static unsafe byte[] Encode(byte[] input, uint width, uint height,
            ImageFormat format, Quality quality = Quality.Normal)
        {
            //pad out the image data to the expected aligned width/height
            input = ImageToMultiple4(input, (int)width, (int)height);

            width  = (uint)(((int)width  + 3) & ~3);  // Align width to the next multiple of 4
            height = (uint)(((int)height + 3) & ~3); // Align height to the next multiple of 4

            IntPtr encodedDataPtr = IntPtr.Zero;
            ulong encodedLen;

            bool isSingle = format == ImageFormat.BC1RgbaUnorm || format == ImageFormat.BC4RUnorm;
            var bitsPerPixel = isSingle ? 8u : 16u;

            try
            {
                fixed (byte* src = &input[0])
                {
                    var result = ImageDdsNative.encode_bytes_x64(
                        width,
                        height,
                        src,
                        (ulong)input.Length,
                        (uint)format,
                        (uint)quality,
                        out encodedDataPtr,
                        out encodedLen
                    );

                    if ((SurfaceErrorCode)result != SurfaceErrorCode.Success)
                        throw new Exception($"[image_dds] {(SurfaceErrorCode)result}.");

                    byte[] encodedData = new byte[encodedLen];
                    Marshal.Copy(encodedDataPtr, encodedData, 0, (int)encodedLen);
                    return encodedData;
                }
            }
            finally
            {
                if (encodedDataPtr != IntPtr.Zero)
                    ImageDdsNative.free_encoded_data(encodedDataPtr);
            }
        }

        public static byte[] ImageToMultiple4(byte[] data, int width, int height)
        {
            // Calculate the new dimensions, rounding up to the nearest multiple of 4
            int newWidth = (width + 3) & ~3;  // Align width to the next multiple of 4
            int newHeight = (height + 3) & ~3; // Align height to the next multiple of 4

            // Check if padding is necessary
            if (newWidth != width || newHeight != height)
            {
                byte[] paddedRgbaBytes = new byte[newWidth * newHeight * 4];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int srcIndex = (y * width + x) * 4;
                        int destIndex = (y * newWidth + x) * 4;

                        // Copy RGBA values
                        paddedRgbaBytes[destIndex] = data[srcIndex];
                        paddedRgbaBytes[destIndex + 1] = data[srcIndex + 1];
                        paddedRgbaBytes[destIndex + 2] = data[srcIndex + 2];
                        paddedRgbaBytes[destIndex + 3] = data[srcIndex + 3];
                    }
                }
                return paddedRgbaBytes;
            }
            return data;
        }

        //Calculates mip size
        static uint mip_size(uint width, uint height, uint block_width, uint block_height, uint block_size_in_bytes)
        {
            uint v = div_round_up(width, block_width) * div_round_up(height, block_height);
            return v * block_size_in_bytes;
        }

        static uint div_round_up(uint x, uint n) {
            return ((x + n - 1) / n) * n;
        }

        public enum SurfaceErrorCode
        {
            Success = 0,
            ZeroSizedSurface = 1,
            PixelCountWouldOverflow = 2,
            NonIntegralDimensionsInBlocks = 3,
            NotEnoughData = 4,
            UnsupportedEncodeFormat = 5,
            InvalidMipmapCount = 6,
            MipmapDataOutOfBounds = 7,
            UnsupportedDdsFormat = 8,
            UnexpectedMipmapCount = 9,
        }

        public enum ImageFormat
        {
            R8Unorm,
            Rgba8Unorm,
            Rgba8UnormSrgb,
            Rgba16Float,
            Rgba32Float,
            Bgra8Unorm,
            Bgra8UnormSrgb,
            Bgra4Unorm,
            /// DXT1
            BC1RgbaUnorm,
            /// DXT1
            BC1RgbaUnormSrgb,
            /// DXT3
            BC2RgbaUnorm,
            /// DXT3
            BC2RgbaUnormSrgb,
            /// DXT5
            BC3RgbaUnorm,
            /// DXT5
            BC3RgbaUnormSrgb,
            /// RGTC1
            BC4RUnorm,
            /// RGTC1
            BC4RSnorm,
            /// RGTC2
            BC5RgUnorm,
            /// RGTC2
            BC5RgSnorm,
            /// BPTC (float)
            BC6hRgbUfloat,
            /// BPTC (float)
            BC6hRgbSfloat,
            /// BPTC (unorm)
            BC7RgbaUnorm,
            /// BPTC (unorm)
            BC7RgbaUnormSrgb,
        }

        public enum Quality
        {
            Fast,
            Normal,
            Slow,
        }
    }

    class ImageDdsNative
    {
        [DllImport("image_dds", EntryPoint = "decode_bytes_c")]
        public static unsafe extern uint decode_bytes_x64(
            uint width,
            uint height,
            byte* data,
            ulong data_len,
            uint format,
            out IntPtr out_decoded_data,
            out ulong out_decoded_len);

        [DllImport("image_dds", EntryPoint = "decode_floats_c")]
        public static unsafe extern uint decode_floats_x64(
            uint width,
            uint height,
            byte* data,
            ulong data_len,
            uint format,
            out IntPtr out_decoded_data,
            out ulong out_decoded_len);

        [DllImport("image_dds", EntryPoint = "decode_bytes_c")]
        public static unsafe extern uint deccode_bytes_x86(
            uint width,
            uint height,
            byte* data,
            uint data_len,
            uint format,
            out IntPtr out_decoded_data,
            out uint out_decoded_len);

        [DllImport("image_dds", EntryPoint = "decode_floats_c")]
        public static unsafe extern uint deccode_floats_x86(
            uint width,
            uint height,
            byte* data,
            uint data_len,
            uint format,
            out IntPtr out_decoded_data,
            out uint out_decoded_len);

        [DllImport("image_dds", EntryPoint = "encode_bytes_c")]
        public static unsafe extern uint encode_bytes_x64(
                    uint width,
                    uint height,
                    byte* data,
                    ulong data_len,
                    uint format,
                    uint quality,
                    out IntPtr out_encoded_data,
                    out ulong out_encoded_len);

        [DllImport("image_dds", EntryPoint = "encode_floats_c")]
        public static unsafe extern uint encode_floats_x64(
            uint width,
            uint height,
            float* data,
            ulong data_len,
            uint format,
            uint quality,
            out IntPtr out_encoded_data,
            out ulong out_encoded_len);

        [DllImport("image_dds", EntryPoint = "encode_bytes_c")]
        public static unsafe extern uint encode_bytes_x86(
            uint width,
            uint height,
            byte* data,
            uint data_len,
            uint format,
            uint quality,
            out IntPtr out_encoded_data,
            out uint out_encoded_len);

        [DllImport("image_dds", EntryPoint = "encode_floats_c")]
        public static unsafe extern uint encode_floats_x86(
            uint width,
            uint height,
            float* data,
            uint data_len,
            uint format,
            uint quality,
            out IntPtr out_encoded_data,
            out uint out_encoded_len);

        [DllImport("image_dds", EntryPoint = "free_encoded_data")]
        public static unsafe extern uint free_encoded_data(IntPtr encoded_data);


        [DllImport("image_dds", EntryPoint = "free_decoded_data")]
        public static unsafe extern uint free_decoded_data(IntPtr encoded_data);
    }
}
