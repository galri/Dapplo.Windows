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

using System;
using System.Runtime.InteropServices;
using System.Windows;

#endregion

namespace Dapplo.Windows.Common.Structs
{
    /// <summary>
    ///     This structure should be used everywhere where native methods need a size struct.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    public struct NativeSize
    {
        private int _width;
        private int _height;

        /// <summary>
        ///     The Width of the size struct
        /// </summary>
        public int Width
        {
            get { return _width; }
            set { _width = value; }
        }

        /// <summary>
        ///     The Width of the size struct
        /// </summary>
        public int Height
        {
            get { return _height; }
            set { _height = value; }
        }

        /// <summary>
        ///     Returns an empty size
        /// </summary>
        public static NativeSize Empty { get; } = new NativeSize(0, 0);

        /// <summary>
        ///     Constructor from S.W.Size
        /// </summary>
        /// <param name="size"></param>
        public NativeSize(Size size)
            : this((int) size.Width, (int) size.Height)
        {
        }

        /// <summary>
        ///     Constructor from S.D.Size
        /// </summary>
        /// <param name="size"></param>
        public NativeSize(System.Drawing.Size size) : this(size.Width, size.Height)
        {
        }

        /// <summary>
        ///     Size contructor
        /// </summary>
        /// <param name="width">int</param>
        /// <param name="height">int</param>
        public NativeSize(int width, int height)
        {
            _width = width;
            _height = height;
        }

        /// <summary>
        ///     Checks if the width * height are 0
        /// </summary>
        /// <returns>true if the size is empty</returns>
        public bool IsEmpty => _width * _height == 0;

        /// <summary>
        ///     Implicit cast from NativeSize to Size
        /// </summary>
        /// <param name="size">NativeSize</param>
        public static implicit operator Size(NativeSize size)
        {
            return new Size(size.Width, size.Height);
        }

        /// <summary>
        ///     Implicit cast from Size to NativeSize
        /// </summary>
        /// <param name="size">Size</param>
        public static implicit operator NativeSize(Size size)
        {
            return new NativeSize((int) size.Width, (int) size.Height);
        }

        /// <summary>
        ///     Implicit cast from NativeSize to System.Drawing.Size
        /// </summary>
        /// <param name="size">NativeSize</param>
        public static implicit operator System.Drawing.Size(NativeSize size)
        {
            return new System.Drawing.Size(size.Width, size.Height);
        }

        /// <summary>
        ///     Implicit cast from System.Drawing.Size to NativeSize
        /// </summary>
        /// <param name="size">System.Drawing.Size</param>
        public static implicit operator NativeSize(System.Drawing.Size size)
        {
            return new NativeSize(size.Width, size.Height);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{{Width: {_width}; Height: {_height};}}";
        }
    }
}