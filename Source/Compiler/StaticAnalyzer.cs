﻿//-----------------------------------------------------------------------
// <copyright file="StaticAnalyzer.cs">
//      Copyright (c) 2015 Pantazis Deligiannis (p.deligiannis@imperial.ac.uk)
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

using System;

using Microsoft.CodeAnalysis;

using Microsoft.PSharp.StaticAnalysis;
using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Static analyser for the P# language.
    /// </summary>
    internal static class StaticAnalyzer
    {
        /// <summary>
        /// Starts the P# static analyser.
        /// </summary>
        public static void Run()
        {
            if (!Configuration.RunStaticAnalysis)
            {
                return;
            }

            foreach (var project in ProgramInfo.Solution.Projects)
            {
                Output.Print(". Analyzing " + project.Name);
                StaticAnalyzer.AnalyseProgramUnit(project);
            }

            // Prints error statistics and profiling results.
            AnalysisErrorReporter.PrintStats();

            // Prints program statistics.
            if (Configuration.ShowProgramStatistics)
            {
                AnalysisContext.PrintStatistics();
            }
        }

        /// <summary>
        /// Analyse the given P# project.
        /// </summary>
        /// <param name="project">Project</param>
        private static void AnalyseProgramUnit(Project project)
        {
            // Starts profiling the analysis.
            if (Configuration.ShowRuntimeResults)
            {
                Profiler.StartMeasuringExecutionTime();
            }

            // Create a P# analysis context.
            AnalysisContext.Create(project);

            // Runs an analysis that performs an initial sanity checking
            // to see if machine code behaves as it should.
            SanityCheckingAnalysis.Run();

            // Runs an analysis that finds if a machine exposes any fields
            // or methods to other machines.
            RuntimeOnlyDirectAccessAnalysis.Run();

            // Runs an analysis that computes the summary for every
            // method in each machine.
            MethodSummaryAnalysis.Run();
            if (Configuration.ShowGivesUpInformation)
            {
                MethodSummaryAnalysis.PrintGivesUpResults();
            }

            // Runs an analysis that constructs the state transition graph
            // for each machine.
            if (Configuration.DoStateTransitionAnalysis)
            {
                StateTransitionAnalysis.Run();
            }

            // Runs an analysis that detects if all methods in each machine
            // respect given up ownerships.
            RespectsOwnershipAnalysis.Run();

            // Stops profiling the analysis.
            if (Configuration.ShowRuntimeResults)
            {
                Profiler.StopMeasuringExecutionTime();
            }

            Profiler.PrintResults();
        }
    }
}
