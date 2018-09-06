#if NET46 || NET45
// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.IO;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// Monitors the program being tested for code coverage.
    /// </summary>
    internal static class CodeCoverageMonitor
    {
        /// <summary>
        /// Configuration.
        /// </summary>
        private static Configuration Configuration;

        /// <summary>
        /// Monitoring process is running.
        /// </summary>
        internal static bool IsRunning;

        /// <summary>
        /// Starts the code coverage monitor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        internal static void Start(Configuration configuration)
        {
            if (IsRunning)
            {
                throw new InvalidOperationException("Process has already started.");
            }

            Configuration = configuration;
            RunMonitorProcess(true);
            IsRunning = true;
        }

        /// <summary>
        /// Stops the code coverage monitor.
        /// </summary>
        internal static void Stop()
        {
            if (Configuration == null)
            {
                throw new InvalidOperationException("Process has not been configured.");
            }

            if (!IsRunning)
            {
                throw new InvalidOperationException("Process is not running.");
            }

            RunMonitorProcess(false);
            IsRunning = false;
        }

        private static void RunMonitorProcess(bool isStarting)
        {
            var error = string.Empty;
            var exitCode = 0;
            var outputFile = GetOutputName();
            var arguments = isStarting ? $"/start:coverage /output:{outputFile}" : "/shutdown";
            var timedOut = false;
            using (var monitorProc = new Process())
            {
                monitorProc.StartInfo.FileName = CodeCoverageInstrumentation.GetToolPath("VSPerfCmdToolPath", "VSPerfCmd");
                monitorProc.StartInfo.Arguments = arguments;
                monitorProc.StartInfo.UseShellExecute = false;
                monitorProc.StartInfo.RedirectStandardOutput = true;
                monitorProc.StartInfo.RedirectStandardError = true;
                monitorProc.Start();

                Output.WriteLine($"... {(isStarting ? "Starting" : "Shutting down")} code coverage monitor");

                // timedOut can only become true on shutdown (non-infinite timeout value)
                timedOut = !monitorProc.WaitForExit(isStarting ? Timeout.Infinite : 5000);
                if (!timedOut)
                {
                    exitCode = monitorProc.ExitCode;
                    if (exitCode != 0)
                    {
                        error = monitorProc.StandardError.ReadToEnd();
                    }
                }
            }

            if (exitCode != 0 || error.Length > 0)
            {
                if (error.Length == 0)
                {
                    error = "<no error message returned>";
                }
                Output.WriteLine($"Warning: 'VSPerfCmd {arguments}' exit code {exitCode}: {error}");
            }
            if (!isStarting)
            {
                if (timedOut)
                {
                    Output.WriteLine($"Warning: VsPerfCmd timed out on shutdown");
                }

                if (File.Exists(outputFile))
                {
                    var fileInfo = new FileInfo(outputFile);
                    Output.WriteLine($"..... Created {outputFile}");
                }
                else
                {
                    Output.WriteLine($"Warning: Code coverage output file {outputFile} was not created");
                }
            }
        }

        /// <summary>
        /// Returns the output name.
        /// </summary>
        /// <returns>Name</returns>
        private static string GetOutputName()
        {
            string file = Path.GetFileNameWithoutExtension(Configuration.AssemblyToBeAnalyzed);
            string directory = CodeCoverageInstrumentation.OutputDirectory;
            return $"{directory}{file}.coverage";
        }
    }
}
#endif
