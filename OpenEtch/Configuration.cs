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


using Nett;
using System.IO;

namespace OpenEtch
{
    /// <summary>
    /// This holds the configuration settings for OpenEtch.
    /// </summary>
    internal class Configuration
    {
        /// <summary>
        /// The size of each pixel (mm per pixel)
        /// </summary>
        public double PixelSize { get; set; }


        /// <summary>
        /// The X coordinate of the top-left corner, in mm
        /// </summary>
        public double OriginX { get; set; }


        /// <summary>
        /// The Y coordinate of the top-left corner, in mm
        /// </summary>
        public double OriginY { get; set; }


        /// <summary>
        /// The Z height to set the laser cutter during etching, in mm
        /// </summary>
        public double ZHeight { get; set; }


        /// <summary>
        /// The speed to move the head between etching operations
        /// (when the laser is off), in mm per minute
        /// </summary>
        public double TravelSpeed { get; set; }


        /// <summary>
        /// The speed to move the head during etching operations
        /// (when the laser is on), in mm per minute
        /// </summary>
        public double EtchSpeed { get; set; }


        /// <summary>
        /// The G-code command to turn the laser off
        /// </summary>
        public string LaserOffCommand { get; set; }


        /// <summary>
        /// The G-code command to turn the laser on, but
        /// at a low power level (used for the pre-etch trace preview)
        /// </summary>
        public string LaserLowCommand { get; set; }


        /// <summary>
        /// The G-code command to turn the laser on full
        /// power during etching
        /// </summary>
        public string LaserHighCommand { get; set; }


        /// <summary>
        /// The G-code command to use during moves
        /// </summary>
        public string MoveCommand { get; set; }


        /// <summary>
        /// The G-code comment format to use
        /// </summary>
        public CommentMode CommentMode { get; set; }


        /// <summary>
        /// True to perform the pre-etch boundary trace preview,
        /// false to disable it and get right to etching
        /// </summary>
        public bool IsBoundaryPreviewEnabled { get; set; }


        /// <summary>
        /// The delay, in milliseconds, to wait at the start and end
        /// of the pre-etch trace preview
        /// </summary>
        public int PreviewDelay { get; set; }


        /// <summary>
        /// The filename of the configuration file
        /// </summary>
        private readonly string ConfigFileName;


        /// <summary>
        /// Creates a new <see cref="Configuration"/> instance.
        /// </summary>
        /// <param name="ConfigFileName">The filename of the configuration file</param>
        public Configuration(string ConfigFileName)
        {
            this.ConfigFileName = ConfigFileName;

            // Default settings
            PixelSize = 0.02;
            OriginX = 70;
            OriginY = 140;
            ZHeight = 50;
            TravelSpeed = 1000;
            EtchSpeed = 100;
            LaserOffCommand = "M107";
            LaserLowCommand = "M106 S16";
            LaserHighCommand = "M106 S255";
            MoveCommand = "G0";
            CommentMode = CommentMode.Semicolon;
            IsBoundaryPreviewEnabled = true;
            PreviewDelay = 5000;
        }


        /// <summary>
        /// Loads a configuration from an existing config file.
        /// </summary>
        public void Load()
        {
            if(!File.Exists(ConfigFileName))
            {
                return;
            }

            TomlTable settingsTable = Toml.ReadFile(ConfigFileName);
            // Physical Parameters
            if (settingsTable.TryGetValue("Physical-Parameters", out TomlObject physicalParametersObject))
            {
                TomlTable physicalParameters = (TomlTable)physicalParametersObject;

                // PixelSize
                if (physicalParameters.TryGetValue("PixelSize", out TomlObject pixelSizeObject))
                {
                    PixelSize = pixelSizeObject.Get<double>();
                }

                // OriginX
                if (physicalParameters.TryGetValue("OriginX", out TomlObject originXObject))
                {
                    OriginX = originXObject.Get<double>();
                }

                // OriginY
                if (physicalParameters.TryGetValue("OriginY", out TomlObject originYObject))
                {
                    OriginY = originYObject.Get<double>();
                }

                // ZHeight
                if (physicalParameters.TryGetValue("ZHeight", out TomlObject zHeightObject))
                {
                    ZHeight = zHeightObject.Get<double>();
                }

                // TravelSpeed
                if (physicalParameters.TryGetValue("TravelSpeed", out TomlObject travelSpeedObject))
                {
                    TravelSpeed = travelSpeedObject.Get<double>();
                }

                // EtchSpeed
                if (physicalParameters.TryGetValue("EtchSpeed", out TomlObject etchSpeedObject))
                {
                    EtchSpeed = etchSpeedObject.Get<double>();
                }
            }

            // G-Code Commands
            if (settingsTable.TryGetValue("G-Code-Commands", out TomlObject gcodeCommandsObject))
            {
                TomlTable gcodeCommands = (TomlTable)gcodeCommandsObject;

                // LaserOffCommand
                if (gcodeCommands.TryGetValue("LaserOffCommand", out TomlObject laserOffCommandObject))
                {
                    LaserOffCommand = laserOffCommandObject.Get<string>();
                }

                // LaserLowCommand
                if (gcodeCommands.TryGetValue("LaserLowCommand", out TomlObject laserLowCommandObject))
                {
                    LaserLowCommand = laserLowCommandObject.Get<string>();
                }

                // LaserHighCommand
                if (gcodeCommands.TryGetValue("LaserHighCommand", out TomlObject laserHighCommandObject))
                {
                    LaserHighCommand = laserHighCommandObject.Get<string>();
                }

                // MoveCommand
                if (gcodeCommands.TryGetValue("MoveCommand", out TomlObject moveCommandObject))
                {
                    MoveCommand = moveCommandObject.Get<string>();
                }

                // CommentMode
                if (gcodeCommands.TryGetValue("CommentMode", out TomlObject commentModeObject))
                {
                    string commentModeString = commentModeObject.Get<string>();
                    if (commentModeString == "Parentheses")
                    {
                        CommentMode = CommentMode.Parentheses;
                    }
                    else
                    {
                        CommentMode = CommentMode.Semicolon;
                    }
                }
            }

            // Pre-Etch Trace Preview
            if(settingsTable.TryGetValue("Pre-Etch-Trace-Preview", out TomlObject preEtchTracePreviewObject))
            {
                TomlTable preEtchTracePreview = (TomlTable)preEtchTracePreviewObject;

                // IsBoundaryPreviewEnabled
                if (preEtchTracePreview.TryGetValue("IsBoundaryPreviewEnabled", out TomlObject isBoundaryPreviewEnabledObject))
                {
                    IsBoundaryPreviewEnabled = isBoundaryPreviewEnabledObject.Get<bool>();
                }

                // PreviewDelay
                if (preEtchTracePreview.TryGetValue("PreviewDelay", out TomlObject previewDelayObject))
                {
                    PreviewDelay = previewDelayObject.Get<int>();
                }
            }
        }


