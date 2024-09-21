using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using AvaloniaToolbox.Core;
using AvaloniaToolbox.Core.IO;
using AvaloniaToolbox.Core.Textures;
using BCnEncoder.Shared;
using CafeLibrary.Formats.FF16.Shared;
using CommunityToolkit.HighPerformance.Buffers;
using FF16Converter;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Vortice.DirectStorage;
using Vortice.DXGI;

namespace FinalFantasy16
{
    //Info based on https://github.com/Nenkai/FF16Tools/blob/master/FF16Tools.Files/Textures/TextureFile.cs
    public class TexFile
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Header
        {
            public Magic Magic = "TEX ";
            public byte Version = 4;
            public byte Flags = 1;
            public byte Unknown = 16;
            public byte Padding;

            public byte TextureCount;
            public byte Padding2;
            public ushort ChunkCount;
            public uint Unknown2;
            public uint Unknown3;
            public uint UnknownFlags = 0x13;

            public uint Padding3;
            public uint Padding4;
            public uint Padding5;
            public uint Padding6;
        }

        public class Texture
        {
            public uint Flags;

            public TextureFormat Format;
            public ushort MipCount;
            public ushort Width;
            public ushort Height;
            public ushort Depth;
            public uint ChunkOffset;
            public uint ChunkSize;
            public uint Color;
            public ushort ChunkIndex;
            public ushort ChunkCount;

            public List<Chunk> Chunks = new List<Chunk>();

            public byte Dimension
            {
                get => (byte)BitUtils.GetBits((int)Flags, 0, 2);
                set => Flags = (uint)BitUtils.SetBits((int)Flags, (int)value, 0, 2);
            }

            public bool SignedDistanceField
            {
                get => BitUtils.GetBit((int)Flags, 2);
                set => Flags = (uint)BitUtils.SetBit((int)Flags, value, 2);
            }

            public bool NoChunks
            {
                get => BitUtils.GetBit((int)Flags, 3);
                set => Flags = (uint)BitUtils.SetBit((int)Flags, value, 3);
            }

            public byte UnknownBits1
            {
                get => (byte)BitUtils.GetBits((int)Flags, 4, 2);
                set => Flags = (uint)BitUtils.SetBits((int)Flags, (int)value, 4, 2);
            }

            public byte UnknownBits2
            {
                get => (byte)BitUtils.GetBits((int)Flags, 6, 2);
                set => Flags = (uint)BitUtils.SetBits((int)Flags, (int)value, 6, 2);
            }

            public uint UnknownBits24
            {
                get => (uint)BitUtils.GetBits((int)Flags, 8, 24);
                set => Flags = (uint)BitUtils.SetBits((int)Flags, (int)value, 8, 24);
            }

            public Texture()
            {
                this.Dimension = 1;
                this.UnknownBits2 = 2;
                this.UnknownBits24 = 0xFFFFFF;
                this.Depth = 1;
                this.Color = 0xFFA6AFC7;
            }

            public byte[] GetImageData()
            {
                byte[] decomp = ByteUtil.CombineByteArray(Chunks.Select(x => x.DecompressedBuffer).ToArray());
                return TextureDataUtil.CalculateSurfacePadding(this, decomp);
            }

            public void SetImageData(List<byte[]> mipmap_list)
            {
                Chunks.Clear();
                Chunks.Add(new Chunk()
                {
                    DecompressedBuffer = mipmap_list[0], //base level image
                    ChunkType = 0, //todo what does this flag do
                });
                //Mip data stored in second chunk
                if (mipmap_list.Count > 1)
                {
                    var mipData = ByteUtil.CombineByteArray(mipmap_list.Skip(1).ToArray());
                    Chunks.Add(new Chunk()
                    {
                        DecompressedBuffer = mipData,
                        ChunkIndex = 1,
                        ChunkType = 1, //todo what does this flag do
                    });
                }
            }

            public Image<Rgba32> ToImage()
            {
                var data = GetImageData();
                var w = this.GetAlignedWidth();
                var h = this.GetAlignedHeight();
                //rgba convert
                var formatDecoder = TexFile.FormatList[(int)this.Format];
                var rgba = formatDecoder.Decode(data, w, h);

                var image = Image.LoadPixelData<Rgba32>(rgba, (int)w, (int)h);
                image.Mutate(x => x.Crop(this.Width, this.Height));
                return image;
            }

