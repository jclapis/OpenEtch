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
        /// The configuration settings for the program
        /// </summary>
        private readonly Configuration Config;


        /// <summary>
        /// The writer to write the G-code to
        /// </summary>
        private StreamWriter Writer;


        /// <summary>
        /// Creates a new <see cref="GcodeExporter"/> instance
        /// </summary>
        /// <param name="Config">The configuration settings for the program</param>
        public GcodeExporter(Configuration Config)
        {
            this.Config = Config;
        }


        /// <summary>
        /// Converts the provided route to G-code with the given settings, and saves it to the 
        /// provided file.
        /// </summary>
        /// <param name="TargetFilename">The file path of the target G-code file to write</param>
        /// <param name="OriginalFilename">The original name of the image that was loaded</param>
        /// <param name="Route">The etching route for the image</param>
        public void ExportGcode(
            string TargetFilename,
            string OriginalFilename,
            Route Route)
        {
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
                    WriteCommandLine(Config.LaserOffCommand, "Disable the laser");
                    WriteCommandLine("G90", "Set to absolute positioning mode");
                    WriteCommandLine("G21", "Use millimeters");
                    if(Config.ZHeight != -1)
                    {
                        WriteCommandLine($"{Config.MoveCommand} Z{Config.ZHeight}", "Set the desired Z position (assuming it is already homed)");
                    }
                    if(Config.HomeXY)
                    {
                        WriteCommandLine($"G28 X Y", "Home the X and Y axes");
                    }
                    WriteCommandLine($"{Config.MoveCommand} X{Config.OriginX} Y{Config.OriginY} F6000", "Move to the image origin");
                    WriteCommandLine("M400", "Wait for the move to finish before starting the laser");
                    writer.WriteLine();

                    // Run the trace if requested
                    if (Config.IsBoundaryPreviewEnabled)
                    {
                        WriteCommandLine(null, "Perform the pre-etch trace preview");
                        WriteCommandLine(Config.LaserLowCommand, "Enable the laser in low-power mode");
                        if (Config.PreviewDelay > 0)
                        {
                            WriteCommandLine($"G4 P{Config.PreviewDelay}", $"Wait for {Config.PreviewDelay}ms before starting the trace");
                        }
                        foreach (Move move in Route.PreEtchTrace)
                        {
                            (string x, string y) = ConvertPointToGcodeCoordinates(move.End);
                            WriteCommandLine($"{Config.MoveCommand} X{x} Y{y} F{Config.TravelSpeed}", null);
                        }
                        if (Config.PreviewDelay > 0)
                        {
                            WriteCommandLine($"G4 P{Config.PreviewDelay}", $"Wait for {Config.PreviewDelay}ms before ending the trace");
                        }
                        WriteCommandLine(Config.LaserOffCommand, "Disable the laser");
                        writer.WriteLine();
                    }

                    // Run the etch route
                    double travelSpeed_MmPerMs = Config.TravelSpeed / 60000.0;
                    double etchSpeed_MmPerMs = Config.EtchSpeed / 60000.0;
                    (TimeSpan runtimeEstimate, double totalDistance) = Route.EstimateTimeAndDistance(false);
                    runtimeEstimate *= Config.Passes;
                    totalDistance *= Config.Passes;

                    double distanceSoFar = 0;
                    double timeSoFar = 0;
                    int lastRemainingMinutes = (int)Math.Round(runtimeEstimate.TotalMinutes);
                    int lastPercentComplete = 0;

                    for (int pass = 0; pass < Config.Passes; pass++)
                    {
                        WriteCommandLine(null, $"Main image etching route - Pass {pass + 1}");
                        for (int moveIndex = 0; moveIndex < Route.EtchMoves.Count; moveIndex++)
                        {
                            Move move = Route.EtchMoves[moveIndex];
                            double moveTime = 0;
                            double moveLength = move.Length * Config.PixelSize;

                            // Write the laser mode and move commands
                            (string x, string y) = ConvertPointToGcodeCoordinates(move.End);
                            if (move.Type == MoveType.Etch)
                            {
                                WriteCommandLine(Config.LaserHighCommand, null);
                                WriteCommandLine($"{Config.MoveCommand} X{x} Y{y} F{Config.EtchSpeed}", null);
                                WriteCommandLine("M400", null);
                                moveTime = moveLength / etchSpeed_MmPerMs;
                            }
                            else if (move.Type == MoveType.Travel)
                            {
                                WriteCommandLine(Config.LaserOffCommand, null);
                                WriteCommandLine($"{Config.MoveCommand} X{x} Y{y} F{Config.TravelSpeed}", null);
                                WriteCommandLine("M400", null);
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
                        writer.WriteLine();
                    }

                    // Run the post-etch cleanup process so Prusa printers don't complain about incomplete files
                    WriteCommandLine(null, "Post-etch cleanup");
                    WriteCommandLine(Config.LaserOffCommand, null);
                    WriteCommandLine("G4", "Wait for moves to finish");
                    WriteCommandLine($"{Config.MoveCommand} X0 F6000", "Move the X-axis out of the way for easy target access");
                    WriteCommandLine("M84", "Disable motors");

                    // Done!
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
                switch (Config.CommentMode)
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
            double x = Point.X * Config.PixelSize + Config.OriginX;
            double y = Config.OriginY - Point.Y * Config.PixelSize; // Y is inverted because (0,0) on the printer is the bottom-left, instead of the top-left
            string xString = x.ToString("N3");
            string yString = y.ToString("N3");
            return (xString, yString);
        }

    }
}
