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


using System;
using System.Collections.Generic;
using System.Text;

namespace OpenEtch
{
    /// <summary>
    /// This class constructs a routing path for etching a processed image.
    /// </summary>
    internal class Router
    {
        /// <summary>
        /// Creates a new <see cref="Router"/> instance.
        /// </summary>
        public Router()
        {

        }


        /// <summary>
        /// Creates a route for etching the provided image.
        /// </summary>
        /// <param name="Image">The image to etch</param>
        /// <param name="EnablePreEtchTrace">True if the pre-etch trace preview
        /// should be included, false if it should be ignored.</param>
        /// <returns>A route for etching the provided image</returns>
        public Route Route(ProcessedImage Image)
        {
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
            foreach(Line line in Image.EtchLines)
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
            Route route = new Route(preEtchTrace, etchMoves);
            return route;
        }

    }
}
