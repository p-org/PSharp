//-----------------------------------------------------------------------
// <copyright file="IOLogger.cs">
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

namespace Microsoft.PSharp.Utilities
{
    /// <summary>
    /// Static class for setting a custom logger.
    /// </summary>
    public static class IOLogger
    {
        /// <summary>
        /// Starts writing all output to the provided logger,
        /// which is of type TextWriter. The logger must at
        /// minimum override the method Write(char).
        /// </summary>
        /// <param name="logger">TextWriter</param>
        public static void InstallCustomLogger(TextWriter logger)
        {
            IO.InstallCustomLogger(logger);
        }

        /// <summary>
        /// Remove the custom logger provided previously
        /// (Defaults to the console)
        /// </summary>
        public static void RemoveCustomLogger()
        {
            IO.InstallCustomLogger(null);
        }
    }
}