            public void Export(string path)
            {
                if (path.EndsWith(".dds"))
                {
                    var dds = new DDS();
                    dds.MainHeader.Width = this.Width;
                    dds.MainHeader.Height = this.Height;
                    dds.MainHeader.Depth = Depth;
                    dds.MainHeader.MipCount = MipCount;

                    dds.ImageData = TextureDataUtil.GetUnalignedData(this, this.GetImageData());
                    dds.MainHeader.PitchOrLinearSize = (uint)dds.ImageData.Length;

                    DDS.DXGI_FORMAT format = DDS.DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM;

                    var formatEncoder = TexFile.FormatList[(int)this.Format];
                    if (formatEncoder is Bcn)
                        format = ((Bcn)formatEncoder).GetDxgiFormat();
                    else
                    {
                        switch (this.Format)
                        {
                            case TextureFormat.R8_UNORM:
                                format = DDS.DXGI_FORMAT.DXGI_FORMAT_R8_UNORM;
                                break;
                            case TextureFormat.R8_SNORM:
                                format = DDS.DXGI_FORMAT.DXGI_FORMAT_R8_SNORM;
                                break;
                            case TextureFormat.R32G32B32A32_FLOAT:
                                format = DDS.DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_FLOAT;
                                break;
                            case TextureFormat.R32G32B32_FLOAT:
                                format = DDS.DXGI_FORMAT.DXGI_FORMAT_R32G32B32_FLOAT;
                                break;
                            case TextureFormat.R32G32_FLOAT:
                                format = DDS.DXGI_FORMAT.DXGI_FORMAT_R32G32_FLOAT;
                                break;
                            case TextureFormat.R32G32_UINT:
                                format = DDS.DXGI_FORMAT.DXGI_FORMAT_R32G32_UINT;
                                break;
                            case TextureFormat.R16G16B16A16_FLOAT:
                                format = DDS.DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_FLOAT;
                                break;
                            case TextureFormat.R16G16_FLOAT:
                                format = DDS.DXGI_FORMAT.DXGI_FORMAT_R16G16_FLOAT;
                                break;
                            case TextureFormat.R16_FLOAT:
                                format = DDS.DXGI_FORMAT.DXGI_FORMAT_R16_FLOAT;
                                break;
                            case TextureFormat.R10G10B10A2_UNORM:
                                format = DDS.DXGI_FORMAT.DXGI_FORMAT_R10G10B10A2_UNORM;
                                break;
                            default:
                                throw new Exception($"Unsupported rgba format {this.Format}!");
                        }
                    }

                    dds.SetFlags(format, false, false);
                    if (dds.IsDX10)
                    {
                        dds.Dx10Header = new DDS.DX10Header();
                        dds.Dx10Header.ResourceDim = 3;
                        dds.Dx10Header.ArrayCount = 1;
                        dds.Dx10Header.DxgiFormat = (uint)format;
                    }
                    dds.Save(path);
                }
                else
                {
                    var data = TextureDataUtil.GetUnalignedData(this, GetImageData());
                    //rgba convert
                    var formatDecoder = TexFile.FormatList[(int)this.Format];
                    var rgba = formatDecoder.Decode(data, this.Width, this.Height);

                    var image = Image.LoadPixelData<Rgba32>(rgba, (int)this.Width, (int)this.Height);
                    image.SaveAsPng(path);
                }
            }

