using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvaloniaToolbox.Core.Textures
{
    public interface ImageEncoder
    {
        uint BitsPerPixel { get; }
        uint BytesPerPixel => (BitsPerPixel + 7) / 8;
        byte[] Decode(byte[] data, uint width, uint height);

        byte[] Encode(byte[] data, uint width, uint height);
        uint CalculateSize(int width, int height);
    }
}
