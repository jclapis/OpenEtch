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


using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OpenEtch
{
    /// <summary>
    /// This class processes images, splitting them into lines and etch segments.
    /// </summary>
    internal class ImageProcessor
    {
        /// <summary>
        /// Creates a new <see cref="ImageProcessor"/> instance.
        /// </summary>
        public ImageProcessor()
        {

        }


        /// <summary>
        /// Breaks the provided image into etch segments and sets it as the source of the
        /// provided image control.
        /// </summary>
        /// <param name="ImagePath">The path on the filesystem of the image to parse</param>
        /// <returns>A collection of lines and segments to etch for the image</returns>
        public ProcessedImage ProcessImage(string ImagePath)
        {
            List<Line> lines = new List<Line>();
            Image<Rgba32> image = (Image<Rgba32>)Image.Load(ImagePath);
            WriteableBitmap bitmap = new WriteableBitmap(
                new PixelSize(image.Width, image.Height),
                new Vector(96, 96), // I don't think this actually does anything
                PixelFormat.Bgra8888);

            using (ILockedFramebuffer bitmapBuffer = bitmap.Lock())
            {
                IntPtr bitmapAddress = bitmapBuffer.Address;

                for(int y = 0; y < image.Height; y++)
                {
                    Line line = new Line(y);
                    bool isInEtchSegment = false;
                    int etchStart = 0;

                    for(int x = 0; x < image.Width; x++)
                    {
                        Rgba32 pixel = image[x, y];

                        // Convert the pixel to grayscale
                        // See: https://stackoverflow.com/a/17619494
                        double c_linear = 
                            0.2126 * (pixel.R / 255.0) + 
                            0.7152 * (pixel.G / 255.0) + 
                            0.0722 * (pixel.B / 255.0);
                        double c_srgb;
                        if(c_linear <= 0.0031308)
                        {
                            c_srgb = 12.92 * c_linear;
                        }
                        else
                        {
                            c_srgb = 1.055 * Math.Pow(c_linear, 1 / 2.4) - 0.055;
                        }

                        // Pixels that are 0.5 or higher are considered white.
                        // TODO: should I make a slider that controls this cutoff level?
                        if(c_srgb > 0.5)
                        {
                            // Write a white pixel to the bitmap buffer
                            Marshal.WriteInt32(bitmapAddress, unchecked((int)0xFFFFFFFF));
                            if(isInEtchSegment)
                            {
                                // Create a new etch segment from the recorded start to the previous pixel
                                EtchSegment segment = new EtchSegment(etchStart, x - 1);
                                line.Segments.Add(segment);
                                isInEtchSegment = false;
                            }
                        }
                        else
                        {
                            // Write a black pixel to the bitmap buffer (with alpha 255)
                            Marshal.WriteInt32(bitmapAddress, unchecked((int)0xFF000000));
                            if(!isInEtchSegment)
                            {
                                // Start a new etch segment
                                isInEtchSegment = true;
                                etchStart = x;
                            }

                            // Check if we're at the last pixel in the row but still in an etch segment
                            else if(x == image.Width - 1)
                            {
                                EtchSegment segment = new EtchSegment(etchStart, x);
                                line.Segments.Add(segment);
                                isInEtchSegment = false;
                            }
                        }

                        // Move to the next pixel in the bitmap buffer
                        bitmapAddress += 4;
                    }

                    // Ignore lines with no etch segments
                    if(line.Segments.Count > 0)
                    {
                        lines.Add(line);
                    }
                }
            }

            ProcessedImage processedImage = new ProcessedImage(
                image.Width, image.Height, bitmap, lines);
            return processedImage;
        }

    }
}