            public void Replace(string path)
            {
                if (path.EndsWith(".dds"))
                {
                    var dds = new DDS(path);
                    this.Width = (ushort)dds.MainHeader.Width;
                    this.Height = (ushort)dds.MainHeader.Height;
                    this.Depth = (ushort)dds.MainHeader.Depth;
                    this.MipCount = (ushort)dds.MainHeader.MipCount;

                    var encoder = DDS.FormatList[(int)dds.Format];
                    if (!FormatList.Any(x => x.Value.ToString() == encoder.ToString()))
                        throw new Exception($"Texture does not support format {encoder.ToString()}");

                    this.Format = (TextureFormat)FormatList.FirstOrDefault(X => X.Value.ToString() == encoder.ToString()).Key;

                    SetImageData(TextureDataUtil.GetAlignedData(this, dds.ImageData));
                }
                else
                {
                    var image = Image.Load<Rgba32>(path);
                    this.Width = (ushort)image.Width;
                    this.Height = (ushort)image.Height;
                    var mipCount = (ushort)CalculateMipCount();
                    //Use original mip count unless computed is too low
                    if (this.MipCount < mipCount)
                        this.MipCount = mipCount;

                    var mipmaps = ImageSharpTextureHelper.GenerateMipmaps(image, this.MipCount);

                    //padding for rgba
                    List<byte[]> encoded_mips = new List<byte[]>();
                    for (int i = 0; i < MipCount; i++)
                    {
                        var w = this.GetAlignedWidth(i);
                        var h = this.GetAlignedHeight(i);
                        var imageMipmap = mipmaps[i];
                        var rgba = imageMipmap.GetSourceInBytes();

                        //Create an image with the padded sizes if necessary
                        if (w != this.Height || h != this.Width)
                        {
                            using var paddedImage = new Image<Rgba32>((int)w, (int)h, new Rgba32());
                            paddedImage.Mutate(x => x.DrawImage(imageMipmap, new Point(0, 0), 1f));
                            rgba = paddedImage.GetSourceInBytes();
                        }

                        //rgba convert
                        var formatEncoder = TexFile.FormatList[(int)this.Format];
                        var encoded = formatEncoder.Encode(rgba, w, h);
                        encoded_mips.Add(encoded);

                        image?.Dispose();
                    }
                    SetImageData(TextureDataUtil.GetAlignedData(this, ByteUtil.CombineByteArray(encoded_mips.ToArray())));
                }
            }



            private uint CalculateMipCount() {
                return Math.Max((uint)Math.Floor(Math.Log(Math.Max(Width, Height), 2)), 1);
            }

            public uint GetAlignedWidth(int mip_level = 0)
            {
                var width = (uint)TextureDataUtil.CalculateMipDimension(this.Width, mip_level);

                int paddedWidth = (int)Align(width, 64);
                if (mip_level >= 1 && this.Width == 512)
                    paddedWidth = (int)Align(width, 128);

                if (this.SignedDistanceField) 
                    paddedWidth = (int)Align(width, 256);

                //pain
                if (this.Format == TextureFormat.BC4_UNORM &&
                    (this.MipCount == 9 || this.MipCount == 7))
                    paddedWidth = (int)Align(width, 128);

                return (uint)paddedWidth;
            }

            public uint GetAlignedHeight(int mip_level = 0)
            {
                var height = (uint)TextureDataUtil.CalculateMipDimension(this.Height, mip_level);

                if (this.Format.ToString().StartsWith("BC")) //Align by block size
                    return (uint)(height > 4 ? (uint)Align(height, 4) : height);
                else
                    return height;
            }

            private static uint Align(uint value, uint alignment) {
                return (value + (alignment - 1)) & ~(alignment - 1);
            }
        }

        public class Chunk
        {
            public uint DataOffset;
            public uint Flags;
            public uint DecompressedSize = 1;
            public uint MiscFlags;

            public uint CompressedSize
            {
                get => (uint)BitUtils.GetBits((int)Flags, 2, 30);
                set => Flags = (uint)BitUtils.SetBits((int)Flags,(int)value, 2, 30);
            }

            public uint ChunkType
            {
                get => (uint)BitUtils.GetBits((int)Flags, 0, 2);
                set => Flags = (uint)BitUtils.SetBits((int)Flags, (int)value, 0, 2);
            }

            public uint ChunkIndex
            {
                get => (uint)BitUtils.GetBits((int)MiscFlags, 0, 7);
                set => MiscFlags = (uint)BitUtils.SetBits((int)MiscFlags, (int)value, 0, 7);
            }

            public bool IsCompressed = true;

            public byte[] DecompressedBuffer;

            public byte[] Decompress(byte[] data)
            {
                IsCompressed = DecompressedSize != this.CompressedSize;
                if (!IsCompressed)
                {
                    return data;
                }
                else
                {
                    MemoryOwner<byte> decompressedBuffer = MemoryOwner<byte>.Allocate((int)DecompressedSize);
                    GDeflate.Decompress(data, decompressedBuffer.Span);
                    return decompressedBuffer.Span.ToArray();
                }
            }

