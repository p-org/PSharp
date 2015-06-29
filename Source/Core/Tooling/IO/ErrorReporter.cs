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

namespace Microsoft.PSharp.Tooling
{
    /// <summary>
    /// Reports errors and warnings to the user.
    /// </summary>
    public class ErrorReporter
    {
        #region public API

        /// <summary>
        /// Reports a generic error to the user.
        /// </summary>
        /// <param name="message">Message</param>
        public static void Report(string message)
        {
            Console.Write("Error: ");
            Console.WriteLine(message);
        }

        /// <summary>
        /// Reports a generic warning to the user.
        /// </summary>
        /// <param name="message">Message</param>
        public static void ReportWarning(string message)
        {
            Console.Write("Warning: ");
            Console.WriteLine(message);
        }

        /// <summary>
        /// Reports a generic error to the user and exits.
        /// </summary>
        /// <param name="s">String</param>
        public static void ReportAndExit(string s)
        {
            Console.Write("Error: ");
            Console.WriteLine(s);
            Environment.Exit(1);
        }

        /// <summary>
        /// Reports a generic error to the user and exits.
        /// </summary>
        /// <param name="s">String</param>
        /// <param name="args">Parameters</param>
        public static void ReportAndExit(string s, params object[] args)
        {
            string message = Output.Format(s, args);
            Console.Write("Error: ");
            Console.WriteLine(message);
            Environment.Exit(1);
        }

        /// <summary>
        /// Reports a generic message to the user and exits.
        /// </summary>
        /// <param name="s">String</param>
        /// <param name="args">Parameters</param>
        public static void WriteLineAndExit(string s, params object[] args)
        {
            string message = Output.Format(s, args);
            Console.WriteLine(message);
            Environment.Exit(1);
        }

        #endregion
    }
}
