﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Globalization;

namespace Microsoft.PSharp.IO
{
    /// <summary>
    /// Static class implementing debug reporting methods.
    /// </summary>
    public static class Debug
    {
        /// <summary>
        /// Checks if debugging is enabled.
        /// </summary>
        internal static bool IsEnabled = false;

        /// <summary>
        /// Writes the debugging information, followed by the current line terminator,
        /// to the output stream. The print occurs only if debugging is enabled.
        /// </summary>
        public static void WriteLine(string format, object arg0)
        {
            if (IsEnabled)
            {
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, format, arg0));
            }
        }

        /// <summary>
        /// Writes the debugging information, followed by the current line terminator,
        /// to the output stream. The print occurs only if debugging is enabled.
        /// </summary>
        public static void WriteLine(string format, object arg0, object arg1)
        {
            if (IsEnabled)
            {
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, format, arg0, arg1));
            }
        }

        /// <summary>
        /// Writes the debugging information, followed by the current line terminator,
        /// to the output stream. The print occurs only if debugging is enabled.
        /// </summary>
        public static void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            if (IsEnabled)
            {
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2));
            }
        }

        /// <summary>
        /// Writes the debugging information, followed by the current line terminator,
        /// to the output stream. The print occurs only if debugging is enabled.
        /// </summary>
        public static void WriteLine(string format, params object[] args)
        {
            if (IsEnabled)
            {
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, format, args));
            }
        }
    }
}
