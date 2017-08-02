//-----------------------------------------------------------------------
// <copyright file="Debug.cs">
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
    /// Static class implementing debug reporting methods.
    /// </summary>
    public static class Debug
    {
        #region fields

        /// <summary>
        /// Checks if debugging is enabled.
        /// </summary>
        internal static bool IsEnabled;

        #endregion

        #region constructors

        /// <summary>
        /// Static constructor.
        /// </summary>
        static Debug()
        {
            IsEnabled = false;
        }

        #endregion

        #region methods

        /// <summary>
        /// Writes the debugging information to the output stream. The
        /// print occurs only if debugging is enabled.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Arguments</param>
        public static void Write(string format, params object[] args)
        {
            if (IsEnabled)
            {
                string message = Utilities.Format(format, args);
                Console.Write(message);
            }
        }

        /// <summary>
        /// Writes the debugging information, followed by the current
        /// line terminator, to the output stream. The print occurs
        /// only if debugging is enabled.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Arguments</param>
        public static void WriteLine(string format, params object[] args)
        {
            if (IsEnabled)
            {
                string message = Utilities.Format(format, args);
                Console.WriteLine(message);
            }
        }

        #endregion
    }
}
