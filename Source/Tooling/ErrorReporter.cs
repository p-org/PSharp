//-----------------------------------------------------------------------
// <copyright file="ErrorReporter.cs">
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
using System.Globalization;

namespace Microsoft.PSharp.Tooling
{
    /// <summary>
    /// Reports errors and warnings to the user.
    /// </summary>
    public class ErrorReporter
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
        /// Reports a generic error to the user.
        /// </summary>
        /// <param name="s">String</param>
        /// <param name="args">Parameters</param>
        public static void Report(string s, params object[] args)
        {
            string message = ErrorReporter.Format(s, args);
            ConsoleColor previous = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("Error: ");
            Console.ForegroundColor = previous;
            Console.WriteLine(message);
            ErrorReporter.ErrorCount++;
        }

        /// <summary>
        /// Prints error statistics.
        /// </summary>
        public static void PrintStats()
        {
            var errorStr = "error";
            if (ErrorReporter.ErrorCount > 1)
            {
                errorStr = "errors";
            }

            var warningStr = "warning";
            if (ErrorReporter.WarningCount > 1)
            {
                warningStr = "warnings";
            }

            if ((ErrorReporter.ErrorCount > 0 || ErrorReporter.WarningCount > 0) &&
                Configuration.ShowWarnings)
            {
                Console.WriteLine("P# static analyser detected '{0}' {1} and reported '{2}' {3}.",
                    ErrorReporter.ErrorCount, errorStr,
                    ErrorReporter.WarningCount, warningStr);
                Console.WriteLine("(but absolutely no warranty provided)");
            }
            else if (ErrorReporter.ErrorCount > 0)
            {
                Console.WriteLine("P# static analyser detected '{0}' {1}.",
                    ErrorReporter.ErrorCount, errorStr);
                Console.WriteLine("(but absolutely no warranty provided)");
            }
            else
            {
                Console.WriteLine("P# static analyser did not detect any errors.");
                Console.WriteLine("(but absolutely no warranty provided)");
            }
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Formats a string.
        /// </summary>
        /// <param name="s">String</param>
        /// <param name="args">Parameters</param>
        /// <returns>string</returns>
        protected static string Format(string s, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, s, args);
        }

        #endregion
    }
}
