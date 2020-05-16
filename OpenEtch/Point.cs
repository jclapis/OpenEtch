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

namespace OpenEtch
{
    /// <summary>
    /// This represents a single point / pixel on an image.
    /// </summary>
    internal struct Point
    {
        /// <summary>
        /// The X coordinate (in pixels)
        /// </summary>
        public int X { get; }


        /// <summary>
        /// The Y coordinate (in pixels)
        /// </summary>
        public int Y { get; }


        /// <summary>
        /// Creates a new <see cref="Point"/> instance.
        /// </summary>
        /// <param name="X">The X coordinate (in pixels)</param>
        /// <param name="Y">The Y coordinate (in pixels)</param>
        public Point(int X, int Y)
        {
            this.X = X;
            this.Y = Y;
        }


        /// <summary>
        /// Gets the distance, in pixel space, from this point to another
        /// point.
        /// </summary>
        /// <param name="OtherPoint">The point to get the distance to</param>
        /// <returns>The distance from this point to the other point</returns>
        public double GetDistance(Point OtherPoint)
        {
            double xDistance = X - OtherPoint.X;
            double yDistance = Y - OtherPoint.Y;
            if (xDistance == 0)
            {
                return Math.Abs(yDistance);
            }
            else if (yDistance == 0)
            {
                return Math.Abs(xDistance);
            }
            else
            {
                return Math.Sqrt(xDistance * xDistance + yDistance * yDistance);
            }
        }


        /// <summary>
        /// Compares two points to see if they have the same coordinates.
        /// </summary>
        /// <param name="Other">The object to compare to this point</param>
        /// <returns>True if <paramref name="Other"/> is a <see cref="Point"/>
        /// with the same X and Y values as this one, otherwise false.</returns>
        public override bool Equals(object Other)
        {
            if(Other is Point otherPoint)
            {
                return X == otherPoint.X &&
                       Y == otherPoint.Y;
            }
            return false;
        }


        /// <summary>
        /// Gets a suitably random hash for this point based on its X and Y coordinates.
        /// </summary>
        /// <returns>A hash for the point</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

    }
}
