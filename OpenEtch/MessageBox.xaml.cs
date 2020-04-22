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
using System.Threading.Tasks;

namespace OpenEtch
{
    /// <summary>
    /// This is a simple modal dialog for displaying messages, such as errors or status updates.
    /// </summary>
    public class MessageBox : Window
    {
        /// <summary>
        /// Creates a new <see cref="MessageBox"/> instance.
        /// </summary>
        public MessageBox()
        {
            AvaloniaXamlLoader.Load(this);
#if DEBUG
            this.AttachDevTools();
#endif
        }


        /// <summary>
        /// Shows a <see cref="MessageBox"/> modal dialog.
        /// </summary>
        /// <param name="Parent">The <see cref="Window"/> that owns this dialog</param>
        /// <param name="MessageText">The text to display in the body of the dialog</param>
        /// <param name="WindowTitle">The dialog's window title</param>
        /// <returns>Nothing</returns>
        public static async Task Show(Window Parent, string MessageText, string WindowTitle)
        {
            MessageBox messageBox = new MessageBox()
            {
                Title = WindowTitle,
                Owner = Parent
            };
            TextBlock messageTextBox = messageBox.FindControl<TextBlock>("MessageText");
            messageTextBox.Text = MessageText;

            await messageBox.ShowDialog(Parent);
        }


        /// <summary>
        /// Closes the dialog when the OK button is pressed.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        public void OkButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Close();
        }

    }
}
