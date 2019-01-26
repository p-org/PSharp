//-----------------------------------------------------------------------
// <copyright file="ThreadSafeInMemoryLogger.cs">
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
            lock (this.Writer)
            {
                this.Writer.Write(value);
            }
        }

        /// <summary>
        /// Writes the text representation of the specified array of objects.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Arguments</param>
        public override void Write(string format, params object[] args)
        {
            lock (this.Writer)
            {
                this.Writer.Write(format, args);
            }
        }

        /// <summary>
        /// Writes the specified string value, followed by the
        /// current line terminator.
        /// </summary>
        /// <param name="value">Text</param>
        public override void WriteLine(string value)
        {
            lock (this.Writer)
            {
                this.Writer.WriteLine(value);
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
            lock (this.Writer)
            {
                this.Writer.WriteLine(format, args);
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
