//-----------------------------------------------------------------------
// <copyright file="TestingProcessScheduler.cs">
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

using Microsoft.PSharp.TestingServices.Coverage;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// The P# testing reporter.
    /// </summary>
    internal sealed class Reporter
    {
        #region internal static methods

        /// <summary>
        /// Emits the testing coverage report.
        /// </summary>
        /// <param name="report">TestReport</param>
        /// <param name="processId">Optional process id that produced the report</param>
        /// <param name="isDebug">Is a debug report</param>
        internal static void EmitTestingCoverageReport(TestReport report, uint? processId = null, bool isDebug = false)
        {
            string file = Path.GetFileNameWithoutExtension(report.Configuration.AssemblyToBeAnalyzed);
            if (isDebug && processId != null)
            {
                file += "_" + processId;
            }

            string directory = "";
            if (isDebug)
            {
                directory = GetOutputDirectory(report.Configuration.AssemblyToBeAnalyzed, "CoverageDebug");
            }
            else
            {
                directory = GetOutputDirectory(report.Configuration.AssemblyToBeAnalyzed);
            }

            EmitTestingCoverageOutputFiles(report, directory, file);
        }

        /// <summary>
        /// Returns (and creates if it does not exist) the output
        /// directory with an optional suffix.
        /// </summary>
        /// <param name="path">Path</param>
        /// <param name="suffix">Optional suffix</param>
        /// <returns>Path</returns>
        internal static string GetOutputDirectory(string path, string suffix = "")
        {
            string directoryPath = Path.GetDirectoryName(path) +
                Path.DirectorySeparatorChar + "Output" + Path.DirectorySeparatorChar;
            if (suffix.Length > 0)
            {
                directoryPath += suffix + Path.DirectorySeparatorChar;
            }

            Directory.CreateDirectory(directoryPath);
            return directoryPath;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Emits all the testing coverage related output files.
        /// </summary>
        /// <param name="report">TestReport</param>
        /// <param name="directory">Directory name</param>
        /// <param name="file">File name</param>
        private static void EmitTestingCoverageOutputFiles(TestReport report, string directory, string file)
        {
            var codeCoverageReporter = new CodeCoverageReporter(report.CoverageInfo);

            string[] graphFiles = Directory.GetFiles(directory, file + "_*.dgml");
            string graphFilePath = directory + file + "_" + graphFiles.Length + ".dgml";

            IO.Error.PrintLine($"..... Writing {graphFilePath}");
            codeCoverageReporter.EmitVisualizationGraph(graphFilePath);

            string[] coverageFiles = Directory.GetFiles(directory, file + "_*.coverage.txt");
            string coverageFilePath = directory + file + "_" + coverageFiles.Length + ".coverage.txt";

            IO.Error.PrintLine($"..... Writing {coverageFilePath}");
            codeCoverageReporter.EmitCoverageReport(coverageFilePath);

            string[] serFiles = Directory.GetFiles(directory, file + "_*.sci");
            string serFilePath = directory + file + "_" + serFiles.Length + ".sci";

            IO.Error.PrintLine($"..... Writing {serFilePath}");
            using (var fs = new FileStream(serFilePath, FileMode.Create))
            {
                var serializer = new DataContractSerializer(typeof(CoverageInfo));
                serializer.WriteObject(fs, report.CoverageInfo);
            }
        }

        #endregion
    }
}
