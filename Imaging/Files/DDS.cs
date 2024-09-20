using BCnEncoder.Encoder;
using BCnEncoder.Shared.ImageFiles;
using SixLabors.ImageSharp.ColorSpaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;

namespace AvaloniaToolbox.Core.Textures
{
    /// <summary>
    /// Represents an DDS file binary format.
    /// </summary>
    public class DDS
    {
        #region Constants

        public const uint FOURCC_DXT1 = 0x31545844;
        public const uint FOURCC_DXT2 = 0x32545844;
        public const uint FOURCC_DXT3 = 0x33545844;
        public const uint FOURCC_DXT4 = 0x34545844;
        public const uint FOURCC_DXT5 = 0x35545844;
        public const uint FOURCC_ATI1 = 0x31495441;
        public const uint FOURCC_BC4U = 0x55344342;
        public const uint FOURCC_BC4S = 0x53344342;
        public const uint FOURCC_BC5U = 0x55354342;
        public const uint FOURCC_BC5S = 0x53354342;
        public const uint FOURCC_DX10 = 0x30315844;

        public const uint FOURCC_ATI2 = 0x32495441;
        public const uint FOURCC_RXGB = 0x42475852;
        public const uint FOURCC_R32  = 0x00000072;

        // RGBA Masks
        private static int[] A1R5G5B5_MASKS = { 0x7C00, 0x03E0, 0x001F, 0x8000 };
        private static int[] X1R5G5B5_MASKS = { 0x7C00, 0x03E0, 0x001F, 0x0000 };
        private static int[] A4R4G4B4_MASKS = { 0x0F00, 0x00F0, 0x000F, 0xF000 };
        private static int[] X4R4G4B4_MASKS = { 0x0F00, 0x00F0, 0x000F, 0x0000 };
        private static int[] R5G6B5_MASKS = { 0xF800, 0x07E0, 0x001F, 0x0000 };
        private static int[] R8G8B8_MASKS = { 0xFF0000, 0x00FF00, 0x0000FF, 0x000000 };
        private static uint[] A8B8G8R8_MASKS = { 0x000000FF, 0x0000FF00, 0x00FF0000, 0xFF000000 };
        private static int[] X8B8G8R8_MASKS = { 0x000000FF, 0x0000FF00, 0x00FF0000, 0x00000000 };
        private static uint[] A8R8G8B8_MASKS = { 0x00FF0000, 0x0000FF00, 0x000000FF, 0xFF000000 };
        private static int[] X8R8G8B8_MASKS = { 0x00FF0000, 0x0000FF00, 0x000000FF, 0x00000000 };

        private static int[] L8_MASKS = { 0x000000FF, 0x0000, };
        private static int[] A8L8_MASKS = { 0x000000FF, 0x0F00, };

        #endregion

        #region enums

        public enum CubemapFace
        {
            PosX,
            NegX,
            PosY,
            NegY,
            PosZ,
            NegZ
        }

        [Flags]
        public enum DDSD : uint
        {
            CAPS = 0x00000001,
            HEIGHT = 0x00000002,
            WIDTH = 0x00000004,
            PITCH = 0x00000008,
            PIXELFORMAT = 0x00001000,
            MIPMAPCOUNT = 0x00020000,
            LINEARSIZE = 0x00080000,
            DEPTH = 0x00800000
        }
        [Flags]
        public enum DDPF : uint
        {
            ALPHAPIXELS = 0x00000001,
            ALPHA = 0x00000002,
            FOURCC = 0x00000004,
            RGB = 0x00000040,
            YUV = 0x00000200,
            LUMINANCE = 0x00020000,
        }
        [Flags]
        public enum DDSCAPS : uint
        {
            COMPLEX = 0x00000008,
            TEXTURE = 0x00001000,
            MIPMAP = 0x00400000,
        }
        [Flags]
        public enum DDSCAPS2 : uint
        {
            CUBEMAP = 0x00000200,
            CUBEMAP_POSITIVEX = 0x00000400 | CUBEMAP,
            CUBEMAP_NEGATIVEX = 0x00000800 | CUBEMAP,
            CUBEMAP_POSITIVEY = 0x00001000 | CUBEMAP,
            CUBEMAP_NEGATIVEY = 0x00002000 | CUBEMAP,
            CUBEMAP_POSITIVEZ = 0x00004000 | CUBEMAP,
            CUBEMAP_NEGATIVEZ = 0x00008000 | CUBEMAP,
            CUBEMAP_ALLFACES = (CUBEMAP_POSITIVEX | CUBEMAP_NEGATIVEX |
                                  CUBEMAP_POSITIVEY | CUBEMAP_NEGATIVEY |
                                  CUBEMAP_POSITIVEZ | CUBEMAP_NEGATIVEZ),
            VOLUME = 0x00200000
        }