        /// <summary>
        /// Saves the current configuration out to the config file.
        /// </summary>
        public void Save()
        {
            // Physical Paramters
            TomlTable physicalParameters = Toml.Create();

            // PixelSize
            TomlFloat pixelSize = physicalParameters.Add("PixelSize", PixelSize).Added;
            pixelSize.AddComment(" The size of each pixel (mm per pixel)", CommentLocation.Append);

            // OriginX
            TomlFloat originX = physicalParameters.Add("OriginX", OriginX).Added;
            originX.AddComment(" The X coordinate of the top-left corner, in mm", CommentLocation.Append);

            // OriginY
            TomlFloat originY = physicalParameters.Add("OriginY", OriginY).Added;
            originY.AddComment(" The Y coordinate of the top-left corner, in mm", CommentLocation.Append);

            // ZHeight
            TomlFloat zHeight = physicalParameters.Add("ZHeight", ZHeight).Added;
            zHeight.AddComment(" The Z height to set the laser cutter during etching, in mm", CommentLocation.Append);

            // TravelSpeed
            TomlFloat travelSpeed = physicalParameters.Add("TravelSpeed", TravelSpeed).Added;
            travelSpeed.AddComment(" The speed to move the head between etching operations (when the laser is off), in mm per minute", CommentLocation.Append);

            // EtchSpeed
            TomlFloat etchSpeed = physicalParameters.Add("EtchSpeed", EtchSpeed).Added;
            etchSpeed.AddComment(" The speed to move the head during etching operations (when the laser is on), in mm per minute", CommentLocation.Append);

            // G-code Commands
            TomlTable gCodeCommands = Toml.Create();

            // LaserOffCommand
            TomlString laserOffCommand = gCodeCommands.Add("LaserOffCommand", LaserOffCommand).Added;
            laserOffCommand.AddComment(" The G-code command to turn the laser off", CommentLocation.Append);

            // LaserLowCommand
            TomlString laserLowCommand = gCodeCommands.Add("LaserLowCommand", LaserLowCommand).Added;
            laserLowCommand.AddComment(" The G-code command to turn the laser on, but at a low power level (used for the pre-etch trace preview)", CommentLocation.Append);

            // LaserHighCommand
            TomlString laserHighCommand = gCodeCommands.Add("LaserHighCommand", LaserHighCommand).Added;
            laserHighCommand.AddComment(" The G-code command to turn the laser on full power during etching", CommentLocation.Append);

            // MoveCommand
            TomlString moveCommand = gCodeCommands.Add("MoveCommand", MoveCommand).Added;
            moveCommand.AddComment(" The G-code command to use during moves", CommentLocation.Append);

            // CommentMode
            TomlString commentMode = gCodeCommands.Add("CommentMode", CommentMode.ToString()).Added;
            commentMode.AddComment(" The G-code comment format to use (Semicolon or Parentheses)", CommentLocation.Append);

            // Pre-Etch Trace Preview
            TomlTable preEtchTracePreview = Toml.Create();

            // IsBoundaryPreviewEnabled
            TomlBool isBoundaryPreviewEnabled = preEtchTracePreview.Add("IsBoundaryPreviewEnabled", IsBoundaryPreviewEnabled).Added;
            isBoundaryPreviewEnabled.AddComment(" True to perform the pre-etch boundary trace preview, false to disable it and get right to etching", CommentLocation.Append);

            // PreviewDelay
            TomlInt previewDelay = preEtchTracePreview.Add("PreviewDelay", PreviewDelay).Added;
            previewDelay.AddComment(" The delay, in milliseconds, to wait at the start and end of the pre-etch trace preview", CommentLocation.Append);

            TomlTable settingsTable = Toml.Create();
            settingsTable.Add("Physical-Parameters", physicalParameters);
            settingsTable.Add("G-Code-Commands", gCodeCommands);
            settingsTable.Add("Pre-Etch-Trace-Preview", preEtchTracePreview);

            Toml.WriteFile(settingsTable, ConfigFileName);
        }

    }
}
