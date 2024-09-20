using BCnEncoder.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AvaloniaToolbox.Core.Textures
{
    public class Astc : ImageEncoder, ImageBlockFormat
    {
        public AstcFormat Format { get; }

        public bool IsSRGB = false;
        public uint BitsPerPixel { get; } = 16;
        public uint BlockWidth { get; }
        public uint BlockHeight { get; }
        public uint BlockDepth { get; } = 1;
        public uint BytesPerPixel => BitsPerPixel / 8;

        public Astc(uint x, uint y, bool is_srgb = false)
        {
            Format = Enum.Parse<Astc.AstcFormat>($"ASTC_{x}x{y}");
            IsSRGB = is_srgb;
            BlockWidth = x;
            BlockHeight = y;
        }

        public Astc(AstcFormat format, bool is_srgb = false)
        {
            Format = format;
            IsSRGB = is_srgb;

            // Use Regex to find block width/height/depth by format name
            MatchCollection matches = Regex.Matches(format.ToString(), @"\d+");

            BlockWidth = uint.Parse(matches[0].Value);
            BlockHeight = uint.Parse(matches[1].Value);
            if (matches.Count == 3)
                BlockDepth = uint.Parse(matches[2].Value);
        }

        public uint CalculateSize(int width, int height)
        {
            int blocksWidth = (width + (int)BlockWidth - 1) / (int)BlockWidth;  
            int blocksHeight = (height + (int)BlockHeight - 1) / (int)BlockHeight; 

            int numBlocks = blocksWidth * blocksHeight;
            return (uint)(numBlocks * BitsPerPixel);  
        }

        public override string ToString() {
            return $"{Format}{(IsSRGB ? "_SRGB" : "_UNORM")}";
        }

        public byte[] Decode(byte[] data, uint width, uint height)
        {
            throw new NotImplementedException();
        }

        public byte[] Encode(byte[] data, uint width, uint height)
        {
            throw new NotImplementedException();
        }

        public enum AstcFormat
        {
            ASTC_4x4 = 27,
            ASTC_5x4,
            ASTC_5x5,
            ASTC_6x5,
            ASTC_6x6,
            ASTC_8x5,
            ASTC_8x6,
            ASTC_8x8,
            ASTC_10x5,
            ASTC_10x6,
            ASTC_10x8,
            ASTC_10x10,
            ASTC_12x10,
            ASTC_12x12,

            ASTC_3x3x3,
            ASTC_4x3x3,
            ASTC_4x4x3,
            ASTC_4x4x4,
            ASTC_5x4x4,
            ASTC_5x5x4,
            ASTC_5x5x5,
            ASTC_6x5x5,
            ASTC_6x6x5,
            ASTC_6x6x6,
        }
    }
}