        public enum DXGI_FORMAT
        {
            DXGI_FORMAT_UNKNOWN = 0,
            DXGI_FORMAT_R32G32B32A32_TYPELESS = 1,
            DXGI_FORMAT_R32G32B32A32_FLOAT = 2,
            DXGI_FORMAT_R32G32B32A32_UINT = 3,
            DXGI_FORMAT_R32G32B32A32_SINT = 4,
            DXGI_FORMAT_R32G32B32_TYPELESS = 5,
            DXGI_FORMAT_R32G32B32_FLOAT = 6,
            DXGI_FORMAT_R32G32B32_UINT = 7,
            DXGI_FORMAT_R32G32B32_SINT = 8,
            DXGI_FORMAT_R16G16B16A16_TYPELESS = 9,
            DXGI_FORMAT_R16G16B16A16_FLOAT = 10,
            DXGI_FORMAT_R16G16B16A16_UNORM = 11,
            DXGI_FORMAT_R16G16B16A16_UINT = 12,
            DXGI_FORMAT_R16G16B16A16_SNORM = 13,
            DXGI_FORMAT_R16G16B16A16_SINT = 14,
            DXGI_FORMAT_R32G32_TYPELESS = 15,
            DXGI_FORMAT_R32G32_FLOAT = 16,
            DXGI_FORMAT_R32G32_UINT = 17,
            DXGI_FORMAT_R32G32_SINT = 18,
            DXGI_FORMAT_R32G8X24_TYPELESS = 19,
            DXGI_FORMAT_D32_FLOAT_S8X24_UINT = 20,
            DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS = 21,
            DXGI_FORMAT_X32_TYPELESS_G8X24_UINT = 22,
            DXGI_FORMAT_R10G10B10A2_TYPELESS = 23,
            DXGI_FORMAT_R10G10B10A2_UNORM = 24,
            DXGI_FORMAT_R10G10B10A2_UINT = 25,
            DXGI_FORMAT_R11G11B10_FLOAT = 26,
            DXGI_FORMAT_R8G8B8A8_TYPELESS = 27,
            DXGI_FORMAT_R8G8B8A8_UNORM = 28,
            DXGI_FORMAT_R8G8B8A8_UNORM_SRGB = 29,
            DXGI_FORMAT_R8G8B8A8_UINT = 30,
            DXGI_FORMAT_R8G8B8A8_SNORM = 31,
            DXGI_FORMAT_R8G8B8A8_SINT = 32,
            DXGI_FORMAT_R16G16_TYPELESS = 33,
            DXGI_FORMAT_R16G16_FLOAT = 34,
            DXGI_FORMAT_R16G16_UNORM = 35,
            DXGI_FORMAT_R16G16_UINT = 36,
            DXGI_FORMAT_R16G16_SNORM = 37,
            DXGI_FORMAT_R16G16_SINT = 38,
            DXGI_FORMAT_R32_TYPELESS = 39,
            DXGI_FORMAT_D32_FLOAT = 40,
            DXGI_FORMAT_R32_FLOAT = 41,
            DXGI_FORMAT_R32_UINT = 42,
            DXGI_FORMAT_R32_SINT = 43,
            DXGI_FORMAT_R24G8_TYPELESS = 44,
            DXGI_FORMAT_D24_UNORM_S8_UINT = 45,
            DXGI_FORMAT_R24_UNORM_X8_TYPELESS = 46,
            DXGI_FORMAT_X24_TYPELESS_G8_UINT = 47,
            DXGI_FORMAT_R8G8_TYPELESS = 48,
            DXGI_FORMAT_R8G8_UNORM = 49,
            DXGI_FORMAT_R8G8_UINT = 50,
            DXGI_FORMAT_R8G8_SNORM = 51,
            DXGI_FORMAT_R8G8_SINT = 52,
            DXGI_FORMAT_R16_TYPELESS = 53,
            DXGI_FORMAT_R16_FLOAT = 54,
            DXGI_FORMAT_D16_UNORM = 55,
            DXGI_FORMAT_R16_UNORM = 56,
            DXGI_FORMAT_R16_UINT = 57,
            DXGI_FORMAT_R16_SNORM = 58,
            DXGI_FORMAT_R16_SINT = 59,
            DXGI_FORMAT_R8_TYPELESS = 60,
            DXGI_FORMAT_R8_UNORM = 61,
            DXGI_FORMAT_R8_UINT = 62,
            DXGI_FORMAT_R8_SNORM = 63,
            DXGI_FORMAT_R8_SINT = 64,
            DXGI_FORMAT_A8_UNORM = 65,
            DXGI_FORMAT_R1_UNORM = 66,
            DXGI_FORMAT_R9G9B9E5_SHAREDEXP = 67,
            DXGI_FORMAT_R8G8_B8G8_UNORM = 68,
            DXGI_FORMAT_G8R8_G8B8_UNORM = 69,
            DXGI_FORMAT_BC1_TYPELESS = 70,
            DXGI_FORMAT_BC1_UNORM = 71,
            DXGI_FORMAT_BC1_UNORM_SRGB = 72,
            DXGI_FORMAT_BC2_TYPELESS = 73,
            DXGI_FORMAT_BC2_UNORM = 74,
            DXGI_FORMAT_BC2_UNORM_SRGB = 75,
            DXGI_FORMAT_BC3_TYPELESS = 76,
            DXGI_FORMAT_BC3_UNORM = 77,
            DXGI_FORMAT_BC3_UNORM_SRGB = 78,
            DXGI_FORMAT_BC4_TYPELESS = 79,
            DXGI_FORMAT_BC4_UNORM = 80,
            DXGI_FORMAT_BC4_SNORM = 81,
            DXGI_FORMAT_BC5_TYPELESS = 82,
            DXGI_FORMAT_BC5_UNORM = 83,
            DXGI_FORMAT_BC5_SNORM = 84,
            DXGI_FORMAT_B5G6R5_UNORM = 85,
            DXGI_FORMAT_B5G5R5A1_UNORM = 86,
            DXGI_FORMAT_B8G8R8A8_UNORM = 87,
            DXGI_FORMAT_B8G8R8X8_UNORM = 88,
            DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM = 89,
            DXGI_FORMAT_B8G8R8A8_TYPELESS = 90,
            DXGI_FORMAT_B8G8R8A8_UNORM_SRGB = 91,
            DXGI_FORMAT_B8G8R8X8_TYPELESS = 92,
            DXGI_FORMAT_B8G8R8X8_UNORM_SRGB = 93,
            DXGI_FORMAT_BC6H_TYPELESS = 94,
            DXGI_FORMAT_BC6H_UF16 = 95,
            DXGI_FORMAT_BC6H_SF16 = 96,
            DXGI_FORMAT_BC7_TYPELESS = 97,
            DXGI_FORMAT_BC7_UNORM = 98,
            DXGI_FORMAT_BC7_UNORM_SRGB = 99,
            DXGI_FORMAT_AYUV = 100,
            DXGI_FORMAT_Y410 = 101,
            DXGI_FORMAT_Y416 = 102,
            DXGI_FORMAT_NV12 = 103,
            DXGI_FORMAT_P010 = 104,
            DXGI_FORMAT_P016 = 105,
            DXGI_FORMAT_420_OPAQUE = 106,
            DXGI_FORMAT_YUY2 = 107,
            DXGI_FORMAT_Y210 = 108,
            DXGI_FORMAT_Y216 = 109,
            DXGI_FORMAT_NV11 = 110,
            DXGI_FORMAT_AI44 = 111,
            DXGI_FORMAT_IA44 = 112,
            DXGI_FORMAT_P8 = 113,
            DXGI_FORMAT_A8P8 = 114,
            DXGI_FORMAT_B4G4R4A4_UNORM = 115,
            DXGI_FORMAT_P208 = 130,
            DXGI_FORMAT_V208 = 131,
            DXGI_FORMAT_V408 = 132,
            DXGI_FORMAT_SAMPLER_FEEDBACK_MIN_MIP_OPAQUE,
            DXGI_FORMAT_SAMPLER_FEEDBACK_MIP_REGION_USED_OPAQUE,
            DXGI_FORMAT_FORCE_UINT = -1,
        };

