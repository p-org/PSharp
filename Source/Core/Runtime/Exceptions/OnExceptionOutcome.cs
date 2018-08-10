//-----------------------------------------------------------------------
// <copyright file="OnExceptionOutcome.cs">
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

namespace Microsoft.PSharp.Runtime
{
    /// <summary>
    /// The outcome when a machine throws an exception.
    /// </summary>
    public enum OnExceptionOutcome
    {
        /// <summary>
        /// Throw the exception causing the runtime to fail.
        /// </summary>
        ThrowException = 0,

        /// <summary>
        /// The exception was handled and Machine should continue execution.
        /// </summary>
        HandledException = 1,

        /// <summary>
        /// Halt the machine (do not throw the exception).
        /// </summary>
        HaltMachine = 2
    }
}