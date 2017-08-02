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
        internal static void Instrument(string binary)
        {
            string VSInstrToolPath = GetToolPath();
            var instrProc = new Process();

            int exitCode;
            string error;

            try
            {
                instrProc.StartInfo.FileName = VSInstrToolPath;
                instrProc.StartInfo.Arguments = "/coverage " + binary;
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
                Error.Report($"[PSharpTester] 'VSInstr' failed to instrument '{binary}'.");
                IO.Debug.WriteLine(error);
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Returns the tool path to the code coverage instrumentor.
        /// </summary>
        /// <returns>Tool path</returns>
        private static string GetToolPath()
        {
            string tool = "";
            try
            {
                tool = ConfigurationManager.AppSettings["VSInstrToolPath"];
            }
            catch (ConfigurationErrorsException)
            {
                Error.ReportAndExit("[PSharpTester] required 'VSInstrToolPath' value is not set in configuration file.");
            }

            if (!File.Exists(tool))
            {
                Error.ReportAndExit($"[PSharpTester] 'VSInstr' tool '{tool}' not found.");
            }

            return tool;
        }
    }
}