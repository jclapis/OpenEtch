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

namespace OpenEtch
{
    /// <summary>
    /// This represents a complete etching route for an image.
    /// </summary>
    internal class Route
    {
        /// <summary>
        /// The list of moves for the pre-etch preview trace around the
        /// target boundary if enabled, or null if disabled.
        /// </summary>
        public List<Move> PreEtchTrace { get; }


        /// <summary>
        /// The list of moves involved in etching the target
        /// </summary>
        public List<Move> EtchMoves { get; }


        /// <summary>
        /// Creates a new <see cref="Route"/> instance.
        /// </summary>
        /// <param name="PreEtchTrace">The list of moves for the pre-etch preview trace around the
        /// target boundary if enabled, or null if disabled.</param>
        /// <param name="EtchMoves">The list of moves involved in etching the target</param>
        public Route(List<Move> PreEtchTrace, List<Move> EtchMoves)
        {
            this.PreEtchTrace = PreEtchTrace;
            this.EtchMoves = EtchMoves;
        }


        /// <summary>
        /// Estimates the amount of time a route will take to etch.
        /// </summary>
        /// <param name="PixelSize">The size of each pixel, in millimeters</param>
        /// <param name="TravelSpeed">The travel speed setting, in millimeters per minute</param>
        /// <param name="EtchSpeed">The etch speed setting, in millimeters per minute</param>
        /// <param name="IncludeTrace">True to include the pre-etch trace preview in the 
        /// estimates, false to ignore it.</param>
        /// <param name="TraceStartPause">The number of milliseconds to wait before starting the 
        /// pre-etch boundary preview trace</param>
        /// <param name="TraceEndPause">The number of milliseconds to wait after finishing the
        /// pre-etch boundary preview trace</param>
        /// <returns>An estimate of the total etching time and total travel distance for this route</returns>
        public (TimeSpan, double) EstimateTimeAndDistance(double PixelSize, double TravelSpeed, double EtchSpeed, 
            bool IncludeTrace, double TraceStartPause, double TraceEndPause)
        {
            // Convert the speeds to mm per millisecond
            double travelSpeed_MmPerMs = TravelSpeed / 60000.0;
            double etchSpeed_MmPerMs = EtchSpeed / 60000.0;
            double traceSpeed_MmPerMs = travelSpeed_MmPerMs;
            double totalMilliseconds = TraceStartPause + TraceEndPause;
            double totalDistance = 0;

            // Calculate the pre-etch trace preview
            if (IncludeTrace)
            {
                foreach (Move move in PreEtchTrace)
                {
                    double length = move.Length * PixelSize;
                    double time = length / traceSpeed_MmPerMs;
                    totalDistance += length;
                    totalMilliseconds += time;
                }
            }

            // Calculate each move in the main etch sequence
            foreach(Move move in EtchMoves)
            {
                double length = move.Length * PixelSize;
                double time = 0;
                if (move.Type == MoveType.Travel)
                {
                    time = length / travelSpeed_MmPerMs;
                }
                else if(move.Type == MoveType.Etch)
                {
                    time = length / etchSpeed_MmPerMs;
                }
                totalDistance += length;
                totalMilliseconds += time;
            }

            // Get the total estimate as a TimeSpan
            int roundedMilliseconds = (int)Math.Round(totalMilliseconds);
            TimeSpan runtimeEstimate = new TimeSpan(0, 0, 0, 0, roundedMilliseconds);
            return (runtimeEstimate, totalDistance);
        }

    }
}
