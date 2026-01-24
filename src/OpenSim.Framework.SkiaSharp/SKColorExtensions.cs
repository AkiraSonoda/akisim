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
    /// Extension methods for SKColor to provide System.Drawing.Color compatibility
    /// </summary>
    public static class SKColorExtensions
    {
        /// <summary>
        /// Gets the hue-saturation-lightness (HSL) brightness value for this SKColor structure.
        /// Matches System.Drawing.Color.GetBrightness() behavior.
        /// </summary>
        /// <param name="color">The color</param>
        /// <returns>The brightness of this SKColor. The brightness ranges from 0.0 through 1.0, where 0.0 represents black and 1.0 represents white.</returns>
        public static float GetBrightness(this SKColor color)
        {
            float r = color.Red / 255.0f;
            float g = color.Green / 255.0f;
            float b = color.Blue / 255.0f;

            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));

            return (max + min) / 2.0f;
        }

        /// <summary>
        /// Creates an SKColor from a 32-bit ARGB value (0xAARRGGBB format).
        /// Matches System.Drawing.Color.FromArgb(int) behavior.
        /// </summary>
        /// <param name="argb">A value specifying the 32-bit ARGB value</param>
        /// <returns>The SKColor that this method creates</returns>
        public static SKColor FromArgb(int argb)
        {
            return new SKColor((uint)argb);
        }

        /// <summary>
        /// Creates an SKColor from the four ARGB component values.
        /// Matches System.Drawing.Color.FromArgb(int, int, int, int) behavior.
        /// </summary>
        /// <param name="alpha">The alpha component value</param>
        /// <param name="red">The red component value</param>
        /// <param name="green">The green component value</param>
        /// <param name="blue">The blue component value</param>
        /// <returns>The SKColor that this method creates</returns>
        public static SKColor FromArgb(int alpha, int red, int green, int blue)
        {
            return new SKColor((byte)red, (byte)green, (byte)blue, (byte)alpha);
        }

        /// <summary>
        /// Creates an SKColor from the three RGB component values.
        /// Matches System.Drawing.Color.FromArgb(int, int, int) behavior with full opacity.
        /// </summary>
        /// <param name="red">The red component value</param>
        /// <param name="green">The green component value</param>
        /// <param name="blue">The blue component value</param>
        /// <returns>The SKColor that this method creates</returns>
        public static SKColor FromArgb(int red, int green, int blue)
        {
            return new SKColor((byte)red, (byte)green, (byte)blue, 255);
        }

        /// <summary>
        /// Creates an SKColor from the specified SKColor but with the new alpha value.
        /// Matches System.Drawing.Color.FromArgb(int, Color) behavior.
        /// </summary>
        /// <param name="alpha">The alpha value for the new color</param>
        /// <param name="baseColor">The SKColor from which to create the new SKColor</param>
        /// <returns>The SKColor that this method creates</returns>
        public static SKColor FromArgb(int alpha, SKColor baseColor)
        {
            return new SKColor(baseColor.Red, baseColor.Green, baseColor.Blue, (byte)alpha);
        }
    }
}
