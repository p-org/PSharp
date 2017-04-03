//-----------------------------------------------------------------------
// <copyright file="Utilities.cs">
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
