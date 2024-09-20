using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvaloniaToolbox.Core.Textures
{
    public class ImageFormats
    {
        public static ImageEncoder Rgb10a2(bool srgb = false) => new Rgba(10, 10, 10, 2) { IsSRGB = srgb };
        public static ImageEncoder R11g11b10() => new R11G11B10();
        public static ImageEncoder Rgba32(bool srgb = false) => new Rgba(32, 32, 32, 32) { IsSRGB = srgb };
        public static ImageEncoder Rgba16(bool srgb = false) => new Rgba(16, 16, 16, 16) { IsSRGB = srgb };
        public static ImageEncoder R32(bool srgb = false) => new Rgba(32) { IsSRGB = srgb, ChannelOrder = "RRR1", IsLuminance = true };
        public static ImageEncoder Rg32(bool srgb = false) => new Rgba(32, 32) { IsSRGB = srgb };
        public static ImageEncoder R16(bool srgb = false) => new Rgba(16) { IsSRGB = srgb };
        public static ImageEncoder Rg16(bool srgb = false) => new Rgba(16, 16) { IsSRGB = srgb };
        public static ImageEncoder Rgba8(bool srgb = false) => new Rgba(8, 8, 8, 8) { IsSRGB = srgb };
        public static ImageEncoder Rgb8(bool srgb = false) => new Rgba(8, 8, 8) { IsSRGB = srgb };
        public static ImageEncoder Rg8() => new Rgba(8, 8);
        public static ImageEncoder Rg8Signed() => new Rgba(8, 8) { IsSigned = true, };
        public static ImageEncoder R8() => new Rgba(8) { ChannelOrder = "RRR1", IsLuminance = true };
        public static ImageEncoder A8() => new Rgba(8);
        public static ImageEncoder A4() => new Rgba(4);
        public static ImageEncoder R4() => new Rgba(4);
        public static ImageEncoder Rgba4() => new Rgba(4, 4, 4, 4);
        public static ImageEncoder Rg4() => new Rgba(4, 4);
        public static ImageEncoder Rgba565(bool srgb = false) => new Rgba(5, 6, 5) { IsSRGB = srgb, };
        public static ImageEncoder Rgba5551() => new Rgba(5, 5, 5, 1);

        //Reverse order
        public static ImageEncoder Bgra4(bool srgb = false) => new Rgba(4, 4, 4, 4) { IsSRGB = srgb, ChannelOrder = "BGRA" };
        public static ImageEncoder Bgra565(bool srgb = false) => new Rgba(5, 6, 5) { IsSRGB = srgb, ChannelOrder = "BGRA" };
        public static ImageEncoder Bgra8(bool srgb = false) => new Rgba(8, 8, 8, 8) { IsSRGB = srgb, ChannelOrder = "BGRA" };
        public static ImageEncoder Bgr8(bool srgb = false) => new Rgba(8, 8, 8) { IsSRGB = srgb, ChannelOrder = "BGRA" };
        public static ImageEncoder Bgra5551() => new Rgba(5, 5, 5, 1) { ChannelOrder = "BGRA" };


        public static ImageEncoder La4() => new LA4();
        public static ImageEncoder L4() => new L4();
        public static ImageEncoder La8() => new LA8();
        public static ImageEncoder L8() => new L8();
        public static ImageEncoder Bc1(bool srgb = false) => new Bcn(BcnFormats.BC1, srgb);
        public static ImageEncoder Bc2(bool srgb = false) => new Bcn(BcnFormats.BC2, srgb);
        public static ImageEncoder Bc3(bool srgb = false) => new Bcn(BcnFormats.BC3, srgb);
        public static ImageEncoder Bc4(bool snorm = false) => new Bcn(BcnFormats.BC4, snorm);
        public static ImageEncoder Bc4A(bool snorm = false) => new Bcn(BcnFormats.BC4, snorm) { IsAlpha = true };
        public static ImageEncoder Bc5(bool snorm = false) => new Bcn(BcnFormats.BC5, snorm);
        public static ImageEncoder Bc5A(bool snorm = false) => new Bcn(BcnFormats.BC5, snorm) { IsAlpha = true };
        public static ImageEncoder Bc6(bool signed = false) => new Bcn(BcnFormats.BC6, signed);
        public static ImageEncoder Bc7(bool srgb = false) => new Bcn(BcnFormats.BC7, srgb);

        public static ImageEncoder ETC1() => new L8();
        public static ImageEncoder ETC1A4() => new L8();

        public static ImageEncoder Astc4x4(bool srgb = false) => new Astc(Astc.AstcFormat.ASTC_4x4, srgb);
        public static ImageEncoder Astc5x4(bool srgb = false) => new Astc(Astc.AstcFormat.ASTC_5x4, srgb);
        public static ImageEncoder Astc5x5(bool srgb = false) => new Astc(Astc.AstcFormat.ASTC_5x5, srgb);
        public static ImageEncoder Astc6x5(bool srgb = false) => new Astc(Astc.AstcFormat.ASTC_6x5, srgb);
        public static ImageEncoder Astc6x6(bool srgb = false) => new Astc(Astc.AstcFormat.ASTC_6x6, srgb);
        public static ImageEncoder Astc8x5(bool srgb = false) => new Astc(Astc.AstcFormat.ASTC_8x5, srgb);
        public static ImageEncoder Astc8x6(bool srgb = false) => new Astc(Astc.AstcFormat.ASTC_8x6, srgb);
        public static ImageEncoder Astc8x8(bool srgb = false) => new Astc(Astc.AstcFormat.ASTC_8x8, srgb);
        public static ImageEncoder Astc10x5(bool srgb = false) => new Astc(Astc.AstcFormat.ASTC_10x5, srgb);
        public static ImageEncoder Astc10x6(bool srgb = false) => new Astc(Astc.AstcFormat.ASTC_10x6, srgb);
        public static ImageEncoder Astc10x8(bool srgb = false) => new Astc(Astc.AstcFormat.ASTC_10x8, srgb);
        public static ImageEncoder Astc10x10(bool srgb = false) => new Astc(Astc.AstcFormat.ASTC_10x10, srgb);
        public static ImageEncoder Astc12x10(bool srgb = false) => new Astc(Astc.AstcFormat.ASTC_12x10, srgb);
        public static ImageEncoder Astc12x12(bool srgb = false) => new Astc(Astc.AstcFormat.ASTC_12x12, srgb);

        public static uint GetBlockWidth(ImageEncoder format)
        {
            if (format is ImageBlockFormat blockFormat) return blockFormat.BlockWidth;
            return 1;
        }

        public static uint GetBlockHeight(ImageEncoder format)
        {
            if (format is ImageBlockFormat blockFormat) return blockFormat.BlockHeight;
            return 1;
        }

        public static uint GetBlockDepth(ImageEncoder format)
        {
            if (format is ImageBlockFormat blockFormat) return blockFormat.BlockDepth;
            return 1;
        }
    }
}
