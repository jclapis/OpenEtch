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
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace OpenEtch
{
    /// <summary>
    /// This class constructs a routing path for the given image in
    /// stencil mode, where it will only cut out the outline of each entity
    /// in an image.
    /// </summary>
    internal class StencilRouter
    {
        /// <summary>
        /// The configuration settings for the program
        /// </summary>
        private readonly Configuration Config;


        /// <summary>
        /// Creates a new <see cref="StencilRouter"/> instance.
        /// </summary>
        /// <param name="Config">The configuration settings for the program</param>
        public StencilRouter(Configuration Config)
        {
            this.Config = Config;
        }


        /// <summary>
        /// Creates an etching route to etch the outlines of all of the bodies in the image,
        /// thus providing a stencil.
        /// </summary>
        /// <param name="Image">The image to etch</param>
        /// <returns>A route for etching a stencil of the provided image</returns>
        public Route Route(EtchableImage Image)
        {
            Point origin = new Point(0, 0);

            // Handle the pre-etch trace first
            Point topRight = new Point(Image.Width - 1, 0);
            Point bottomRight = new Point(Image.Width - 1, Image.Height - 1);
            Point bottomLeft = new Point(0, Image.Height - 1);
            Path preEtchTrace = new Path(
                new List<Point>()
                {
                    origin,
                    topRight,
                    bottomRight,
                    bottomLeft,
                    origin
                });

            // Handle the outline etching
            List<Body> bodies = FindBodies(Image);
            List<Path> outlines = new List<Path>();
            foreach (Body body in bodies)
            {
                outlines.Add(body.Outline);
            }

            // Done!
            Route route = new Route(Config, preEtchTrace, outlines);
            return route;
        }


        /// <summary>
        /// Finds all of the bodies in the provided image (areas of connected black pixels).
        /// </summary>
        /// <param name="Image">The image to find the bodies in</param>
        /// <returns>A collection of bodies in the image</returns>
        public List<Body> FindBodies(EtchableImage Image)
        {
            List<Body> bodies = new List<Body>();
            bool[,] visitedPixels = new bool[Image.Width, Image.Height];

            using(ILockedFramebuffer buffer = Image.Bitmap.Lock())
            {
                IntPtr address = buffer.Address;

                for (int y = 0; y < Image.Height; y++)
                {
                    IntPtr rowOffsetAddress = address + (y * Image.Width * 4);

                    for (int x = 0; x < Image.Width; x++)
                    {
                        // Ignore pixels we've already looked at
                        if (visitedPixels[x, y])
                        {
                            continue;
                        }

                        // Set the flag for this pixel because now we've looked at it
                        visitedPixels[x, y] = true;

                        // Ignore white pixels
                        IntPtr pixelAddress = rowOffsetAddress + (x * 4);

                        // Get the blue channel since that isn't used for path drawing,
                        // so it will always have the correct pixel value of 0 or 255
                        byte pixelValue = Marshal.ReadByte(pixelAddress);
                        if(pixelValue == 0xFF)
                        {
                            continue;
                        }

                        // If we get here, we have a new body!
                        Body body = ProcessBody(buffer, x, y, visitedPixels);
                        bodies.Add(body);
                    }
                }
            }

            return bodies;
        }


        /// <summary>
        /// Creates a new body for the provided pixel, capturing all of the pixels that belong to the same
        /// body in the process.
        /// </summary>
        /// <param name="ImageBuffer">The buffer containing the pixel values of the image being processed</param>
        /// <param name="StartX">The X coordinate of the point to start searching from</param>
        /// <param name="StartY">The Y coordinate of the point to start searching from</param>
        /// <param name="VisitedPixels">The map of pixels that have been processed already</param>
        /// <returns></returns>
        private Body ProcessBody(ILockedFramebuffer ImageBuffer, int StartX, int StartY, bool[,] VisitedPixels)
        {
            // Seed the search with this point
            List<Point> bodyPoints = new List<Point>();
            Queue<Point> pointsToSearch = new Queue<Point>();
            pointsToSearch.Enqueue(new Point(StartX, StartY));

            // Do a breadth-first search over all of the points' neighbors, propagating out
            while(pointsToSearch.Count > 0)
            {
                // Add this point to the body being processed
                Point point = pointsToSearch.Dequeue();
                bodyPoints.Add(point);

                // Create a collection of points representing this pixel's neighbors
                List<Point> neighbors = GetNeighbors(point, ImageBuffer.Size);

                // If the neighboring point is a black pixel that hasn't been visited it,
                // add it to the queue
                foreach(Point neighbor in neighbors)
                {
                    if (CheckIfPointBelongsToBody(ImageBuffer, neighbor, VisitedPixels))
                    {
                        pointsToSearch.Enqueue(neighbor);
                    }
                }
            }

            // Find the outline for the body and return the whole ensemble
            Path outline = GetOutlinePath(ImageBuffer, bodyPoints[0]);
            Body body = new Body(bodyPoints, outline);
            return body;
        }


        /// <summary>
        /// Checks to see if a point should be included in the current body. This will also flag the point as having
        /// been visited.
        /// </summary>
        /// <param name="ImageBuffer">The buffer containing the pixel values of the image being processed</param>
        /// <param name="Point">The point for the pixel to check</param>
        /// <param name="VisitedPixels">The map of pixels that have been processed already</param>
        /// <returns>True if the point has not been seen and is a black pixel, and thus should be included
        /// in the body; false otherwise.</returns>
        private bool CheckIfPointBelongsToBody(ILockedFramebuffer ImageBuffer, Point Point, bool[,] VisitedPixels)
        {
            if(VisitedPixels[Point.X, Point.Y])
            {
                return false;
            }

            VisitedPixels[Point.X, Point.Y] = true;
            byte pixelValue = GetPixelValueAtPoint(ImageBuffer, Point);
            return pixelValue == 0x00;
        }


        /// <summary>
        /// Finds the outline for a body given its starting (top left) pixel, in the form of a continuous path
        /// that's suitable for etching.
        /// </summary>
        /// <param name="ImageBuffer">The buffer containing the pixel values of the image being processed</param>
        /// <param name="StartPoint">The top-left point of the body</returns>
        public Path GetOutlinePath(ILockedFramebuffer ImageBuffer, Point StartPoint)
        {
            List<Point> outline = new List<Point>();
            HashSet<Point> visitedPoints = new HashSet<Point>();

            Point currentPoint = StartPoint;
            List<Point> currentNeighbors = GetNeighbors(currentPoint, ImageBuffer.Size);
            while (true)
            {
                // Add this point to the outline
                outline.Add(currentPoint);
                visitedPoints.Add(currentPoint);

                // Check each neighbor point to find the next one in the outline
                bool foundNextOutlinePoint = false;
                foreach(Point currentNeighbor in currentNeighbors)
                {
                    // If this point is already part of the outline, ignore it
                    if(visitedPoints.Contains(currentNeighbor))
                    {
                        continue;
                    }

                    // If this point is white, ignore it
                    byte currentNeighborValue = GetPixelValueAtPoint(ImageBuffer, currentNeighbor);
                    if(currentNeighborValue == 0xFF)
                    {
                        continue;
                    }

                    // Check if this point has any white pixel neighbors; if it does, it is on the outline and
                    // becomes the next point. Start the search from the current point, going clockwise, so it
                    // will always try to find the outermost point and won't cut through the middle of really thin
                    // bodies.
                    List<Point> nextNeighbors = GetNeighbors(currentNeighbor, ImageBuffer.Size, currentPoint);
                    foreach(Point nextNeighbor in nextNeighbors)
                    {
                        byte nextNeighborValue = GetPixelValueAtPoint(ImageBuffer, nextNeighbor);
                        if (nextNeighborValue == 0xFF)
                        {
                            currentPoint = currentNeighbor;
                            currentNeighbors = nextNeighbors;
                            foundNextOutlinePoint = true;
                            break;
                        }
                    }
                    if (foundNextOutlinePoint)
                    {
                        break;
                    }

                    // Check if this point is at the image boundary; if it is, it's on the outline and
                    // becomes the next point.
                    if (currentNeighbor.Y == 0 ||
                        currentNeighbor.Y == ImageBuffer.Size.Height - 1 ||
                        currentNeighbor.X == 0 ||
                        currentNeighbor.X == ImageBuffer.Size.Width - 1)
                    {
                        currentPoint = currentNeighbor;
                        currentNeighbors = nextNeighbors;
                        foundNextOutlinePoint = true;
                        break;
                    }
                }

                // We couldn't find any more points that belong to the outline, so we're done.
                if(!foundNextOutlinePoint)
                {
                    Path path = new Path(outline);
                    return path;
                }
            }
        }


        /// <summary>
        /// Gets a list of neighboring points for a given point, in clockwise order optionally starting
        /// at a reference neighbor.
        /// </summary>
        /// <param name="Point">The point to get the neighbors of</param>
        /// <param name="ImageSize">The size of the image (for bounds checking)</param>
        /// <param name="StartingNeighbor">The neighbor to start from (defaults to the top middle)</param>
        /// <returns>A list of valid neighboring points.</returns>
        private List<Point> GetNeighbors(Point Point, PixelSize ImageSize, Point? StartingNeighbor = null)
        {
            int up = Point.Y - 1;
            int left = Point.X - 1;
            int right = Point.X + 1;
            int down = Point.Y + 1;
            List<Point> neighbors = new List<Point>();

            // Create the list of neighboring points, starting from the top middle and working clockwise
            if(up >= 0)
            {
                // Top mid
                neighbors.Add(new Point(Point.X, up));

                // Top right
                if(right < ImageSize.Width)
                {
                    neighbors.Add(new Point(right, up));
                }
            }

            // Mid right
            if(right < ImageSize.Width)
            {
                neighbors.Add(new Point(right, Point.Y));
            }

            if(down < ImageSize.Height)
            {
                // Bottom right
                if (right < ImageSize.Width)
                {
                    neighbors.Add(new Point(right, down));
                }

                // Bottom mid
                neighbors.Add(new Point(Point.X, down));

                // Bottom left
                if (left >= 0)
                {
                    neighbors.Add(new Point(left, down));
                }
            }

            if (left >= 0)
            {
                // Mid left
                neighbors.Add(new Point(left, Point.Y));

                // Top left
                if (up >= 0)
                {
                    neighbors.Add(new Point(left, up));
                }
            }

            // Find the starting neighbor in the list, if it exists
            if (StartingNeighbor != null)
            {
                for (int i = 0; i < neighbors.Count; i++)
                {
                    Point neighbor = neighbors[i];
                    if(neighbor.Equals(StartingNeighbor.Value))
                    {
                        // If it exists, reorder the neighbors so that one comes first and preserve the clockwise order.
                        Point[] reorderedNeighbors = new Point[neighbors.Count];
                        for(int j = 0; j < neighbors.Count; j++)
                        {
                            int newIndex = j + i;
                            if(newIndex >= neighbors.Count)
                            {
                                newIndex -= neighbors.Count;
                            }
                            reorderedNeighbors[j] = neighbors[newIndex];
                        }
                        return reorderedNeighbors.ToList();
                    }
                }
            }


            return neighbors;
        }


        /// <summary>
        /// Gets the value of the pixel at the provided point in the image.
        /// </summary>
        /// <param name="ImageBuffer">The buffer containing the pixel values of the image being processed</param>
        /// <param name="Point">The coordinates of the pixel to get</param>
        /// <returns>The value of the pixel, from 0 to 255.</returns>
        private byte GetPixelValueAtPoint(ILockedFramebuffer ImageBuffer, Point Point)
        {
            IntPtr rowOffsetAddress = ImageBuffer.Address + (Point.Y * ImageBuffer.Size.Width * 4);
            IntPtr pixelAddress = rowOffsetAddress + (Point.X * 4);

            // Get the blue channel since that isn't used for path drawing,
            // so it will always have the correct pixel value of 0 or 255
            byte pixelValue = Marshal.ReadByte(pixelAddress);
            return pixelValue;
        }

    }
}
