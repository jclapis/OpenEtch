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


using System.Collections.Generic;

namespace OpenEtch
{
    /// <summary>
    /// This class represents a single body in an image, which is a collection of
    /// all of the black pixels that are connected (via nearest-neighbor, including
    /// diagonals). 
    /// </summary>
    internal class Body
    {
        /// <summary>
        /// All of the points that make up this body
        /// </summary>
        public List<Point> Points { get; }


        /// <summary>
        /// The points representing the outline (border) of the body, ordered
        /// to form a continous path.
        /// </summary>
        public List<Point> Outline { get; }


        /// <summary>
        /// Creates a new <see cref="Body"/> instance.
        /// </summary>
        /// <param name="Points">All of the points that make up this body</param>
        /// <param name="Outline">The points representing the outline (border) of
        /// the body, ordered to form a continous path.</param>
        public Body(List<Point> Points, List<Point> Outline)
        {
            this.Points = Points;
            this.Outline = Outline;
        }

    }
}
