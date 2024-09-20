using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvaloniaToolbox.Core
{
    public class ByteUtil
    {
        public static string ToHexString(byte[] data, bool gap = false)
        {
            StringBuilder hex = new StringBuilder(data.Length * 2);
            for (int i = 0; i < data.Length; i++)
            {
                hex.AppendFormat("{0:x2}", data[i]);
                if (gap && i < data.Length - 1)
                    hex.Append(" ");
            }
            return hex.ToString();
        }

        public static byte[] StringToByteArray(string text)
        {
            text = text.Replace(" ", "");

            // if length is even
            if (text.Length % 2 == 0)
            {
                byte[] result = new byte[text.Length / 2];
                int byteId = 0;
                for (int strId = 0; strId < text.Length; strId += 2)
                {
                    result[byteId] = byte.Parse(text.Substring(strId, 2), System.Globalization.NumberStyles.HexNumber);
                    byteId++;
                }
                return result;
            }
            return new byte[0];
        }

        public static byte[] SubArray(byte[] data, uint offset)
        {
            return new ArraySegment<byte>(data, (int)offset, (int)(data.Length - offset)).ToArray();
        }

        public static byte[] SubArray(byte[] data, int offset, int length)
        {
            return SubArray(data, (uint)offset, (uint)length);
        }

        public static byte[] SubArray(byte[] data, uint offset, uint length)
        {
            return new ArraySegment<byte>(data, (int)offset, (int)length).ToArray();
        }

        public static byte[] CombineByteArray(params byte[][] arrays)
        {
            byte[] rv = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                System.Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }

        static void AlignBytes(BinaryWriter writer, int align, byte pad_val = 0)
        {
            var startPos = writer.BaseStream.Position;
            long position = writer.Seek((int)(-writer.BaseStream.Position % align + align) % align, SeekOrigin.Current);

            writer.Seek((int)startPos, System.IO.SeekOrigin.Begin);
            while (writer.BaseStream.Position != position) {
                writer.Write((byte)pad_val);
            }
        }
    }
}
