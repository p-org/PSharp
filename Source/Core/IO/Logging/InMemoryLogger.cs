// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.IO;

namespace Microsoft.PSharp.IO
{
    /// <summary>
    /// Logger that writes text in-memory.
    /// </summary>
    internal sealed class InMemoryLogger : MachineLogger
    {
        /// <summary>
        /// Underlying string writer.
        /// </summary>
        private readonly StringWriter Writer;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryLogger"/> class.
        /// </summary>
        public InMemoryLogger()
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
            this.Writer.Write(value);
        }

        /// <summary>
        /// Writes the text representation of the specified argument.
        /// </summary>
        public override void Write(string format, object arg0)
        {
            this.Writer.Write(format, arg0.ToString());
        }

        /// <summary>
        /// Writes the text representation of the specified arguments.
        /// </summary>
        public override void Write(string format, object arg0, object arg1)
        {
            this.Writer.Write(format, arg0.ToString(), arg1.ToString());
        }

        /// <summary>
        /// Writes the text representation of the specified arguments.
        /// </summary>
        public override void Write(string format, object arg0, object arg1, object arg2)
        {
            this.Writer.Write(format, arg0.ToString(), arg1.ToString(), arg2.ToString());
        }

        /// <summary>
        /// Writes the text representation of the specified array of objects.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Arguments</param>
        public override void Write(string format, params object[] args)
        {
            this.Writer.Write(format, args);
        }

        /// <summary>
        /// Writes the specified string value, followed by the
        /// current line terminator.
        /// </summary>
        /// <param name="value">Text</param>
        public override void WriteLine(string value)
        {
            this.Writer.WriteLine(value);
        }

        /// <summary>
        /// Writes the text representation of the specified argument, followed by the
        /// current line terminator.
        /// </summary>
        public override void WriteLine(string format, object arg0)
        {
            this.Writer.WriteLine(format, arg0.ToString());
        }

        /// <summary>
        /// Writes the text representation of the specified arguments, followed by the
        /// current line terminator.
        /// </summary>
        public override void WriteLine(string format, object arg0, object arg1)
        {
            this.Writer.WriteLine(format, arg0.ToString(), arg1.ToString());
        }

        /// <summary>
        /// Writes the text representation of the specified arguments, followed by the
        /// current line terminator.
        /// </summary>
        public override void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            this.Writer.WriteLine(format, arg0.ToString(), arg1.ToString(), arg2.ToString());
        }

        /// <summary>
        /// Writes the text representation of the specified array of objects,
        /// followed by the current line terminator.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Arguments</param>
        public override void WriteLine(string format, params object[] args)
        {
            this.Writer.WriteLine(format, args);
        }

        /// <summary>
        /// Returns the logged text as a string.
        /// </summary>
        public override string ToString()
        {
            return this.Writer.ToString();
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
