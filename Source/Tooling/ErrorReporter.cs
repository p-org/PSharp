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
        #region public API

        /// <summary>
        /// Reports a generic error to the user and exits.
        /// </summary>
        /// <param name="s">String</param>
        public static void ReportErrorAndExit(string s)
        {
            ConsoleColor previous = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("Error: ");
            Console.ForegroundColor = previous;
            Console.WriteLine(s);
            Environment.Exit(1);
        }

        /// <summary>
        /// Reports a generic error to the user and exits.
        /// </summary>
        /// <param name="s">String</param>
        /// <param name="args">Parameters</param>
        public static void ReportErrorAndExit(string s, params object[] args)
        {
            string message = ErrorReporter.Format(s, args);
            ConsoleColor previous = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("Error: ");
            Console.ForegroundColor = previous;
            Console.WriteLine(message);
            Environment.Exit(1);
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
