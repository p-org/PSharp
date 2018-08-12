//-----------------------------------------------------------------------
// <copyright file="IMachineRuntimeManager.cs">
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
    /// The interface of the P# runtime manager. It provides APIs for
    /// accessing internal functionality of the runtime.
    /// </summary>
    internal interface IMachineRuntimeManager : IRuntimeManager
    {
        #region notifications

        /// <summary>
        /// Notifies that a machine called Receive.
        /// </summary>
        /// <param name="machine">The machine.</param>
        void NotifyReceiveCalled(Machine machine);

        /// <summary>
        /// Notifies that a machine is waiting to receive one or more events.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="eventInfoInInbox">The event info if it is in the inbox, else null</param>
        void NotifyWaitEvents(Machine machine, EventInfo eventInfoInInbox);

        /// <summary>
        /// Notifies that a machine received an <see cref="Event"/> that it was waiting for.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="eventInfo">The event metadata.</param>
        void NotifyReceivedEvent(Machine machine, EventInfo eventInfo);

        #endregion
    }
}
