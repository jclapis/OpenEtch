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

namespace OpenEtch
{
    /// <summary>
    /// This is the main UI window for OpenEtch.
    /// </summary>
    public class MainWindow : Window
    {
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
            ImageProcessor = new ImageProcessor();
            Router = new Router();
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
        public void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            // NYI
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

            TimeSpan estimate = Route.EstimateTime(pixelSize, travelSpeed, etchSpeed, traceStartDelay, traceStopDelay);
            TextBlock runtimeLabel = this.FindControl<TextBlock>("RuntimeLabel");
            runtimeLabel.Text = string.Format("{0:%d}d {0:%h}h {0:%m}m {0:%s}s", estimate);
        }

    }
}
