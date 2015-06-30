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
using System.Globalization;

namespace Microsoft.PSharp.Tooling
{
    internal static class Output
    {
        internal static string Format(string s, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, s, args);
        }

        internal static void Print(string s)
        {
            Console.WriteLine(s);
        }

        internal static void Write(string s, params object[] args)
        {
            string message = Output.Format(s, args);
            Console.Write(message);
        }

        internal static void WriteLine(string s, params object[] args)
        {
            string message = Output.Format(s, args);
            Console.WriteLine(message);
        }

        internal static void Verbose(string s, params object[] args)
        {
            if (Configuration.Verbose >= 1)
            {
                return;
            }

            string message = Output.Format(s, args);
            Console.WriteLine(message);
        }

        internal static void Debug(DebugType type, string s, params object[] args)
        {
            if (!Configuration.Debug.Contains(DebugType.All) &&
                !Configuration.Debug.Contains(type))
            {
                return;
            }

            string message = Output.Format(s, args);
            Console.WriteLine(message);
        }
    }
}