            public byte[] Compress(byte[] data)
            {
                if (!IsCompressed)
                {
                    DecompressedSize = (uint)data.Length;
                    CompressedSize = (uint)data.Length;

                    return data;
                }
                else
                {
                    DecompressedSize = (uint)data.Length;

                    long sizeCompressed = GDeflate.CompressionSize(DecompressedSize);

                    MemoryOwner<byte> compressedBuffer = MemoryOwner<byte>.Allocate((int)sizeCompressed);
                    CompressedSize = GDeflate.Compress(data, compressedBuffer.Span);

                    return compressedBuffer.Slice(0, (int)CompressedSize).Span.ToArray();
                }
            }
        }

        public List<Texture> Textures = new List<Texture>();

        public Header TexHeader = new Header();

        public TexFile()
        {
            Textures.Add(new Texture()
            {
                Format = TextureFormat.BC7_UNORM_SRGB,
            });
        }

        public TexFile(Stream stream)
        {
            Read(stream);
        }

        public TexFile(string path)
        {
            Read(File.OpenRead(path));
        }

        private void Read(Stream stream)
        {
            using (var reader = new FileReader(stream))
            {
                TexHeader = reader.ReadStruct<Header>();
                for (int i = 0; i < TexHeader.TextureCount; i++)
                {
                    Textures.Add(new Texture()
                    {
                        Flags = reader.ReadUInt32(),
                        Format = (TextureFormat)reader.ReadUInt32(),
                        MipCount = reader.ReadUInt16(),
                        Width = reader.ReadUInt16(),
                        Height = reader.ReadUInt16(),
                        Depth = reader.ReadUInt16(),
                        ChunkOffset = reader.ReadUInt32(),
                        ChunkSize = reader.ReadUInt32(),
                        Color = reader.ReadUInt32(),
                        ChunkIndex = reader.ReadUInt16(),
                        ChunkCount = reader.ReadUInt16(),
                    });
                }

                Chunk[] chunks = new Chunk[TexHeader.ChunkCount];
                for (int i = 0; i < TexHeader.ChunkCount; i++)
                {
                    chunks[i] = new Chunk()
                    {
                        DataOffset = reader.ReadUInt32(),
                        Flags = reader.ReadUInt32(),
                        DecompressedSize = reader.ReadUInt32(),
                        MiscFlags = reader.ReadUInt32(),
                    };
                    using (reader.BaseStream.TemporarySeek(chunks[i].DataOffset, SeekOrigin.Begin))
                    {
                        var data = reader.ReadBytes((int)chunks[i].CompressedSize);
                        chunks[i].DecompressedBuffer = chunks[i].Decompress(data);
                    }
                }

                for (int i = 0; i < TexHeader.TextureCount; i++)
                {
                    for (int j = 0; j < Textures[i].ChunkCount; j++)
                        Textures[i].Chunks.Add(chunks[j + Textures[i].ChunkIndex]);
                }
            }
        }

