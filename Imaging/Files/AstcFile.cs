using AvaloniaToolbox.Core.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace AvaloniaToolbox.Core.Textures
{
    /// <summary>
    /// Represents an ASTC file binary format.
    /// </summary>
    public class AstcFile
    {
        public const int MagicFileConstant = 0x5CA1AB13;

        /// <summary>
        /// The block width of the format.
        /// </summary>
        public byte BlockDimX;

        /// <summary>
        /// The block height of the format.
        /// </summary>
        public byte BlockDimY;

        /// <summary>
        /// The block depth of the format.
        /// </summary>
        public byte BlockDimZ;

        /// <summary>
        /// The image width.
        /// </summary>
        public uint Width;

        /// <summary>
        /// The image height.
        /// </summary>
        public uint Height;

        /// <summary>
        /// The image deoth.
        /// </summary>
        public uint Depth;

        /// <summary>
        /// The data encoded as ASTC.
        /// </summary>
        public byte[] DataBlock;

        public AstcFile(Astc encoder, uint width, uint height, uint depth, byte[] data)
        {
            BlockDimX = (byte)encoder.BlockWidth;
            BlockDimY = (byte)encoder.BlockHeight;
            BlockDimZ = (byte)encoder.BlockDepth;
            Width = width;
            Height = height;
            Depth = depth;
            DataBlock = data;
        }

        public AstcFile(Stream stream) => Read(new FileReader(stream));

        public void Save(Stream stream) => Write(new FileWriter(stream));
        public void Save(string path) => Write(new FileWriter(path));

        private void Read(FileReader reader)
        {
            var magic = reader.ReadUInt32();
            if (magic != MagicFileConstant)
                throw new Exception($"Invalid ASTC header magic {magic.ToString("X")}. Expected {MagicFileConstant.ToString("X")}");

            BlockDimX = reader.ReadByte();
            BlockDimY = reader.ReadByte();
            BlockDimZ = reader.ReadByte();

            var xsize = reader.ReadBytes(3);
            var ysize = reader.ReadBytes(3);
            var zsize = reader.ReadBytes(3);

            Width = (uint)(xsize[0] + 256 * xsize[1] + 65536 * xsize[2]);
            Height = (uint)(ysize[0] + 256 * ysize[1] + 65536 * ysize[2]);
            Depth = (uint)(zsize[0] + 256 * zsize[1] + 65536 * zsize[2]);

            reader.BaseStream.Seek(0x10, SeekOrigin.Begin);
            DataBlock = reader.ReadBytes((int)(reader.BaseStream.Length - reader.Position));
        }

        private void Write(FileWriter writer)
        {
            writer.Write(MagicFileConstant);
            writer.Write(BlockDimX);
            writer.Write(BlockDimY);
            writer.Write(BlockDimZ);
            writer.Write(IntTo3Bytes((int)Width));
            writer.Write(IntTo3Bytes((int)Height));
            writer.Write(IntTo3Bytes((int)Depth));
            writer.Write(DataBlock);
        }

        private static byte[] IntTo3Bytes(int value)
        {
            byte[] newValue = new byte[3];
            newValue[0] = (byte)(value & 0xFF);
            newValue[1] = (byte)((value >> 8) & 0xFF);
            newValue[2] = (byte)((value >> 16) & 0xFF);
            return newValue;
        }

        public Astc.AstcFormat GetFormat()
        {
            string format = $"ASTC_{this.BlockDimX}x{this.BlockDimY}";
            if (this.BlockDimZ > 1)
                format = $"ASTC_{this.BlockDimX}x{this.BlockDimY}x{this.BlockDimZ}";

            return Enum.Parse<Astc.AstcFormat>(format);
        }
    }
}
