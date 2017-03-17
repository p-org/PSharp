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

namespace Microsoft.PSharp.IO
{
    /// <summary>
    /// Reports errors and warnings to the user.
    /// </summary>
    internal static class ErrorReporter
    {
        #region fields

        /// <summary>
        /// Report warnings if true.
        /// </summary>
        internal static bool ShowWarnings;

        #endregion

        #region public methods

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
        /// <param name="logger">ILogger</param>
        /// <param name="value">Text</param>
        internal static void Report(ILogger logger, string value)
        {
            Report(logger, "Error: ", ConsoleColor.Red);
            Report(logger, value, ConsoleColor.Yellow);
            logger?.WriteLine("");
        }

        /// <summary>
        /// Reports a generic warning to the user.
        /// </summary>
        /// <param name="logger">ILogger</param>
        /// <param name="value">Text</param>
        internal static void ReportWarning(ILogger logger, string value)
        {
            if (ErrorReporter.ShowWarnings)
            {
                Report(logger, "Warning: ", ConsoleColor.Red);
                Report(logger, value, ConsoleColor.Yellow);
                logger?.WriteLine("");
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Reports a generic error to the user using the specified color.
        /// </summary>
        /// <param name="logger">ILogger</param>
        /// <param name="value">Text</param>
        /// <param name="color">ConsoleColor</param>
        private static void Report(ILogger logger, string value, ConsoleColor color)
        {
            var previousForegroundColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            logger?.Write(value);
            Console.ForegroundColor = previousForegroundColor;
        }

        #endregion
    }
}
