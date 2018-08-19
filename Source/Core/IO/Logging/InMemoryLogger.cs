//-----------------------------------------------------------------------
// <copyright file="InMemoryLogger.cs">
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
    /// Logger that writes text in-memory.
    /// </summary>
    internal sealed class InMemoryLogger : MachineLogger
    {
        /// <summary>
        /// Underlying string writer.
        /// </summary>
        private StringWriter Writer;

        /// <summary>
        /// Creates a new in-memory logger that logs everything by default.
        /// </summary>
        public InMemoryLogger()
            : base(0)
        {
            Writer = new StringWriter();
        }

        /// <summary>
        /// Writes the specified string value.
        /// </summary>
        /// <param name="value">Text</param>
        public override void Write(string value)
        {
            Writer.Write(value);
        }

        /// <summary>
        /// Writes the text representation of the specified array of objects.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Arguments</param>
        public override void Write(string format, params object[] args)
        {
            Writer.Write(format, args);
        }

        /// <summary>
        /// Writes the specified string value, followed by the
        /// current line terminator.
        /// </summary>
        /// <param name="value">Text</param>
        public override void WriteLine(string value)
        {
            Writer.WriteLine(value);
        }

        /// <summary>
        /// Writes the text representation of the specified array of objects,
        /// followed by the current line terminator.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Arguments</param>
        public override void WriteLine(string format, params object[] args)
        {
            Writer.WriteLine(format, args);
        }

        /// <summary>
        /// Returns the logged text as a string.
        /// </summary>
        public override string ToString()
        {
            return Writer.ToString();
        }

        /// <summary>
        /// Disposes the logger.
        /// </summary>
        public override void Dispose()
        {
            Writer.Dispose();
        }
    }
}