        #endregion

        #region Structs

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Header
        {
            public uint Magic; //"DDS "
            public uint Size;
            public uint Flags;
            public uint Height;
            public uint Width;
            public uint PitchOrLinearSize;
            public uint Depth;
            public uint MipCount;

            public uint Reserved0;
            public uint Reserved1;
            public uint Reserved2;
            public uint Reserved3;
            public uint Reserved4;
            public uint Reserved5;
            public uint Reserved6;
            public uint Reserved7;
            public uint Reserved8;
            public uint Reserved9;
            public uint Reserved10;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DDSPFHeader
        {
            public uint Size;
            public uint Flags;
            public uint FourCC;
            public uint RgbBitCount;
            public uint RBitMask;
            public uint GBitMask;
            public uint BBitMask;
            public uint ABitMask;
            public uint Caps1;
            public uint Caps2;
            public uint Caps3;
            public uint Caps4;
            public uint Reserved;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DX10Header
        {
            public uint DxgiFormat;
            public uint ResourceDim;
            public uint MiscFlags1;
            public uint ArrayCount;
            public uint MiscFlags2;
        }

        #endregion

        public DXGI_FORMAT Format = DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM;

        public Header MainHeader;
        public DDSPFHeader PfHeader;
        public DX10Header Dx10Header;

        public byte[] ImageData;
        public bool IsDX10 => PfHeader.FourCC == FOURCC_DX10;

        public bool IsCubeMap
        {
            get { return PfHeader.Caps2 == (uint)DDSCAPS2.CUBEMAP_ALLFACES; }
            set
            {
                if (value)
                    PfHeader.Caps2 = (uint)DDSCAPS2.CUBEMAP_ALLFACES;
                else
                    PfHeader.Caps2 = 0;
            }
        }

        public uint ArrayCount
        {
            get
            {
                if (IsDX10) return Dx10Header.ArrayCount;
                if (this.IsCubeMap) return 6u;

                return 1u;
            }
        }

        public DDS(bool isDxt10 = false) {

            MainHeader = new Header();
            PfHeader = new DDSPFHeader();
            if (isDxt10)
            {
                Dx10Header = new DX10Header();
                Dx10Header.ResourceDim = 3;
                Dx10Header.ArrayCount = 1;
                PfHeader.FourCC = BitConverter.ToUInt32(Encoding.ASCII.GetBytes("DX10"));
            }

            MainHeader.Magic = 0x20534444;
            MainHeader.Size = 124;
            MainHeader.Flags = (uint)(DDSD.CAPS | DDSD.HEIGHT | DDSD.WIDTH | DDSD.PIXELFORMAT | DDSD.MIPMAPCOUNT | DDSD.LINEARSIZE);

            PfHeader.Size = 0x20;
            PfHeader.Flags = 4;
            PfHeader.Caps1 = (uint)DDSCAPS.TEXTURE;
        }

        public DDS(string filePath) { Load(filePath); }
        public DDS(Stream stream) { Load(stream); }


        public void Load(string fileName)
        {
            Load(File.OpenRead(fileName));
        }

        public void Load(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                stream.Read(AsSpan(ref MainHeader));
                stream.Read(AsSpan(ref PfHeader));

                reader.BaseStream.Seek(MainHeader.Size + 4, SeekOrigin.Begin);

                if (IsDX10)
                    stream.Read(AsSpan(ref Dx10Header));

                SetupFormat();

                ImageData = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
            }

            if (!DDS.FormatList.ContainsKey((int)this.Format))
                throw new Exception($"Format {this.Format} not supported!");
        }

        public void Save(string path)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write)) {
                Save(fs);
            }
        }

        public void Save(Stream stream)
        {
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(AsSpan(ref MainHeader));
                writer.Write(AsSpan(ref PfHeader));
                writer.BaseStream.Seek(MainHeader.Size + 4, SeekOrigin.Begin);
                if (IsDX10)
                    writer.Write(AsSpan(ref Dx10Header));
                writer.Write(ImageData);
            }
        }

        public void SetFlags(DXGI_FORMAT format, bool ForceDX10 = false, bool isCubeMap = false)
        {
            this.Format = format;

            MainHeader.Flags = (uint)(DDSD.CAPS | DDSD.HEIGHT | DDSD.WIDTH | DDSD.PIXELFORMAT | DDSD.MIPMAPCOUNT | DDSD.LINEARSIZE);
            PfHeader.Caps1 = (uint)DDSCAPS.TEXTURE;
            if (MainHeader.MipCount > 1)
                PfHeader.Caps1 |= (uint)(DDSCAPS.COMPLEX | DDSCAPS.MIPMAP);

            if (isCubeMap)
            {
                PfHeader.Caps2 |= (uint)(DDSCAPS2.CUBEMAP | DDSCAPS2.CUBEMAP_POSITIVEX | DDSCAPS2.CUBEMAP_NEGATIVEX |
                                      DDSCAPS2.CUBEMAP_POSITIVEY | DDSCAPS2.CUBEMAP_NEGATIVEY |
                                      DDSCAPS2.CUBEMAP_POSITIVEZ | DDSCAPS2.CUBEMAP_NEGATIVEZ);
            }

            ConvertDXGIFormatToFlags(format);

            if (IsDX10 || ForceDX10)
            {
                PfHeader.Flags = (uint)DDPF.FOURCC;
                PfHeader.FourCC = FOURCC_DX10;
                Dx10Header = new DX10Header();
                Dx10Header.DxgiFormat = (uint)Format;
                if (isCubeMap)
                {
                    Dx10Header.ArrayCount = (ArrayCount / 6);
                    Dx10Header.MiscFlags1 = 0x4;
                }
                return;
            }
        }

        private void ConvertDXGIFormatToFlags(DXGI_FORMAT format)
        {
            switch (Format)
            {
                case DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM_SRGB:
                    PfHeader.Flags = (uint)(DDPF.RGB | DDPF.ALPHAPIXELS);
                    PfHeader.RgbBitCount = 0x8 * 4;
                    PfHeader.RBitMask = 0x000000FF;
                    PfHeader.GBitMask = 0x0000FF00;
                    PfHeader.BBitMask = 0x00FF0000;
                    PfHeader.ABitMask = 0xFF000000;
                    break;
                case DXGI_FORMAT.DXGI_FORMAT_R8G8_UNORM:
                    PfHeader.Flags = (uint)(DDPF.RGB | DDPF.ALPHAPIXELS);
                    PfHeader.RgbBitCount = 0x8 * 3;
                    PfHeader.RBitMask = (uint)R8G8B8_MASKS[0];
                    PfHeader.GBitMask = (uint)R8G8B8_MASKS[1];
                    PfHeader.BBitMask = (uint)R8G8B8_MASKS[2];
                    PfHeader.ABitMask = (uint)R8G8B8_MASKS[3];
                    break;
                case DXGI_FORMAT.DXGI_FORMAT_R8_UNORM:
                    PfHeader.Flags = (uint)(DDPF.LUMINANCE);
                    PfHeader.RgbBitCount = 0x8;
                    PfHeader.RBitMask = (uint)L8_MASKS[0];
                    PfHeader.GBitMask = (uint)L8_MASKS[1];
                    PfHeader.BBitMask = (uint)0;
                    PfHeader.ABitMask = (uint)0;
                    break;
                case DXGI_FORMAT.DXGI_FORMAT_B5G6R5_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_B5G5R5A1_UNORM:
                    PfHeader.Flags = (uint)(DDPF.RGB);
                    PfHeader.RgbBitCount = 5+6+5;
                    PfHeader.RBitMask = (uint)R5G6B5_MASKS[0];
                    PfHeader.GBitMask = (uint)R5G6B5_MASKS[1];
                    PfHeader.BBitMask = (uint)R5G6B5_MASKS[2];
                    PfHeader.ABitMask = (uint)R5G6B5_MASKS[3];
                    break;
                case DXGI_FORMAT.DXGI_FORMAT_B4G4R4A4_UNORM:
                    PfHeader.Flags = (uint)(DDPF.RGB | DDPF.ALPHAPIXELS);
                    PfHeader.RgbBitCount = 4+4+4+4;
                    PfHeader.RBitMask = (uint)X4R4G4B4_MASKS[0];
                    PfHeader.GBitMask = (uint)X4R4G4B4_MASKS[1];
                    PfHeader.BBitMask = (uint)X4R4G4B4_MASKS[2];
                    PfHeader.ABitMask = (uint)X4R4G4B4_MASKS[3];
                    break;
                case DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM_SRGB:
                case DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM:
                    PfHeader.Flags = (uint)DDPF.FOURCC;
                    PfHeader.FourCC = FOURCC_DXT1;
                    break;
                case DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM_SRGB:
                case DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM:
                    PfHeader.Flags = (uint)DDPF.FOURCC;
                    PfHeader.FourCC = FOURCC_DXT3;
                    break;
                case DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM_SRGB:
                case DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM:
                    PfHeader.Flags = (uint)DDPF.FOURCC;
                    PfHeader.FourCC = FOURCC_DXT5;
                    break;
                case DXGI_FORMAT.DXGI_FORMAT_BC4_UNORM:
                    PfHeader.Flags = (uint)DDPF.FOURCC;
                    PfHeader.FourCC = FOURCC_BC4U;
                    break;
                case DXGI_FORMAT.DXGI_FORMAT_BC4_SNORM:
                    PfHeader.Flags = (uint)DDPF.FOURCC;
                    PfHeader.FourCC = FOURCC_BC4S;
                    break;
                case DXGI_FORMAT.DXGI_FORMAT_BC5_UNORM:
                    PfHeader.Flags = (uint)DDPF.FOURCC;
                    PfHeader.FourCC = FOURCC_BC5U;
                    break;
                case DXGI_FORMAT.DXGI_FORMAT_BC5_SNORM:
                    PfHeader.Flags = (uint)DDPF.FOURCC;
                    PfHeader.FourCC = FOURCC_BC5S;
                    break;
                case DXGI_FORMAT.DXGI_FORMAT_BC6H_UF16:
                case DXGI_FORMAT.DXGI_FORMAT_BC6H_SF16:
                case DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM:
                    PfHeader.Flags = (uint)DDPF.FOURCC;
                    PfHeader.FourCC = FOURCC_DX10;
                    Dx10Header.DxgiFormat = (uint)Format;
                    break;
                case DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM_SRGB:
                    PfHeader.Flags = (uint)DDPF.FOURCC;
                    PfHeader.FourCC = FOURCC_DX10;
                    Dx10Header.DxgiFormat = (uint)DDS.DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM;
                    break;
            }
        }

        public uint CalculateSize()
        {
            uint size = 0;

            int CalculateMipDimension(uint baseLevelDimension, int mipLevel) {
                return (int)baseLevelDimension / (int)Math.Pow(2, mipLevel);
            }

            var formatInfo = FormatList[(int)this.Format];
            for (int arrayLevel = 0; arrayLevel < ArrayCount; arrayLevel++)
            {
                for (int mipLevel = 0; mipLevel < this.MainHeader.MipCount; mipLevel++)
                {
                    int mipWidth = CalculateMipDimension(this.MainHeader.Width, mipLevel);
                    int mipHeight = CalculateMipDimension(this.MainHeader.Height, mipLevel);

                    uint imageSize = (uint)(mipWidth * mipHeight * formatInfo.BitsPerPixel);
                    size += imageSize;
                }
            }
            return size;
        }

        public byte[] GetSurfaces(int mip_level = 0)
        {
            List<byte> surfaces = new List<byte>();

            for (int i = 0; i < this.ArrayCount; i++)
                surfaces.AddRange(GetSurface(i, mip_level));

            return surfaces.ToArray();  
        }

        public byte[] GetSurface(int arrayIndex, int mip_level = 0)
        {
            Span<byte> buffer = ImageData;
            uint offset = 0;

            int CalculateMipDimension(uint baseLevelDimension, int mipLevel) {
                return (int)baseLevelDimension / (int)Math.Pow(2, mipLevel);
            }

            var formatInfo = FormatList[(int)this.Format];
            for (int arrayLevel = 0; arrayLevel < ArrayCount; arrayLevel++)
            {
                for (int mipLevel = 0; mipLevel < this.MainHeader.MipCount; mipLevel++)
                {
                    int mipWidth = CalculateMipDimension(this.MainHeader.Width, mipLevel);
                    int mipHeight = CalculateMipDimension(this.MainHeader.Height, mipLevel);

                    uint imageSize = (uint)(mipWidth * mipHeight * formatInfo.BytesPerPixel);
                    if (arrayIndex == arrayLevel && mip_level == mipLevel)
                        return buffer.Slice((int)offset, (int)imageSize).ToArray();

                    offset += imageSize;
                }
            }
            return ImageData;
        }

        public bool IsBCNCompressed()
        {
            switch (Format)
            {
                case DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC4_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC5_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC6H_UF16:
                case DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM:
                    return true;
                case DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM_SRGB:
                case DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM_SRGB:
                case DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM_SRGB:
                case DXGI_FORMAT.DXGI_FORMAT_BC4_SNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC5_SNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC6H_SF16:
                case DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM_SRGB:
                    return true;
            }
            return false;
        }

        private static Span<byte> AsSpan<T>(ref T val) where T : unmanaged
        {
            Span<T> valSpan = MemoryMarshal.CreateSpan(ref val, 1);
            return MemoryMarshal.Cast<T, byte>(valSpan);
        }

        private void SetupFormat()
        {
            if (IsDX10)
            {
                this.Format = (DXGI_FORMAT)Dx10Header.DxgiFormat;
                return;
            }

            switch (PfHeader.FourCC)
            {
                case FOURCC_DXT1:
                    this.Format = DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM;
                    break;
                case FOURCC_DXT2:
                case FOURCC_DXT3:
                    this.Format = DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM;
                    break;
                case FOURCC_DXT4:
                case FOURCC_DXT5:
                    this.Format = DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM;
                    break;
                case FOURCC_ATI1:
                case FOURCC_BC4U:
                    this.Format = DXGI_FORMAT.DXGI_FORMAT_BC4_UNORM;
                    break;
                case FOURCC_ATI2:
                case FOURCC_BC5U:
                    this.Format = DXGI_FORMAT.DXGI_FORMAT_BC5_UNORM;
                    break;
                case FOURCC_BC5S:
                    this.Format = DXGI_FORMAT.DXGI_FORMAT_BC5_SNORM;
                    break;
                case FOURCC_RXGB:
                    this.Format = DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM;
                    break;
                case FOURCC_R32:
                    this.Format = DXGI_FORMAT.DXGI_FORMAT_R32_FLOAT;
                    return;
                    break;
                default:
                    this.Format = DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM;
                    break;
            }

            bool Compressed = false;
            bool HasLuminance = false;
            bool HasAlpha = false;
            bool IsRGB = false;

            switch (PfHeader.Flags)
            {
                case 4:
                    Compressed = true;
                    break;
                case 2:
                case (uint)DDPF.LUMINANCE:
                    HasLuminance = true;
                    break;
                case (uint)DDPF.RGB:
                    IsRGB = true;
                    break;
                case 0x41:
                    IsRGB = true;
                    HasAlpha = true;
                    break;
            }

            if (!this.IsBCNCompressed())
            {
                byte[] Components = new byte[4] { 0, 1, 2, 3 };
                this.Format = DDS_RGBA.GetUncompressedType(this, Components, IsRGB, HasAlpha, HasLuminance, PfHeader);
            }
        }

        public static Dictionary<int, ImageEncoder> FormatList = new Dictionary<int, ImageEncoder>()
        {
            { (int)DDS.DXGI_FORMAT.DXGI_FORMAT_R8_UNORM,        ImageFormats.R8() },
            { (int)DDS.DXGI_FORMAT.DXGI_FORMAT_R8G8_UNORM,      ImageFormats.Rg8() },
            { (int)DDS.DXGI_FORMAT.DXGI_FORMAT_R8G8_SNORM,      ImageFormats.Rg8Signed() },
            { (int)DDS.DXGI_FORMAT.DXGI_FORMAT_R32_FLOAT,       ImageFormats.R32() },
            { (int)DDS.DXGI_FORMAT.DXGI_FORMAT_R32G32_FLOAT,    ImageFormats.Rg32() },

            { (int)DDS.DXGI_FORMAT.DXGI_FORMAT_R16_FLOAT,        ImageFormats.R16() },
            { (int)DDS.DXGI_FORMAT.DXGI_FORMAT_R16G16_FLOAT,     ImageFormats.Rg16() },

            { (int)DDS.DXGI_FORMAT.DXGI_FORMAT_R11G11B10_FLOAT,    ImageFormats.R11g11b10() },
            { (int)DDS.DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_FLOAT, ImageFormats.Rgba16() },
            { (int)DDS.DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_FLOAT, ImageFormats.Rgba32() },

            { (int)DDS.DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,      ImageFormats.Bgra8() },
            { (int)DDS.DXGI_FORMAT.DXGI_FORMAT_B8G8R8X8_UNORM,      ImageFormats.Bgr8() },

            //RGB
            { (int)DDS.DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM,      ImageFormats.Rgba8() },
            { (int)DDS.DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM_SRGB, ImageFormats.Rgba8(true) },

            { (int)DDS.DXGI_FORMAT.DXGI_FORMAT_B5G6R5_UNORM,   ImageFormats.Bgra565() },
            { (int)DDS.DXGI_FORMAT.DXGI_FORMAT_B4G4R4A4_UNORM, ImageFormats.Bgra4() },
            { (int)DDS.DXGI_FORMAT.DXGI_FORMAT_B5G5R5A1_UNORM, ImageFormats.Bgra5551() },

            //BC
            { (int)DDS.DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM,  ImageFormats.Bc1() },
            { (int)DDS.DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM,  ImageFormats.Bc2() },
            { (int)DDS.DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM,  ImageFormats.Bc3() },
            { (int)DDS.DXGI_FORMAT.DXGI_FORMAT_BC4_UNORM,  ImageFormats.Bc4() },
            { (int)DDS.DXGI_FORMAT.DXGI_FORMAT_BC5_UNORM,  ImageFormats.Bc5() },
            { (int)DDS.DXGI_FORMAT.DXGI_FORMAT_BC6H_UF16,  ImageFormats.Bc6() },
            { (int)DDS.DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM,  ImageFormats.Bc7() },

            //SNORM
            { (int)DDS.DXGI_FORMAT.DXGI_FORMAT_BC4_SNORM, ImageFormats.Bc4(true) },
            { (int)DDS.DXGI_FORMAT.DXGI_FORMAT_BC5_SNORM, ImageFormats.Bc5(true) },
            { (int)DDS.DXGI_FORMAT.DXGI_FORMAT_BC6H_SF16, ImageFormats.Bc6(true) },

            //SRGB
            { (int)DDS.DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM_SRGB, ImageFormats.Bc1(true) },
            { (int)DDS.DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM_SRGB, ImageFormats.Bc2(true) },
            { (int)DDS.DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM_SRGB, ImageFormats.Bc3(true) },
            { (int)DDS.DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM_SRGB, ImageFormats.Bc7(true) },
        };
    }
}
