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
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OpenEtch
{
    /// <summary>
    /// This class constructs a routing path for raster-etching a processed image.
    /// </summary>
    internal class RasterRouter
    {
        /// <summary>
        /// The configuration settings for the program
        /// </summary>
        private readonly Configuration Config;


        /// <summary>
        /// Creates a new <see cref="RasterRouter"/> instance.
        /// </summary>
        /// <param name="Config">The configuration settings for the program</param>
        public RasterRouter(Configuration Config)
        {
            this.Config = Config;
        }


        /// <summary>
        /// Creates a route for raster-etching the provided image.
        /// </summary>
        /// <param name="Image">The image to etch</param>
        /// <param name="EnablePreEtchTrace">True if the pre-etch trace preview
        /// should be included, false if it should be ignored.</param>
        /// <returns>A route for etching the provided image</returns>
        public Route Route(EtchableImage Image)
        {
            List<Line> etchLines = GetRasterEtchLines(Image);
            Point origin = new Point(0, 0);

            // Handle the pre-etch trace first
            Point topRight = new Point(Image.Width - 1, 0);
            Point bottomRight = new Point(Image.Width - 1, Image.Height - 1);
            Point bottomLeft = new Point(0, Image.Height - 1);
            List<Move> preEtchTrace = new List<Move>()
            {
                new Move(MoveType.Trace, origin, topRight),
                new Move(MoveType.Trace, topRight, bottomRight),
                new Move(MoveType.Trace, bottomRight, bottomLeft),
                new Move(MoveType.Trace, bottomLeft, origin)
            };

            // Handle the image etching
            List<Move> etchMoves = new List<Move>();
            Point lastPoint = origin;
            foreach(Line line in etchLines)
            {
                // Check if the start of the line or the end is closest to the last point
                Point lineStart = new Point(line.Start, line.YIndex);
                Point lineEnd = new Point(line.End, line.YIndex);
                double startDistance = lastPoint.GetDistance(lineStart);
                double endDistance = lastPoint.GetDistance(lineEnd);

                // Go left-to-right
                if(startDistance <= endDistance)
                {
                    for(int i = 0; i < line.Segments.Count; i++)
                    {
                        EtchSegment segment = line.Segments[i];
                        Point etchStart = new Point(segment.Start, line.YIndex);
                        Point etchEnd = new Point(segment.End, line.YIndex);

                        // Travel from the last point to the start of this etch
                        Move travel = new Move(MoveType.Travel, lastPoint, etchStart);
                        etchMoves.Add(travel);

                        // Run the etch
                        Move etch = new Move(MoveType.Etch, etchStart, etchEnd);
                        etchMoves.Add(etch);

                        // Set the last known point to the end of this etch
                        lastPoint = etchEnd;
                    }
                }

                // Go right-to-left
                else
                {
                    for (int i = line.Segments.Count - 1; i >= 0; i--)
                    {
                        EtchSegment segment = line.Segments[i];
                        Point etchStart = new Point(segment.End, line.YIndex);
                        Point etchEnd = new Point(segment.Start, line.YIndex);

                        // Travel from the last point to the start of this etch
                        Move travel = new Move(MoveType.Travel, lastPoint, etchStart);
                        etchMoves.Add(travel);

                        // Run the etch
                        Move etch = new Move(MoveType.Etch, etchStart, etchEnd);
                        etchMoves.Add(etch);

                        // Set the last known point to the end of this etch
                        lastPoint = etchEnd;
                    }
                }
            }

            // Done!
            Route route = new Route(Config, preEtchTrace, etchMoves);
            return route;
        }


        /// <summary>
        /// Breaks the provided image into horizontal raster etch segments.
        /// </summary>
        /// <param name="Image">The image to get the raster etch lines for</param>
        /// <returns>A collection of horizontal lines (each of which can have multiple segments)
        /// necessary to etch the image in raster mode.</returns>
        private List<Line> GetRasterEtchLines(EtchableImage Image)
        {
            List<Line> lines = new List<Line>();
            using (ILockedFramebuffer bitmapBuffer = Image.Bitmap.Lock())
            {
                IntPtr bitmapAddress = bitmapBuffer.Address;

                for (int y = 0; y < Image.Height; y++)
                {
                    Line line = new Line(y);
                    bool isInEtchSegment = false;
                    int etchStart = 0;

                    for (int x = 0; x < Image.Width; x++)
                    {
                        // Get the blue channel - since this is a black and white image, the channel doesn't really matter
                        byte pixelValue = Marshal.ReadByte(bitmapAddress);

                        // This is a white pixel
                        if(pixelValue > 127)
                        {
                            if (isInEtchSegment)
                            {
                                // Create a new etch segment from the recorded start to the previous pixel
                                EtchSegment segment = new EtchSegment(etchStart, x - 1);
                                line.Segments.Add(segment);
                                isInEtchSegment = false;
                            }
                        }

                        // This is a black pixel
                        else
                        {
                            if (!isInEtchSegment)
                            {
                                // Start a new etch segment
                                isInEtchSegment = true;
                                etchStart = x;
                            }

                            // Check if we're at the last pixel in the row but still in an etch segment
                            else if (x == Image.Width - 1)
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
                    if (line.Segments.Count > 0)
                    {
                        lines.Add(line);
                    }
                }
            }

            return lines;
        }

    }
}
