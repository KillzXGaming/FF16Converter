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
        /// <summary>
        /// Gives the output mip dimension given the base size and level.
        /// </summary>
        public static int CalculateMipDimension(uint baseLevelDimension, int mipLevel) {
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
                var w = tex.GetAlignedWidth(mipLevel);
                var h = tex.GetAlignedHeight(mipLevel);

                var aligned_size = TexFile.FormatList[(int)tex.Format].CalculateSize((int)w, (int)h);
                if (ofs + aligned_size > data.Length)
                    aligned_size = (uint)(data.Length - ofs);

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
                    var blocksWide = (mipWidth + 3) / 4;
                    var blocksHigh = (mipHeight + 3) / 4;

                    int alignedWidth = (int)(tex.GetAlignedWidth(mipLevel) + 3) / 4;
                    int alignedRowSize = alignedWidth * blockSize;
                    int originalRowSize = blocksWide * blockSize;

                    Console.WriteLine($"{mipLevel} {mipWidth}x{mipHeight} {alignedRowSize} {alignedWidth * blocksHigh * blockSize}");

                    for (int row = 0; row < blocksHigh; row++)
                    {
                        int alignedRowStart = mipOffset + (row * alignedRowSize);

                        byte[] alignedRowData = data.Skip(alignedRowStart).Take(alignedRowSize).ToArray();
                        wr.Write(alignedRowData, 0, originalRowSize);
                    }
                    mipOffset += alignedWidth * blocksHigh * blockSize;
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
                    int alignedWidth = (int)tex.GetAlignedWidth(i);

                    for (int y = 0; y < mipHeight; y++)
                    {
                        int alignedRowSize = alignedWidth * bytesPerPixel;  // Aligned row size
                        int originalRowSize = mipWidth * bytesPerPixel; // Expected data row size

                        int rowStart = y * alignedRowSize;
                        byte[] alignedRowData = data.Skip(mipOffset + rowStart).Take(alignedRowSize).ToArray();

                        // Write only the original width's worth of pixels (ignore the padding)
                        wr.Write(alignedRowData, 0, originalRowSize);
                    }

                    mipOffset += alignedWidth * mipHeight * bytesPerPixel;
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
                int alignedWidth = (int)tex.GetAlignedWidth(i);
                int blockHeight = (mipHeight + 3) / 4;

                if (i == tex.MipCount - 1 && i > 0)
                    alignedWidth = Math.Max(mipWidth, 4);

                int alignedRowSize = (alignedWidth / 4) * blockSize;
                int totalAlignedSize = alignedRowSize * blockHeight;
                byte[] alignedData = new byte[totalAlignedSize];

                for (int y = 0; y < mipHeight; y += 4)
                {
                    int originalRowSize = (mipWidth / 4) * blockSize;
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

                var alignedWidth = (int)tex.GetAlignedWidth(i);
                int alignedRowSize = alignedWidth * bytesPerPixel;

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
    }
}
