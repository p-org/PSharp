//-----------------------------------------------------------------------
// <copyright file="ExecutionCanceledException.cs">
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

namespace Microsoft.PSharp.Runtime
{
    /// <summary>
    /// The exception that is thrown in a P# machine upon cancellation
    /// of execution by the P# runtime.
    /// </summary>
    public sealed class ExecutionCanceledException : RuntimeException
    {
        /// <summary>
        /// Initializes a new instance of the exception.
        /// </summary>
        internal ExecutionCanceledException() { }
    }
}
