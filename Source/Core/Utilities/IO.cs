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
    internal static class IO
    {
        #region fields

        /// <summary>
        /// Text writer.
        /// </summary>
        private static StringWriter TextWriter;

        /// <summary>
        /// Enable writting to memory.
        /// </summary>
        private static bool WriteToMemory;

        /// <summary>
        /// Enables debug information.
        /// </summary>
        internal static bool Debugging;

        #endregion

        #region API

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
        internal static int Get()
        {
            return Console.Read();
        }

        /// <summary>
        /// Returns the next line of characters from the standard input stream.
        /// </summary>
        /// <returns>string</returns>
        internal static string GetLine()
        {
            return Console.ReadLine();
        }

        /// <summary>
        ///  Writes the specified string value to the output stream.
        /// </summary>
        /// <param name="s">String</param>
        internal static void Print(string s)
        {
            if (IO.WriteToMemory)
            {
                IO.TextWriter.Write(s);
            }
            else
            {
                Console.Write(s);
            }
        }

        /// <summary>
        /// Writes the text representation of the specified array
        /// of objects to the output stream.
        /// </summary>
        /// <param name="s">String</param>
        /// <param name="args">Arguments</param>
        internal static void Print(string s, params object[] args)
        {
            if (IO.WriteToMemory)
            {
                IO.TextWriter.Write(s, args);
            }
            else
            {
                Console.Write(s, args);
            }
        }

        /// <summary>
        /// Writes the specified string value, followed by the
        /// current line terminator, to the output stream.
        /// </summary>
        /// <param name="s">String</param>
        internal static void PrintLine(string s)
        {
            if (IO.WriteToMemory)
            {
                IO.TextWriter.WriteLine(s);
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
        internal static void PrintLine(string s, params object[] args)
        {
            if (IO.WriteToMemory)
            {
                IO.TextWriter.WriteLine(s, args);
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
        internal static void PrettyPrint(string s, params object[] args)
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
        internal static void PrettyPrintLine(string s, params object[] args)
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
        internal static void Log(string s, params object[] args)
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
        internal static void Debug(string s, params object[] args)
        {
            if (!IO.Debugging)
            {
                return;
            }

            string message = IO.Format(s, args);
            IO.PrintLine(message);
        }

        /// <summary>
        /// Returns the output that was written to memory.
        /// </summary>
        internal static string GetOutput()
        {
            return IO.TextWriter.ToString();
        }

        /// <summary>
        /// Starts writing all output to memory.
        /// </summary>
        internal static void StartWritingToMemory()
        {
            IO.WriteToMemory = true;
            IO.TextWriter = new StringWriter();
        }

        /// <summary>
        /// Stops writing all output to memory.
        /// </summary>
        internal static void StopWritingToMemory()
        {
            IO.WriteToMemory = false;
            IO.TextWriter.Dispose();
            IO.TextWriter = null;
        }

        #endregion
    }
}
