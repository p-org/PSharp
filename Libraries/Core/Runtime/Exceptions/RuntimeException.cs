//-----------------------------------------------------------------------
// <copyright file="RuntimeException.cs">
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

namespace Microsoft.PSharp
{
    /// <summary>
    /// An exception that is thrown by the P# runtime.
    /// </summary>
    internal class RuntimeException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the exception.
        /// </summary>
        public RuntimeException()
        {

        }

        /// <summary>
        /// Initializes a new instance of the exception.
        /// </summary>
        public RuntimeException(string message)
            : base(message)
        {

        }
    }
}
