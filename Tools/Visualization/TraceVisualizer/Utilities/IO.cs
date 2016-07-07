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

using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

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
            //openFileDialog.Filter = "TXT Files (*.txt)";

            // Displays OpenFileDialog by calling the ShowDialog method.
            bool? result = openFileDialog.ShowDialog();

            BugTrace trace = null;
            if (result == true)
            {
                string fileName = openFileDialog.FileName;
                using (Stream stream = File.Open(fileName, FileMode.Open))
                {
                    DataContractSerializer serializer = new DataContractSerializer(typeof(BugTrace));
                    trace = (BugTrace)serializer.ReadObject(stream);
                }
            }

            return trace;
        }
    }
}
