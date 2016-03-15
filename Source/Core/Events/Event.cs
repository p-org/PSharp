//-----------------------------------------------------------------------
// <copyright file="Event.cs">
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

using System.Runtime.Serialization;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Abstract class representing an event.
    /// </summary>
    [DataContract]
    public abstract class Event
    {
        #region fields

        /// <summary>
        /// The machine id of the sender of this event.
        /// </summary>
        internal MachineId Sender { get; private set; }

        /// <summary>
        /// The operation id of this event.
        /// </summary>
        internal int OperationId { get; private set; }

        /// <summary>
        /// Specifies that there must not be more than k instances
        /// of e in the input queue of any machine.
        /// </summary>
        [DataMember]
        protected internal readonly int Assert;

        /// <summary>
        /// Speciﬁes that during testing, an execution that increases
        /// the cardinality of e beyond k in some queue must not be
        /// generated.
        /// </summary>
        [DataMember]
        protected internal readonly int Assume;

        #endregion

        #region protected methods

        /// <summary>
        /// Constructor.
        /// </summary>
        protected Event()
        {
            this.Assert = -1;
            this.Assume = -1;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="assert">Assert</param>
        /// <param name="assume">Assume</param>
        protected Event(int assert, int assume)
        {
            this.Assert = assert;
            this.Assume = assume;
        }

        #endregion

        #region internal methods

        /// <summary>
        /// Sets the machine id of the sender of this event.
        /// </summary>
        /// <param name="mid">MachineId</param>
        internal void SetSenderMachine(MachineId mid)
        {
            this.Sender = mid;
        }

        /// <summary>
        /// Sets the operation id of this event.
        /// </summary>
        /// <param name="opid">OperationId</param>
        internal void SetOperationId(int opid)
        {
            this.OperationId = opid;
        }

        #endregion
    }
}
