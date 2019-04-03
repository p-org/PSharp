// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.PSharp.IO;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// Reports static analysis errors and warnings to the user.
    /// </summary>
    internal class ErrorReporter
    {
        /// <summary>
        /// Configuration.
        /// </summary>
        private readonly Configuration Configuration;

        /// <summary>
        /// Enables colored console output.
        /// </summary>
        internal bool EnableColoredConsoleOutput;

        /// <summary>
        /// The installed logger.
        /// </summary>
        internal ILogger Logger { get; private set; }

        /// <summary>
        /// Set of reported messages;
        /// </summary>
        internal HashSet<string> ReportedMessages { get; }

        /// <summary>
        /// Number of errors discovered in the analysis.
        /// </summary>
        internal int ErrorCount { get; private set; }

        /// <summary>
        /// Number of warnings reported by the analysis.
        /// </summary>
        internal int WarningCount { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorReporter"/> class.
        /// </summary>
        internal ErrorReporter(Configuration configuration, ILogger logger)
        {
            this.Configuration = configuration;
            this.Logger = logger;
            this.ReportedMessages = new HashSet<string>();
            this.ErrorCount = 0;
            this.WarningCount = 0;
            this.EnableColoredConsoleOutput = false;
        }

        /// <summary>
        /// Returns the static analysis error statistics.
        /// </summary>
        public string GetStats()
        {
            string errorStr = "error";
            if (this.ErrorCount != 1)
            {
                errorStr = "errors";
            }

            string warningStr = "warning";
            if (this.WarningCount != 1)
            {
                warningStr = "warnings";
            }

            if (this.Configuration.ShowWarnings && this.WarningCount > 0)
            {
                return $"Static analysis detected '{this.ErrorCount}' {errorStr}" +
                    $" and '{this.WarningCount}' {warningStr}.";
            }
            else if (this.ErrorCount > 0)
            {
                return $"Static analysis detected '{this.ErrorCount}' {errorStr}.";
            }
            else
            {
                return "No static analysis errors detected.";
            }
        }

        /// <summary>
        /// Reports an error to the user.
        /// </summary>
        internal void Report(string value)
        {
            this.Report("Error: ", ConsoleColor.Red);
            this.Report(value, ConsoleColor.Yellow);
            this.Logger.WriteLine(string.Empty);
            this.ErrorCount++;
        }

        /// <summary>
        /// Reports an error to the user.
        /// </summary>
        internal void Report(string format, params object[] args)
        {
            string message = IO.Utilities.Format(format, args);
            this.Report("Error: ", ConsoleColor.Red);
            this.Report(message, ConsoleColor.Yellow);
            this.Logger.WriteLine(string.Empty);
            this.ErrorCount++;
        }

        /// <summary>
        /// Reports a generic error to the user using the specified color.
        /// </summary>
        private void Report(string value, ConsoleColor color)
        {
            ConsoleColor previousForegroundColor = default;
            if (this.EnableColoredConsoleOutput)
            {
                previousForegroundColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
            }

            this.Logger.Write(value);

            if (this.EnableColoredConsoleOutput)
            {
                Console.ForegroundColor = previousForegroundColor;
            }
        }

        /// <summary>
        /// Reports an error to the user.
        /// </summary>
        internal void Report(TraceInfo trace, string s)
        {
            this.Report(s);
            for (int idx = trace.ErrorTrace.Count - 1; idx >= 0; idx--)
            {
                this.Logger.Write("   at '{0}' ", trace.ErrorTrace[idx].Expression);
                this.Logger.Write("in {0}:", trace.ErrorTrace[idx].File);
                this.Logger.WriteLine("line {0}", trace.ErrorTrace[idx].Line);
            }
        }

        /// <summary>
        /// Reports a warning to the user.
        /// </summary>
        internal void ReportWarning(string value)
        {
            if (this.Configuration.ShowWarnings)
            {
                this.Report("Warning: ", ConsoleColor.Red);
                this.Report(value, ConsoleColor.Yellow);
                this.Logger.WriteLine(string.Empty);
            }

            this.WarningCount++;
        }

        /// <summary>
        /// Reports a warning to the user.
        /// </summary>
        internal void ReportWarning(string format, params object[] args)
        {
            if (this.Configuration.ShowWarnings)
            {
                string message = IO.Utilities.Format(format, args);
                this.Report("Warning: ", ConsoleColor.Red);
                this.Report(message, ConsoleColor.Yellow);
                this.Logger.WriteLine(string.Empty);
            }

            this.WarningCount++;
        }

        /// <summary>
        /// Reports a warning to the user.
        /// </summary>
        internal void ReportWarning(TraceInfo trace, string s)
        {
            this.ReportWarning(s);
            if (this.Configuration.ShowWarnings)
            {
                this.Logger.Write("   at '{0}' ", trace.ErrorTrace[trace.ErrorTrace.Count - 1].Expression);
                this.Logger.Write("in {0}:", trace.ErrorTrace[trace.ErrorTrace.Count - 1].File);
                this.Logger.WriteLine("line {0}", trace.ErrorTrace[trace.ErrorTrace.Count - 1].Line);
            }
        }

        /// <summary>
        /// Reports a warning to the user.
        /// </summary>
        internal void ReportWarning(TraceInfo trace, string s, params object[] args)
        {
            this.ReportWarning(s, args);
            if (this.Configuration.ShowWarnings)
            {
                this.Logger.Write("   at '{0}' ", trace.ErrorTrace[trace.ErrorTrace.Count - 1].Expression);
                this.Logger.Write("in {0}:", trace.ErrorTrace[trace.ErrorTrace.Count - 1].File);
                this.Logger.WriteLine("line {0}", trace.ErrorTrace[trace.ErrorTrace.Count - 1].Line);
            }
        }

        /// <summary>
        /// Reports an access of an object with given-up ownership.
        /// </summary>
        internal void ReportGivenUpOwnershipAccess(TraceInfo trace)
        {
            string message;
            if (trace.State == null)
            {
                message = IO.Utilities.Format(
                    "Method '{0}' of machine '{1}' accesses '{2}' after giving up its ownership.",
                    trace.Method, trace.Machine, trace.Payload);
            }
            else
            {
                message = IO.Utilities.Format(
                    "Method '{0}' in state '{1}' of machine '{2}' accesses '{3}' after giving up its ownership.",
                    trace.Method, trace.State, trace.Machine, trace.Payload);
            }

            this.ReportErrorTrace(trace, message, true);
        }

        /// <summary>
        /// Reports an access of a field that is alias of an object with given-up ownership.
        /// </summary>
        internal void ReportGivenUpOwnershipFieldAccess(TraceInfo trace, IFieldSymbol fieldSymbol)
        {
            string message;
            if (trace.State == null)
            {
                message = IO.Utilities.Format(
                    "Method '{0}' of machine '{1}' accesses '{2}', via field '{3}', after giving up its ownership.",
                    trace.Method, trace.Machine, trace.Payload, fieldSymbol);
            }
            else
            {
                message = IO.Utilities.Format(
                    "Method '{0}' in state '{1}' of machine '{2}' accesses '{3}', via field '{4}', after giving up its ownership.",
                    trace.Method, trace.State, trace.Machine, trace.Payload, fieldSymbol);
            }

            this.ReportErrorTrace(trace, message, true);
        }

        /// <summary>
        /// Reports a given up field ownership error.
        /// </summary>
        internal void ReportGivenUpFieldOwnershipError(TraceInfo trace, ISymbol fieldSymbol)
        {
            string message;
            if (trace.State == null)
            {
                message = IO.Utilities.Format(
                    "Method '{0}' of machine '{1}' sends '{2}', which contains data from field '{3}'.",
                    trace.Method, trace.Machine, trace.Payload, fieldSymbol);
            }
            else
            {
                message = IO.Utilities.Format(
                    "Method '{0}' in state '{1}' of machine '{2}' sends '{3}', which contains data from field '{4}'.",
                    trace.Method, trace.State, trace.Machine, trace.Payload, fieldSymbol);
            }

            this.ReportErrorTrace(trace, message);
        }

        /// <summary>
        /// Reports assignment of given up ownership to a machine field.
        /// </summary>
        internal void ReportGivenUpOwnershipFieldAssignment(TraceInfo trace, ISymbol fieldSymbol)
        {
            string message;
            if (trace.State == null)
            {
                message = IO.Utilities.Format(
                    "Method '{0}' of machine '{1}' assigns '{2}' to field '{3}' after giving up its ownership.",
                    trace.Method, trace.Machine, trace.Payload, fieldSymbol);
            }
            else
            {
                message = IO.Utilities.Format(
                    "Method '{0}' in state '{1}' of machine '{2}' assigns '{3}' to field '{4}' after giving up its ownership.",
                    trace.Method, trace.State, trace.Machine, trace.Payload, fieldSymbol);
            }

            this.ReportErrorTrace(trace, message);
        }

        /// <summary>
        /// Reports sending data with a given up ownership.
        /// </summary>
        internal void ReportGivenUpOwnershipSending(TraceInfo trace, ISymbol argSymbol)
        {
            string message;
            if (trace.State == null)
            {
                message = IO.Utilities.Format(
                    "Method '{0}' of machine '{1}' sends '{2}', the ownership of which has already been given up.",
                    trace.Method, trace.Machine, argSymbol);
            }
            else
            {
                message = IO.Utilities.Format(
                    "Method '{0}' in state '{1}' of machine '{2}' sends '{3}', the ownership of which has already been given up.",
                    trace.Method, trace.State, trace.Machine, argSymbol);
            }

            this.ReportErrorTrace(trace, message);
        }

        /// <summary>
        /// Reports calling a method with unavailable source code,
        /// thus cannot be further analyzed.
        /// </summary>
        internal void ReportExternalInvocation(TraceInfo trace)
        {
            string message;
            if (trace.State == null)
            {
                message = IO.Utilities.Format(
                    "Method '{0}' of machine '{1}' calls a method with unavailable source code, which might be a source of errors.",
                    trace.Method, trace.Machine);
            }
            else
            {
                message = IO.Utilities.Format(
                    "Method '{0}' in state '{1}' of machine '{2}' calls a method with unavailable source code, which might be a source of errors.",
                    trace.Method, trace.State, trace.Machine);
            }

            this.ReportWarningTrace(trace, message);
        }

        /// <summary>
        /// Reports calling a virtual method with unknown overrider,
        /// thus cannot be further analyzed.
        /// </summary>
        internal void ReportUnknownVirtualCall(TraceInfo trace)
        {
            string message;
            if (trace.State == null)
            {
                message = IO.Utilities.Format(
                    "Method '{0}' of machine '{1}' calls a virtual method that cannot be further analyzed.",
                    trace.Method, trace.Machine);
            }
            else
            {
                message = IO.Utilities.Format(
                    "Method '{0}' in state '{1}' of machine '{2}' calls a virtual method that cannot be further analyzed.",
                    trace.Method, trace.State, trace.Machine);
            }

            this.ReportWarningTrace(trace, message);
        }

        /// <summary>
        /// Reports an error to the user.
        /// </summary>
        private void ReportErrorTrace(TraceInfo trace, string message, bool allowMultiple = false)
        {
            if (!allowMultiple && this.ReportedMessages.Contains(message))
            {
                return;
            }

            this.Report(message);
            this.PrintTrace(trace);
            this.ReportedMessages.Add(message);
        }

        /// <summary>
        /// Reports a warning to the user.
        /// </summary>
        private void ReportWarningTrace(TraceInfo trace, string message)
        {
            if (this.ReportedMessages.Contains(message))
            {
                return;
            }

            this.ReportWarning(message);
            this.PrintTrace(trace);
            this.ReportedMessages.Add(message);
        }

        /// <summary>
        /// Prints the trace.
        /// </summary>
        private void PrintTrace(TraceInfo trace)
        {
            for (int idx = trace.ErrorTrace.Count - 1; idx >= 0; idx--)
            {
                if (idx == 0)
                {
                    this.Logger.WriteLine("   --- Source of giving up ownership ---");
                    this.Logger.Write("   at '{0}' ", trace.ErrorTrace[idx].Expression);
                    this.Logger.Write("in {0}:", trace.ErrorTrace[idx].File);
                    this.Logger.WriteLine("line {0}", trace.ErrorTrace[idx].Line);
                }
                else
                {
                    this.Logger.Write("   at '{0}' ", trace.ErrorTrace[idx].Expression);
                    this.Logger.Write("in {0}:", trace.ErrorTrace[idx].File);
                    this.Logger.WriteLine("line {0}", trace.ErrorTrace[idx].Line);
                }
            }
        }
    }
}
