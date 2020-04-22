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
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Visuals.Media.Imaging;
using System;
using System.Threading.Tasks;

namespace OpenEtch
{
    /// <summary>
    /// This is the main UI window for OpenEtch.
    /// </summary>
    public class MainWindow : Window
    {
        /// <summary>
        /// Creates a new MainWindow instance.
        /// </summary>
        public MainWindow()
        {
            AvaloniaXamlLoader.Load(this);
#if DEBUG
            this.AttachDevTools();
#endif
        }


        /// <summary>
        /// Loads an image from the filesystem.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        public async void LoadImage_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
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

            // Try to load the image
            string imagePath = selection[0];
            Image preview = this.FindControl<Image>("Preview");
            try
            {
                preview.Source = new Bitmap(imagePath);
            }
            catch(Exception ex)
            {
                await MessageBox.Show(this, $"Error loading image: {ex.Message}", "Error loading image");
                return;
            }

            Button calculatePathsButton = this.FindControl<Button>("CalculatePathsButton");
            Button exportButton = this.FindControl<Button>("ExportButton");
            calculatePathsButton.IsEnabled = true;
            exportButton.IsEnabled = false;
        }


        /// <summary>
        /// Determines the optimal etching route for the loaded image.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        public void CalculatePaths_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // NYI
        }


        /// <summary>
        /// Exports the calcualted etching route to a G-code file.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        public void Export_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // NYI
        }

    }
}
