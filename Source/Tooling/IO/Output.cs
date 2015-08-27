//-----------------------------------------------------------------------
// <copyright file="Output.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
//      EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
//      OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
//      The example companies, organizations, products, domain names,
//      e-mail addresses, logos, people, places, and events depicted
//      herein are fictitious.  No association with any real company,
//      organization, product, domain name, email address, logo, person,
//      places, or events is intended or should be inferred.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.PSharp.Tooling
{
    internal static class Output
    {
        #region fields

        internal static bool Debugging;

        #endregion

        #region API

        /// <summary>
        /// Static constructor.
        /// </summary>
        static Output()
        {
            Output.Debugging = false;
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
        /// Writes the text representation of the specified array
        /// of objects to the output stream.
        /// </summary>
        /// <param name="s">String</param>
        /// <param name="args">Arguments</param>
        internal static void Print(string s, params object[] args)
        {
            Console.Write(s, args);
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
            Console.WriteLine(s, args);
        }

        /// <summary>
        /// Writes the text representation of the specified array
        /// of objects to the output stream. The text is formatted.
        /// </summary>
        /// <param name="s">String</param>
        /// <param name="args">Arguments</param>
        internal static void PrettyPrint(string s, params object[] args)
        {
            string message = Output.Format(s, args);
            Console.Write(message);
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
            string message = Output.Format(s, args);
            Console.WriteLine(message);
        }

        /// <summary>
        /// Prints the logging information, followed by the current
        /// line terminator, to the output stream.
        /// </summary>
        /// <param name="s">String</param>
        /// <param name="args">Arguments</param>
        internal static void Log(string s, params object[] args)
        {
            string message = Output.Format(s, args);
            Console.WriteLine(message);
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
            if (!Output.Debugging)
            {
                return;
            }

            string message = Output.Format(s, args);
            Console.WriteLine(message);
        }

        #endregion
    }
}
