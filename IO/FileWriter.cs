using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvaloniaToolbox.Core.IO
{
    public class FileWriter : BinaryWriter
    {
        public long Position => this.BaseStream.Position;

        public FileWriter(Stream stream) : base(stream) { }
        public FileWriter(string path) : base(new FileStream(path, FileMode.Create, FileAccess.Write)) { }

        public void WriteSignature(string signature) => Write(Encoding.ASCII.GetBytes(signature));

        public void WriteString(string str)
        {
            Write(Encoding.UTF8.GetBytes(str));
            Write((byte)0);
        }

        public void SeekBegin(uint Offset) { BaseStream.Seek(Offset, SeekOrigin.Begin); }
        public void SeekBegin(int Offset) { BaseStream.Seek(Offset, SeekOrigin.Begin); }
        public void SeekBegin(long Offset) { BaseStream.Seek(Offset, SeekOrigin.Begin); }

        public void WriteSectionSizeU32(long position, long size)
        {
            using (BaseStream.TemporarySeek(position, System.IO.SeekOrigin.Begin)) {
                Write((uint)(size));
            }
        }

        public void WriteUint32Offset(long target)
        {
            long pos = BaseStream.Position;
            using (BaseStream.TemporarySeek(target, SeekOrigin.Begin))
            {
                Write((uint)pos);
            }
        }

        /// <summary>
        /// Aligns the data by writing bytes (rather than seeking)
        /// </summary>
        /// <param name="alignment"></param>
        /// <param name="value"></param>
        public void AlignBytes(int alignment, byte value = 0x00)
        {
            var startPos = BaseStream.Position;
            long position = Seek((-(int)BaseStream.Position % alignment + alignment) % alignment, SeekOrigin.Current);

            Seek((int)startPos, System.IO.SeekOrigin.Begin);
            while (BaseStream.Position != position)
            {
                Write(value);
            }
        }

        public void WriteStruct<T>(T item) => Write(item.StructToBytes(false));

        public void WriteFixedString(string value, int count)
        {
            var buffer = Encoding.UTF8.GetBytes(value);
            //clamp string
            if (buffer.Length > count)
            {
                buffer = buffer.AsSpan().Slice(0, count).ToArray();
                Console.WriteLine($"Warning! String {value} too long!");
            }

            Write(buffer);
            Write(buffer.Length - count);
        }
    }
}
