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
using System.Runtime.InteropServices;

namespace OpenEtch
{
    /// <summary>
    /// This represents a version of an original image that has been converted to a black and white
    /// (1-bit-per-pixel) format that is suitable for etching.
    /// </summary>
    internal class EtchableImage
    {
        /// <summary>
        /// The image's width, in pixels.
        /// </summary>
        public int Width
        {
            get
            {
                return Bitmap.PixelSize.Width;
            }
        }


        /// <summary>
        /// The image's height, in pixels.
        /// </summary>
        public int Height
        {
            get
            {
                return Bitmap.PixelSize.Height;
            }
        }


        /// <summary>
        /// A bitmap containing the processed image data, which can be used
        /// as an image source.
        /// </summary>
        public WriteableBitmap Bitmap { get; }


        /// <summary>
        /// The luminance values of each pixel in the original reference image
        /// </summary>
        private readonly double[,] OriginalLuminanceValues;


        /// <summary>
        /// Creates a new <see cref="EtchableImage"/> instance from the provided source on the filesystem.
        /// Note that this will not actually process the image into an etchable image; you must call
        /// <see cref="ProcessImage(double)"/> with your desired luminance threshold first.
        /// </summary>
        /// <param name="ImagePath">The path on the filesystem of the image to parse</param>
        public EtchableImage(string ImagePath)
        {
            // Load the image from the filesystem
            Image<Rgba32> originalImage = (Image<Rgba32>)Image.Load(ImagePath);

            // Create the bitmap that will hold the processed image
            Bitmap = new WriteableBitmap(
                new PixelSize(originalImage.Width, originalImage.Height),
                new Vector(96, 96), // I don't think this actually does anything
                PixelFormat.Bgra8888);

            // Calculate the grayscale luminance values for each pixel in the original image
            OriginalLuminanceValues = new double[originalImage.Width, originalImage.Height];
            for (int y = 0; y < originalImage.Height; y++)
            {
                for (int x = 0; x < originalImage.Width; x++)
                {
                    Rgba32 pixel = originalImage[x, y];

                    // Convert the pixel to grayscale
                    // See: https://stackoverflow.com/a/17619494
                    double c_linear =
                        0.2126 * (pixel.R / 255.0) +
                        0.7152 * (pixel.G / 255.0) +
                        0.0722 * (pixel.B / 255.0);
                    double c_srgb;
                    if (c_linear <= 0.0031308)
                    {
                        c_srgb = 12.92 * c_linear;
                    }
                    else
                    {
                        c_srgb = 1.055 * Math.Pow(c_linear, 1 / 2.4) - 0.055;
                    }
                    OriginalLuminanceValues[x, y] = c_srgb;
                }
            }
        }


        /// <summary>
        /// Processes an image, converting it from its original form into an etchable black-and-white
        /// (1-bit-per-pixel) form using the specified luminance threshold.
        /// </summary>
        /// <param name="WhiteThreshold">A threshold from 0 to 1; pixels in the original image with a
        /// luminance greater than or equal to this value will be considered white, and pixels with
        /// a luminance less than this will be considered black.</param>
        public void ProcessImage(double WhiteThreshold)
        {
            using (ILockedFramebuffer bitmapBuffer = Bitmap.Lock())
            {
                IntPtr bitmapAddress = bitmapBuffer.Address;

                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        double luminanceValue = OriginalLuminanceValues[x, y];

                        // This is a white pixel
                        if (luminanceValue >= WhiteThreshold)
                        {
                            // Write a white pixel to the bitmap buffer (with alpha 255)
                            Marshal.WriteInt32(bitmapAddress, unchecked((int)0xFFFFFFFF));
                        }
                        else
                        {
                            // Write a black pixel to the bitmap buffer (with alpha 255)
                            Marshal.WriteInt32(bitmapAddress, unchecked((int)0xFF000000));
                        }

                        // Move to the next pixel in the bitmap buffer
                        bitmapAddress += 4;
                    }
                }
            }
        }

    }
}
