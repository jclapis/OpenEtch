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


namespace OpenEtch
{
    /// <summary>
    /// This represents a single move of the laser etcher.
    /// </summary>
    internal class Move
    {
        /// <summary>
        /// The type of this move
        /// </summary>
        public MoveType Type { get; }


        /// <summary>
        /// The starting point of the move
        /// </summary>
        public Point Start { get; }


        /// <summary>
        /// The end point of the move
        /// </summary>
        public Point End { get; }


        /// <summary>
        /// The overall length of the move, in pixel space units.
        /// </summary>
        public double Length { get; }


        /// <summary>
        /// Creates a new <see cref="Move"/> instance.
        /// </summary>
        /// <param name="Type">The type of this move</param>
        /// <param name="Start">The starting point of the move</param>
        /// <param name="End">The end point of the move</param>
        public Move(MoveType Type, Point Start, Point End)
        {
            this.Type = Type;
            this.Start = Start;
            this.End = End;

            Length = Start.GetDistance(End);
        }

    }
}
