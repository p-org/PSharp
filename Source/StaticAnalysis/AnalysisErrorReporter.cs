//-----------------------------------------------------------------------
// <copyright file="AnalysisErrorReporter.cs">
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

using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// Reports static analysis errors and warnings to the user.
    /// </summary>
    public class AnalysisErrorReporter : ErrorReporter
    {
        #region Fields

        /// <summary>
        /// Number of errors discovered in the analysis.
        /// </summary>
        protected static int ErrorCount = 0;

        /// <summary>
        /// Number of warnings reported by the analysis.
        /// </summary>
        protected static int WarningCount = 0;

        #endregion

        #region public API

        /// <summary>
        /// Prints static analysis error statistics.
        /// </summary>
        public static void PrintStats()
        {
            var errorStr = "error";
            if (AnalysisErrorReporter.ErrorCount > 1)
            {
                errorStr = "errors";
            }

            var warningStr = "warning";
            if (AnalysisErrorReporter.WarningCount > 1)
            {
                warningStr = "warnings";
            }

            if ((AnalysisErrorReporter.ErrorCount > 0 || AnalysisErrorReporter.WarningCount > 0) &&
                Configuration.ShowWarnings)
            {
                Output.Print("... Static analysis detected '{0}' {1} and reported '{2}' {3}",
                    AnalysisErrorReporter.ErrorCount, errorStr,
                    AnalysisErrorReporter.WarningCount, warningStr);
            }
            else if (AnalysisErrorReporter.ErrorCount > 0)
            {
                Output.Print("... Static analysis detected '{0}' {1}",
                    AnalysisErrorReporter.ErrorCount, errorStr);
            }
            else
            {
                Output.Print("... No static analysis errors detected (but absolutely no warranty provided)");
            }
        }
        
        #endregion

        #region generic errors API

        /// <summary>
        /// Reports use of external asynchrony.
        /// </summary>
        /// <param name="log">Log</param>
        internal static void ReportExternalAsynchronyUsage(Log log)
        {
            AnalysisErrorReporter.ReportGenericError(log,
                "Machine '{0}' is trying to use non-P# asynchronous operations. " +
                "This can lead to data races and is *strictly* not allowed.",
                log.Machine);
            Environment.Exit(1);
        }

        /// <summary>
        /// Reports a runtime only method access error.
        /// </summary>
        /// <param name="log">Log</param>
        internal static void ReportRuntimeOnlyMethodAccess(Log log)
        {
            if (log.State == null)
            {
                AnalysisErrorReporter.ReportGenericError(log,
                    "Method '{0}' of machine '{1}' is trying to access a P# " +
                    "runtime only method.", log.Method, log.Machine);
            }
            else
            {
                AnalysisErrorReporter.ReportGenericError(log,
                    "Method '{0}' in state {1} of machine '{2}' is trying to " +
                    "access a P# runtime only method.",
                    log.Method, log.State, log.Machine);
            }

            Environment.Exit(1);
        }

        /// <summary>
        /// Reports an explicit state initialisation error.
        /// </summary>
        /// <param name="log">Log</param>
        internal static void ReportExplicitStateInitialisation(Log log)
        {
            if (log.State == null)
            {
                AnalysisErrorReporter.ReportGenericError(log,
                    "Method '{0}' of machine '{1}' is trying to explicitly " +
                    "initialize a machine state.",
                    log.Method, log.Machine);
            }
            else
            {
                AnalysisErrorReporter.ReportGenericError(log,
                    "Method '{0}' in state {1} of machine '{2}' is trying to " +
                    "explicitly initialize a machine state.",
                    log.Method, log.State, log.Machine);
            }

            Environment.Exit(1);
        }

        #endregion

        #region data race source errors API

        /// <summary>
        /// Reports a given up field ownership error.
        /// </summary>
        /// <param name="log">Log</param>
        internal static void ReportGivenUpFieldOwnershipError(Log log)
        {
            if (log.State == null)
            {
                AnalysisErrorReporter.ReportDataRaceSource(log,
                    "Method '{0}' of machine '{1}' sends payload '{2}', which " +
                    "contains data from a machine field.",
                    log.Method, log.Machine, log.Payload);
            }
            else
            {
                AnalysisErrorReporter.ReportDataRaceSource(log,
                    "Method '{0}' in state '{1}' of machine '{2}' sends payload " +
                    "'{3}', which contains data from a machine field.",
                    log.Method, log.State, log.Machine, log.Payload);
            }
        }

        /// <summary>
        /// Reports assignment of payload to a machine field.
        /// </summary>
        /// <param name="log">Log</param>
        internal static void ReportPayloadFieldAssignment(Log log)
        {
            if (log.State == null)
            {
                AnalysisErrorReporter.ReportDataRaceSource(log,
                    "Method '{0}' of machine '{1}' assigns the latest received " +
                    "payload to a machine field.",
                    log.Method, log.Machine);
            }
            else
            {
                AnalysisErrorReporter.ReportDataRaceSource(log,
                    "Method '{0}' in state '{1}' of machine '{2}' assigns " +
                    "the latest received payload to a machine field.",
                    log.Method, log.State, log.Machine);
            }
        }

        #endregion

        #region ownership errors API

        /// <summary>
        /// Reports assignment of given up ownership to a machine field.
        /// </summary>
        /// <param name="log">Log</param>
        internal static void ReportGivenUpOwnershipFieldAssignment(Log log)
        {
            if (log.State == null)
            {
                AnalysisErrorReporter.ReportOwnershipError(log,
                    "Method '{0}' of machine '{1}' assigns '{2}' to " +
                    "a machine field after giving up its ownership.",
                    log.Method, log.Machine, log.Payload);
            }
            else
            {
                AnalysisErrorReporter.ReportOwnershipError(log,
                    "Method '{0}' in state '{1}' of machine '{2}' assigns " +
                    "'{3}' to a machine field after giving up its ownership.",
                    log.Method, log.State, log.Machine, log.Payload);
            }
        }

        /// <summary>
        /// Reports sending data with a given up ownership.
        /// </summary>
        /// <param name="log">Log</param>
        internal static void ReportGivenUpOwnershipSending(Log log)
        {
            if (log.State == null)
            {
                AnalysisErrorReporter.ReportOwnershipError(log,
                    "Method '{0}' of machine '{1}' sends an event that contains " +
                    "payload with already given up ownership.",
                    log.Method, log.Machine);
            }
            else
            {
                AnalysisErrorReporter.ReportOwnershipError(log,
                    "Method '{0}' in state '{1}' of machine '{2}' sends an event that " +
                    "contains payload with already given up ownership.",
                    log.Method, log.State, log.Machine);
            }
        }

        /// <summary>
        /// Reports a potendial data race.
        /// </summary>
        /// <param name="log">Log</param>
        internal static void ReportPotentialDataRace(Log log)
        {
            if (log.State == null)
            {
                AnalysisErrorReporter.ReportOwnershipError(log,
                    "Method '{0}' of machine '{1}' accesses '{2}' after " +
                    "giving up its ownership.",
                    log.Method, log.Machine, log.Payload);
            }
            else
            {
                AnalysisErrorReporter.ReportOwnershipError(log,
                    "Method '{0}' in state '{1}' of machine '{2}' accesses " +
                    "'{3}' after giving up its ownership.",
                    log.Method, log.State, log.Machine, log.Payload);
            }
        }

        #endregion

        #region warnings API

        /// <summary>
        /// Reports calling a method with unavailable source code,
        /// thus cannot be further analysed.
        /// </summary>
        /// <param name="log">Log</param>
        internal static void ReportUnknownInvocation(Log log)
        {
            if (log.State == null)
            {
                AnalysisErrorReporter.ReportWarning(log,
                    "Method '{0}' of machine '{1}' calls a method with unavailable " +
                    "source code, which might be a source of errors.",
                    log.Method, log.Machine);
            }
            else
            {
                AnalysisErrorReporter.ReportWarning(log,
                    "Method '{0}' in state '{1}' of machine '{2}' calls a method " +
                    "with unavailable source code, which might be a source of errors.",
                    log.Method, log.State, log.Machine);
            }
        }

        /// <summary>
        /// Reports calling a virtual method with unknown overrider,
        /// thus cannot be further analysed.
        /// </summary>
        /// <param name="log">Log</param>
        internal static void ReportUnknownVirtualCall(Log log)
        {
            if (log.State == null)
            {
                AnalysisErrorReporter.ReportWarning(log,
                    "Method '{0}' of machine '{1}' calls a virtual method that " +
                    "cannot be further analysed.",
                    log.Method, log.Machine);
            }
            else
            {
                AnalysisErrorReporter.ReportWarning(log,
                    "Method '{0}' in state '{1}' of machine '{2}' calls a virtual " +
                    "method that cannot be further analysed.",
                    log.Method, log.State, log.Machine);
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Reports a data race source related error to the user.
        /// </summary>
        /// <param name="log">Log</param>
        /// <param name="s">String</param>
        /// <param name="args">Parameters</param>
        private static void ReportDataRaceSource(Log log, string s, params object[] args)
        {
            string message = Output.Format(s, args);
            ConsoleColor previous = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("Error: Potential source for data race detected. ");
            Console.ForegroundColor = previous;
            Output.Print(message);

            for (int idx = log.ErrorTrace.Count - 1; idx >= 0; idx--)
            {
                if (idx == 0)
                {
                    Output.Print("   --- Point of sending the payload ---");
                    Console.Write("   at '{0}' ", log.ErrorTrace[idx].Item1);
                    Console.Write("in {0}:", log.ErrorTrace[idx].Item2);
                    Output.Print("line {0}", log.ErrorTrace[idx].Item3);
                }
                else
                {
                    Console.Write("   at '{0}' ", log.ErrorTrace[idx].Item1);
                    Console.Write("in {0}:", log.ErrorTrace[idx].Item2);
                    Output.Print("line {0}", log.ErrorTrace[idx].Item3);
                }
            }

            AnalysisErrorReporter.ErrorCount++;
        }

        /// <summary>
        /// Reports an ownership related error to the user.
        /// </summary>
        /// <param name="log">Log</param>
        /// <param name="s">String</param>
        /// <param name="args">Parameters</param>
        private static void ReportOwnershipError(Log log, string s, params object[] args)
        {
            string message = Output.Format(s, args);
            ConsoleColor previous = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("Error: Potential data race detected. ");
            Console.ForegroundColor = previous;
            Output.Print(message);

            for (int idx = log.ErrorTrace.Count - 1; idx >= 0; idx--)
            {
                if (idx == 0)
                {
                    Output.Print("   --- Source of giving up ownership ---");
                    Console.Write("   at '{0}' ", log.ErrorTrace[idx].Item1);
                    Console.Write("in {0}:", log.ErrorTrace[idx].Item2);
                    Output.Print("line {0}", log.ErrorTrace[idx].Item3);
                }
                else
                {
                    Console.Write("   at '{0}' ", log.ErrorTrace[idx].Item1);
                    Console.Write("in {0}:", log.ErrorTrace[idx].Item2);
                    Output.Print("line {0}", log.ErrorTrace[idx].Item3);
                }
            }

            AnalysisErrorReporter.ErrorCount++;
        }

        /// <summary>
        /// Reports a warning to the user.
        /// </summary>
        /// <param name="log">Log</param>
        /// <param name="s">String</param>
        /// <param name="args">Parameters</param>
        private static void ReportWarning(Log log, string s, params object[] args)
        {
            if (!Configuration.ShowWarnings)
            {
                return;
            }

            string message = Output.Format(s, args);
            ConsoleColor previous = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("Warning: ");
            Console.ForegroundColor = previous;
            Output.Print(message);

            Console.Write("   at '{0}' ", log.ErrorTrace[log.ErrorTrace.Count - 1].Item1);
            Console.Write("in {0}:", log.ErrorTrace[log.ErrorTrace.Count - 1].Item2);
            Output.Print("line {0}", log.ErrorTrace[log.ErrorTrace.Count - 1].Item3);

            AnalysisErrorReporter.WarningCount++;
        }

        /// <summary>
        /// Reports a generic error to the user.
        /// </summary>
        /// <param name="log">Log</param>
        /// <param name="s">String</param>
        /// <param name="args">Parameters</param>
        private static void ReportGenericError(Log log, string s, params object[] args)
        {
            string message = Output.Format(s, args);
            ConsoleColor previous = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("Error: ");
            Console.ForegroundColor = previous;
            Output.Print(message);

            for (int idx = log.ErrorTrace.Count - 1; idx >= 0; idx--)
            {
                Console.Write("   at '{0}' ", log.ErrorTrace[idx].Item1);
                Console.Write("in {0}:", log.ErrorTrace[idx].Item2);
                Output.Print("line {0}", log.ErrorTrace[idx].Item3);
            }

            AnalysisErrorReporter.ErrorCount++;
        }

        #endregion
    }
}
