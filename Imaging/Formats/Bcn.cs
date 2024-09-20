using BCnEncoder.Decoder;
using BCnEncoder.Encoder;
using BCnEncoder.Shared;
using SixLabors.ImageSharp.Formats.Bmp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AvaloniaToolbox.Core.Textures
{
    public class Bcn : ImageEncoder, ImageBlockFormat
    {
        public static int QualityLevel = 1;

        public BcnFormats Format { get; } = BcnFormats.BC1;
        public uint BitsPerPixel { get; }
        public uint BlockWidth { get; } = 4;
        public uint BlockHeight { get; } = 4;
        public uint BlockDepth { get; } = 1;

        public bool IsSigned = false;
        public bool IsSRGB = false;
        public bool IsAlpha = false;

        public bool UseBc1Alpha = true;

        public Bcn(BcnFormats format, bool srgb_or_snorm = false)
        {
            Format = format;

            if (format == BcnFormats.BC5 || format == BcnFormats.BC4 || format == BcnFormats.BC6)
                IsSigned = srgb_or_snorm;
            else
                IsSRGB = srgb_or_snorm;

            bool isSingle = format == BcnFormats.BC1 || format == BcnFormats.BC4;
            BitsPerPixel = isSingle ? 8u : 16u;
        }

        public override string ToString()
        {
            switch (this.Format)
            {
                //Snorm formats
                case BcnFormats.BC4:
                case BcnFormats.BC5:
                    return $"{this.Format}{(this.IsSigned ? "_SNORM" : "_UNORM")}";
                //Float formats
                case BcnFormats.BC6:
                    return $"{this.Format}{(this.IsSigned ? "_FLOAT" : "_UFLOAT")}";
                //Srgb formats
                default:
                    if (this.Format == BcnFormats.BC1 && this.UseBc1Alpha)
                        return $"{this.Format}Alpha{(this.IsSRGB ? "_SRGB" : "_UNORM")}";

                    return $"{this.Format}{(this.IsSRGB ? "_SRGB" : "_UNORM")}";
            }
        }

        public uint CalculateSize(int width, int height)
        {
            int blocksWidth  = (width + 3) / 4;
            int blocksHeight = (height + 3) / 4; 

            int numBlocks = blocksWidth * blocksHeight;
            return (uint)(numBlocks * BitsPerPixel);  
        }

        public byte[] Decode(byte[] input, uint width, uint height)
        {
            BcDecoder decoder = new BcDecoder();
            decoder.InputOptions.DdsBc1ExpectAlpha = this.UseBc1Alpha;
            decoder.OutputOptions.Bc4Component = ColorComponent.Luminance;

            CompressionFormat compressionFormat = CompressionFormat.Bc1;

            switch (Format)
            {
                case BcnFormats.BC1: compressionFormat = CompressionFormat.Bc1WithAlpha; break;
                case BcnFormats.BC2: compressionFormat = CompressionFormat.Bc2; break;
                case BcnFormats.BC3: compressionFormat = CompressionFormat.Bc3; break;
                case BcnFormats.BC4: compressionFormat = CompressionFormat.Bc4; break;
                case BcnFormats.BC5: compressionFormat = CompressionFormat.Bc5; break;
                case BcnFormats.BC6:
                    compressionFormat = IsSigned ? CompressionFormat.Bc6S : CompressionFormat.Bc6U;
                    break;
                case BcnFormats.BC7: compressionFormat = CompressionFormat.Bc7; break;
            }

            var colors = decoder.DecodeRaw(new MemoryStream(input), (int)width, (int)height, compressionFormat);

            byte[] output = new byte[colors.Length * 4];
            for (int i = 0; i < colors.Length; i++)
            {
                int offset = i * 4;

                output[offset + 0] = colors[i].r;
                output[offset + 1] = colors[i].g;
                output[offset + 2] = colors[i].b;
                output[offset + 3] = colors[i].a;
            }
            return output;
        }

        public byte[] Encode(byte[] input, uint width, uint height)
        {
            //Use better, faster encoder if possible
            if (ImageDds.CanUse())
                return EncodeWithImageDds(input, width, height);

            var encoder = new BcEncoder();
            encoder.OutputOptions.Format = CompressionFormat.Bc1;
            encoder.OutputOptions.Quality = BCnEncoder.Encoder.CompressionQuality.Fast;

            var quality = (ImageDds.Quality)QualityLevel;

            //BC5 RRRG
            if (Format == BcnFormats.BC5 && IsAlpha)
            {
                encoder.InputOptions.Bc5Component1 = ColorComponent.Luminance;
                encoder.InputOptions.Bc5Component2 = ColorComponent.A;
            } //BC4 AAAA
            else if (Format == BcnFormats.BC4 && IsAlpha)
            {
                encoder.InputOptions.Bc4Component = ColorComponent.A;
            }

            switch (Format)
            {
                case BcnFormats.BC1: encoder.OutputOptions.Format = CompressionFormat.Bc1; break;
                case BcnFormats.BC2: encoder.OutputOptions.Format = CompressionFormat.Bc2; break;
                case BcnFormats.BC3: encoder.OutputOptions.Format = CompressionFormat.Bc3; break;
                case BcnFormats.BC4: encoder.OutputOptions.Format = CompressionFormat.Bc4; break;
                case BcnFormats.BC5: encoder.OutputOptions.Format = CompressionFormat.Bc5; break;
                case BcnFormats.BC6: encoder.OutputOptions.Format = CompressionFormat.Bc6S; break;
                case BcnFormats.BC7: encoder.OutputOptions.Format = CompressionFormat.Bc7; break;
            }

            var colors = encoder.EncodeToRawBytes(input, (int)width, (int)height, BCnEncoder.Encoder.PixelFormat.Rgba32);
            return colors[0];
        }

        //Preferred encoder that is fast, but todo more platform binaries need to be made
        public byte[] EncodeWithImageDds(byte[] input, uint width, uint height)
        {
            if (Format == BcnFormats.BC2)
            {
                //only format not supported yet
                return new byte[CalculateSize((int)width, (int)height)];
            }

            ImageDds.ImageFormat format = ImageDds.ImageFormat.BC1RgbaUnorm;

            switch (Format)
            {
                case BcnFormats.BC1: format = ImageDds.ImageFormat.BC1RgbaUnorm; break;
                case BcnFormats.BC2: format = ImageDds.ImageFormat.BC2RgbaUnorm; break;
                case BcnFormats.BC3: format = ImageDds.ImageFormat.BC3RgbaUnorm; break;
                case BcnFormats.BC4: format = ImageDds.ImageFormat.BC4RUnorm; break;
                case BcnFormats.BC5: format = ImageDds.ImageFormat.BC5RgUnorm; break;
                case BcnFormats.BC6: format = ImageDds.ImageFormat.BC6hRgbUfloat; break;
                case BcnFormats.BC7: format = ImageDds.ImageFormat.BC7RgbaUnorm; break;
            }
            if (IsSigned)
            {
                switch (Format)
                {
                    case BcnFormats.BC4: format = ImageDds.ImageFormat.BC4RSnorm; break;
                    case BcnFormats.BC5: format = ImageDds.ImageFormat.BC5RgSnorm; break;
                    case BcnFormats.BC6: format = ImageDds.ImageFormat.BC6hRgbSfloat; break;
                }
            }
            return ImageDds.Encode(input, width, height, format, (ImageDds.Quality)QualityLevel);
        }

        public DDS.DXGI_FORMAT GetDxgiFormat() //for DDS
        {
            switch (this.Format)
            {
                case BcnFormats.BC1: return DDS.DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM;
                case BcnFormats.BC2: return DDS.DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM;
                case BcnFormats.BC3: return DDS.DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM;
                case BcnFormats.BC4: return DDS.DXGI_FORMAT.DXGI_FORMAT_BC4_UNORM;
                case BcnFormats.BC5: return IsSigned ? DDS.DXGI_FORMAT.DXGI_FORMAT_BC5_SNORM :
                         DDS.DXGI_FORMAT.DXGI_FORMAT_BC5_UNORM;
                case BcnFormats.BC6:
                    return IsSigned ? DDS.DXGI_FORMAT.DXGI_FORMAT_BC6H_SF16 :
                         DDS.DXGI_FORMAT.DXGI_FORMAT_BC6H_UF16;
                case BcnFormats.BC7: return DDS.DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM;
            }
            throw new Exception($"Invalid format {this.Format}!");
        }
    }

    public enum BcnFormats
    {
        BC1, BC2, BC3, BC4, BC5, BC6, BC7,
    }
}
