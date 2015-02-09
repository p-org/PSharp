//-----------------------------------------------------------------------
// <copyright file="Program.cs">
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

namespace PSharp
{
    /// <summary>
    /// Static analyser for the P# domain specific language using the
    /// Roslyn compiler-as-a-service framework.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            // Parses the command line options.
            new CommandLineOptions(args).Parse();

            // Starts profiling the analysis.
            if (Configuration.ShowRuntimeResults)
            {
                Profiler.StartMeasuringExecutionTime();
            }

            // Create a new P# analysis context.
            AnalysisContext.Create();

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

            // Prints error statistics and profiling results.
            ErrorReporter.PrintStats();
            Profiler.PrintResults();

            // Prints program statistics.
            AnalysisContext.PrintStatistics();
        }
    }
}
