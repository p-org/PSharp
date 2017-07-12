//-----------------------------------------------------------------------
// <copyright file="SharedCounterEvent.cs">
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

namespace Microsoft.PSharp.SharedObjects
{
    /// <summary>
    /// Event used to communicate with a shared counter machine.
    /// </summary>
    internal class SharedCounterEvent : Event
    {
        /// <summary>
        /// Supported shared counter operations.
        /// </summary>
        internal enum SharedCounterOperation { GET, SET, INC, DEC, ADD, CAS };

        /// <summary>
        /// The operation stored in this event.
        /// </summary>
        public SharedCounterOperation Operation { get; private set; }

        /// <summary>
        /// The shared counter value stored in this event.
        /// </summary>
        public int Value { get; private set; }

        /// <summary>
        /// Comparand value stored in this event.
        /// </summary>
        public int Comparand { get; private set; }

        /// <summary>
        /// The sender machine stored in this event.
        /// </summary>
        public MachineId Sender { get; private set; }

        /// <summary>
        /// Creates a new event with the specified operation.
        /// </summary>
        /// <param name="op">SharedCounterOperation</param>
        /// <param name="value">Value</param>
        /// <param name="comparand">Comparand</param>
        /// <param name="sender">Sender</param>
        SharedCounterEvent(SharedCounterOperation op, int value, int comparand, MachineId sender)
        {
            Operation = op;
            Value = value;
            Comparand = comparand;
            Sender = sender;
        }

        /// <summary>
        /// Creates a new event for the 'INC' operation.
        /// </summary>
        /// <returns>SharedCounterEvent</returns>
        public static SharedCounterEvent IncrementEvent()
        {
            return new SharedCounterEvent(SharedCounterOperation.INC, 0, 0, null);
        }

        /// <summary>
        /// Creates a new event for the 'DEC' operation.
        /// </summary>
        /// <returns>SharedCounterEvent</returns>
        public static SharedCounterEvent DecrementEvent()
        {
            return new SharedCounterEvent(SharedCounterOperation.DEC, 0, 0, null);
        }

        /// <summary>
        /// Creates a new event for the 'SET' operation.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="value">Value</param>
        /// <returns>SharedCounterEvent</returns>
        public static SharedCounterEvent SetEvent(MachineId sender, int value)
        {
            return new SharedCounterEvent(SharedCounterOperation.SET, value, 0, sender);
        }

        /// <summary>
        /// Creates a new event for the 'GET' operation.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <returns>SharedCounterEvent</returns>
        public static SharedCounterEvent GetEvent(MachineId sender)
        {
            return new SharedCounterEvent(SharedCounterOperation.GET, 0, 0, sender);
        }

        /// <summary>
        /// Creates a new event for the 'ADD' operation.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="value">Value</param>
        /// <returns>SharedCounterEvent</returns>
        public static SharedCounterEvent AddEvent(MachineId sender, int value)
        {
            return new SharedCounterEvent(SharedCounterOperation.ADD, value, 0, sender);
        }

        /// <summary>
        /// Creates a new event for the 'CAS' operation.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="value">Value</param>
        /// <param name="comparand">Comparand</param>
        /// <returns>SharedCounterEvent</returns>
        public static SharedCounterEvent CasEvent(MachineId sender, int value, int comparand)
        {
            return new SharedCounterEvent(SharedCounterOperation.CAS, value, comparand, sender);
        }

    }
}
