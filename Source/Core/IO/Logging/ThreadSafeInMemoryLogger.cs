// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.IO;

namespace Microsoft.PSharp.IO
{
    /// <summary>
    /// Thread safe logger that writes text in-memory.
    /// </summary>
    public sealed class ThreadSafeInMemoryLogger : MachineLogger
    {
        /// <summary>
        /// Underlying string writer.
        /// </summary>
        private readonly StringWriter Writer;

        /// <summary>
        /// Creates a new in-memory logger that logs everything by default.
        /// </summary>
        public ThreadSafeInMemoryLogger()
            : base(0)
        {
            this.Writer = new StringWriter();
        }

        /// <summary>
        /// Writes the specified string value.
        /// </summary>
        /// <param name="value">Text</param>
        public override void Write(string value)
        {
            try
            {
                lock (this.Writer)
                {
                    this.Writer.Write(value);
                }
            }
            catch (ObjectDisposedException)
            {
                // The writer was disposed.
            }
        }

        /// <summary>
        /// Writes the text representation of the specified array of objects.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Arguments</param>
        public override void Write(string format, params object[] args)
        {
            try
            {
                lock (this.Writer)
                {
                    this.Writer.Write(format, args);
                }
            }
            catch (ObjectDisposedException)
            {
                // The writer was disposed.
            }
        }

        /// <summary>
        /// Writes the specified string value, followed by the
        /// current line terminator.
        /// </summary>
        /// <param name="value">Text</param>
        public override void WriteLine(string value)
        {
            try
            {
                lock (this.Writer)
                {
                    this.Writer.WriteLine(value);
                }
            }
            catch (ObjectDisposedException)
            {
                // The writer was disposed.
            }
        }

        /// <summary>
        /// Writes the text representation of the specified array of objects,
        /// followed by the current line terminator.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Arguments</param>
        public override void WriteLine(string format, params object[] args)
        {
            try
            {
                lock (this.Writer)
                {
                    this.Writer.WriteLine(format, args);
                }
            }
            catch (ObjectDisposedException)
            {
                // The writer was disposed.
            }
        }

        /// <summary>
        /// Returns the logged text as a string.
        /// </summary>
        public override string ToString()
        {
            lock (this.Writer)
            {
                return this.Writer.ToString();
            }
        }

        /// <summary>
        /// Disposes the logger.
        /// </summary>
        public override void Dispose()
        {
            this.Writer.Dispose();
        }
    }
}
