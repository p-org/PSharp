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
using System.Collections.Generic;

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
        private static int ErrorCount;

        /// <summary>
        /// Number of warnings reported by the analysis.
        /// </summary>
        private static int WarningCount;

        /// <summary>
        /// Set of reported messages;
        /// </summary>
        private static HashSet<string> ReportedMessages;

        #endregion

        #region public API

        /// <summary>
        /// Static constructor.
        /// </summary>
        static AnalysisErrorReporter()
        {
            AnalysisErrorReporter.ErrorCount = 0;
            AnalysisErrorReporter.WarningCount = 0;
            AnalysisErrorReporter.ReportedMessages = new HashSet<string>();
        }

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

            if (AnalysisErrorReporter.WarningCount > 0)
            {
                return "... Static analysis detected '" + AnalysisErrorReporter.ErrorCount + "' " + errorStr +
                    " and '" + AnalysisErrorReporter.WarningCount + "' " + warningStr;
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
            AnalysisErrorReporter.ReportedMessages = new HashSet<string>();
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
        /// Reports an access of an object with given-up ownership.
        /// </summary>
        /// <param name="trace">TraceInfo</param>
        internal static void ReportGivenUpOwnershipAccess(TraceInfo trace)
        {
            string message;
            if (trace.State == null)
            {
                message = IO.Format("Method '{0}' of machine '{1}' " +
                    "accesses '{2}' after giving up its ownership.",
                    trace.Method, trace.Machine, trace.Payload);
            }
            else
            {
                message = IO.Format("Method '{0}' in state '{1}' of machine " +
                    "'{2}' accesses '{3}' after giving up its ownership.",
                    trace.Method, trace.State, trace.Machine, trace.Payload);
            }

            AnalysisErrorReporter.ReportErrorTrace(trace, message, true);
        }

        /// <summary>
        /// Reports an access of a field that is alias of
        /// an object with given-up ownership.
        /// </summary>
        /// <param name="trace">TraceInfo</param>
        /// <param name="fieldSymbol">ISymbol</param>
        internal static void ReportGivenUpOwnershipFieldAccess(TraceInfo trace,
            IFieldSymbol fieldSymbol)
        {
            string message;
            if (trace.State == null)
            {
                message = IO.Format("Method '{0}' of machine '{1}' accesses " +
                    "'{2}', via field '{3}', after giving up its ownership.",
                    trace.Method, trace.Machine, trace.Payload, fieldSymbol);
            }
            else
            {
                message = IO.Format("Method '{0}' in state '{1}' of machine '{2}' " +
                    "accesses '{3}', via field '{4}', after giving up its ownership.",
                    trace.Method, trace.State, trace.Machine, trace.Payload,
                    fieldSymbol);
            }

            AnalysisErrorReporter.ReportErrorTrace(trace, message, true);
        }

        /// <summary>
        /// Reports a given up field ownership error.
        /// </summary>
        /// <param name="trace">TraceInfo</param>
        /// <param name="fieldSymbol">ISymbol</param>
        internal static void ReportGivenUpFieldOwnershipError(TraceInfo trace,
            ISymbol fieldSymbol)
        {
            string message;
            if (trace.State == null)
            {
                message = IO.Format("Method '{0}' of machine '{1}' sends '{2}', " +
                    "which contains data from field '{3}'.", trace.Method,
                    trace.Machine, trace.Payload, fieldSymbol);
            }
            else
            {
                message = IO.Format("Method '{0}' in state '{1}' of machine '{2}' " +
                    "sends '{3}', which contains data from field '{4}'.", trace.Method,
                    trace.State, trace.Machine, trace.Payload, fieldSymbol);
            }

            AnalysisErrorReporter.ReportErrorTrace(trace, message);
        }

        /// <summary>
        /// Reports assignment of given up ownership to a machine field.
        /// </summary>
        /// <param name="trace">TraceInfo</param>
        /// <param name="fieldSymbol">ISymbol</param>
        internal static void ReportGivenUpOwnershipFieldAssignment(TraceInfo trace,
            ISymbol fieldSymbol)
        {
            string message;
            if (trace.State == null)
            {
                message = IO.Format("Method '{0}' of machine '{1}' assigns '{2}' " +
                    "to field '{3}' after giving up its ownership.", trace.Method,
                    trace.Machine, trace.Payload, fieldSymbol);
            }
            else
            {
                message = IO.Format("Method '{0}' in state '{1}' of machine '{2}' " +
                    "assigns '{3}' to field '{4}' after giving up its ownership.",
                    trace.Method, trace.State, trace.Machine, trace.Payload, fieldSymbol);
            }

            AnalysisErrorReporter.ReportErrorTrace(trace, message);
        }

        /// <summary>
        /// Reports sending data with a given up ownership.
        /// </summary>
        /// <param name="trace">TraceInfo</param>
        /// <param name="argSymbol">ISymbol</param>
        internal static void ReportGivenUpOwnershipSending(TraceInfo trace, ISymbol argSymbol)
        {
            string message;
            if (trace.State == null)
            {
                message = IO.Format("Method '{0}' of machine '{1}' sends '{2}', " +
                    "the ownership of which has already been given up.", trace.Method,
                    trace.Machine, argSymbol);
            }
            else
            {
                message = IO.Format("Method '{0}' in state '{1}' of machine '{2}' " +
                    "sends '{3}', the ownership of which has already been given up.",
                    trace.Method, trace.State, trace.Machine, argSymbol);
            }

            AnalysisErrorReporter.ReportErrorTrace(trace, message);
        }

        /// <summary>
        /// Reports calling a method with unavailable source code,
        /// thus cannot be further analysed.
        /// </summary>
        /// <param name="trace">TraceInfo</param>
        internal static void ReportExternalInvocation(TraceInfo trace)
        {
            string message;
            if (trace.State == null)
            {
                message = IO.Format("Method '{0}' of machine '{1}' calls " +
                    "a method with unavailable source code, which might " +
                    "be a source of errors.", trace.Method, trace.Machine);
            }
            else
            {
                message = IO.Format("Method '{0}' in state '{1}' of machine " +
                    "'{2}' calls a method with unavailable source code, which " +
                    "might be a source of errors.", trace.Method, trace.State,
                    trace.Machine);
            }

            AnalysisErrorReporter.ReportWarningTrace(trace, message);
        }

        /// <summary>
        /// Reports calling a virtual method with unknown overrider,
        /// thus cannot be further analysed.
        /// </summary>
        /// <param name="trace">TraceInfo</param>
        internal static void ReportUnknownVirtualCall(TraceInfo trace)
        {
            string message;
            if (trace.State == null)
            {
                message = IO.Format("Method '{0}' of machine '{1}' calls " +
                    "a virtual method that cannot be further analysed.",
                    trace.Method, trace.Machine);
            }
            else
            {
                message = IO.Format("Method '{0}' in state '{1}' of machine '{2}' " +
                    "calls a virtual method that cannot be further analysed.",
                    trace.Method, trace.State, trace.Machine);
            }

            AnalysisErrorReporter.ReportWarningTrace(trace, message);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Reports an error to the user.
        /// </summary>
        /// <param name="trace">TraceInfo</param>
        /// <param name="message">Message</param>
        /// <param name="allowMultiple">Allow multiple messages</param>
        private static void ReportErrorTrace(TraceInfo trace, string message,
            bool allowMultiple = false)
        {
            if (!allowMultiple &&
                AnalysisErrorReporter.ReportedMessages.Contains(message))
            {
                return;
            }
            
            AnalysisErrorReporter.Report(message);
            AnalysisErrorReporter.PrintTrace(trace);
            AnalysisErrorReporter.ReportedMessages.Add(message);
        }

        /// <summary>
        /// Reports a warning to the user.
        /// </summary>
        /// <param name="trace">TraceInfo</param>
        /// <param name="message">Message</param>
        private static void ReportWarningTrace(TraceInfo trace, string message)
        {
            if (AnalysisErrorReporter.ReportedMessages.Contains(message))
            {
                return;
            }

            AnalysisErrorReporter.ReportWarning(message);
            AnalysisErrorReporter.PrintTrace(trace);
            AnalysisErrorReporter.ReportedMessages.Add(message);
        }

        /// <summary>
        /// Prints the trace.
        /// </summary>
        /// <param name="trace">TraceInfo</param>
        private static void PrintTrace(TraceInfo trace)
        {
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
