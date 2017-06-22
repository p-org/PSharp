//-----------------------------------------------------------------------
// <copyright file="CodeCoverageInstrumentation.cs">
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

using Microsoft.PSharp.IO;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// Instruments a binary for code coverage.
    /// </summary>
    internal static class CodeCoverageInstrumentation
    {
        private static Configuration Configuration;
        internal static string OutputDirectory = string.Empty;

        internal static void Instrument(Configuration configuration)
        {
            Configuration = configuration;
            SetOutputDirectory();

            string VSInstrToolPath = GetToolPath("VSInstrToolPath", "VSInstr");
            var instrProc = new Process();

            int exitCode;
            string error;

            try
            {
                instrProc.StartInfo.FileName = VSInstrToolPath;
                instrProc.StartInfo.Arguments = $"/coverage {Configuration.AssemblyToBeAnalyzed}";
                instrProc.StartInfo.UseShellExecute = false;
                instrProc.StartInfo.RedirectStandardOutput = true;
                instrProc.StartInfo.RedirectStandardError = true;
                instrProc.Start();

                error = instrProc.StandardError.ReadToEnd();

                instrProc.WaitForExit();
                exitCode = instrProc.ExitCode;
            }
            finally
            {
                instrProc.Close();
            }

            // Exit code 0 means that the file was instrumented successfully.
            // Exit code 4 means that the file was already instrumented.
            if (exitCode != 0 && exitCode != 4)
            {
                Error.Report($"[PSharpTester] 'VSInstr' failed to instrument '{Configuration.AssemblyToBeAnalyzed}'.");
                IO.Debug.WriteLine(error);
                Environment.Exit(1);
            }
        }

        internal static void Restore()
        {
            // VSInstr creates a backup of the uninstrumented .exe with the suffix ".exe.orig", and
            // writes an instrumented .pdb with the suffix ".instr.pdb". We must restore the uninstrumented
            // .exe after the coverage run, and viewing the coverage file requires the instrumented .exe,
            // so move the instrumented files to the output directory and restore the uninstrumented .exe.
            var origExe = $"{Configuration.AssemblyToBeAnalyzed}.orig";
            var origDir = Path.GetDirectoryName(Configuration.AssemblyToBeAnalyzed) + Path.DirectorySeparatorChar;
            var instrExe = $"{OutputDirectory}{Path.GetFileName(Configuration.AssemblyToBeAnalyzed)}";
            var instrPdb = $"{Path.GetFileNameWithoutExtension(Configuration.AssemblyToBeAnalyzed)}.instr.pdb";
            try
            {
                if (!string.IsNullOrEmpty(OutputDirectory) && File.Exists(origExe))
                {
                    if (TestingProcessScheduler.ProcessCanceled)
                    {
                        File.Delete(Configuration.AssemblyToBeAnalyzed);
                        File.Delete(instrPdb);
                        Directory.Delete(OutputDirectory, true);
                    }
                    else
                    {
                        File.Move(Configuration.AssemblyToBeAnalyzed, instrExe);
                        File.Move($"{origDir}{instrPdb}", $"{OutputDirectory}{instrPdb}");
                    }
                    File.Move(origExe, Configuration.AssemblyToBeAnalyzed);
                }
            }
            catch (IOException ex)
            {
                // Don't exit here as we're already shutting down the app.
                Error.Report($"[PSharpTester] Failed to restore non-instrumented '{Configuration.AssemblyToBeAnalyzed}': {ex.Message}.");
            }
            finally
            {
                OutputDirectory = string.Empty;
            }
        }

        /// <summary>
        /// Returns the tool path to the code coverage instrumentor.
        /// </summary>
        /// <returns>Tool path</returns>
        internal static string GetToolPath(string settingName, string toolName)
        {
            string toolPath = "";
            try
            {
                toolPath = ConfigurationManager.AppSettings[settingName];
            }
            catch (ConfigurationErrorsException)
            {
                Error.ReportAndExit($"[PSharpTester] required '{settingName}' value is not set in configuration file.");
            }

            if (!File.Exists(toolPath))
            {
                Error.ReportAndExit($"[PSharpTester] '{toolName}' tool '{toolPath}' not found.");
            }
            return toolPath;
        }

        /// <summary>
        /// Returns a unique output directory name based on current date and time.
        /// </summary>
        /// <returns>Name</returns>
        private static void SetOutputDirectory()
        {
            var now = DateTime.Now;
            var suffix = $"CodeCoverage{Path.DirectorySeparatorChar}{now:MM}{now:dd}{now:yy}_{now:HH}{now:mm}{now:ss}";
            OutputDirectory = Reporter.GetOutputDirectory(Configuration.OutputFilePath, Configuration.AssemblyToBeAnalyzed, suffix);
        }
    }
}