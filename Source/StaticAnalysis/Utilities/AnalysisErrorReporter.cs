//-----------------------------------------------------------------------
// <copyright file="AnalysisErrorReporter.cs">
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

using System;

using Microsoft.CodeAnalysis;

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
            IO.PrintLine(AnalysisErrorReporter.GetStats());
        }

        /// <summary>
        /// Returns the static analysis error statistics.
        /// </summary>
        public static string GetStats()
        {
            string errorStr = "error";
            if (AnalysisErrorReporter.ErrorCount != 1)
            {
                errorStr = "errors";
            }

            string warningStr = "warning";
            if (AnalysisErrorReporter.WarningCount != 1)
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
            IO.Print(ConsoleColor.Red, "Error: ");
            IO.Print(ConsoleColor.Yellow, s);
            IO.PrintLine();
            AnalysisErrorReporter.ErrorCount++;
        }

        /// <summary>
        /// Reports an error to the user.
        /// </summary>
        /// <param name="s">String</param>
        /// <param name="args">Parameters</param>
        internal static void Report(string s, params object[] args)
        {
            string message = IO.Format(s, args);
            IO.Print(ConsoleColor.Red, "Error: ");
            IO.Print(ConsoleColor.Yellow, message);
            IO.PrintLine();
            AnalysisErrorReporter.ErrorCount++;
        }

        /// <summary>
        /// Reports an error to the user.
        /// </summary>
        /// <param name="trace">TraceInfo</param>
        /// <param name="s">String</param>
        internal static void Report(TraceInfo trace, string s)
        {
            AnalysisErrorReporter.Report(s);
            for (int idx = trace.ErrorTrace.Count - 1; idx >= 0; idx--)
            {
                IO.Print("   at '{0}' ", trace.ErrorTrace[idx].Expression);
                IO.Print("in {0}:", trace.ErrorTrace[idx].File);
                IO.PrintLine("line {0}", trace.ErrorTrace[idx].Line);
            }
        }

        /// <summary>
        /// Reports an error to the user.
        /// </summary>
        /// <param name="trace">TraceInfo</param>
        /// <param name="s">String</param>
        /// <param name="args">Parameters</param>
        internal static void Report(TraceInfo trace, string s, params object[] args)
        {
            AnalysisErrorReporter.Report(s, args);
            for (int idx = trace.ErrorTrace.Count - 1; idx >= 0; idx--)
            {
                IO.Print("   at '{0}' ", trace.ErrorTrace[idx].Expression);
                IO.Print("in {0}:", trace.ErrorTrace[idx].File);
                IO.PrintLine("line {0}", trace.ErrorTrace[idx].Line);
            }
        }

        /// <summary>
        /// Reports a warning to the user.
        /// </summary>
        /// <param name="s">String</param>
        internal static void ReportWarning(string s)
        {
            if (ErrorReporter.ShowWarnings)
            {
                IO.Print(ConsoleColor.Red, "Warning: ");
                IO.Print(ConsoleColor.Yellow, s);
                IO.PrintLine();
            }
                
            AnalysisErrorReporter.WarningCount++;
        }

        /// <summary>
        /// Reports a warning to the user.
        /// </summary>
        /// <param name="s">String</param>
        /// <param name="args">Parameters</param>
        internal static void ReportWarning(string s, params object[] args)
        {
            if (ErrorReporter.ShowWarnings)
            {
                string message = IO.Format(s, args);
                IO.Print(ConsoleColor.Red, "Warning: ");
                IO.Print(ConsoleColor.Yellow, message);
                IO.PrintLine();
            }
            
            AnalysisErrorReporter.WarningCount++;
        }

        /// <summary>
        /// Reports a warning to the user.
        /// </summary>
        /// <param name="trace">TraceInfo</param>
        /// <param name="s">String</param>
        internal static void ReportWarning(TraceInfo trace, string s)
        {
            AnalysisErrorReporter.ReportWarning(s);
            if (ErrorReporter.ShowWarnings)
            {
                IO.Print("   at '{0}' ", trace.ErrorTrace[trace.ErrorTrace.Count - 1].Expression);
                IO.Print("in {0}:", trace.ErrorTrace[trace.ErrorTrace.Count - 1].File);
                IO.PrintLine("line {0}", trace.ErrorTrace[trace.ErrorTrace.Count - 1].Line);
            }
        }

        /// <summary>
        /// Reports a warning to the user.
        /// </summary>
        /// <param name="trace">TraceInfo</param>
        /// <param name="s">String</param>
        /// <param name="args">Parameters</param>
        internal static void ReportWarning(TraceInfo trace, string s, params object[] args)
        {
            AnalysisErrorReporter.ReportWarning(s, args);
            if (ErrorReporter.ShowWarnings)
            {
                IO.Print("   at '{0}' ", trace.ErrorTrace[trace.ErrorTrace.Count - 1].Expression);
                IO.Print("in {0}:", trace.ErrorTrace[trace.ErrorTrace.Count - 1].File);
                IO.PrintLine("line {0}", trace.ErrorTrace[trace.ErrorTrace.Count - 1].Line);
            }
        }

        /// <summary>
        /// Reports a given up field ownership error.
        /// </summary>
        /// <param name="trace">TraceInfo</param>
        internal static void ReportGivenUpFieldOwnershipError(TraceInfo trace)
        {
            if (trace.State == null)
            {
                AnalysisErrorReporter.ReportDataRaceSource(trace,
                    "Method '{0}' of machine '{1}' sends '{2}', which " +
                    "contains data from a field.",
                    trace.Method, trace.Machine, trace.Payload);
            }
            else
            {
                AnalysisErrorReporter.ReportDataRaceSource(trace,
                    "Method '{0}' in state '{1}' of machine '{2}' sends " +
                    "'{3}', which contains data from a field.",
                    trace.Method, trace.State, trace.Machine, trace.Payload);
            }
        }

        /// <summary>
        /// Reports assignment of payload to a machine field.
        /// </summary>
        /// <param name="trace">TraceInfo</param>
        internal static void ReportPayloadFieldAssignment(TraceInfo trace)
        {
            if (trace.State == null)
            {
                AnalysisErrorReporter.ReportDataRaceSource(trace,
                    "Method '{0}' of machine '{1}' assigns the latest received " +
                    "event payload to a field.",
                    trace.Method, trace.Machine);
            }
            else
            {
                AnalysisErrorReporter.ReportDataRaceSource(trace,
                    "Method '{0}' in state '{1}' of machine '{2}' assigns " +
                    "the latest received event payload to a field.",
                    trace.Method, trace.State, trace.Machine);
            }
        }

        /// <summary>
        /// Reports assignment of given up ownership to a machine field.
        /// </summary>
        /// <param name="trace">TraceInfo</param>
        /// <param name="fieldSymbol">IFieldSymbol</param>
        internal static void ReportGivenUpOwnershipFieldAssignment(TraceInfo trace, IFieldSymbol fieldSymbol)
        {
            if (trace.State == null)
            {
                AnalysisErrorReporter.ReportOwnershipError(trace,
                    "Method '{0}' of machine '{1}' assigns '{2}' to field '{3}' " +
                    "after giving up its ownership.",
                    trace.Method, trace.Machine, trace.Payload, fieldSymbol);
            }
            else
            {
                AnalysisErrorReporter.ReportOwnershipError(trace,
                    "Method '{0}' in state '{1}' of machine '{2}' assigns '{3}' to " +
                    "field '{4}' after giving up its ownership.",
                    trace.Method, trace.State, trace.Machine, trace.Payload, fieldSymbol);
            }
        }

        /// <summary>
        /// Reports sending data with a given up ownership.
        /// </summary>
        /// <param name="trace">TraceInfo</param>
        internal static void ReportGivenUpOwnershipSending(TraceInfo trace)
        {
            if (trace.State == null)
            {
                AnalysisErrorReporter.ReportOwnershipError(trace,
                    "Method '{0}' of machine '{1}' sends an event that contains " +
                    "payload with already given up ownership.",
                    trace.Method, trace.Machine);
            }
            else
            {
                AnalysisErrorReporter.ReportOwnershipError(trace,
                    "Method '{0}' in state '{1}' of machine '{2}' sends an event that " +
                    "contains payload with already given up ownership.",
                    trace.Method, trace.State, trace.Machine);
            }
        }

        /// <summary>
        /// Reports a potendial data race.
        /// </summary>
        /// <param name="trace">TraceInfo</param>
        internal static void ReportPotentialDataRace(TraceInfo trace)
        {
            if (trace.State == null)
            {
                AnalysisErrorReporter.ReportOwnershipError(trace,
                    "Method '{0}' of machine '{1}' accesses '{2}' after " +
                    "giving up its ownership.",
                    trace.Method, trace.Machine, trace.Payload);
            }
            else
            {
                AnalysisErrorReporter.ReportOwnershipError(trace,
                    "Method '{0}' in state '{1}' of machine '{2}' accesses " +
                    "'{3}' after giving up its ownership.",
                    trace.Method, trace.State, trace.Machine, trace.Payload);
            }
        }

        /// <summary>
        /// Reports calling a method with unavailable source code,
        /// thus cannot be further analysed.
        /// </summary>
        /// <param name="trace">TraceInfo</param>
        internal static void ReportExternalInvocation(TraceInfo trace)
        {
            if (trace.State == null)
            {
                AnalysisErrorReporter.ReportWarning(trace,
                    "Method '{0}' of machine '{1}' calls a method with unavailable " +
                    "source code, which might be a source of errors.",
                    trace.Method, trace.Machine);
            }
            else
            {
                AnalysisErrorReporter.ReportWarning(trace,
                    "Method '{0}' in state '{1}' of machine '{2}' calls a method " +
                    "with unavailable source code, which might be a source of errors.",
                    trace.Method, trace.State, trace.Machine);
            }
        }

        /// <summary>
        /// Reports calling a virtual method with unknown overrider,
        /// thus cannot be further analysed.
        /// </summary>
        /// <param name="trace">TraceInfo</param>
        internal static void ReportUnknownVirtualCall(TraceInfo trace)
        {
            if (trace.State == null)
            {
                AnalysisErrorReporter.ReportWarning(trace,
                    "Method '{0}' of machine '{1}' calls a virtual method that " +
                    "cannot be further analysed.",
                    trace.Method, trace.Machine);
            }
            else
            {
                AnalysisErrorReporter.ReportWarning(trace,
                    "Method '{0}' in state '{1}' of machine '{2}' calls a virtual " +
                    "method that cannot be further analysed.",
                    trace.Method, trace.State, trace.Machine);
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Reports a data race source related error to the user.
        /// </summary>
        /// <param name="trace">TraceInfo</param>
        /// <param name="s">String</param>
        /// <param name="args">Parameters</param>
        private static void ReportDataRaceSource(TraceInfo trace, string s, params object[] args)
        {
            AnalysisErrorReporter.Report(s, args);
            for (int idx = trace.ErrorTrace.Count - 1; idx >= 0; idx--)
            {
                if (idx == 0)
                {
                    IO.PrintLine("   --- Point of sending the event payload ---");
                    IO.Print("   at '{0}' ", trace.ErrorTrace[idx].Expression);
                    IO.Print("in {0}:", trace.ErrorTrace[idx].File);
                    IO.PrintLine("line {0}", trace.ErrorTrace[idx].Line);
                }
                else
                {
                    IO.Print("   at '{0}' ", trace.ErrorTrace[idx].Expression);
                    IO.Print("in {0}:", trace.ErrorTrace[idx].File);
                    IO.PrintLine("line {0}", trace.ErrorTrace[idx].Line);
                }
            }
        }

        /// <summary>
        /// Reports an ownership related error to the user.
        /// </summary>
        /// <param name="trace">TraceInfo</param>
        /// <param name="s">String</param>
        /// <param name="args">Parameters</param>
        private static void ReportOwnershipError(TraceInfo trace, string s, params object[] args)
        {
            AnalysisErrorReporter.Report(s, args);
            for (int idx = trace.ErrorTrace.Count - 1; idx >= 0; idx--)
            {
                if (idx == 0)
                {
                    IO.PrintLine("   --- Source of giving up ownership ---");
                    IO.Print("   at '{0}' ", trace.ErrorTrace[idx].Expression);
                    IO.Print("in {0}:", trace.ErrorTrace[idx].File);
                    IO.PrintLine("line {0}", trace.ErrorTrace[idx].Line);
                }
                else
                {
                    IO.Print("   at '{0}' ", trace.ErrorTrace[idx].Expression);
                    IO.Print("in {0}:", trace.ErrorTrace[idx].File);
                    IO.PrintLine("line {0}", trace.ErrorTrace[idx].Line);
                }
            }
        }

        #endregion
    }
}
