//-----------------------------------------------------------------------
// <copyright file="IO.cs">
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
using System.Globalization;
using System.IO;

namespace Microsoft.PSharp.Utilities
{
    /// <summary>
    /// Static class implementing IO methods.
    /// </summary>
    public static class IO
    {
        #region error

        /// <summary>
        /// Static class implementing IO error
        /// reporting methods.
        /// </summary>
        public static class Error
        {
            /// <summary>
            ///  Writes the specified string value to the error stream.
            /// </summary>
            /// <param name="s">String</param>
            public static void Print(string s)
            {
                Console.Error.Write(s);
            }

            /// <summary>
            ///  Writes the specified string value to the error stream.
            /// </summary>
            /// <param name="color">ConsoleColor</param>
            /// <param name="s">String</param>
            public static void Print(ConsoleColor color, string s)
            {
                var previousForegroundColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.Error.Write(s);
                Console.ForegroundColor = previousForegroundColor;
            }

            /// <summary>
            /// Writes the text representation of the specified array
            /// of objects to the error stream.
            /// </summary>
            /// <param name="s">String</param>
            /// <param name="args">Arguments</param>
            public static void Print(string s, params object[] args)
            {
                Console.Error.Write(s, args);
            }

            /// <summary>
            /// Writes the text representation of the specified array
            /// of objects to the error stream.
            /// </summary>
            /// <param name="color">ConsoleColor</param>
            /// <param name="s">String</param>
            /// <param name="args">Arguments</param>
            public static void Print(ConsoleColor color, string s, params object[] args)
            {
                var previousForegroundColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.Error.Write(s, args);
                Console.ForegroundColor = previousForegroundColor;
            }

            /// <summary>
            /// Writes a new line, to the error stream.
            /// </summary>
            public static void PrintLine()
            {
                Console.Error.WriteLine();
            }

            /// <summary>
            /// Writes the specified string value, followed by the
            /// current line terminator, to the error stream.
            /// </summary>
            /// <param name="s">String</param>
            public static void PrintLine(string s)
            {
                Console.Error.WriteLine(s);
            }

            /// <summary>
            /// Writes the text representation of the specified array
            /// of objects, followed by the current line terminator, to
            /// the error stream.
            /// </summary>
            /// <param name="s">String</param>
            /// <param name="args">Arguments</param>
            public static void PrintLine(string s, params object[] args)
            {
                Console.Error.WriteLine(s, args);
            }

            /// <summary>
            /// Writes the text representation of the specified array
            /// of objects to the error stream. The text is formatted.
            /// </summary>
            /// <param name="s">String</param>
            /// <param name="args">Arguments</param>
            public static void PrettyPrint(string s, params object[] args)
            {
                string message = IO.Format(s, args);
                IO.Error.Print(message);
            }

            /// <summary>
            /// Writes the text representation of the specified array
            /// of objects, followed by the current line terminator, to
            /// the error stream. The text is formatted.
            /// </summary>
            /// <param name="s">String</param>
            /// <param name="args">Arguments</param>
            public static void PrettyPrintLine(string s, params object[] args)
            {
                string message = IO.Format(s, args);
                IO.Error.PrintLine(message);
            }

            /// <summary>
            /// Reports a generic error to the user.
            /// </summary>
            /// <param name="s">String</param>
            public static void Report(string s)
            {
                IO.Error.Print(ConsoleColor.Red, "Error: ");
                IO.Error.Print(ConsoleColor.Yellow, s);
                IO.Error.PrintLine();
            }

            /// <summary>
            /// Reports a generic error to the user.
            /// </summary>
            /// <param name="s">String</param>
            /// <param name="args">Parameters</param>
            public static void Report(string s, params object[] args)
            {
                string message = IO.Format(s, args);
                IO.Error.Print(ConsoleColor.Red, "Error: ");
                IO.Error.Print(ConsoleColor.Yellow, message);
                IO.Error.PrintLine();
            }

            /// <summary>
            /// Reports a generic error to the user and exits.
            /// </summary>
            /// <param name="s">String</param>
            public static void ReportAndExit(string s)
            {
                IO.Error.Print(ConsoleColor.Red, "Error: ");
                IO.Error.Print(ConsoleColor.Yellow, s);
                IO.Error.PrintLine();
                Environment.Exit(1);
            }

            /// <summary>
            /// Reports a generic error to the user and exits.
            /// </summary>
            /// <param name="s">String</param>
            /// <param name="args">Parameters</param>
            public static void ReportAndExit(string s, params object[] args)
            {
                string message = IO.Format(s, args);
                IO.Error.Print(ConsoleColor.Red, "Error: ");
                IO.Error.Print(ConsoleColor.Yellow, message);
                IO.Error.PrintLine();
                Environment.Exit(1);
            }
        }

        #endregion

        #region fields

        /// <summary>
        /// Text writer.
        /// </summary>
        private static TextWriter Logger;

        /// <summary>
        /// Enable writting to the installed text writer.
        /// </summary>
        private static bool WriteToInstalledLogger;

        /// <summary>
        /// Enables debug information.
        /// </summary>
        internal static bool Debugging;

        #endregion

        #region public methods

        /// <summary>
        /// Static constructor.
        /// </summary>
        static IO()
        {
            IO.Debugging = false;
        }

        /// <summary>
        /// Formats the given string.
        /// </summary>
        /// <param name="s">String</param>
        /// <param name="args">Arguments</param>
        /// <returns></returns>
        internal static string Format(string s, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, s, args);
        }

        /// <summary>
        /// Returns the next character from the standard input stream.
        /// </summary>
        /// <returns>int</returns>
        public static int Get()
        {
            return Console.Read();
        }

