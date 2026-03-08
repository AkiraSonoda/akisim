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
using SkiaSharp;

namespace OpenSim.Framework.SkiaSharp
{
    /// <summary>
    /// Extension methods for SKCanvas to provide System.Drawing.Graphics compatibility
    /// </summary>
    public static class SKCanvasExtensions
    {
        /// <summary>
        /// Draws a string with the specified font and paint at the specified location.
        /// Provides a Graphics.DrawString-like interface.
        /// </summary>
        /// <param name="canvas">The canvas</param>
        /// <param name="text">The text to draw</param>
        /// <param name="font">The font to use</param>
        /// <param name="paint">The paint to use for color and other effects</param>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        public static void DrawString(this SKCanvas canvas, string text, SKFont font, SKPaint paint, float x, float y)
        {
            if (canvas == null)
                throw new ArgumentNullException(nameof(canvas));
            if (font == null)
                throw new ArgumentNullException(nameof(font));
            if (paint == null)
                throw new ArgumentNullException(nameof(paint));

            canvas.DrawText(text ?? string.Empty, x, y, font, paint);
        }

        /// <summary>
        /// Measures the size of the specified string when drawn with the specified font.
        /// Provides a Graphics.MeasureString-like interface.
        /// </summary>
        /// <param name="canvas">The canvas (not actually used but included for API compatibility)</param>
        /// <param name="text">The text to measure</param>
        /// <param name="font">The font to use for measurement</param>
        /// <returns>The size of the text</returns>
        public static SKSize MeasureString(this SKCanvas canvas, string text, SKFont font)
        {
            if (font == null)
                throw new ArgumentNullException(nameof(font));

            var width = font.MeasureText(text ?? string.Empty);
            var metrics = font.Metrics;
            var height = metrics.Descent - metrics.Ascent;
            return new SKSize(width, height);
        }

        /// <summary>
        /// Measures the size of the specified string when drawn with the specified font.
        /// Static version that doesn't require a canvas instance.
        /// </summary>
        /// <param name="text">The text to measure</param>
        /// <param name="font">The font to use for measurement</param>
        /// <returns>The size of the text</returns>
        public static SKSize MeasureText(string text, SKFont font)
        {
            if (font == null)
                throw new ArgumentNullException(nameof(font));

            var width = font.MeasureText(text ?? string.Empty);
            var metrics = font.Metrics;
            var height = metrics.Descent - metrics.Ascent;
            return new SKSize(width, height);
        }
    }
}
