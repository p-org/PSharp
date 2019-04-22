﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.PSharp.IO
{
    /// <summary>
    /// Logger that writes text to the console.
    /// </summary>
    public sealed class ConsoleLogger : MachineLogger
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleLogger"/> class.
        /// </summary>
        /// <param name="isVerbose">If true, then messages are logged.</param>
        public ConsoleLogger(bool isVerbose)
            : base(isVerbose)
        {
        }

        /// <summary>
        /// Writes the specified string value.
        /// </summary>
        public override void Write(string value)
        {
            if (this.IsVerbose)
            {
                Console.Write(value);
            }
        }

        /// <summary>
        /// Writes the text representation of the specified argument.
        /// </summary>
        public override void Write(string format, object arg0)
        {
            if (this.IsVerbose)
            {
                Console.Write(format, arg0.ToString());
            }
        }

        /// <summary>
        /// Writes the text representation of the specified arguments.
        /// </summary>
        public override void Write(string format, object arg0, object arg1)
        {
            if (this.IsVerbose)
            {
                Console.Write(format, arg0.ToString(), arg1.ToString());
            }
        }

        /// <summary>
        /// Writes the text representation of the specified arguments.
        /// </summary>
        public override void Write(string format, object arg0, object arg1, object arg2)
        {
            if (this.IsVerbose)
            {
                Console.Write(format, arg0.ToString(), arg1.ToString(), arg2.ToString());
            }
        }

        /// <summary>
        /// Writes the text representation of the specified array of objects.
        /// </summary>
        public override void Write(string format, params object[] args)
        {
            if (this.IsVerbose)
            {
                Console.Write(format, args);
            }
        }

        /// <summary>
        /// Writes the specified string value, followed by the
        /// current line terminator.
        /// </summary>
        public override void WriteLine(string value)
        {
            if (this.IsVerbose)
            {
                Console.WriteLine(value);
            }
        }

        /// <summary>
        /// Writes the text representation of the specified argument, followed by the
        /// current line terminator.
        /// </summary>
        public override void WriteLine(string format, object arg0)
        {
            if (this.IsVerbose)
            {
                Console.WriteLine(format, arg0.ToString());
            }
        }

        /// <summary>
        /// Writes the text representation of the specified arguments, followed by the
        /// current line terminator.
        /// </summary>
        public override void WriteLine(string format, object arg0, object arg1)
        {
            if (this.IsVerbose)
            {
                Console.WriteLine(format, arg0.ToString(), arg1.ToString());
            }
        }

        /// <summary>
        /// Writes the text representation of the specified arguments, followed by the
        /// current line terminator.
        /// </summary>
        public override void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            if (this.IsVerbose)
            {
                Console.WriteLine(format, arg0.ToString(), arg1.ToString(), arg2.ToString());
            }
        }

        /// <summary>
        /// Writes the text representation of the specified array of objects,
        /// followed by the current line terminator.
        /// </summary>
        public override void WriteLine(string format, params object[] args)
        {
            if (this.IsVerbose)
            {
                Console.WriteLine(format, args);
            }
        }

        /// <summary>
        /// Disposes the logger.
        /// </summary>
        public override void Dispose()
        {
        }
    }
}
