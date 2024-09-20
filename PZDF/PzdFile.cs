using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using AvaloniaToolbox.Core.IO;

namespace CafeLibrary
{
    public class PzdFile
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Header
        {
            public Magic Magic = "PZDF";
            public uint Version = 2;
            public uint Padding1;
            public uint Padding2;
            public uint Padding3;
            public uint Padding4;
            public uint Padding5;
            public uint Padding6;

            public uint TextContentOffset;
            public uint TextContentCount;

            public uint Padding7;
            public uint Padding8;
        }

        public class TextContent
        {
            [XmlAttribute]
            public uint ID;

            [XmlAttribute]
            public uint Unknown1;
            [XmlAttribute]
            public uint Unknown2;
            [XmlAttribute]
            public uint Unknown3;
            [XmlAttribute]
            public uint Unknown4;

            public string Message;
            public string Voice;
            public string String;
        }

        public class NexSerialization
        {
            public string Struct;
            public uint Size;
        }

        public List<TextContent> TextContents = new List<TextContent>();
        public List<NexSerialization> Serialization = new List<NexSerialization>();

        public Header PzdHeader = new Header();

        public PzdFile() { }

        public PzdFile(Stream stream)
        {
            Read(stream);
        }

        public PzdFile(string path)
        {
            Read(File.OpenRead(path));
        }

        private void Read(Stream stream)
        {
            using (var reader = new FileReader(stream))
            {
                PzdHeader = reader.ReadStruct<Header>();
                reader.SeekBegin(PzdHeader.TextContentOffset);
                for (int i = 0; i < PzdHeader.TextContentCount; i++)
                {
                    var startpos = reader.Position;
                    TextContents.Add(new TextContent()
                    {
                        ID = reader.ReadUInt32(),
                        Message = GetString(reader, startpos),
                        Unknown1 = reader.ReadUInt32(),
                        Unknown2 = reader.ReadUInt32(),
                        Voice = GetString(reader, startpos),
                        Unknown3 = reader.ReadUInt32(),
                        String = GetString(reader, startpos),
                        Unknown4 = reader.ReadUInt32(),
                    });
                }

                {
                    var pos = reader.Position;

                    uint serializeOffset = reader.ReadUInt32();
                    uint serializeCount = reader.ReadUInt32();
                    reader.ReadUInt32(); //0
                    reader.ReadUInt32(); //0

                    reader.ReadUInt32(); //0
                    reader.ReadUInt32(); //0
                    reader.ReadUInt32(); //0
                    reader.ReadUInt32(); //0

                    reader.SeekBegin(pos + serializeOffset);
                    for (int i = 0; i < serializeCount; i++)
                    {
                        var startpos = reader.Position;
                        Serialization.Add(new NexSerialization()
                        {
                            Struct = GetString(reader, startpos),
                            Size = reader.ReadUInt32(),
                        });
                    }
                }
            }
        }

        private string GetString(FileReader reader, long pos_start)
        {
            var offset = reader.ReadUInt32() + pos_start;
            using (reader.BaseStream.TemporarySeek(offset, SeekOrigin.Begin)) {
                return reader.ReadStringZeroTerminated(); 
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
            PzdHeader.TextContentCount = (uint)this.TextContents.Count;
            PzdHeader.Magic = "PZDF";

            Dictionary<string, List<(long, long)>> savedStrings = new Dictionary<string, List<(long, long)>>();

            using (var writer = new FileWriter(stream))
            {
                void SaveString(string str, long relative_pos)
                {
                    if (!savedStrings.ContainsKey(str))
                        savedStrings.Add(str, new List<(long, long)>());

                    savedStrings[str].Add((writer.Position, relative_pos));
                    writer.Write(0);
                }


                writer.WriteStruct(PzdHeader);
                writer.SeekBegin(PzdHeader.TextContentOffset);
                for (int i = 0; i < TextContents.Count; i++)
                {
                    var relative_pos = writer.Position;

                    writer.Write(TextContents[i].ID);
                    SaveString(TextContents[i].Message, relative_pos);
                    writer.Write(TextContents[i].Unknown1);
                    writer.Write(TextContents[i].Unknown2);
                    SaveString(TextContents[i].Voice, relative_pos);
                    writer.Write(TextContents[i].Unknown3);
                    SaveString(TextContents[i].String, relative_pos);
                    writer.Write(TextContents[i].Unknown4);
                }

                var serialize_pos = writer.Position;

                writer.Write(32); //serialize offset
                writer.Write(this.Serialization.Count); //4
                for (int i = 0; i < 2 + 4; i++)
                    writer.Write(0u);

                for (int i = 0; i < Serialization.Count; i++)
                {
                    var relative_pos = writer.Position;

                    SaveString(Serialization[i].Struct, relative_pos);
                    writer.Write(Serialization[i].Size);
                }

                writer.AlignBytes(16);
                foreach (var str in savedStrings)
                {
                    var target = writer.BaseStream.Position;

                    foreach (var ofs in str.Value)
                    {
                        var relative_pos = ofs.Item2;
                        var abs_ofs = ofs.Item1;

                        using (writer.BaseStream.TemporarySeek(abs_ofs, SeekOrigin.Begin)) {
                            writer.Write((uint)(target - relative_pos));
                        }
                    }

                    writer.Write(Encoding.UTF8.GetBytes(str.Key));
                    writer.Write((byte)0);
                }
                writer.AlignBytes(4);

                var sect_ofs = serialize_pos - writer.BaseStream.Position;

                writer.Write(Encoding.ASCII.GetBytes("BVLD"));
                writer.Write(1);
                writer.Write((int)sect_ofs); //offset to last chunk
                writer.Write(0);
            }
        }

        public string ToXml()
        {
            using (var writer = new System.IO.StringWriter())
            {
                var serializer = new XmlSerializer(typeof(PzdFile));
                serializer.Serialize(writer, this);
                writer.Flush();
                return writer.ToString();
            }
        }

        public void FromXML(string xml)
        {
            var xmlSerializer = new XmlSerializer(typeof(PzdFile));
            using (var stringReader = new StringReader(xml))
            {
                PzdFile ob = (PzdFile)xmlSerializer.Deserialize(stringReader);
                this.TextContents = ob.TextContents;
                this.PzdHeader = ob.PzdHeader;
                this.Serialization = ob.Serialization;
            }
        }
    }
}