        /// <summary>
        /// Returns the next line of characters from the standard input stream.
        /// </summary>
        /// <returns>string</returns>
        public static string GetLine()
        {
            return Console.ReadLine();
        }

        /// <summary>
        ///  Writes the specified string value to the output stream.
        /// </summary>
        /// <param name="s">String</param>
        public static void Print(string s)
        {
            if (IO.WriteToInstalledLogger)
            {
                IO.Logger.Write(s);
            }
            else
            {
                Console.Write(s);
            }
        }

        /// <summary>
        ///  Writes the specified string value to the output stream.
        /// </summary>
        /// <param name="color">ConsoleColor</param>
        /// <param name="s">String</param>
        public static void Print(ConsoleColor color, string s)
        {
            if (IO.WriteToInstalledLogger)
            {
                IO.Logger.Write(s);
            }
            else
            {
                var previousForegroundColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.Write(s);
                Console.ForegroundColor = previousForegroundColor;
            }
        }

        /// <summary>
        /// Writes the text representation of the specified array
        /// of objects to the output stream.
        /// </summary>
        /// <param name="s">String</param>
        /// <param name="args">Arguments</param>
        public static void Print(string s, params object[] args)
        {
            if (IO.WriteToInstalledLogger)
            {
                IO.Logger.Write(s, args);
            }
            else
            {
                Console.Write(s, args);
            }
        }

        /// <summary>
        /// Writes the text representation of the specified array
        /// of objects to the output stream.
        /// </summary>
        /// <param name="color">ConsoleColor</param>
        /// <param name="s">String</param>
        /// <param name="args">Arguments</param>
        public static void Print(ConsoleColor color, string s, params object[] args)
        {
            if (IO.WriteToInstalledLogger)
            {
                IO.Logger.Write(s, args);
            }
            else
            {
                var previousForegroundColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.Write(s, args);
                Console.ForegroundColor = previousForegroundColor;
            }
        }

        /// <summary>
        /// Writes a new line, to the output stream.
        /// </summary>
        public static void PrintLine()
        {
            if (IO.WriteToInstalledLogger)
            {
                IO.Logger.WriteLine();
            }
            else
            {
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Writes the specified string value, followed by the
        /// current line terminator, to the output stream.
        /// </summary>
        /// <param name="s">String</param>
        public static void PrintLine(string s)
        {
            if (IO.WriteToInstalledLogger)
            {
                IO.Logger.WriteLine(s);
            }
            else
            {
                Console.WriteLine(s);
            }
        }

        /// <summary>
        /// Writes the text representation of the specified array
        /// of objects, followed by the current line terminator, to
        /// the output stream.
        /// </summary>
        /// <param name="s">String</param>
        /// <param name="args">Arguments</param>
        public static void PrintLine(string s, params object[] args)
        {
            if (IO.WriteToInstalledLogger)
            {
                IO.Logger.WriteLine(s, args);
            }
            else
            {
                Console.WriteLine(s, args);
            }
        }

        /// <summary>
        /// Writes the text representation of the specified array
        /// of objects to the output stream. The text is formatted.
        /// </summary>
        /// <param name="s">String</param>
        /// <param name="args">Arguments</param>
        public static void PrettyPrint(string s, params object[] args)
        {
            string message = IO.Format(s, args);
            IO.Print(message);
        }

        /// <summary>
        /// Writes the text representation of the specified array
        /// of objects, followed by the current line terminator, to
        /// the output stream. The text is formatted.
        /// </summary>
        /// <param name="s">String</param>
        /// <param name="args">Arguments</param>
        public static void PrettyPrintLine(string s, params object[] args)
        {
            string message = IO.Format(s, args);
            IO.PrintLine(message);
        }

        /// <summary>
        /// Prints the logging information, followed by the current
        /// line terminator, to the output stream.
        /// </summary>
        /// <param name="s">String</param>
        /// <param name="args">Arguments</param>
        public static void Log(string s, params object[] args)
        {
            string message = IO.Format(s, args);
            IO.PrintLine(message);
        }

        /// <summary>
        /// Prints the debugging information, followed by the current
        /// line terminator, to the output stream. The print occurs
        /// only if debugging is enabled.
        /// </summary>
        /// <param name="s">String</param>
        /// <param name="args">Arguments</param>
        public static void Debug(string s, params object[] args)
        {
            if (!IO.Debugging)
            {
                return;
            }

            string message = IO.Format(s, args);
            IO.PrintLine(message);
        }

        #endregion

        #region internal methods

        /// <summary>
        /// Returns the output that was written to memory.
        /// </summary>
        internal static string GetOutput()
        {
            if (!IO.WriteToInstalledLogger)
            {
                throw new PSharpIOException("Custom logger not installed.");
            }

            return IO.Logger.ToString();
        }

        /// <summary>
        /// Starts writing all output to memory.
        /// </summary>
        internal static void StartWritingToMemory()
        {
            if (IO.WriteToInstalledLogger)
            {
                throw new PSharpIOException("Remove the previous logger " +
                    "before installing a new one.");
            }

            IO.WriteToInstalledLogger = true;
            IO.Logger = new StringWriter();
        }

        /// <summary>
        /// Stops writing all output to memory.
        /// </summary>
        internal static void StopWritingToMemory()
        {
            IO.WriteToInstalledLogger = false;
            IO.Logger.Dispose();
            IO.Logger = null;
        }

        /// <summary>
        /// Starts writing all output to the provided logger.
        /// </summary>
        /// <param name="logger">TextWriter</param>
        internal static void InstallCustomLogger(TextWriter logger)
        {
            IO.WriteToInstalledLogger = logger == null ? false : true;
            IO.Logger = logger;
        }

        #endregion
    }
}
