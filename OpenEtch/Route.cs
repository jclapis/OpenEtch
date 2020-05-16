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
        /// The configuration settings for the program
        /// </summary>
        private readonly Configuration Config;

        /// <summary>
        /// The path for the pre-etch preview trace around the
        /// target boundary if enabled, or null if disabled.
        /// </summary>
        public Path PreEtchTrace { get; }


        /// <summary>
        /// The list of paths involved in etching the target
        /// </summary>
        public List<Path> EtchPaths { get; }


        /// <summary>
        /// Creates a new <see cref="Route"/> instance.
        /// </summary>
        /// <param name="Config">The configuration settings for the program</param>
        /// <param name="PreEtchTrace">The list of moves for the pre-etch preview trace around the
        /// target boundary if enabled, or null if disabled.</param>
        /// <param name="EtchMoves">The list of moves involved in etching the target</param>
        public Route(Configuration Config, Path PreEtchTrace, List<Path> EtchMoves)
        {
            this.Config = Config;
            this.PreEtchTrace = PreEtchTrace;
            this.EtchPaths = EtchMoves;
        }


        /// <summary>
        /// Estimates the amount of time a route will take to etch.
        /// </summary>
        /// <param name="IncludeTrace">True to include the pre-etch trace preview in the 
        /// estimates, false to ignore it.</param>
        /// <returns>An estimate of the total etching time and total travel distance for this route</returns>
        public (TimeSpan, double) EstimateTimeAndDistance(bool IncludeTrace)
        {
            // Convert the speeds to mm per millisecond
            double travelSpeed_MmPerMs = Config.TravelSpeed / 60000.0;
            double etchSpeed_MmPerMs = Config.EtchSpeed / 60000.0;
            double traceSpeed_MmPerMs = travelSpeed_MmPerMs;
            double totalMilliseconds = 0;
            double totalDistance = 0;

            // Calculate the pre-etch trace preview
            if (IncludeTrace)
            {
                totalMilliseconds = Config.PreviewDelay * 2;

                double length = PreEtchTrace.Length * Config.PixelSize;
                double time = length / traceSpeed_MmPerMs;
                totalDistance += length;
                totalMilliseconds += time;
            }

            // Calculate the etch time for each path in the main etch sequence
            foreach(Path move in EtchPaths)
            {
                double length = move.Length * Config.PixelSize;
                double time = length / etchSpeed_MmPerMs;
                totalDistance += length;
                totalMilliseconds += time;
            }

            // Calculate the travel time between paths
            for(int i = 0; i < EtchPaths.Count - 1; i++)
            {
                Path firstPath = EtchPaths[i];
                Path secondPath = EtchPaths[i + 1];
                Point startPoint = firstPath.Points[^1];
                Point endPoint = secondPath.Points[0];

                double length = startPoint.GetDistance(endPoint) * Config.PixelSize;
                double time = length / travelSpeed_MmPerMs;
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
