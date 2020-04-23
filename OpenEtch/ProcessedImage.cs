/* ========================================================================
 * Copyright (C) 2020 Joe Clapis.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * ======================================================================== */


using Avalonia.Media.Imaging;
using System.Collections.Generic;

namespace OpenEtch
{
    /// <summary>
    /// This represents an image that has been processed by an <see cref="ImageProcessor"/>,
    /// converting it to a black-and-white representation and routing it into etch segments.
    /// </summary>
    internal class ProcessedImage
    {
        /// <summary>
        /// The image's width, in pixels.
        /// </summary>
        public int Width { get; }


        /// <summary>
        /// The image's height, in pixels.
        /// </summary>
        public int Height { get; }


        /// <summary>
        /// A bitmap containing the image data, which can be used
        /// as an image source.
        /// </summary>
        public WriteableBitmap Bitmap { get; }


        /// <summary>
        /// The collection of lines and segments that the laser will need
        /// to etch to produce this image.
        /// </summary>
        public List<Line> EtchLines { get; }


        /// <summary>
        /// Creates a new <see cref="ProcessedImage"/> instance.
        /// </summary>
        /// <param name="Width">The image's width, in pixels.</param>
        /// <param name="Height">The image's height, in pixels.</param>
        /// <param name="Bitmap">A bitmap containing the image data, which can be used
        /// as an image source.</param>
        /// <param name="EtchLines">The collection of lines and segments that the laser will need
        /// to etch to produce this image.</param>
        public ProcessedImage(int Width, int Height, WriteableBitmap Bitmap, List<Line> EtchLines)
        {
            this.Width = Width;
            this.Height = Height;
            this.Bitmap = Bitmap;
            this.EtchLines = EtchLines;
        }

    }
}
