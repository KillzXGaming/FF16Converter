using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AvaloniaToolbox.Core.IO
{
    public static class IOExtensions
    {
        public readonly ref struct TemporarySeekHandle
        {
            private readonly Stream Stream;
            private readonly long RetPos;

            public TemporarySeekHandle(Stream stream, long retpos)
            {
                this.Stream = stream;
                this.RetPos = retpos;
            }

            public readonly void Dispose()
            {
                Stream.Seek(RetPos, SeekOrigin.Begin);
            }
        }

        public static TemporarySeekHandle TemporarySeek(this Stream stream, long offset, SeekOrigin origin)
        {
            long ret = stream.Position;
            stream.Seek(offset, origin);
            return new TemporarySeekHandle(stream, ret);
        }

        //Structs can be a bit faster and more memory efficent
        //From https://github.com/IcySon55/Kuriimu/blob/master/src/Kontract/IO/Extensions.cs
        //Read
        public static unsafe T BytesToStruct<T>(this byte[] buffer, bool isBigEndian = false, int offset = 0)
        {
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));

            AdjustBigEndianByteOrder(typeof(T), buffer, isBigEndian);

            fixed (byte* pBuffer = buffer)
                return Marshal.PtrToStructure<T>((IntPtr)pBuffer + offset);
        }

        // Write
        public static unsafe byte[] StructToBytes<T>(this T item, bool isBigEndian)
        {
            var buffer = new byte[Marshal.SizeOf(typeof(T))];

            fixed (byte* pBuffer = buffer)
                Marshal.StructureToPtr(item, (IntPtr)pBuffer, false);

            AdjustBigEndianByteOrder(typeof(T), buffer, isBigEndian);

            return buffer;
        }

        //Adjust byte order for big endian
        private static void AdjustBigEndianByteOrder(Type type, byte[] buffer, bool isBigEndian, int startOffset = 0)
        {
            if (!isBigEndian)
                return;

            if (type.IsPrimitive)
            {
                if (type == typeof(short) || type == typeof(ushort) ||
                 type == typeof(int) || type == typeof(uint) ||
                 type == typeof(long) || type == typeof(ulong) ||
                  type == typeof(double) || type == typeof(float))
                {
                    Array.Reverse(buffer);
                    return;
                }
            }

            foreach (var field in type.GetFields())
            {
                var fieldType = field.FieldType;

                // Ignore static fields
                if (field.IsStatic) continue;

                if (fieldType.BaseType == typeof(Enum))
                    fieldType = fieldType.GetFields()[0].FieldType;

                var offset = Marshal.OffsetOf(type, field.Name).ToInt32();
                // Enums
                if (fieldType.IsEnum)
                    fieldType = Enum.GetUnderlyingType(fieldType);

                // Check for sub-fields to recurse if necessary
                var subFields = fieldType.GetFields().Where(subField => subField.IsStatic == false).ToArray();
                var effectiveOffset = startOffset + offset;

                if (fieldType == typeof(short) || fieldType == typeof(ushort) ||
                    fieldType == typeof(int) || fieldType == typeof(uint) ||
                    fieldType == typeof(long) || fieldType == typeof(ulong) ||
                    fieldType == typeof(double) || fieldType == typeof(float))
                {
                    if (subFields.Length == 0)
                        Array.Reverse(buffer, effectiveOffset, Marshal.SizeOf(fieldType));

                }

                if (subFields.Length > 0)
                    AdjustBigEndianByteOrder(fieldType, buffer, isBigEndian, effectiveOffset);
            }
        }
    }
}
