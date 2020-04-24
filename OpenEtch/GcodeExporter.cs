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
using System.IO;

namespace OpenEtch
{
    /// <summary>
    /// This converts routes to G-code and exports them to a file.
    /// </summary>
    internal class GcodeExporter
    {
        /// <summary>
        /// The comment format to use in G-code
        /// </summary>
        private CommentMode CommentMode;


        /// <summary>
        /// The writer to write the G-code to
        /// </summary>
        private StreamWriter Writer;


        /// <summary>
        /// The X value of the target's origin, in mm
        /// </summary>
        private double OriginX;


        /// <summary>
        /// The Y value of the target's origin, in mm
        /// </summary>
        private double OriginY;


        /// <summary>
        /// The number of mm per pixel
        /// </summary>
        private double PixelSize;


        /// <summary>
        /// Creates a new <see cref="GcodeExporter"/> instance
        /// </summary>
        public GcodeExporter()
        {

        }


        /// <summary>
        /// Converts the provided route to G-code with the given settings, and saves it to the 
        /// provided file.
        /// </summary>
        /// <param name="TargetFilename">The file path of the target G-code file to write</param>
        /// <param name="OriginalFilename">The original name of the image that was loaded</param>
        /// <param name="Route">The etching route for the image</param>
        /// <param name="CommentMode">The G-code comment format to use</param>
        /// <param name="PixelSize">The size of each pixel (mm per pixel)</param>
        /// <param name="OriginX">The X coordinate of the top-left corner, in mm</param>
        /// <param name="OriginY">The Y coordinate of the top-left corner, in mm</param>
        /// <param name="ZHeight">The Z height to set the laser cutter during etching, in mm</param>
        /// <param name="TravelSpeed">The speed to move the head between etching operations
        /// (when the laser is off), in mm per minute</param>
        /// <param name="EtchSpeed">The speed to move the head during etching operations
        /// (when the laser is on), in mm per minute</param>
        /// <param name="LaserOffCommand">The G-code command to turn the laser off</param>
        /// <param name="LaserLowCommand">The G-code command to turn the laser on, but
        /// at a low power level (used for the pre-etch trace preview)</param>
        /// <param name="LaserEtchCommand">The G-code command to turn the laser on full
        /// power during etching</param>
        /// <param name="MoveCommand">The G-code command to use during moves</param>
        /// <param name="PerformPreEtchTrace">True to perform the pre-etch boundary trace preview,
        /// false to disable it and get right to etching</param>
        /// <param name="EtchTraceDelay">The delay, in milliseconds, to wait at the start and end
        /// of the pre-etch trace preview</param>
        public void ExportGcode(
            string TargetFilename,
            string OriginalFilename,
            Route Route,
            CommentMode CommentMode,
            double PixelSize,
            double OriginX,
            double OriginY,
            double ZHeight,
            double TravelSpeed,
            double EtchSpeed,
            string LaserOffCommand,
            string LaserLowCommand,
            string LaserEtchCommand,
            string MoveCommand,
            bool PerformPreEtchTrace,
            int EtchTraceDelay)
        {
            this.CommentMode = CommentMode;
            this.OriginX = OriginX;
            this.OriginY = OriginY;
            this.PixelSize = PixelSize;

            try
            {
                using (FileStream stream = new FileStream(TargetFilename, FileMode.Create, FileAccess.Write))
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    Writer = writer;
                    // Write the documentation header at the start of the file
                    WriteCommandLine(null, $"Created with OpenEtch v{VersionInfo.Version}");
                    WriteCommandLine(null, $"Exported on {DateTime.Now}");
                    WriteCommandLine(null, $"Generated from [{OriginalFilename}]");
                    writer.WriteLine();

                    // Initialize the machine
                    WriteCommandLine(null, "Initialize the machine");
                    WriteCommandLine(LaserOffCommand, "Disable the laser");
                    WriteCommandLine("G90", "Set to absolute positioning mode");
                    WriteCommandLine("G21", "Use millimeters");
                    WriteCommandLine($"{MoveCommand} Z{ZHeight}", "Set the desired Z position (assuming it is already homed)");
                    WriteCommandLine($"G28 X Y", "Home the X and Y axes");
                    WriteCommandLine($"{MoveCommand} X{OriginX} Y{OriginY} F{TravelSpeed}", "Move to the image origin");
                    writer.WriteLine();

                    // Run the trace if requested
                    if (PerformPreEtchTrace)
                    {
                        WriteCommandLine(null, "Perform the pre-etch trace preview");
                        WriteCommandLine(LaserLowCommand, "Enable the laser in low-power mode");
                        if (EtchTraceDelay > 0)
                        {
                            WriteCommandLine($"G4 P{EtchTraceDelay}", $"Wait for {EtchTraceDelay}ms before starting the trace");
                        }
                        foreach (Move move in Route.PreEtchTrace)
                        {
                            (string x, string y) = ConvertPointToGcodeCoordinates(move.End);
                            WriteCommandLine($"{MoveCommand} X{x} Y{y} F{TravelSpeed}", null);
                        }
                        if (EtchTraceDelay > 0)
                        {
                            WriteCommandLine($"G4 P{EtchTraceDelay}", $"Wait for {EtchTraceDelay}ms before ending the trace");
                        }
                        WriteCommandLine(LaserOffCommand, "Disable the laser");
                        writer.WriteLine();
                    }

                    // Run the etch route
                    double travelSpeed_MmPerMs = TravelSpeed / 60000.0;
                    double etchSpeed_MmPerMs = EtchSpeed / 60000.0;
                    (TimeSpan runtimeEstimate, double totalDistance) = Route.EstimateTimeAndDistance(PixelSize, TravelSpeed, EtchSpeed, false, 0, 0);
                    double distanceSoFar = 0;
                    double timeSoFar = 0;
                    int lastRemainingMinutes = (int)Math.Round(runtimeEstimate.TotalMinutes);
                    int lastPercentComplete = 0;

                    WriteCommandLine(null, "Main image etching route");
                    for (int i = 0; i < Route.EtchMoves.Count; i++)
                    {
                        Move move = Route.EtchMoves[i];
                        double moveTime = 0;
                        double moveLength = move.Length * PixelSize;

                        // Write the laser mode and move commands
                        (string x, string y) = ConvertPointToGcodeCoordinates(move.End);
                        if (move.Type == MoveType.Etch)
                        {
                            WriteCommandLine(LaserEtchCommand, null);
                            WriteCommandLine($"{MoveCommand} X{x} Y{y} F{EtchSpeed}", null);
                            moveTime = moveLength / etchSpeed_MmPerMs;
                        }
                        else if (move.Type == MoveType.Travel)
                        {
                            WriteCommandLine(LaserOffCommand, null);
                            WriteCommandLine($"{MoveCommand} X{x} Y{y} F{TravelSpeed}", null);
                            moveTime = moveLength / travelSpeed_MmPerMs;
                        }

                        // Calculate the percentage completed in terms of overall travel
                        distanceSoFar += moveLength;
                        int percentComplete = (int)(distanceSoFar / totalDistance * 100);

                        // Calculate the remaining time estimate
                        timeSoFar += moveTime;
                        double timeRemaining = runtimeEstimate.TotalMilliseconds - timeSoFar;
                        int minutesRemaining = (int)Math.Round(timeRemaining / 60000.0);

                        if (lastPercentComplete != percentComplete || 
                            lastRemainingMinutes != minutesRemaining)
                        {
                            // Update the percentage and remaining time indicator
                            lastRemainingMinutes = minutesRemaining;
                            lastPercentComplete = percentComplete;
                            WriteCommandLine($"M73 P{percentComplete} R{minutesRemaining}", null);
                        }
                    }

                    // Done!
                    WriteCommandLine(LaserOffCommand, null);
                    writer.Flush();
                }

            }
            finally
            {
                Writer = null;
            }
        }


