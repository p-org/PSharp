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
using Microsoft.PSharp.IO;

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
            ErrorCount = 0;
            WarningCount = 0;
            ReportedMessages = new HashSet<string>();
        }

        /// <summary>
        /// Returns the number of found errors.
        /// </summary>
        /// <returns>Number of errors</returns>
        public static int GetErrorCount()
        {
            return ErrorCount;
        }

        /// <summary>
        /// Returns the number of found warnings.
        /// </summary>
        /// <returns>Number of warnings</returns>
        public static int GetWarningCount()
        {
            return WarningCount;
        }

        /// <summary>
        /// Returns the static analysis error statistics.
        /// </summary>
        public static string GetStats()
        {
            string errorStr = "error";
            if (ErrorCount != 1)
            {
                errorStr = "errors";
            }

            string warningStr = "warning";
            if (WarningCount != 1)
            {
                warningStr = "warnings";
            }

            if (ErrorReporter.ShowWarnings && WarningCount > 0)
            {
                return $"Static analysis detected '{ErrorCount}' {errorStr}" +
                    $" and '{WarningCount}' {warningStr}.";
            }
            else if (ErrorCount > 0)
            {
                return $"Static analysis detected '{ErrorCount}' {errorStr}.";
            }
            else
            {
                return "No static analysis errors detected.";
            }
        }

        /// <summary>
        /// Prints the static analysis error statistics.
        /// </summary>
        public static void PrintStats()
        {
            Output.WriteLine(GetStats());
        }

        /// <summary>
        /// Resets the error statistics.
        /// </summary>
        public static void ResetStats()
        {
            ErrorCount = 0;
            WarningCount = 0;
            ReportedMessages = new HashSet<string>();
        }

        #endregion

        #region error reporting methods

        /// <summary>
        /// Reports an error to the user.
        /// </summary>
        /// <param name="value">Text</param>
        internal static void Report(string value)
        {
            Report("Error: ", ConsoleColor.Red);
            Report(value, ConsoleColor.Yellow);
            Output.WriteLine("");
            ErrorCount++;
        }

        /// <summary>
        /// Reports an error to the user.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Parameters</param>
        internal static void Report(string format, params object[] args)
        {
            string message = IO.Utilities.Format(format, args);
            Report("Error: ", ConsoleColor.Red);
            Report(message, ConsoleColor.Yellow);
            Output.WriteLine("");
            ErrorCount++;
        }

        /// <summary>
        /// Reports a generic error to the user using the specified color.
        /// </summary>
        /// <param name="value">Text</param>
        /// <param name="color">ConsoleColor</param>
        private static void Report(string value, ConsoleColor color)
        {
            var previousForegroundColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Output.Write(value);
            Console.ForegroundColor = previousForegroundColor;
        }

        /// <summary>
        /// Reports an error to the user.
        /// </summary>
        /// <param name="trace">TraceInfo</param>
        /// <param name="s">String</param>
        internal static void Report(TraceInfo trace, string s)
        {
            Report(s);
            for (int idx = trace.ErrorTrace.Count - 1; idx >= 0; idx--)
            {
                Output.Write("   at '{0}' ", trace.ErrorTrace[idx].Expression);
                Output.Write("in {0}:", trace.ErrorTrace[idx].File);
                Output.WriteLine("line {0}", trace.ErrorTrace[idx].Line);
            }
        }

        /// <summary>
        /// Reports a warning to the user.
        /// </summary>
        /// <param name="value">Text</param>
        internal static void ReportWarning(string value)
        {
            if (ErrorReporter.ShowWarnings)
            {
                Report("Warning: ", ConsoleColor.Red);
                Report(value, ConsoleColor.Yellow);
                Output.WriteLine("");
            }

            WarningCount++;
        }

        /// <summary>
        /// Reports a warning to the user.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Parameters</param>
        internal static void ReportWarning(string format, params object[] args)
        {
            if (ErrorReporter.ShowWarnings)
            {
                string message = IO.Utilities.Format(format, args);
                Report("Warning: ", ConsoleColor.Red);
                Report(message, ConsoleColor.Yellow);
                Output.WriteLine("");
            }

            WarningCount++;
        }

        /// <summary>
        /// Reports a warning to the user.
        /// </summary>
        /// <param name="trace">TraceInfo</param>
        /// <param name="s">String</param>
        internal static void ReportWarning(TraceInfo trace, string s)
        {
            ReportWarning(s);
            if (ErrorReporter.ShowWarnings)
            {
                Output.Write("   at '{0}' ", trace.ErrorTrace[trace.ErrorTrace.Count - 1].Expression);
                Output.Write("in {0}:", trace.ErrorTrace[trace.ErrorTrace.Count - 1].File);
                Output.WriteLine("line {0}", trace.ErrorTrace[trace.ErrorTrace.Count - 1].Line);
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
            ReportWarning(s, args);
            if (ErrorReporter.ShowWarnings)
            {
                Output.Write("   at '{0}' ", trace.ErrorTrace[trace.ErrorTrace.Count - 1].Expression);
                Output.Write("in {0}:", trace.ErrorTrace[trace.ErrorTrace.Count - 1].File);
                Output.WriteLine("line {0}", trace.ErrorTrace[trace.ErrorTrace.Count - 1].Line);
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
                message = IO.Utilities.Format("Method '{0}' of machine '{1}' " +
                    "accesses '{2}' after giving up its ownership.",
                    trace.Method, trace.Machine, trace.Payload);
            }
            else
            {
                message = IO.Utilities.Format("Method '{0}' in state '{1}' of machine " +
                    "'{2}' accesses '{3}' after giving up its ownership.",
                    trace.Method, trace.State, trace.Machine, trace.Payload);
            }

            ReportErrorTrace(trace, message, true);
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
                message = IO.Utilities.Format("Method '{0}' of machine '{1}' accesses " +
                    "'{2}', via field '{3}', after giving up its ownership.",
                    trace.Method, trace.Machine, trace.Payload, fieldSymbol);
            }
            else
            {
                message = IO.Utilities.Format("Method '{0}' in state '{1}' of machine '{2}' " +
                    "accesses '{3}', via field '{4}', after giving up its ownership.",
                    trace.Method, trace.State, trace.Machine, trace.Payload,
                    fieldSymbol);
            }

            ReportErrorTrace(trace, message, true);
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
                message = IO.Utilities.Format("Method '{0}' of machine '{1}' sends '{2}', " +
                    "which contains data from field '{3}'.", trace.Method,
                    trace.Machine, trace.Payload, fieldSymbol);
            }
            else
            {
                message = IO.Utilities.Format("Method '{0}' in state '{1}' of machine '{2}' " +
                    "sends '{3}', which contains data from field '{4}'.", trace.Method,
                    trace.State, trace.Machine, trace.Payload, fieldSymbol);
            }

            ReportErrorTrace(trace, message);
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
                message = IO.Utilities.Format("Method '{0}' of machine '{1}' assigns '{2}' " +
                    "to field '{3}' after giving up its ownership.", trace.Method,
                    trace.Machine, trace.Payload, fieldSymbol);
            }
            else
            {
                message = IO.Utilities.Format("Method '{0}' in state '{1}' of machine '{2}' " +
                    "assigns '{3}' to field '{4}' after giving up its ownership.",
                    trace.Method, trace.State, trace.Machine, trace.Payload, fieldSymbol);
            }

            ReportErrorTrace(trace, message);
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
                message = IO.Utilities.Format("Method '{0}' of machine '{1}' sends '{2}', " +
                    "the ownership of which has already been given up.", trace.Method,
                    trace.Machine, argSymbol);
            }
            else
            {
                message = IO.Utilities.Format("Method '{0}' in state '{1}' of machine '{2}' " +
                    "sends '{3}', the ownership of which has already been given up.",
                    trace.Method, trace.State, trace.Machine, argSymbol);
            }

            ReportErrorTrace(trace, message);
        }

        /// <summary>
        /// Reports calling a method with unavailable source code,
        /// thus cannot be further analyzed.
        /// </summary>
        /// <param name="trace">TraceInfo</param>
        internal static void ReportExternalInvocation(TraceInfo trace)
        {
            string message;
            if (trace.State == null)
            {
                message = IO.Utilities.Format("Method '{0}' of machine '{1}' calls " +
                    "a method with unavailable source code, which might " +
                    "be a source of errors.", trace.Method, trace.Machine);
            }
            else
            {
                message = IO.Utilities.Format("Method '{0}' in state '{1}' of machine " +
                    "'{2}' calls a method with unavailable source code, which " +
                    "might be a source of errors.", trace.Method, trace.State,
                    trace.Machine);
            }

            ReportWarningTrace(trace, message);
        }

        /// <summary>
        /// Reports calling a virtual method with unknown overrider,
        /// thus cannot be further analyzed.
        /// </summary>
        /// <param name="trace">TraceInfo</param>
        internal static void ReportUnknownVirtualCall(TraceInfo trace)
        {
            string message;
            if (trace.State == null)
            {
                message = IO.Utilities.Format("Method '{0}' of machine '{1}' calls " +
                    "a virtual method that cannot be further analyzed.",
                    trace.Method, trace.Machine);
            }
            else
            {
                message = IO.Utilities.Format("Method '{0}' in state '{1}' of machine '{2}' " +
                    "calls a virtual method that cannot be further analyzed.",
                    trace.Method, trace.State, trace.Machine);
            }

            ReportWarningTrace(trace, message);
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
                ReportedMessages.Contains(message))
            {
                return;
            }
            
            Report(message);
            PrintTrace(trace);
            ReportedMessages.Add(message);
        }

        /// <summary>
        /// Reports a warning to the user.
        /// </summary>
        /// <param name="trace">TraceInfo</param>
        /// <param name="message">Message</param>
        private static void ReportWarningTrace(TraceInfo trace, string message)
        {
            if (ReportedMessages.Contains(message))
            {
                return;
            }

            ReportWarning(message);
            PrintTrace(trace);
            ReportedMessages.Add(message);
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
                    Output.WriteLine("   --- Source of giving up ownership ---");
                    Output.Write("   at '{0}' ", trace.ErrorTrace[idx].Expression);
                    Output.Write("in {0}:", trace.ErrorTrace[idx].File);
                    Output.WriteLine("line {0}", trace.ErrorTrace[idx].Line);
                }
                else
                {
                    Output.Write("   at '{0}' ", trace.ErrorTrace[idx].Expression);
                    Output.Write("in {0}:", trace.ErrorTrace[idx].File);
                    Output.WriteLine("line {0}", trace.ErrorTrace[idx].Line);
                }
            }
        }

        #endregion
    }
}
