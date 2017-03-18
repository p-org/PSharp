//-----------------------------------------------------------------------
// <copyright file="ILogger.cs">
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

using System;

namespace Microsoft.PSharp.IO
{
    /// <summary>
    /// Interface for the P# logger.
    /// </summary>
    public interface ILogger : IDisposable
    {
        /// <summary>
        /// Writes the specified string value.
        /// </summary>
        /// <param name="message">Text</param>
        void Write(string message);

        /// <summary>
        /// Writes the text representation of the specified array of objects.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Arguments</param>
        void Write(string format, params object[] args);

        /// <summary>
        /// Writes the specified string value, followed by the
        /// current line terminator.
        /// </summary>
        /// <param name="value">Text</param>
        void WriteLine(string value);

        /// <summary>
        /// Writes the text representation of the specified array of objects,
        /// followed by the current line terminator.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Arguments</param>
        void WriteLine(string format, params object[] args);
    }
}
