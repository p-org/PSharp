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

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// Reports static analysis errors and warnings to the user.
    /// </summary>
    internal static class AnalysisErrorReporter
    {
        #region fields

        /// <summary>
        /// Number of errors discovered in the analysis.
        /// </summary>
        private static int ErrorCount = 0;

        /// <summary>
        /// Number of warnings reported by the analysis.
        /// </summary>
        private static int WarningCount = 0;

        #endregion

        #region public API

        /// <summary>
        /// Prints the static analysis error statistics.
        /// </summary>
        public static void PrintStats()
        {
            string errorStr = "error";
            if (AnalysisErrorReporter.ErrorCount > 1)
            {
                errorStr = "errors";
            }

            string warningStr = "warning";
            if (AnalysisErrorReporter.WarningCount > 1)
            {
                warningStr = "warnings";
            }

            if ((AnalysisErrorReporter.ErrorCount > 0 || AnalysisErrorReporter.WarningCount > 0) &&
                ErrorReporter.ShowWarnings)
            {
                IO.PrintLine("... Static analysis detected '{0}' {1} and reported '{2}' {3}",
                    AnalysisErrorReporter.ErrorCount, errorStr,
                    AnalysisErrorReporter.WarningCount, warningStr);
            }
            else if (AnalysisErrorReporter.ErrorCount > 0)
            {
                IO.PrintLine("... Static analysis detected '{0}' {1}",
                    AnalysisErrorReporter.ErrorCount, errorStr);
            }
            else
            {
                IO.PrintLine("... No static analysis errors detected (but absolutely no warranty provided)");
            }
        }

        /// <summary>
        /// Returns the static analysis error statistics.
        /// </summary>
        public static string GetStats()
        {
            string errorStr = "error";
            if (AnalysisErrorReporter.ErrorCount > 1)
            {
                errorStr = "errors";
            }

            string warningStr = "warning";
            if (AnalysisErrorReporter.WarningCount > 1)
            {
                warningStr = "warnings";
            }

            if ((AnalysisErrorReporter.ErrorCount > 0 || AnalysisErrorReporter.WarningCount > 0) &&
                ErrorReporter.ShowWarnings)
            {
                return "... Static analysis detected '" + AnalysisErrorReporter.ErrorCount + "' " + errorStr +
                    " and reported '" + AnalysisErrorReporter.WarningCount + "' " + warningStr;
            }
            else if (AnalysisErrorReporter.ErrorCount > 0)
            {
                return "... Static analysis detected '" + AnalysisErrorReporter.ErrorCount + "' " + errorStr;
            }
            else
            {
                return "... No static analysis errors detected (but absolutely no warranty provided)";
            }
        }

        /// <summary>
        /// Resets the error statistics.
        /// </summary>
        public static void ResetStats()
        {
            AnalysisErrorReporter.ErrorCount = 0;
            AnalysisErrorReporter.WarningCount = 0;
        }

        #endregion

        #region error reporting methods

        /// <summary>
        /// Reports an error to the user.
        /// </summary>
        /// <param name="s">String</param>
        internal static void Report(string s)
        {
            ErrorReporter.Report(s);
            AnalysisErrorReporter.ErrorCount++;
        }

        /// <summary>
        /// Reports an error to the user.
        /// </summary>
        /// <param name="s">String</param>
        /// <param name="args">Parameters</param>
        internal static void Report(string s, params object[] args)
        {
            ErrorReporter.Report(s, args);
            AnalysisErrorReporter.ErrorCount++;
        }

        /// <summary>
        /// Reports an error to the user.
        /// </summary>
        /// <param name="log">Log</param>
        /// <param name="s">String</param>
        internal static void Report(Log log, string s)
        {
            ErrorReporter.Report(s);

            for (int idx = log.ErrorTrace.Count - 1; idx >= 0; idx--)
            {
                IO.Print("   at '{0}' ", log.ErrorTrace[idx].Item1);
                IO.Print("in {0}:", log.ErrorTrace[idx].Item2);
                IO.PrintLine("line {0}", log.ErrorTrace[idx].Item3);
            }

            AnalysisErrorReporter.ErrorCount++;
        }

        /// <summary>
        /// Reports an error to the user.
        /// </summary>
        /// <param name="log">Log</param>
        /// <param name="s">String</param>
        /// <param name="args">Parameters</param>
        internal static void Report(Log log, string s, params object[] args)
        {
            ErrorReporter.Report(s, args);

            for (int idx = log.ErrorTrace.Count - 1; idx >= 0; idx--)
            {
                IO.Print("   at '{0}' ", log.ErrorTrace[idx].Item1);
                IO.Print("in {0}:", log.ErrorTrace[idx].Item2);
                IO.PrintLine("line {0}", log.ErrorTrace[idx].Item3);
            }

            AnalysisErrorReporter.ErrorCount++;
        }

        /// <summary>
        /// Reports a warning to the user.
        /// </summary>
        /// <param name="s">String</param>
        internal static void ReportWarning(string s)
        {
            ErrorReporter.ReportWarning(s);
            AnalysisErrorReporter.WarningCount++;
        }

        /// <summary>
        /// Reports a warning to the user.
        /// </summary>
        /// <param name="s">String</param>
        /// <param name="args">Parameters</param>
        internal static void ReportWarning(string s, params object[] args)
        {
            ErrorReporter.ReportWarning(s, args);
            AnalysisErrorReporter.WarningCount++;
        }

        /// <summary>
        /// Reports a warning to the user.
        /// </summary>
        /// <param name="log">Log</param>
        /// <param name="s">String</param>
        internal static void ReportWarning(Log log, string s)
        {
            ErrorReporter.ReportWarning(s);

            IO.Print("   at '{0}' ", log.ErrorTrace[log.ErrorTrace.Count - 1].Item1);
            IO.Print("in {0}:", log.ErrorTrace[log.ErrorTrace.Count - 1].Item2);
            IO.PrintLine("line {0}", log.ErrorTrace[log.ErrorTrace.Count - 1].Item3);

            AnalysisErrorReporter.WarningCount++;
        }

        /// <summary>
        /// Reports a warning to the user.
        /// </summary>
        /// <param name="log">Log</param>
        /// <param name="s">String</param>
        /// <param name="args">Parameters</param>
        internal static void ReportWarning(Log log, string s, params object[] args)
        {
            ErrorReporter.ReportWarning(s, args);

            IO.Print("   at '{0}' ", log.ErrorTrace[log.ErrorTrace.Count - 1].Item1);
            IO.Print("in {0}:", log.ErrorTrace[log.ErrorTrace.Count - 1].Item2);
            IO.PrintLine("line {0}", log.ErrorTrace[log.ErrorTrace.Count - 1].Item3);

            AnalysisErrorReporter.WarningCount++;
        }

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
            string message = IO.Format(s, args);
            IO.Print("Error: Potential source for data race detected. ");
            IO.PrintLine(message);

            for (int idx = log.ErrorTrace.Count - 1; idx >= 0; idx--)
            {
                if (idx == 0)
                {
                    IO.PrintLine("   --- Point of sending the payload ---");
                    IO.Print("   at '{0}' ", log.ErrorTrace[idx].Item1);
                    IO.Print("in {0}:", log.ErrorTrace[idx].Item2);
                    IO.PrintLine("line {0}", log.ErrorTrace[idx].Item3);
                }
                else
                {
                    IO.Print("   at '{0}' ", log.ErrorTrace[idx].Item1);
                    IO.Print("in {0}:", log.ErrorTrace[idx].Item2);
                    IO.PrintLine("line {0}", log.ErrorTrace[idx].Item3);
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
            string message = IO.Format(s, args);
            IO.Print("Error: Potential data race detected. ");
            IO.PrintLine(message);

            for (int idx = log.ErrorTrace.Count - 1; idx >= 0; idx--)
            {
                if (idx == 0)
                {
                    IO.PrintLine("   --- Source of giving up ownership ---");
                    IO.Print("   at '{0}' ", log.ErrorTrace[idx].Item1);
                    IO.Print("in {0}:", log.ErrorTrace[idx].Item2);
                    IO.PrintLine("line {0}", log.ErrorTrace[idx].Item3);
                }
                else
                {
                    IO.Print("   at '{0}' ", log.ErrorTrace[idx].Item1);
                    IO.Print("in {0}:", log.ErrorTrace[idx].Item2);
                    IO.PrintLine("line {0}", log.ErrorTrace[idx].Item3);
                }
            }

            AnalysisErrorReporter.ErrorCount++;
        }

        #endregion
    }
}