        public void Save(string path)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write)) {
                Save(fs);
            }
        }

        public void Save(Stream stream)
        {
            TexHeader.ChunkCount = (ushort)Textures.Sum(x => x.Chunks.Count);
            TexHeader.TextureCount =  (byte)Textures.Count;
            TexHeader.Magic = "TEX ";

            using (var writer = new FileWriter(stream))
            { 
                writer.WriteStruct(TexHeader);

                List<byte[]> compressed = new List<byte[]>();

                foreach (var chunk in Textures.SelectMany(x => x.Chunks))
                    compressed.Add(chunk.Compress(chunk.DecompressedBuffer));

                int chunkIndex = 0;

                long textureHeaderPos = writer.Position;
                for (int i = 0; i < Textures.Count; i++)
                {
                    uint totalSize = 0;
                    foreach (var chunk in Textures[i].Chunks)
                    {
                        //alignment
                        totalSize += (uint)(-totalSize % 8 + 8) % 8;
                        //compression size
                        totalSize += chunk.CompressedSize;
                    }

                    Textures[i].ChunkSize = totalSize;  

                    writer.Write(Textures[i].Flags);
                    writer.Write((uint)Textures[i].Format);
                    writer.Write(Textures[i].MipCount);
                    writer.Write(Textures[i].Width);
                    writer.Write(Textures[i].Height);
                    writer.Write(Textures[i].Depth);
                    writer.Write(0); //offset set later
                    writer.Write(Textures[i].ChunkSize);
                    writer.Write(Textures[i].Color);
                    writer.Write((ushort)chunkIndex);
                    writer.Write((ushort)Textures[i].Chunks.Count);

                    chunkIndex += Textures[i].Chunks.Count;
                }

                var chunk_start = writer.Position;
                foreach (var chunk in Textures.SelectMany(x => x.Chunks))
                {
                    writer.Write(0); //offset set later
                    writer.Write(chunk.Flags);
                    writer.Write(chunk.DecompressedSize);
                    writer.Write(chunk.MiscFlags);
                }

                writer.AlignBytes(16);

                //texture data offset
                writer.WriteUint32Offset(textureHeaderPos + (0 * 32) + 16);

                for (int i = 0; i < compressed.Count; i++)
                {
                    writer.AlignBytes(8);
                    //chunk data offset
                    writer.WriteUint32Offset(chunk_start + (i * 16));
                    writer.Write(compressed[i]);
                }
            }
        }


        public static Dictionary<int, ImageEncoder> FormatList = new Dictionary<int, ImageEncoder>()
        {
            { (int)TextureFormat.R8G8B8A8_UNORM, ImageFormats.Rgba8() },
            { (int)TextureFormat.R8G8_UNORM, ImageFormats.Rg8() },
            { (int)TextureFormat.R8_UNORM, ImageFormats.R8() },
            { (int)TextureFormat.R32G32B32A32_FLOAT, ImageFormats.Rgba32() },
            { (int)TextureFormat.R32G32_FLOAT, ImageFormats.Rg32() },
            { (int)TextureFormat.R32G32_UINT, ImageFormats.Rg32() },
            { (int)TextureFormat.R32_FLOAT, ImageFormats.R32() },
            { (int)TextureFormat.R16_FLOAT, ImageFormats.R16() },
            { (int)TextureFormat.R16_UINT, ImageFormats.R16() },
            { (int)TextureFormat.R16_UNORM, ImageFormats.R16() },
            { (int)TextureFormat.R16G16B16A16_FLOAT, ImageFormats.Rgba16() },
            { (int)TextureFormat.R11G11B10_FLOAT, ImageFormats.R11g11b10() },
            { (int)TextureFormat.R10G10B10A2_UNORM, ImageFormats.Rgb10a2() },
            { (int)TextureFormat.A8_UNORM, ImageFormats.A8() },
            { (int)TextureFormat.BC1_UNORM, ImageFormats.Bc1() },
            { (int)TextureFormat.BC1_UNORM_SRGB, ImageFormats.Bc1(true) },
            { (int)TextureFormat.BC2_UNORM, ImageFormats.Bc2() },
            { (int)TextureFormat.BC2_UNORM_SRGB, ImageFormats.Bc2(true) },
            { (int)TextureFormat.BC3_UNORM, ImageFormats.Bc3() },
            { (int)TextureFormat.BC3_UNORM_SRGB, ImageFormats.Bc3(true) },
            { (int)TextureFormat.BC4_UNORM, ImageFormats.Bc4() },
            { (int)TextureFormat.BC4_SNORM, ImageFormats.Bc4(true) },
            { (int)TextureFormat.BC5_UNORM, ImageFormats.Bc5() },
            { (int)TextureFormat.BC5_SNORM, ImageFormats.Bc5(true) },
            { (int)TextureFormat.BC6H_UF16, ImageFormats.Bc6() },
            { (int)TextureFormat.BC6H_SF16, ImageFormats.Bc6(true) },
            { (int)TextureFormat.BC7_UNORM, ImageFormats.Bc7() },
            { (int)TextureFormat.BC7_UNORM_SRGB, ImageFormats.Bc7(true) },
        };

        public enum TextureFormat
        {
            // 8 bpp
            R8_TYPELESS = 0x10130,
            R8_UNORM = 0x11130,
            A8_UNORM = 0x11131,
            R8_SNORM = 0x13130,
            R8_UINT = 0x14130,
            R8_SINT = 0x15130,

            // 16 bpp
            R16_TYPELESS = 0x20140,
            R16_UNORM = 0x21140,
            R16_SNORM = 0x23140,
            R16_UINT = 0x24140,
            R16_SINT = 0x25140,
            R16_FLOAT = 0x26140,
            D16_UNORM = 0x29140,

            // 8+8 bpp
            R8G8_TYPELESS = 0x30240,
            R8G8_UNORM = 0x31240,
            R8G8_UINT = 0x34240,
            R8G8_SNORM = 0x33240,
            R8G8_SINT = 0x35240,

            // 32 bpp
            R32_TYPELESS = 0x40150,
            R32_UINT = 0x44150,
            R32_SINT = 0x45150,
            R32_FLOAT = 0x46150,
            D32_FLOAT = 0x49150,

            // 16+16 bpp
            R16G16_TYPELESS = 0x50250,
            R16G16_UNORM = 0x51250,
            R16G16_SNORM = 0x53250,
            R16G16_UINT = 0x54250,
            R16G16_SINT = 0x55250,
            R16G16_FLOAT = 0x56250,

            // 11+11+10 bpp
            R11G11B10_FLOAT = 0x76350,

            // 11+10+10+2 bpp
            R10G10B10A2_TYPELESS = 0x80450,
            R10G10B10A2_UNORM = 0x81450,
            R10G10B10A2_UINT = 0x84450,

            // 8+8+8+8 bpp
            R8G8B8A8_TYPELESS = 0xA0450,
            R8G8B8A8_UNORM = 0xA1450,
            R8G8B8A8_UNORM_SRGB = 0xA2450,
            R8G8B8A8_UINT = 0xA4450,
            R8G8B8A8_SNORM = 0xA3450,
            R8G8B8A8_SINT = 0xA5450,

            // 32+32 bpp
            R32G32_TYPELESS = 0xB0260,
            R32G32_FLOAT = 0xB6260,
            R32G32_UINT = 0xB4260,
            R32G32_SINT = 0xB5260,

            // 16+16+16+16 bpp
            R16G16B16A16_TYPELESS = 0xC0460,
            R16G16B16A16_FLOAT = 0xC6460,
            R16G16B16A16_UNORM = 0xC1460,
            R16G16B16A16_UINT = 0xC4460,
            R16G16B16A16_SNORM = 0xC3460,
            R16G16B16A16_SINT = 0xC5460,

            // 32+32+32 bpp
            R32G32B32_TYPELESS = 0xD0380,
            R32G32B32_FLOAT = 0xD6380,
            R32G32B32_UINT = 0xD4380,
            R32G32B32_SINT = 0xD5380,

            // 32+32+32+32 bpp
            R32G32B32A32_TYPELESS = 0xE0470,
            R32G32B32A32_UINT = 0xE4470,
            R32G32B32A32_SINT = 0xE5470,
            R32G32B32A32_FLOAT = 0xE6470,

            // 32+8+24bpp
            R32G8X24_TYPELESS = 0xF0360,
            X32_TYPELESS_G8X24_UINT = 0xF4160,
            R32_FLOAT_X8X24_TYPELESS = 0xF6160,
            D32_FLOAT_S8X24_UINT = 0xF9260,

            // bc1
            BC1_UNORM = 0x107420,
            BC1_UNORM_SRGB = 0x108420,

            // bc2
            BC2_UNORM = 0x117430,
            BC2_UNORM_SRGB = 0x118430,

            // bc3
            BC3_UNORM = 0x127430,
            BC3_UNORM_SRGB = 0x128430,

            // bc4
            BC4_UNORM = 0x137120,
            BC4_SNORM = 0x137121,

            // bc5
            BC5_UNORM = 0x147230,
            BC5_SNORM = 0x147231,

            // bc6
            BC6H_UF16 = 0x157330,
            BC6H_SF16 = 0x157331,

            // bc7
            BC7_UNORM = 0x167430,
            BC7_UNORM_SRGB = 0x168430,
        }
    }
}