        /// <summary>
        /// Writes a new line containing a command and a comment. If either value is null or empty,
        /// it will be ignored.
        /// </summary>
        /// <param name="Command">The command to write. Can be null or blank.</param>
        /// <param name="Comment">The comment to append to the line. Can be null or blank.</param>
        private void WriteCommandLine(string Command, string Comment)
        {
            if (!string.IsNullOrEmpty(Command))
            {
                Writer.Write($"{Command} ");
            }
            if (!string.IsNullOrEmpty(Comment))
            {
                switch (CommentMode)
                {
                    case CommentMode.Semicolon:
                        Writer.Write($"; {Comment}");
                        break;

                    case CommentMode.Parentheses:
                        Writer.Write($"({Comment})");
                        break;
                }
            }
            Writer.WriteLine();
        }


        /// <summary>
        /// Converts a point from pixel-space coordinates to absolute G-code coordinates,
        /// returning the X and Y values as string with 3 decimal places of precision.
        /// </summary>
        /// <param name="Point">The point to convert</param>
        /// <returns>The X and Y coordinates of the point in G-code space.</returns>
        private (string, string) ConvertPointToGcodeCoordinates(Point Point)
        {
            double x = Point.X * PixelSize + OriginX;
            double y = OriginY - Point.Y * PixelSize; // Y is inverted because (0,0) on the printer is the bottom-left, instead of the top-left
            string xString = x.ToString("N3");
            string yString = y.ToString("N3");
            return (xString, yString);
        }

    }
}
