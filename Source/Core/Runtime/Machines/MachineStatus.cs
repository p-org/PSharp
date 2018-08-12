//-----------------------------------------------------------------------
// <copyright file="MachineStatus.cs">
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
    /// The status returned by the machine as the result of a runtime operation,
    /// such as enqueuing an event.
    /// </summary>
    internal enum MachineStatus
    {
        /// <summary>
        /// The machine notifies the runtime that an
        /// event handler is already running.
        /// </summary>
        EventHandlerRunning = 0,
        /// <summary>
        /// The machine notifies the runtime that an
        /// event handler is not running.
        /// </summary>
        EventHandlerNotRunning,
        /// <summary>
        /// The machine notifies the runtime that there is no
        /// next event available to dequeue and handle.
        /// </summary>
        NextEventUnavailable,
        /// <summary>
        /// The machine notifies the runtime that the machine is halted.
        /// </summary>
        IsHalted
    }
}
