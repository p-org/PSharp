//-----------------------------------------------------------------------
// <copyright file="IO.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System.IO;
using System.Runtime.Serialization;
using System.Windows;

using Microsoft.Win32;

using Microsoft.PSharp.TestingServices.Tracing.Error;

namespace Microsoft.PSharp.Visualization
{
    internal static class IO
    {
        /// <summary>
        /// Returns the specified trace.
        /// </summary>
        /// <returns>MachineActionTrace</returns>
        internal static BugTrace LoadTrace()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // Sets filter for file extension and default file extension.
            openFileDialog.DefaultExt = ".pstrace";
            openFileDialog.Filter = "P# Trace Files|*.pstrace";

            // Displays OpenFileDialog by calling the ShowDialog method.
            bool? result = openFileDialog.ShowDialog();

            BugTrace trace = null;
            if (result == true)
            {
                string fileName = openFileDialog.FileName;
                if (fileName != null)
                {
                    using (Stream stream = File.Open(fileName, FileMode.Open))
                    {
                        DataContractSerializer serializer = new DataContractSerializer(typeof(BugTrace));
                        try
                        {
                            trace = (BugTrace)serializer.ReadObject(stream);
                        }
                        catch
                        {
                            MessageBox.Show($"Bug trace '{fileName}' is not readable." +
                                "Please make sure you selected a '.pstrace' file.");
                        }
                    }
                }
            }

            return trace;
        }
    }
}
