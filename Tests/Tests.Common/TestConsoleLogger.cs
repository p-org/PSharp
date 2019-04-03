// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.PSharp.IO;
using Xunit.Abstractions;

namespace Microsoft.PSharp.Tests.Common
{
    /// <summary>
    /// Logger that writes to the console.
    /// </summary>
    public sealed class TestConsoleLogger : MachineLogger, ITestOutputHelper
    {
        /// <summary>
        /// Writes the specified string value.
        /// </summary>
        /// <param name="value">Text</param>
        public override void Write(string value)
        {
            Console.Write(value);
        }

        /// <summary>
        /// Writes the text representation of the specified array of objects.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Arguments</param>
        public override void Write(string format, params object[] args)
        {
            Console.Write(format, args);
        }

        /// <summary>
        /// Writes the specified string value, followed by the
        /// current line terminator.
        /// </summary>
        /// <param name="value">Text</param>
        public override void WriteLine(string value)
        {
            Console.WriteLine(value);
        }

        /// <summary>
        /// Writes the text representation of the specified array of objects,
        /// followed by the current line terminator.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Arguments</param>
        public override void WriteLine(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }

        /// <summary>
        /// Disposes the logger.
        /// </summary>
        public override void Dispose()
        {
        }
    }
}
