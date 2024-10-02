using AvaloniaToolbox.Core;
using AvaloniaToolbox.Core.Textures;
using FinalFantasy16;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FF16Converter
{
    public class TextureDataUtil
    {
        public const uint D3D12_TEXTURE_DATA_PITCH_ALIGNMENT = 256;

        private static uint Align(uint value, uint alignment)
        {
            return (value + (alignment - 1)) & ~(alignment - 1);
        }

        /// <summary>
        /// Gives the output mip dimension given the base size and level.
        /// </summary>
        public static int CalculateMipDimension(uint baseLevelDimension, int mipLevel)
        {
            return Math.Max((int)baseLevelDimension / (int)Math.Pow(2, mipLevel), 1);
        }

        /// <summary>
        /// Calculates the full texture surface data, mipmaps included, with padding added for row alignment calculations
        /// </summary>
        public static byte[] CalculateSurfacePadding(TexFile.Texture tex, byte[] data)
        {
            List<byte[]> mipmaps = new List<byte[]>();
            int ofs = 0;
            for (int mipLevel = 0; mipLevel < tex.MipCount; mipLevel++)
            {
                var mipWidth = CalculateMipDimension(tex.Width, mipLevel);
                var mipHeight = CalculateMipDimension(tex.Height, mipLevel);

                CalculateFormatSize(tex.Format, mipWidth, mipHeight, out int pitch, out int slice, out int alignedSlice);

                int aligned_size = alignedSlice;
                if (mipLevel == tex.MipCount - 1 && tex.MipCount > 1) //last mip
                    aligned_size = slice;

                byte[] buffer = new byte[aligned_size];
                data.AsSpan().Slice(ofs, (int)aligned_size).CopyTo(buffer);
                mipmaps.Add(buffer);

                ofs += (int)aligned_size;
            }

            return ByteUtil.CombineByteArray(mipmaps.ToArray());
        }

        /// <summary>
        /// Aligns the image surface based on the tex file alignment parameters.
        /// Returns a list of mipmaps.
        /// </summary>
        public static List<byte[]> GetAlignedData(TexFile.Texture tex, byte[] data)
        {
            var formatDecoder = TexFile.FormatList[(int)tex.Format];
            if (formatDecoder is Bcn)
                return GetCompressedAlignedData(tex, data);
            else //uncompressed
                return GetUncompressedAlignedData(tex, data);
        }


        /// <summary>
        /// Removes alignment of the image data, storing data in the expected row sizes for DDS usage.
        /// </summary>
        public static byte[] GetUnalignedData(TexFile.Texture tex, byte[] data)
        {
            var formatDecoder = TexFile.FormatList[(int)tex.Format];
            if (formatDecoder is Bcn)
                return GetCompressedUnalignedData(tex, data);
            else //uncompressed
                return GetUncompressedUnalignedData(tex, data);
        }

        private static byte[] GetCompressedUnalignedData(TexFile.Texture tex, byte[] data)
        {
            var formatDecoder = (Bcn)TexFile.FormatList[(int)tex.Format];
            // Write compressed data
            bool isSingle = formatDecoder.Format == BcnFormats.BC1 || formatDecoder.Format == BcnFormats.BC4;
            int blockSize = isSingle ? 8 : 16;
            int mipOffset = 0;

            var mem = new MemoryStream();
            using (var wr = new BinaryWriter(mem))
            {
                for (int mipLevel = 0; mipLevel < tex.MipCount; mipLevel++)
                {
                    var mipWidth = CalculateMipDimension(tex.Width, mipLevel);
                    var mipHeight = CalculateMipDimension(tex.Height, mipLevel);

                    //width/height for compressed block types
                    var blocksWidth = (mipWidth + 3) / 4;
                    var blocksHeight = (mipHeight + 3) / 4;

                    int alignedRowSize = (int)Align((uint)(blocksWidth * blockSize), D3D12_TEXTURE_DATA_PITCH_ALIGNMENT);
                    int originalRowSize = blocksWidth * blockSize;
                    int totalAlignedSize = alignedRowSize * blocksHeight;

                    Console.WriteLine($"{mipLevel} {mipWidth}x{mipHeight} {alignedRowSize} {blocksWidth * blocksHeight * blockSize}");

                    for (int row = 0; row < blocksHeight; row++)
                    {
                        int alignedRowStart = mipOffset + (row * alignedRowSize);

                        byte[] alignedRowData = data.Skip(alignedRowStart).Take(alignedRowSize).ToArray();
                        wr.Write(alignedRowData, 0, originalRowSize);
                    }
                    mipOffset += totalAlignedSize;
                }
            }
            return mem.ToArray();
        }

        private static byte[] GetUncompressedUnalignedData(TexFile.Texture tex, byte[] data)
        {
            var formatDecoder = (Rgba)TexFile.FormatList[(int)tex.Format];
            var bitsPerPixel = (uint)(formatDecoder.R + formatDecoder.G + formatDecoder.B + formatDecoder.A);
            int bytesPerPixel = (int)(bitsPerPixel + 7) / 8;

            int mipOffset = 0;
            var mem = new MemoryStream();
            using (var wr = new BinaryWriter(mem))
            {
                for (int i = 0; i < tex.MipCount; i++)
                {
                    var mipWidth = CalculateMipDimension(tex.Width, i);
                    var mipHeight = CalculateMipDimension(tex.Height, i);
                    int alignedRowSize = (int)Align((uint)(mipWidth * bytesPerPixel), D3D12_TEXTURE_DATA_PITCH_ALIGNMENT);
                    int originalRowSize = mipWidth * bytesPerPixel; // Expected data row size

                    for (int y = 0; y < mipHeight; y++)
                    {
                        int rowStart = y * alignedRowSize;
                        byte[] alignedRowData = data.Skip(mipOffset + rowStart).Take(alignedRowSize).ToArray();

                        // Write only the original width's worth of pixels (ignore the padding)
                        wr.Write(alignedRowData, 0, originalRowSize);
                    }

                    mipOffset += alignedRowSize * mipHeight;
                }
            }
            return mem.ToArray();
        }

        private static List<byte[]> GetCompressedAlignedData(TexFile.Texture tex, byte[] data)
        {
            var formatDecoder = (Bcn)TexFile.FormatList[(int)tex.Format];
            // Write compressed data
            bool isSingle = formatDecoder.Format == BcnFormats.BC1 || formatDecoder.Format == BcnFormats.BC4;
            int blockSize = isSingle ? 8 : 16;

            int mipOffset = 0;

            List<byte[]> mipmaps = new List<byte[]>();
            for (int i = 0; i < tex.MipCount; i++)
            {
                var mipWidth = CalculateMipDimension(tex.Width, i);
                var mipHeight = CalculateMipDimension(tex.Height, i);

                // Calculate aligned width and height in blocks
                int blockWidth = (mipWidth + 3) / 4;
                int blockHeight = (mipHeight + 3) / 4;

                int alignedRowSize = (int)Align((uint)(blockWidth * blockSize), D3D12_TEXTURE_DATA_PITCH_ALIGNMENT);
                int totalAlignedSize = alignedRowSize * blockHeight;

                byte[] alignedData = new byte[totalAlignedSize];

                for (int y = 0; y < mipHeight; y += 4)
                {
                    int originalRowSize = blockWidth * blockSize;
                    int rowStart = (y / 4) * originalRowSize;
                    byte[] originalRowData = data.Skip(mipOffset + rowStart).Take(originalRowSize).ToArray();

                    int alignedRowStart = (y / 4) * alignedRowSize;
                    // Transfer data to aligned row
                    Array.Copy(originalRowData, 0, alignedData, alignedRowStart, originalRowSize);
                }

                mipmaps.Add(alignedData);

                // Update the mip offset to move to the next mip level
                mipOffset += (mipWidth / 4) * blockSize * blockHeight;
            }
            return mipmaps;
        }

        private static List<byte[]> GetUncompressedAlignedData(TexFile.Texture tex, byte[] data)
        {
            var formatDecoder = (Rgba)TexFile.FormatList[(int)tex.Format];
            var bitsPerPixel = (uint)(formatDecoder.R + formatDecoder.G + formatDecoder.B + formatDecoder.A);
            int bytesPerPixel = (int)(bitsPerPixel + 7) / 8;

            List<byte[]> mipmaps = new List<byte[]>();
            int mipOffset = 0;
            for (int i = 0; i < tex.MipCount; i++)
            {
                var mipWidth = CalculateMipDimension(tex.Width, i);
                var mipHeight = CalculateMipDimension(tex.Height, i);

                int alignedRowSize = (int)Align((uint)(mipWidth * bytesPerPixel), D3D12_TEXTURE_DATA_PITCH_ALIGNMENT);

                int totalAlignedSize = alignedRowSize * mipHeight;
                byte[] alignedData = new byte[totalAlignedSize];

                for (int y = 0; y < mipHeight; y++)
                {
                    int originalRowSize = mipWidth * (int)bytesPerPixel;
                    int rowStart = y * originalRowSize;
                    byte[] originalRowData = data.Skip(mipOffset + rowStart).Take(originalRowSize).ToArray();

                    int alignedRowStart = y * alignedRowSize;
                    //Transfer data to aligned row
                    Array.Copy(originalRowData, 0, alignedData, alignedRowStart, originalRowSize);
                }
                mipmaps.Add(alignedData);

                mipOffset += mipWidth * mipHeight * bytesPerPixel;
            }
            return mipmaps;
        }

        static int GetBPP(TexFile.TextureFormat format)
        {
            switch (format)
            {
                case TexFile.TextureFormat.R32G32B32A32_FLOAT:
                    return 32;
                case TexFile.TextureFormat.R32G32B32_FLOAT:
                    return 24;
                case TexFile.TextureFormat.R32G32_FLOAT:
                    return 16;
                case TexFile.TextureFormat.R8G8_UNORM:
                case TexFile.TextureFormat.R8G8_SNORM:
                case TexFile.TextureFormat.R8G8_SINT:
                case TexFile.TextureFormat.R8G8_UINT:
                    return 2;
                case TexFile.TextureFormat.R8_UNORM:
                case TexFile.TextureFormat.R8_SNORM:
                case TexFile.TextureFormat.R8_SINT:
                case TexFile.TextureFormat.R8_UINT:
                    return 1;
                case TexFile.TextureFormat.R32_FLOAT:
                case TexFile.TextureFormat.R8G8B8A8_UNORM:
                    return 4;
                default:
                    return 4;
            }
        }

        static void CalculateFormatSize(TexFile.TextureFormat format, int width, int height,
             out int pitch, out int slice, out int alignedSlice)
        {
            switch (format)
            {
                case TexFile.TextureFormat.BC1_UNORM:
                case TexFile.TextureFormat.BC1_UNORM_SRGB:
                case TexFile.TextureFormat.BC4_UNORM:
                case TexFile.TextureFormat.BC4_SNORM:
                    {
                        int blockWidth = (width + 3) / 4;
                        int blockHeight = (height + 3) / 4;
                        //width * bytes per pixel (8)
                        pitch = blockWidth * 8;
                        slice = pitch * blockHeight;
                        alignedSlice = (int)Align((uint)pitch, D3D12_TEXTURE_DATA_PITCH_ALIGNMENT) * blockHeight;
                        break;
                    }
                case TexFile.TextureFormat.BC2_UNORM:
                case TexFile.TextureFormat.BC2_UNORM_SRGB:
                case TexFile.TextureFormat.BC3_UNORM:
                case TexFile.TextureFormat.BC3_UNORM_SRGB:
                case TexFile.TextureFormat.BC5_SNORM:
                case TexFile.TextureFormat.BC5_UNORM:
                case TexFile.TextureFormat.BC6H_UF16:
                case TexFile.TextureFormat.BC6H_SF16:
                case TexFile.TextureFormat.BC7_UNORM:
                case TexFile.TextureFormat.BC7_UNORM_SRGB:
                    {
                        int blockWidth = (width + 3) / 4;
                        int blockHeight = (height + 3) / 4;
                        //width * bytes per pixel (16)
                        pitch = blockWidth * 16;
                        slice = pitch * blockHeight;
                        alignedSlice = (int)Align((uint)pitch, D3D12_TEXTURE_DATA_PITCH_ALIGNMENT) * blockHeight;
                        break;
                    }
                default:
                    {
                        pitch = ((width) * GetBPP(format) + 7) / 8;
                        slice = pitch * height;
                        alignedSlice = (int)Align((uint)pitch, D3D12_TEXTURE_DATA_PITCH_ALIGNMENT) * height;
                        break;
                    }
            }
        }
    }
}
