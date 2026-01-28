/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.IO;
using SkiaSharp;

namespace OpenSim.Framework.SkiaSharp
{
    /// <summary>
    /// Extension methods for SKBitmap to provide System.Drawing.Bitmap compatibility
    /// </summary>
    public static class SKBitmapExtensions
    {
        /// <summary>
        /// Resizes an image with high quality settings.
        /// Matches Util.ResizeImageSolid() behavior from OpenSim.Framework.
        /// </summary>
        /// <param name="source">Source bitmap to resize</param>
        /// <param name="width">Target width</param>
        /// <param name="height">Target height</param>
        /// <returns>A new resized SKBitmap</returns>
        public static SKBitmap ResizeHighQuality(this SKBitmap source, int width, int height)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            // Manual box filter averaging to avoid aliasing artifacts that cause stripes
            // This is especially important when downscaling terrain textures from 256x256 to 16x16
            var result = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque);

            float xRatio = (float)source.Width / width;
            float yRatio = (float)source.Height / height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Calculate source rectangle for this output pixel
                    int srcX1 = (int)(x * xRatio);
                    int srcY1 = (int)(y * yRatio);
                    int srcX2 = (int)((x + 1) * xRatio);
                    int srcY2 = (int)((y + 1) * yRatio);

                    // Average all pixels in the source rectangle
                    int sumR = 0, sumG = 0, sumB = 0;
                    int count = 0;

                    for (int sy = srcY1; sy < srcY2 && sy < source.Height; sy++)
                    {
                        for (int sx = srcX1; sx < srcX2 && sx < source.Width; sx++)
                        {
                            SKColor pixel = source.GetPixel(sx, sy);
                            sumR += pixel.Red;
                            sumG += pixel.Green;
                            sumB += pixel.Blue;
                            count++;
                        }
                    }

                    if (count > 0)
                    {
                        byte avgR = (byte)(sumR / count);
                        byte avgG = (byte)(sumG / count);
                        byte avgB = (byte)(sumB / count);
                        result.SetPixel(x, y, new SKColor(avgR, avgG, avgB, 255));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Saves the bitmap to a file as PNG format.
        /// Matches bitmap.Save(filename, ImageFormat.Png) behavior.
        /// </summary>
        /// <param name="bitmap">Bitmap to save</param>
        /// <param name="filename">File path to save to</param>
        public static void SaveAsPng(this SKBitmap bitmap, string filename)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentNullException(nameof(filename));

            using (var image = SKImage.FromBitmap(bitmap))
            using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
            using (var stream = File.OpenWrite(filename))
            {
                data.SaveTo(stream);
            }
        }

        /// <summary>
        /// Saves the bitmap to a file as JPEG format with specified quality.
        /// Matches bitmap.Save(filename, ImageFormat.Jpeg) behavior.
        /// </summary>
        /// <param name="bitmap">Bitmap to save</param>
        /// <param name="filename">File path to save to</param>
        /// <param name="quality">JPEG quality (0-100, default 90)</param>
        public static void SaveAsJpeg(this SKBitmap bitmap, string filename, int quality = 90)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentNullException(nameof(filename));

            using (var image = SKImage.FromBitmap(bitmap))
            using (var data = image.Encode(SKEncodedImageFormat.Jpeg, quality))
            using (var stream = File.OpenWrite(filename))
            {
                data.SaveTo(stream);
            }
        }

        /// <summary>
        /// Saves the bitmap to a stream with the specified format.
        /// Matches bitmap.Save(stream, imageFormat) behavior.
        /// </summary>
        /// <param name="bitmap">Bitmap to save</param>
        /// <param name="stream">Stream to save to</param>
        /// <param name="format">Image format</param>
        /// <param name="quality">Quality for lossy formats (0-100, default 90)</param>
        public static void Save(this SKBitmap bitmap, Stream stream, SKEncodedImageFormat format, int quality = 90)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            using (var image = SKImage.FromBitmap(bitmap))
            using (var data = image.Encode(format, quality))
            {
                data.SaveTo(stream);
            }
        }

        /// <summary>
        /// Saves the bitmap to a file with the specified format.
        /// Matches bitmap.Save(filename, imageFormat) behavior.
        /// </summary>
        /// <param name="bitmap">Bitmap to save</param>
        /// <param name="filename">File path to save to</param>
        /// <param name="format">Image format</param>
        /// <param name="quality">Quality for lossy formats (0-100, default 90)</param>
        public static void Save(this SKBitmap bitmap, string filename, SKEncodedImageFormat format, int quality = 90)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentNullException(nameof(filename));

            using (var image = SKImage.FromBitmap(bitmap))
            using (var data = image.Encode(format, quality))
            using (var stream = File.OpenWrite(filename))
            {
                data.SaveTo(stream);
            }
        }
    }
}
