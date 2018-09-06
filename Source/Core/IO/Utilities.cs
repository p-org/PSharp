// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Microsoft.PSharp.IO
{
    /// <summary>
    /// IO utilities.
    /// </summary>
    internal static class Utilities
    {
        /// <summary>
        /// Formats the given string.
        /// </summary>
        /// <param name="value">Text</param>
        /// <param name="args">Arguments</param>
        /// <returns></returns>
        internal static string Format(string value, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, value, args);
        }
    }
}
