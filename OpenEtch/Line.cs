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
    /// This represents a single horizontal line of an image.
    /// </summary>
    internal class Line
    {
        /// <summary>
        /// The Y index of the line, where 0 is the top line.
        /// </summary>
        public int YIndex { get; }

        /// <summary>
        /// Gets the starting X-coordinate of the line, which is the 
        /// start of the first segment, or -1 if this line has no segments.
        /// </summary>
        public int Start
        {
            get
            {
                if(Segments.Count == 0)
                {
                    return -1;
                }
                return Segments[0].Start;
            }
        }


        /// <summary>
        /// Gets the ending X-coordinate of the line, which is the end
        /// of the last segment, or -1 if this line has no segments.
        /// </summary>
        public int End
        {
            get
            {
                if (Segments.Count == 0)
                {
                    return -1;
                }
                return Segments[^1].End;
            }
        }


        /// <summary>
        /// The etch segments contained within the line
        /// </summary>
        public List<EtchSegment> Segments { get; }


        /// <summary>
        /// Creates a new <see cref="Line"/> instance.
        /// </summary>
        /// <param name="YIndex">The Y index of the line, where 0 is the top line.</param>
        public Line(int YIndex)
        {
            this.YIndex = YIndex;
            Segments = new List<EtchSegment>();
        }
    }
}
