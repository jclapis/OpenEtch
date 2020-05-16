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
    /// This represents an etching path that the laser etcher will take when
    /// etching one feature of the image.
    /// </summary>
    internal class Path
    {
        /// <summary>
        /// The sequence of points that make up this path
        /// </summary>
        public List<Point> Points { get; }


        /// <summary>
        /// The overall length of the path, in pixel space units.
        /// </summary>
        public double Length { get; }


        /// <summary>
        /// Creates a new <see cref="Path"/> instance.
        /// </summary>
        public Path(List<Point> Points)
        {
            this.Points = Points;

            for(int i = 0; i < Points.Count - 1; i++)
            {
                Point firstPoint = Points[i];
                Point secondPoint = Points[i + 1];
                Length += secondPoint.GetDistance(firstPoint);
            }
        }

    }
}
