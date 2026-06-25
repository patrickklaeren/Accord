using System.IO;
using System.Numerics;
using ImageMagick;

namespace Accord.Services;

internal static class ImageSimilarityService
{
    internal static ulong ComputeDHash(Stream stream)
    {
        using var image = new MagickImage(stream);

        image.AutoOrient();
        image.ColorSpace = ColorSpace.Gray;
        image.Resize(new MagickGeometry(9, 8)
        {
            IgnoreAspectRatio = true
        });

        using var pixels = image.GetPixels();

        ulong hash = 0;
        var bit = 0;

        for (var y = 0; y < 8; y++)
        {
            for (var x = 0; x < 8; x++)
            {
                var left = GetGray(pixels, x, y);
                var right = GetGray(pixels, x + 1, y);

                if (left > right)
                {
                    hash |= 1UL << bit;
                }

                bit++;
            }
        }

        return hash;
    }
    
    /// <summary>
    /// Calculates the Hamming distance between two 64-bit perceptual image hashes.
    /// </summary>
    /// <param name="first">The first image hash.</param>
    /// <param name="second">The second image hash.</param>
    /// <returns>
    /// The number of differing bits between the two hashes, from 0 to 64.
    /// A lower value means the images are more visually similar.
    /// <para>
    /// Typical dHash interpretation:
    /// 0 = identical hash;
    /// 1-5 = near duplicate;
    /// 6-10 = likely the same image with minor changes;
    /// 11-20 = possibly visually similar;
    /// greater than 20 = usually different.
    /// </para>
    /// Thresholds should be tuned against real images from the application's domain.
    /// </returns>
    internal static int Distance(ulong first, ulong second) =>
        BitOperations.PopCount(first ^ second);

    private static byte GetGray(IPixelCollection<byte> pixels, int x, int y) =>
        pixels.GetPixel(x, y).GetChannel(0);
}