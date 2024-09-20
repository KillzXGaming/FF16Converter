using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace AvaloniaToolbox.Core
{
    public class ImageSharpTextureHelper
    {
        /// <summary>
        /// Exports a uncompressed rgba8 image given the file path, data, width and height
        /// </summary>
        public static void ExportFile(string filePath, byte[] data, int width, int height)
        {
            var file = Image.LoadPixelData<Rgba32>(data, width, height);
            file.Save(filePath);
        }

        /// <summary>
        /// Resizes the given image with a new width and height using the Lanczos3 resampler algoritim.
        /// </summary>
        public static void Resize(Image<Rgba32> baseImage, int newWidth, int newHeight) {
            baseImage.Mutate(context => context.Resize(newWidth, newHeight, KnownResamplers.Lanczos3));
        }

        /// <summary>
        /// Generates mipmaps with the given mipmap count from the image provided.
        /// </summary>
        public static Image<Rgba32>[] GenerateMipmaps(Image<Rgba32> baseImage, uint mipLevelCount)
        {
            Image<Rgba32>[] mipLevels = new Image<Rgba32>[mipLevelCount];
            mipLevels[0] = baseImage;
            int i = 1;

            int currentWidth = baseImage.Width;
            int currentHeight = baseImage.Height;
            while ((currentWidth != 1 || currentHeight != 1) && i < mipLevelCount)
            {
                int newWidth = Math.Max(1, currentWidth / 2);
                int newHeight = Math.Max(1, currentHeight / 2);
                Image<Rgba32> newImage = baseImage.Clone(context => context.Resize(newWidth, newHeight, KnownResamplers.Lanczos3));
                Debug.Assert(i < mipLevelCount);
                mipLevels[i] = newImage;

                i++;
                currentWidth = newWidth;
                currentHeight = newHeight;
            }

            Debug.Assert(i == mipLevelCount);

            return mipLevels;
        }
    }
}
