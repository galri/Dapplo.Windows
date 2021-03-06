﻿//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016-2017 Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Windows
// 
//  Dapplo.Windows is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Windows is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#region using

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

#endregion

namespace Dapplo.Windows.Gdi32.Structs
{
    /// <summary>
    /// Specify the color mask when the BITMAPINFOHEADER structure biCompression uses BI_BITFIELDS
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("Sonar Code Smell", "S2292:Trivial properties should be auto-implemented", Justification = "Interop!")]
    [SuppressMessage("ReSharper", "ConvertToAutoProperty")]
    public struct BitfieldColorMask
    {
        private uint _blue;
        private uint _green;
        private uint _red;

        /// <summary>
        /// Blue component of the mask
        /// </summary>
        public uint Blue
        {
            get { return _blue; }
            set { _blue = value; }
        }

        /// <summary>
        /// Green component of the mask
        /// </summary>
        public uint Green
        {
            get { return _green; }
            set { _green = value; }
        }

        /// <summary>
        /// Red component of the mask
        /// </summary>
        public uint Red
        {
            get { return _red; }
            set { _red = value; }
        }


        /// <summary>
        /// Create with BitfieldColorMask defaults
        /// </summary>
        /// <param name="r">byte value for Red component of the mask</param>
        /// <param name="g">byte value for Green component of the mask</param>
        /// <param name="b">byte value for Blue component of the mask</param>
        public static BitfieldColorMask Create(byte r = 255, byte g = 255, byte b = 255)
        {
            return new BitfieldColorMask
            {
                Red = (uint) r << 8,
                Green = (uint) g << 16,
                Blue = (uint) b << 24
            };
        }
    }
}