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


using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace OpenEtch
{
    /// <summary>
    /// This is the main UI window for OpenEtch.
    /// </summary>
    public class MainWindow : Window
    {
        #region Controls

        private readonly TextBox PixelSizeBox;
        private readonly TextBox OriginXBox;
        private readonly TextBox OriginYBox;
        private readonly TextBox ZHeightBox;
        private readonly TextBox TravelSpeedBox;
        private readonly TextBox EtchSpeedBox;
        private readonly TextBox LaserOffCommandBox;
        private readonly TextBox LaserLowCommandBox;
        private readonly TextBox LaserHighCommandBox;
        private readonly ComboBox MoveCommandBox;
        private readonly ComboBox CommentStyleBox;
        private readonly CheckBox BoundaryPreviewToggle;
        private readonly TextBox BoundaryPreviewDelayBox;

        #endregion

        /// <summary>
        /// The settings and configuration
        /// </summary>
        private readonly Configuration Config;


        /// <summary>
        /// The image processor for loading images and parsing them
        /// into etch segments
        /// </summary>
        private readonly ImageProcessor ImageProcessor;


        /// <summary>
        /// The router for converting etch segments into laser moves
        /// </summary>
        private readonly Router Router;


        /// <summary>
        /// The G-code exporter
        /// </summary>
        private readonly GcodeExporter Exporter;


        /// <summary>
        /// The name of the original image being processed
        /// </summary>
        private string OriginalFilename;


        /// <summary>
        /// The loaded image
        /// </summary>
        private ProcessedImage ProcessedImage;


        /// <summary>
        /// The etching route for the loaded image
        /// </summary>
        private Route Route;


        /// <summary>
        /// Creates a new MainWindow instance.
        /// </summary>
        public MainWindow()
        {
            AvaloniaXamlLoader.Load(this);
#if DEBUG
            this.AttachDevTools();
#endif
            // Cache the UI components
            PixelSizeBox = this.FindControl<TextBox>("PixelSizeBox");
            OriginXBox = this.FindControl<TextBox>("OriginXBox");
            OriginYBox = this.FindControl<TextBox>("OriginYBox");
            ZHeightBox = this.FindControl<TextBox>("ZHeightBox");
            TravelSpeedBox = this.FindControl<TextBox>("TravelSpeedBox");
            EtchSpeedBox = this.FindControl<TextBox>("EtchSpeedBox");
            LaserOffCommandBox = this.FindControl<TextBox>("LaserOffCommandBox");
            LaserLowCommandBox = this.FindControl<TextBox>("LaserLowCommandBox");
            LaserHighCommandBox = this.FindControl<TextBox>("LaserHighCommandBox");
            MoveCommandBox = this.FindControl<ComboBox>("MoveCommandBox");
            CommentStyleBox = this.FindControl<ComboBox>("CommentStyleBox");
            BoundaryPreviewToggle = this.FindControl<CheckBox>("BoundaryPreviewToggle");
            BoundaryPreviewDelayBox = this.FindControl<TextBox>("BoundaryPreviewDelayBox");

            // Load the saved configuration, or fallback to the defaults
            Config = new Configuration("Configuration.toml");
            try
            {
                Config.Load();
                
                PixelSizeBox.Text = Config.PixelSize.ToString();
                OriginXBox.Text = Config.OriginX.ToString();
                OriginYBox.Text = Config.OriginY.ToString();
                ZHeightBox.Text = Config.ZHeight.ToString();
                TravelSpeedBox.Text = Config.TravelSpeed.ToString();
                EtchSpeedBox.Text = Config.TravelSpeed.ToString();
                LaserOffCommandBox.Text = Config.LaserOffCommand;
                LaserLowCommandBox.Text = Config.LaserLowCommand;
                LaserHighCommandBox.Text = Config.LaserHighCommand;
                
                if(Config.MoveCommand == "G1")
                {
                    MoveCommandBox.SelectedIndex = 1;
                }
                else
                {
                    MoveCommandBox.SelectedIndex = 0;
                }
                
                if(Config.CommentMode == CommentMode.Parentheses)
                {
                    CommentStyleBox.SelectedIndex = 1;
                }
                else
                {
                    CommentStyleBox.SelectedIndex = 0;
                }

                BoundaryPreviewToggle.IsChecked = Config.IsBoundaryPreviewEnabled;
                BoundaryPreviewDelayBox.Text = Config.PreviewDelay.ToString();
            }
            catch(Exception ex)
            {
                _ = MessageBox.Show(this, $"Error loading configuration: {ex.Message}. " +
                    $"Configuration will be set to the default values.", "Error loading configuration");
            }

            // Load the processors
            ImageProcessor = new ImageProcessor();
            Router = new Router();
            Exporter = new GcodeExporter();
        }


        /// <summary>
        /// Loads an image from the filesystem.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        public async void LoadImageButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Button recalculateButton = this.FindControl<Button>("RecalculateButton");
            Button exportButton = this.FindControl<Button>("ExportButton");
            recalculateButton.IsEnabled = false;
            exportButton.IsEnabled = false;
            ProcessedImage = null;
            Route = null;
            Title = $"OpenEtch v{VersionInfo.Version}";

            // Give the user a new dialog for choosing the image file
            OpenFileDialog dialog = new OpenFileDialog()
            {
                AllowMultiple = false,
                Title = "Select Image",
                Filters =
                {
                    new FileDialogFilter
                    {
                        Name = "Images",
                        Extensions = { "bmp", "png", "jpg", "jpeg" }
                    },
                    new FileDialogFilter
                    {
                        Name = "All Files",
                        Extensions = { "*" }
                    }
                }
            };

            // Get the selection, if they selected something
            string[] selection = await dialog.ShowAsync(this);
            if(selection.Length != 1)
            {
                return;
            }

            // Try to load and process the image
            string imagePath = selection[0];
            try
            {
                LoadImage(imagePath);
                recalculateButton.IsEnabled = true;
                exportButton.IsEnabled = true;
                OriginalFilename = Path.GetFileName(imagePath);
                Title = $"OpenEtch v{VersionInfo.Version} - {OriginalFilename}";
            }
            catch(Exception ex)
            {
                ProcessedImage = null;
                Route = null;
                await MessageBox.Show(this, $"Error loading image: {ex.Message}", "Error loading image");
                return;
            }
        }


        /// <summary>
        /// Recalculates the physical dimensions of the target and estimates the runtime
        /// for the laser etcher based on the processed image route.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        public void RecalculateButton_Click(object sender, RoutedEventArgs e)
        {
            RecalculateDimensionsAndRuntime();
        }


        /// <summary>
        /// Exports the calcualted etching route to a G-code file.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        public async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the target file path to save the g-code to
                SaveFileDialog dialog = new SaveFileDialog()
                {
                    Filters =
                {
                    new FileDialogFilter
                    {
                        Name = "G-code files",
                        Extensions = { "gcode" }
                    }
                },
                    DefaultExtension = "gcode"
                };

                // Sanitize so it always ends in .gcode
                string targetFilename = await dialog.ShowAsync(this);
                if (string.IsNullOrEmpty(targetFilename))
                {
                    return;
                }
                if (!targetFilename.EndsWith(".gcode"))
                {
                    targetFilename = $"{targetFilename}.gcode";
                }

                // Get all of the relevant variables from the UI
                CommentMode commentMode = CommentMode.Semicolon;
                ComboBox commentStyleBox = this.FindControl<ComboBox>("CommentStyleBox");
                if (commentStyleBox.SelectedIndex == 1)
                {
                    commentMode = CommentMode.Parentheses;
                }

                TextBox pixelSizeBox = this.FindControl<TextBox>("PixelSizeBox");
                double pixelSize = double.Parse(pixelSizeBox.Text);

                TextBox originXBox = this.FindControl<TextBox>("OriginXBox");
                double originX = double.Parse(originXBox.Text);

                TextBox originYBox = this.FindControl<TextBox>("OriginYBox");
                double originY = double.Parse(originYBox.Text);

                TextBox zHeightBox = this.FindControl<TextBox>("ZHeightBox");
                double zHeight = double.Parse(zHeightBox.Text);

                TextBox travelSpeedBox = this.FindControl<TextBox>("TravelSpeedBox");
                double travelSpeed = double.Parse(travelSpeedBox.Text);

                TextBox etchSpeedBox = this.FindControl<TextBox>("EtchSpeedBox");
                double etchSpeed = double.Parse(etchSpeedBox.Text);

                TextBox laserOffCommandBox = this.FindControl<TextBox>("LaserOffCommandBox");
                string laserOffCommand = laserOffCommandBox.Text;

                TextBox laserlowCommandBox = this.FindControl<TextBox>("LaserLowCommandBox");
                string laserlowCommand = laserlowCommandBox.Text;

                TextBox laserHighCommandBox = this.FindControl<TextBox>("LaserHighCommandBox");
                string laserHighCommand = laserHighCommandBox.Text;

                ComboBox moveCommandBox = this.FindControl<ComboBox>("MoveCommandBox");
                string moveCommand = ((ComboBoxItem)moveCommandBox.SelectedItem).Content.ToString();

                CheckBox traceToggle = this.FindControl<CheckBox>("PreEtchTraceToggle");
                bool performTrace = false;
                int traceDelay = 0;
                if (traceToggle.IsChecked == true)
                {
                    performTrace = true;
                    TextBox traceDelayBox = this.FindControl<TextBox>("PreEtchTraceOriginDelayBox");
                    traceDelay = (int)Math.Round(double.Parse(traceDelayBox.Text));
                }

                // Run it!
                Exporter.ExportGcode(targetFilename, OriginalFilename, Route, commentMode,
                    pixelSize, originX, originY, zHeight, travelSpeed, etchSpeed,
                    laserOffCommand, laserlowCommand, laserHighCommand, moveCommand, performTrace, traceDelay);

                await MessageBox.Show(this, $"File {Path.GetFileName(targetFilename)} successfully exported.", "Export succeeded");
            }
            catch(Exception ex)
            {
                await MessageBox.Show(this, $"Error exporting file: {ex.Message}", "Export failed");
            }
        }


        /// <summary>
        /// Loads and processes an image from the filesystem.
        /// </summary>
        /// <param name="ImagePath">The path on the filesystem of the image to load</param>
        private void LoadImage(string ImagePath)
        {
            // Clear the previous image if there was one
            Image preview = this.FindControl<Image>("Preview");
            preview.Source = null;
            preview.InvalidateVisual();

            // Process and draw the new image
            ProcessedImage = ImageProcessor.ProcessImage(ImagePath);
            preview.Source = ProcessedImage.Bitmap;
            preview.InvalidateVisual();

            // Set the image dimension labels
            TextBlock imageWidthLabel = this.FindControl<TextBlock>("ImageWidthLabel");
            TextBlock imageHeightLabel = this.FindControl<TextBlock>("ImageHeightLabel");
            imageWidthLabel.Text = $"{ProcessedImage.Width} px";
            imageHeightLabel.Text = $"{ProcessedImage.Height} px";

            // Route it
            Route = Router.Route(ProcessedImage);
            RecalculateDimensionsAndRuntime();
        }


        /// <summary>
        /// Recalculates the physical dimensions of the target and estimates the runtime
        /// for the laser etcher based on the processed image route.
        /// </summary>
        private void RecalculateDimensionsAndRuntime()
        {
            TextBox pixelSizeBox = this.FindControl<TextBox>("PixelSizeBox");
            double pixelSize = double.Parse(pixelSizeBox.Text);
            double targetWidth = ProcessedImage.Width * pixelSize;
            double targetHeight = ProcessedImage.Height * pixelSize;

            TextBlock targetWidthLabel = this.FindControl<TextBlock>("TargetWidthLabel");
            TextBlock targetHeightLabel = this.FindControl<TextBlock>("TargetHeightLabel");
            targetWidthLabel.Text = $"{targetWidth:N2} mm";
            targetHeightLabel.Text = $"{targetHeight:N2} mm";

            TextBox travelSpeedBox = this.FindControl<TextBox>("TravelSpeedBox");
            double travelSpeed = double.Parse(travelSpeedBox.Text);

            TextBox etchSpeedBox = this.FindControl<TextBox>("EtchSpeedBox");
            double etchSpeed = double.Parse(etchSpeedBox.Text);

            CheckBox traceToggle = this.FindControl<CheckBox>("PreEtchTraceToggle");
            double traceStartDelay = 0;
            double traceStopDelay = 0;
            if (traceToggle.IsChecked == true)
            {
                TextBox traceDelayBox = this.FindControl<TextBox>("PreEtchTraceOriginDelayBox");
                traceStartDelay = double.Parse(traceDelayBox.Text);
                traceStopDelay = traceStartDelay;
            }

            (TimeSpan estimate, double _) = Route.EstimateTimeAndDistance(pixelSize, travelSpeed, etchSpeed, true, traceStartDelay, traceStopDelay);
            TextBlock runtimeLabel = this.FindControl<TextBlock>("RuntimeLabel");
            runtimeLabel.Text = string.Format("{0:%d}d {0:%h}h {0:%m}m {0:%s}s", estimate);
        }


        /// <summary>
        /// Saves the configuration upon shutdown.
        /// </summary>
        /// <param name="e">Not used</param>
        protected override void OnClosing(CancelEventArgs e)
        {
            try
            {
                Config.Save();
            }
            catch(Exception ex)
            {
                _ = MessageBox.Show(this, $"Error saving configuration settings: {ex.Message}.", "Error saving configuration");
            }
        }


        #region Input Validators

        /// <summary>
        /// Saves the value in <see cref="PixelSizeBox"/> to the configuration if it's valid,
        /// or resets it to the configuration value if it's invalid.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        public void PixelSizeBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if(double.TryParse(PixelSizeBox.Text, out double newValue))
            {
                Config.PixelSize = newValue;
            }
            else
            {
                PixelSizeBox.Text = Config.PixelSize.ToString();
            }
        }


        /// <summary>
        /// Saves the value in <see cref="OriginXBox"/> to the configuration if it's valid,
        /// or resets it to the configuration value if it's invalid.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        public void OriginXBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(OriginXBox.Text, out double newValue))
            {
                Config.OriginX = newValue;
            }
            else
            {
                OriginXBox.Text = Config.OriginX.ToString();
            }
        }


        /// <summary>
        /// Saves the value in <see cref="OriginYBox"/> to the configuration if it's valid,
        /// or resets it to the configuration value if it's invalid.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        public void OriginYBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(OriginYBox.Text, out double newValue))
            {
                Config.OriginY = newValue;
            }
            else
            {
                OriginYBox.Text = Config.OriginY.ToString();
            }
        }


        /// <summary>
        /// Saves the value in <see cref="ZHeightBox"/> to the configuration if it's valid,
        /// or resets it to the configuration value if it's invalid.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        public void ZHeightBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(ZHeightBox.Text, out double newValue))
            {
                Config.ZHeight = newValue;
            }
            else
            {
                ZHeightBox.Text = Config.ZHeight.ToString();
            }
        }


        /// <summary>
        /// Saves the value in <see cref="TravelSpeedBox"/> to the configuration if it's valid,
        /// or resets it to the configuration value if it's invalid.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        public void TravelSpeedBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(TravelSpeedBox.Text, out double newValue))
            {
                Config.TravelSpeed = newValue;
            }
            else
            {
                TravelSpeedBox.Text = Config.TravelSpeed.ToString();
            }
        }


        /// <summary>
        /// Saves the value in <see cref="EtchSpeedBox"/> to the configuration if it's valid,
        /// or resets it to the configuration value if it's invalid.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        public void EtchSpeedBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(EtchSpeedBox.Text, out double newValue))
            {
                Config.EtchSpeed = newValue;
            }
            else
            {
                EtchSpeedBox.Text = Config.EtchSpeed.ToString();
            }
        }


        /// <summary>
        /// Saves the value in <see cref="LaserOffCommandBox"/> to the configuration.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        public void LaserOffCommandBox_LostFocus(object sender, RoutedEventArgs e)
        {
            Config.LaserOffCommand = LaserOffCommandBox.Text;
        }


        /// <summary>
        /// Saves the value in <see cref="LaserLowCommandBox"/> to the configuration.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        public void LaserLowCommandBox_LostFocus(object sender, RoutedEventArgs e)
        {
            Config.LaserLowCommand = LaserLowCommandBox.Text;
        }


        /// <summary>
        /// Saves the value in <see cref="LaserHighCommandBox"/> to the configuration.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        public void LaserHighCommandBox_LostFocus(object sender, RoutedEventArgs e)
        {
            Config.LaserHighCommand = LaserHighCommandBox.Text;
        }


        /// <summary>
        /// Saves the value in <see cref="MoveCommandBox"/> to the configuration.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        public void MoveCommandBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Config.MoveCommand = ((ComboBoxItem)MoveCommandBox.SelectedItem).Content.ToString();
        }


        /// <summary>
        /// Saves the value in <see cref="CommentStyleBox"/> to the configuration.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        public void CommentStyleBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(CommentStyleBox.SelectedIndex == 0)
            {
                Config.CommentMode = CommentMode.Semicolon;
            }
            else
            {
                Config.CommentMode = CommentMode.Parentheses;
            }
        }


        /// <summary>
        /// Saves the value in <see cref="BoundaryPreviewToggle"/> to the configuration.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        public void BoundaryPreviewToggle_CheckChanged(object sender, RoutedEventArgs e)
        {
            Config.IsBoundaryPreviewEnabled = (BoundaryPreviewToggle.IsChecked == true);
        }


        /// <summary>
        /// Saves the value in <see cref="BoundaryPreviewDelayBox"/> to the configuration if it's valid,
        /// or resets it to the configuration value if it's invalid.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        public void BoundaryPreviewDelayBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(BoundaryPreviewDelayBox.Text, out int newValue))
            {
                Config.PreviewDelay = newValue;
            }
            else
            {
                BoundaryPreviewDelayBox.Text = Config.PreviewDelay.ToString();
            }
        }

        #endregion

    }
}
