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
using System.Collections.Generic;
using SkiaSharp;

namespace OpenSim.Framework.SkiaSharp
{
    /// <summary>
    /// Helper methods for SKColor to provide System.Drawing.Color compatibility
    /// </summary>
    public static class SKColorHelper
    {
        private static readonly Dictionary<string, SKColor> s_namedColors;

        static SKColorHelper()
        {
            // Build color name dictionary matching System.Drawing.Color named colors
            s_namedColors = new Dictionary<string, SKColor>(StringComparer.OrdinalIgnoreCase)
            {
                ["AliceBlue"] = new SKColor(240, 248, 255),
                ["AntiqueWhite"] = new SKColor(250, 235, 215),
                ["Aqua"] = new SKColor(0, 255, 255),
                ["Aquamarine"] = new SKColor(127, 255, 212),
                ["Azure"] = new SKColor(240, 255, 255),
                ["Beige"] = new SKColor(245, 245, 220),
                ["Bisque"] = new SKColor(255, 228, 196),
                ["Black"] = SKColors.Black,
                ["BlanchedAlmond"] = new SKColor(255, 235, 205),
                ["Blue"] = SKColors.Blue,
                ["BlueViolet"] = new SKColor(138, 43, 226),
                ["Brown"] = new SKColor(165, 42, 42),
                ["BurlyWood"] = new SKColor(222, 184, 135),
                ["CadetBlue"] = new SKColor(95, 158, 160),
                ["Chartreuse"] = new SKColor(127, 255, 0),
                ["Chocolate"] = new SKColor(210, 105, 30),
                ["Coral"] = new SKColor(255, 127, 80),
                ["CornflowerBlue"] = new SKColor(100, 149, 237),
                ["Cornsilk"] = new SKColor(255, 248, 220),
                ["Crimson"] = new SKColor(220, 20, 60),
                ["Cyan"] = SKColors.Cyan,
                ["DarkBlue"] = new SKColor(0, 0, 139),
                ["DarkCyan"] = new SKColor(0, 139, 139),
                ["DarkGoldenrod"] = new SKColor(184, 134, 11),
                ["DarkGray"] = new SKColor(169, 169, 169),
                ["DarkGreen"] = new SKColor(0, 100, 0),
                ["DarkKhaki"] = new SKColor(189, 183, 107),
                ["DarkMagenta"] = new SKColor(139, 0, 139),
                ["DarkOliveGreen"] = new SKColor(85, 107, 47),
                ["DarkOrange"] = new SKColor(255, 140, 0),
                ["DarkOrchid"] = new SKColor(153, 50, 204),
                ["DarkRed"] = new SKColor(139, 0, 0),
                ["DarkSalmon"] = new SKColor(233, 150, 122),
                ["DarkSeaGreen"] = new SKColor(143, 188, 143),
                ["DarkSlateBlue"] = new SKColor(72, 61, 139),
                ["DarkSlateGray"] = new SKColor(47, 79, 79),
                ["DarkTurquoise"] = new SKColor(0, 206, 209),
                ["DarkViolet"] = new SKColor(148, 0, 211),
                ["DeepPink"] = new SKColor(255, 20, 147),
                ["DeepSkyBlue"] = new SKColor(0, 191, 255),
                ["DimGray"] = new SKColor(105, 105, 105),
                ["DodgerBlue"] = new SKColor(30, 144, 255),
                ["Firebrick"] = new SKColor(178, 34, 34),
                ["FloralWhite"] = new SKColor(255, 250, 240),
                ["ForestGreen"] = new SKColor(34, 139, 34),
                ["Fuchsia"] = new SKColor(255, 0, 255),
                ["Gainsboro"] = new SKColor(220, 220, 220),
                ["GhostWhite"] = new SKColor(248, 248, 255),
                ["Gold"] = new SKColor(255, 215, 0),
                ["Goldenrod"] = new SKColor(218, 165, 32),
                ["Gray"] = new SKColor(128, 128, 128),
                ["Green"] = SKColors.Green,
                ["GreenYellow"] = new SKColor(173, 255, 47),
                ["Honeydew"] = new SKColor(240, 255, 240),
                ["HotPink"] = new SKColor(255, 105, 180),
                ["IndianRed"] = new SKColor(205, 92, 92),
                ["Indigo"] = new SKColor(75, 0, 130),
                ["Ivory"] = new SKColor(255, 255, 240),
                ["Khaki"] = new SKColor(240, 230, 140),
                ["Lavender"] = new SKColor(230, 230, 250),
                ["LavenderBlush"] = new SKColor(255, 240, 245),
                ["LawnGreen"] = new SKColor(124, 252, 0),
                ["LemonChiffon"] = new SKColor(255, 250, 205),
                ["LightBlue"] = new SKColor(173, 216, 230),
                ["LightCoral"] = new SKColor(240, 128, 128),
                ["LightCyan"] = new SKColor(224, 255, 255),
                ["LightGoldenrodYellow"] = new SKColor(250, 250, 210),
                ["LightGray"] = new SKColor(211, 211, 211),
                ["LightGreen"] = new SKColor(144, 238, 144),
                ["LightPink"] = new SKColor(255, 182, 193),
                ["LightSalmon"] = new SKColor(255, 160, 122),
                ["LightSeaGreen"] = new SKColor(32, 178, 170),
                ["LightSkyBlue"] = new SKColor(135, 206, 250),
                ["LightSlateGray"] = new SKColor(119, 136, 153),
                ["LightSteelBlue"] = new SKColor(176, 196, 222),
                ["LightYellow"] = new SKColor(255, 255, 224),
                ["Lime"] = new SKColor(0, 255, 0),
                ["LimeGreen"] = new SKColor(50, 205, 50),
                ["Linen"] = new SKColor(250, 240, 230),
                ["Magenta"] = SKColors.Magenta,
                ["Maroon"] = new SKColor(128, 0, 0),
                ["MediumAquamarine"] = new SKColor(102, 205, 170),
                ["MediumBlue"] = new SKColor(0, 0, 205),
                ["MediumOrchid"] = new SKColor(186, 85, 211),
                ["MediumPurple"] = new SKColor(147, 112, 219),
                ["MediumSeaGreen"] = new SKColor(60, 179, 113),
                ["MediumSlateBlue"] = new SKColor(123, 104, 238),
                ["MediumSpringGreen"] = new SKColor(0, 250, 154),
                ["MediumTurquoise"] = new SKColor(72, 209, 204),
                ["MediumVioletRed"] = new SKColor(199, 21, 133),
                ["MidnightBlue"] = new SKColor(25, 25, 112),
                ["MintCream"] = new SKColor(245, 255, 250),
                ["MistyRose"] = new SKColor(255, 228, 225),
                ["Moccasin"] = new SKColor(255, 228, 181),
                ["NavajoWhite"] = new SKColor(255, 222, 173),
                ["Navy"] = new SKColor(0, 0, 128),
                ["OldLace"] = new SKColor(253, 245, 230),
                ["Olive"] = new SKColor(128, 128, 0),
                ["OliveDrab"] = new SKColor(107, 142, 35),
                ["Orange"] = new SKColor(255, 165, 0),
                ["OrangeRed"] = new SKColor(255, 69, 0),
                ["Orchid"] = new SKColor(218, 112, 214),
                ["PaleGoldenrod"] = new SKColor(238, 232, 170),
                ["PaleGreen"] = new SKColor(152, 251, 152),
                ["PaleTurquoise"] = new SKColor(175, 238, 238),
                ["PaleVioletRed"] = new SKColor(219, 112, 147),
                ["PapayaWhip"] = new SKColor(255, 239, 213),
                ["PeachPuff"] = new SKColor(255, 218, 185),
                ["Peru"] = new SKColor(205, 133, 63),
                ["Pink"] = new SKColor(255, 192, 203),
                ["Plum"] = new SKColor(221, 160, 221),
                ["PowderBlue"] = new SKColor(176, 224, 230),
                ["Purple"] = new SKColor(128, 0, 128),
                ["Red"] = SKColors.Red,
                ["RosyBrown"] = new SKColor(188, 143, 143),
                ["RoyalBlue"] = new SKColor(65, 105, 225),
                ["SaddleBrown"] = new SKColor(139, 69, 19),
                ["Salmon"] = new SKColor(250, 128, 114),
                ["SandyBrown"] = new SKColor(244, 164, 96),
                ["SeaGreen"] = new SKColor(46, 139, 87),
                ["SeaShell"] = new SKColor(255, 245, 238),
                ["Sienna"] = new SKColor(160, 82, 45),
                ["Silver"] = new SKColor(192, 192, 192),
                ["SkyBlue"] = new SKColor(135, 206, 235),
                ["SlateBlue"] = new SKColor(106, 90, 205),
                ["SlateGray"] = new SKColor(112, 128, 144),
                ["Snow"] = new SKColor(255, 250, 250),
                ["SpringGreen"] = new SKColor(0, 255, 127),
                ["SteelBlue"] = new SKColor(70, 130, 180),
                ["Tan"] = new SKColor(210, 180, 140),
                ["Teal"] = new SKColor(0, 128, 128),
                ["Thistle"] = new SKColor(216, 191, 216),
                ["Tomato"] = new SKColor(255, 99, 71),
                ["Transparent"] = SKColors.Transparent,
                ["Turquoise"] = new SKColor(64, 224, 208),
                ["Violet"] = new SKColor(238, 130, 238),
                ["Wheat"] = new SKColor(245, 222, 179),
                ["White"] = SKColors.White,
                ["WhiteSmoke"] = new SKColor(245, 245, 245),
                ["Yellow"] = SKColors.Yellow,
                ["YellowGreen"] = new SKColor(154, 205, 50)
            };
        }

        /// <summary>
        /// Creates an SKColor from the specified name of a predefined color.
        /// Matches System.Drawing.Color.FromName() behavior.
        /// </summary>
        /// <param name="name">A string that is the name of a predefined color</param>
        /// <returns>The SKColor that this method creates, or Black if the name is not valid</returns>
        public static SKColor FromName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return SKColors.Black;

            return s_namedColors.TryGetValue(name, out var color) ? color : SKColors.Black;
        }
    }
}
