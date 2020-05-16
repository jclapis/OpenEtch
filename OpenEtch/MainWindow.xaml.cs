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
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace OpenEtch
{
    /// <summary>
    /// The mode to use when creating an etching route for the image
    /// </summary>
    internal enum EtchMode
    {
        /// <summary>
        /// Raster mode (where the engraver will go through the image vertically,
        /// horizontal line-by-line, etching all of the black pixels, thus fully
        /// recreating the image)
        /// </summary>
        Raster,


        /// <summary>
        /// Stencil mode (where the engraver will only trace the outlines of
        /// connected bodies of black pixels in the image, but will leave the middle
        /// of the bodies alone - good for creating stencils)
        /// </summary>
        Stencil
    }


    /// <summary>
    /// This is the main UI window for OpenEtch.
    /// </summary>
    public class MainWindow : Window
    {
        #region Controls

        private readonly TextBox PassesBox;
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
        private readonly CheckBox HomeToggle;
        private readonly CheckBox BoundaryPreviewToggle;
        private readonly TextBox BoundaryPreviewDelayBox;

        #endregion

        /// <summary>
        /// The settings and configuration
        /// </summary>
        private readonly Configuration Config;


        /// <summary>
        /// The router for creating raster etch routes from images
        /// </summary>
        private readonly RasterRouter RasterRouter;


        /// <summary>
        /// The router for creating stencil etch routes from images
        /// </summary>
        private readonly StencilRouter StencilRouter;


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
        private EtchableImage ImageToEtch;


        /// <summary>
        /// The etching route for the loaded image
        /// </summary>
        private Route Route;


        /// <summary>
        /// The etching mode to use for routing and G-code generation
        /// </summary>
        private EtchMode Mode;


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
            PassesBox = this.FindControl<TextBox>("PassesBox");
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
            HomeToggle = this.FindControl<CheckBox>("HomeToggle");
            BoundaryPreviewToggle = this.FindControl<CheckBox>("BoundaryPreviewToggle");
            BoundaryPreviewDelayBox = this.FindControl<TextBox>("BoundaryPreviewDelayBox");

            // File drag and drop support
            AddHandler(DragDrop.DropEvent, Drop);

            // Load the saved configuration, or fallback to the defaults
            Config = new Configuration("Configuration.toml");
            try
            {
                Config.Load();

                PassesBox.Text = Config.Passes.ToString();
                PixelSizeBox.Text = Config.PixelSize.ToString();
                OriginXBox.Text = Config.OriginX.ToString();
                OriginYBox.Text = Config.OriginY.ToString();
                ZHeightBox.Text = Config.ZHeight.ToString();
                TravelSpeedBox.Text = Config.TravelSpeed.ToString();
                EtchSpeedBox.Text = Config.EtchSpeed.ToString();
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

                HomeToggle.IsChecked = Config.HomeXY;
                BoundaryPreviewToggle.IsChecked = Config.IsBoundaryPreviewEnabled;
                BoundaryPreviewDelayBox.Text = Config.PreviewDelay.ToString();
            }
            catch(Exception ex)
            {
                _ = MessageBox.Show(this, $"Error loading configuration: {ex.Message}. " +
                    $"Configuration will be set to the default values.", "Error loading configuration");
            }

            // Load the processors
            Mode = EtchMode.Raster;
            RasterRouter = new RasterRouter(Config);
            StencilRouter = new StencilRouter(Config);
            Exporter = new GcodeExporter(Config);
        }


        /// <summary>
        /// Loads an image from the filesystem.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        public async void LoadImageButton_Click(object sender, RoutedEventArgs e)
        {
            Button recalculateButton = this.FindControl<Button>("RecalculateButton");
            Button exportButton = this.FindControl<Button>("ExportButton");
            recalculateButton.IsEnabled = false;
            exportButton.IsEnabled = false;
            ImageToEtch = null;
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
                RecalculateRoute();
                recalculateButton.IsEnabled = true;
                exportButton.IsEnabled = true;
                OriginalFilename = System.IO.Path.GetFileName(imagePath);
                Title = $"OpenEtch v{VersionInfo.Version} - {OriginalFilename}";
            }
            catch(Exception ex)
            {
                ImageToEtch = null;
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
            RecalculateRoute();
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

                // Run it!
                Exporter.ExportGcode(targetFilename, OriginalFilename, Route);

                await MessageBox.Show(this, $"File {System.IO.Path.GetFileName(targetFilename)} successfully exported.", "Export succeeded");
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
            ImageToEtch = new EtchableImage(ImagePath);
            Slider luminanceSlider = this.FindControl<Slider>("LuminanceSlider");
            ImageToEtch.ProcessImage(1.0 - luminanceSlider.Value);
            preview.Source = ImageToEtch.Bitmap;
            preview.InvalidateVisual();

            // Set the image dimension labels
            TextBlock imageWidthLabel = this.FindControl<TextBlock>("ImageWidthLabel");
            TextBlock imageHeightLabel = this.FindControl<TextBlock>("ImageHeightLabel");
            imageWidthLabel.Text = $"{ImageToEtch.Width} px";
            imageHeightLabel.Text = $"{ImageToEtch.Height} px";

            // Route it
            RecalculateRoute();
        }


        /// <summary>
        /// Recalculates the etch route, including the path, the physical dimensions of the target,
        /// and the runtime estimate for the laser etcher.
        /// </summary>
        private void RecalculateRoute()
        {
            if(Mode == EtchMode.Raster)
            {
                Route = RasterRouter.Route(ImageToEtch);
            }
            else
            {
                //Route = StencilRouter.Route(ImageToEtch);
                ColorBodyOutlines();
                return;
            }

            double pixelSize = Config.PixelSize;
            double targetWidth = ImageToEtch.Width * pixelSize;
            double targetHeight = ImageToEtch.Height * pixelSize;

            TextBlock targetWidthLabel = this.FindControl<TextBlock>("TargetWidthLabel");
            TextBlock targetHeightLabel = this.FindControl<TextBlock>("TargetHeightLabel");
            targetWidthLabel.Text = $"{targetWidth:N2} mm";
            targetHeightLabel.Text = $"{targetHeight:N2} mm";

            (TimeSpan estimate, double _) = Route.EstimateTimeAndDistance(true);
            estimate *= Config.Passes;
            TextBlock runtimeLabel = this.FindControl<TextBlock>("RuntimeLabel");
            runtimeLabel.Text = string.Format("{0:%d}d {0:%h}h {0:%m}m {0:%s}s", estimate);

            Button exportButton = this.FindControl<Button>("ExportButton");
            exportButton.IsEnabled = true;
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


        /// <summary>
        /// Loads a picture from a file when it's dropped onto the preview image control.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">The drop event arguments containing the contents that were dropped</param>
        private async void Drop(object sender, DragEventArgs e)
        {
            IEnumerable<string> files = e.Data.GetFileNames();
            if(files.Count() > 0)
            {
                string imagePath = files.ElementAt(0);
                try
                {
                    Button recalculateButton = this.FindControl<Button>("RecalculateButton");
                    Button exportButton = this.FindControl<Button>("ExportButton");
                    recalculateButton.IsEnabled = false;
                    exportButton.IsEnabled = false;
                    ImageToEtch = null;
                    Route = null;
                    Title = $"OpenEtch v{VersionInfo.Version}";

                    LoadImage(imagePath);
                    recalculateButton.IsEnabled = true;
                    exportButton.IsEnabled = true;
                    OriginalFilename = System.IO.Path.GetFileName(imagePath);
                    Title = $"OpenEtch v{VersionInfo.Version} - {OriginalFilename}";
                }
                catch (Exception ex)
                {
                    ImageToEtch = null;
                    Route = null;
                    await MessageBox.Show(this, $"Error loading image: {ex.Message}", "Error loading image");
                    return;
                }
            }
        }


        /// <summary>
        /// Updates the luminance threshold when the brightness slider's value changes.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">The args containing the new slider value</param>
        public void LuminanceSlider_Changed(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if(e.Property.Name != "Value")
            {
                return;
            }

            double newThreshold = (double)e.NewValue;
            if(ImageToEtch != null)
            {
                Image preview = this.FindControl<Image>("Preview");
                ImageToEtch.ProcessImage(1.0 - newThreshold);
                preview.InvalidateVisual();
                Button exportButton = this.FindControl<Button>("ExportButton");
                exportButton.IsEnabled = false;
            }
        }


        /// <summary>
        /// Changes the active mode to raster mode when the Raster Mode radio is
        /// checked.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        public void RasterMode_Checked(object sender, RoutedEventArgs e)
        {
            Mode = EtchMode.Raster;
        }


        /// <summary>
        /// Changes the active mode to stencil mode when the Stencil Mode radio is
        /// checked.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        public void StencilMode_Checked(object sender, RoutedEventArgs e)
        {
            Mode = EtchMode.Stencil;
        }


        /// <summary>
        /// Debug function that finds the bodies in the image with the stencil router, and
        /// assigns each of them a random color in the preview image.
        /// </summary>
        private void ColorImageBodies()
        {
            List<Body> bodies = StencilRouter.FindBodies(ImageToEtch);
            using (Avalonia.Platform.ILockedFramebuffer buffer = ImageToEtch.Bitmap.Lock())
            {
                Random rng = new Random();
                foreach (Body body in bodies)
                {
                    byte randomBlue = (byte)rng.Next(1, 255);
                    byte randomGreen = (byte)rng.Next(1, 255);
                    byte randomRed = (byte)rng.Next(1, 255);
                    int pixelValue;
                    unchecked
                    {
                        pixelValue = (int)0xFF000000;
                        pixelValue |= (randomRed << 16) |
                                      (randomGreen << 8) |
                                      randomBlue;
                    }

                    foreach (Point point in body.Points)
                    {
                        IntPtr rowOffsetAddress = buffer.Address + (point.Y * buffer.Size.Width * 4);
                        IntPtr pixelAddress = rowOffsetAddress + (point.X * 4);
                        Marshal.WriteInt32(pixelAddress, pixelValue);
                    }
                }
            }

            Image preview = this.FindControl<Image>("Preview");
            preview.InvalidateVisual();
        }


        /// <summary>
        /// Debug function that colors the outline of a body, to validate stencil etching paths.
        /// </summary>
        private void ColorBodyOutlines()
        {
            List<Body> bodies = StencilRouter.FindBodies(ImageToEtch);
            List<Path> outlines = new List<Path>();
            foreach(Body body in bodies)
            {
                outlines.Add(body.Outline);
            }

            using (Avalonia.Platform.ILockedFramebuffer buffer = ImageToEtch.Bitmap.Lock())
            {
                foreach (Path outline in outlines)
                {
                    double numberOfPoints = outline.Points.Count;
                    double halfMark = numberOfPoints / 2.0;

                    for(int i = 0; i < numberOfPoints; i++)
                    {
                        Point point = outline.Points[i];

                        // Green stays at 255 until the halfway mark, then linearly decreases to 0
                        double greenScale = Math.Max(0, i - halfMark);
                        byte green = (byte)Math.Round((halfMark - greenScale) / halfMark * 255.0);

                        // Red starts at 0 and linearly increases to 255 at the halfway mark
                        byte red = (byte)Math.Round(Math.Min(1.0, i / halfMark) * 255.0);

                        int pixelValue;
                        unchecked
                        {
                            pixelValue = (int)0xFF000000;
                            pixelValue |= (red << 16) |
                                          (green << 8);
                        }

                        IntPtr rowOffsetAddress = buffer.Address + (point.Y * buffer.Size.Width * 4);
                        IntPtr pixelAddress = rowOffsetAddress + (point.X * 4);
                        Marshal.WriteInt32(pixelAddress, pixelValue);
                    }
                }
            }

            Image preview = this.FindControl<Image>("Preview");
            preview.InvalidateVisual();
        }


        #region Input Validators

        /// <summary>
        /// Saves the value in <see cref="PassesBox"/> to the configuration if it's valid,
        /// or resets it to the configuration value if it's invalid.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        public void PassesBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(PassesBox.Text, out int newValue))
            {
                Config.Passes = newValue;
            }
            else
            {
                PassesBox.Text = Config.Passes.ToString();
            }
        }

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
        /// Saves the value in <see cref="HomeToggle"/> to the configuration.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        public void HomeToggle_CheckChanged(object sender, RoutedEventArgs e)
        {
            Config.HomeXY = (HomeToggle.IsChecked == true);
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
