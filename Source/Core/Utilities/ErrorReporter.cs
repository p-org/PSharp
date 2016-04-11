//-----------------------------------------------------------------------
// <copyright file="ErrorReporter.cs">
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

namespace Microsoft.PSharp.Utilities
{
    /// <summary>
    /// Reports errors and warnings to the user.
    /// </summary>
    public static class ErrorReporter
    {
        #region fields

        /// <summary>
        /// Report warnings if true.
        /// </summary>
        public static bool ShowWarnings;

        #endregion

        #region public API

        /// <summary>
        /// Static constructor.
        /// </summary>
        static ErrorReporter()
        {
            ErrorReporter.ShowWarnings = false;
        }

        /// <summary>
        /// Reports a generic error to the user.
        /// </summary>
        /// <param name="s">String</param>
        public static void Report(string s)
        {
            IO.Print(ConsoleColor.Red, "Error: ");
            IO.Print(ConsoleColor.Yellow, s);
            IO.PrintLine();
        }

        /// <summary>
        /// Reports a generic error to the user.
        /// </summary>
        /// <param name="s">String</param>
        /// <param name="args">Parameters</param>
        public static void Report(string s, params object[] args)
        {
            string message = IO.Format(s, args);
            IO.Print(ConsoleColor.Red, "Error: ");
            IO.Print(ConsoleColor.Yellow, message);
            IO.PrintLine();
        }

        /// <summary>
        /// Reports a generic warning to the user.
        /// </summary>
        /// <param name="s">String</param>
        public static void ReportWarning(string s)
        {
            if (ErrorReporter.ShowWarnings)
            {
                IO.Print(ConsoleColor.Red, "Warning: ");
                IO.Print(ConsoleColor.Yellow, s);
                IO.PrintLine();
            }
        }

        /// <summary>
        /// Reports a generic warning to the user.
        /// </summary>
        /// <param name="s">String</param>
        /// <param name="args">Parameters</param>
        public static void ReportWarning(string s, params object[] args)
        {
            if (ErrorReporter.ShowWarnings)
            {
                string message = IO.Format(s, args);
                IO.Print(ConsoleColor.Red, "Warning: ");
                IO.Print(ConsoleColor.Yellow, message);
                IO.PrintLine();
            }
        }

        /// <summary>
        /// Reports a generic error to the user and exits.
        /// </summary>
        /// <param name="s">String</param>
        public static void ReportAndExit(string s)
        {
            ErrorReporter.Report(s);
            Environment.Exit(1);
        }

        /// <summary>
        /// Reports a generic error to the user and exits.
        /// </summary>
        /// <param name="s">String</param>
        /// <param name="args">Parameters</param>
        public static void ReportAndExit(string s, params object[] args)
        {
            ErrorReporter.Report(s, args);
            Environment.Exit(1);
        }

        #endregion
    }
}
