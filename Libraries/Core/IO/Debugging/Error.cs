//-----------------------------------------------------------------------
// <copyright file="Log.cs">
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

namespace Microsoft.PSharp.IO
{
    /// <summary>
    /// Static class implementing error reporting methods.
    /// </summary>
    internal static class Error
    {
        #region internal methods

        /// <summary>
        /// Reports a generic error to the user.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Parameters</param>
        internal static void Report(string format, params object[] args)
        {
            string message = Utilities.Format(format, args);
            Error.Write(ConsoleColor.Red, "Error: ");
            Error.Write(ConsoleColor.Yellow, message);
            Console.Error.WriteLine("");
        }

        /// <summary>
        /// Reports a generic error to the user and exits.
        /// </summary>
        /// <param name="value">Text</param>
        internal static void ReportAndExit(string value)
        {
            Error.Write(ConsoleColor.Red, "Error: ");
            Error.Write(ConsoleColor.Yellow, value);
            Console.Error.WriteLine("");
            Environment.Exit(1);
        }

        /// <summary>
        /// Reports a generic error to the user and exits.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Parameters</param>
        internal static void ReportAndExit(string format, params object[] args)
        {
            string message = Utilities.Format(format, args);
            Error.Write(ConsoleColor.Red, "Error: ");
            Error.Write(ConsoleColor.Yellow, message);
            Console.Error.WriteLine("");
            Environment.Exit(1);
        }

        #endregion

        #region private methods

        /// <summary>
        ///  Writes the specified string value to the output stream.
        /// </summary>
        /// <param name="color">ConsoleColor</param>
        /// <param name="value">Text</param>
        private static void Write(ConsoleColor color, string value)
        {
            var previousForegroundColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Error.Write(value);
            Console.ForegroundColor = previousForegroundColor;
        }

        #endregion
    }
}
