// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

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
