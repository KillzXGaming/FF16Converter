using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AvaloniaToolbox.Core.IO
{
    public class FileReader : BinaryReader
    {
        public Encoding Encoding = Encoding.UTF8;

        public long Position => this.BaseStream.Position;

        public FileReader(string path, bool closed = false)
       : base(new FileStream(path, FileMode.Open, FileAccess.Read), Encoding.UTF8, closed)
        {

        }

        public FileReader(Stream stream, bool closed = false) 
            : base(stream, Encoding.UTF8, closed)
        {

        }

        public string ReadFixedString(int length)
        {
           return this.Encoding.GetString(this.ReadBytes((int)length)).Replace('\0', ' ');
        }

        public string ReadStringZeroTerminated()
        {
            List<char> chars = new List<char>();
            while (true)
            {
                char value = ReadChar();
                if (value == '\0')
                    break;
                chars.Add(value);
            }
            return new string(chars.ToArray());
        }

        public string GetSignature(int length = 4)
        {
            string magic = Encoding.ASCII.GetString(ReadBytes(length));
            this.BaseStream.Position = 0;

            Debug.WriteLine(magic);

            return magic;
        }

        public void ReadSignature(string expected_magic)
        {
            string magic = Encoding.GetString(ReadBytes(expected_magic.Length));
            if (expected_magic != magic)
                throw new Exception($"Expected {expected_magic} but got {magic} instead.");
        }

        public bool CheckSignature(uint expected_magic, long seek_pos = 0)
        {
            var pos = this.BaseStream.Position;

            if (seek_pos != 0 && seek_pos + sizeof(uint) <= this.BaseStream.Length)
                this.BaseStream.Seek(seek_pos, SeekOrigin.Begin);

            uint magic = ReadUInt32();
            this.BaseStream.Position = pos;

            return magic == expected_magic;
        }

        public bool CheckSignature(string expected_magic, long seek_pos = 0)
        {
            var pos = this.BaseStream.Position;

            if (seek_pos != 0 && seek_pos + expected_magic.Length <= this.BaseStream.Length)
                this.BaseStream.Seek(seek_pos, SeekOrigin.Begin);

            string magic = Encoding.GetString(ReadBytes(expected_magic.Length));

            this.BaseStream.Position = pos;

            return magic == expected_magic;
        }


        public void SeekBegin(long offset) => this.BaseStream.Seek(offset, SeekOrigin.Begin);

        //From kuriimu https://github.com/IcySon55/Kuriimu/blob/master/src/Kontract/IO/BinaryReaderX.cs#L40
        public T ReadStruct<T>() => ReadBytes(Marshal.SizeOf<T>()).BytesToStruct<T>();
        public List<T> ReadMultipleStructs<T>(int count) => Enumerable.Range(0, count).Select(_ => ReadStruct<T>()).ToList();
        public List<T> ReadMultipleStructs<T>(uint count) => Enumerable.Range(0, (int)count).Select(_ => ReadStruct<T>()).ToList();
    }
}
