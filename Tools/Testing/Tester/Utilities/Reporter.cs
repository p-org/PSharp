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

using Microsoft.PSharp.IO;
using Microsoft.PSharp.TestingServices.Coverage;

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

            string directory = CodeCoverageInstrumentation.OutputDirectory;
            if (isDebug)
            {
                directory += $"{Path.DirectorySeparatorChar}CoverageDebug";
            }

            EmitTestingCoverageOutputFiles(report, directory, file);
        }

        /// <summary>
        /// Returns (and creates if it does not exist) the output
        /// directory with an optional suffix.
        /// </summary>
        /// <param name="assemblyPath">Path of input assembly</param>
        /// <param name="userOutputDir">User-provided output path</param>
        /// <param name="suffix">Optional suffix</param>
        /// <returns>Path</returns>
        internal static string GetOutputDirectory(string userOutputDir, string assemblyPath, string suffix = "")
        {
            string directoryPath;

            if (userOutputDir != "")
            {
                directoryPath = userOutputDir + Path.DirectorySeparatorChar;
            }
            else
            {
                var subpath = Path.GetDirectoryName(assemblyPath);
                if (subpath == "")
                {
                    subpath = ".";
                }

                directoryPath = subpath +
                    Path.DirectorySeparatorChar + "Output" + Path.DirectorySeparatorChar;
            }

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
        /// <param name="report">TestReport containing CoverageInfo</param>
        /// <param name="directory">Output directory name, unique for this run</param>
        /// <param name="file">Output file name</param>
        private static void EmitTestingCoverageOutputFiles(TestReport report, string directory, string file)
        {
            var codeCoverageReporter = new CodeCoverageReporter(report.CoverageInfo);
            var filePath = $"{directory}{file}";

            string graphFilePath = $"{filePath}.dgml";
            Output.WriteLine($"..... Writing {graphFilePath}");
            codeCoverageReporter.EmitVisualizationGraph(graphFilePath);

            string coverageFilePath = $"{filePath}.coverage.txt";
            Output.WriteLine($"..... Writing {coverageFilePath}");
            codeCoverageReporter.EmitCoverageReport(coverageFilePath);

            string serFilePath = $"{filePath}.sci";
            Output.WriteLine($"..... Writing {serFilePath}");
            using (var fs = new FileStream(serFilePath, FileMode.Create))
            {
                var serializer = new DataContractSerializer(typeof(CoverageInfo));
                serializer.WriteObject(fs, report.CoverageInfo);
            }
        }

        #endregion
    }
}
