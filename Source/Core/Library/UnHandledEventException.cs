//-----------------------------------------------------------------------
// <copyright file="UnHandledEventException.cs">
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
    /// Signals that a machine received an unhandled event
    /// </summary>
    internal sealed class UnHandledEventException : RuntimeException
    {
        /// <summary>
        /// The machine that threw the exception
        /// </summary>
        public MachineId mid;

        /// <summary>
        /// Name of the current state of the machine
        /// </summary>
        public string CurrentStateName;

        /// <summary>
        ///  The event
        /// </summary>
        public Event UnhandledEvent;

        /// <summary>
        /// Initializes a new instance of the exception.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="CurrentStateName">Current state name</param>
        /// <param name="UnhandledEvent">The event that was unhandled</param>
        /// <param name="message">Message</param>
        internal UnHandledEventException(MachineId mid, string CurrentStateName, Event UnhandledEvent, string message)
            : base(message)
        {
            this.mid = mid;
            this.CurrentStateName = CurrentStateName;
            this.UnhandledEvent = UnhandledEvent;
        }

    }
}

