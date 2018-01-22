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
        /// Specifies that there must not be more than k instances
        /// of e in the input queue of any machine.
        /// </summary>
        protected internal int Assert { get; private set; }

        /// <summary>
        /// Speciﬁes that during testing, an execution that increases
        /// the cardinality of e beyond k in some queue must not be
        /// generated.
        /// </summary>
        protected internal int Assume { get; private set; }

        /// <summary> 
        /// User-defined hash of the event payload. Override to improve the
        /// accuracy of liveness checking when state-caching is enabled.
        /// </summary> 
        public virtual int HashedState => 0;

        #endregion

        #region constructors

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
            this.SetCardinalityConstraints(assert, assume);
        }

        /// <summary>
        /// Allows override of constructor cardinality constraints.
        /// </summary>
        /// <param name="assert">Assert</param>
        /// <param name="assume">Assume</param>
        protected void SetCardinalityConstraints(int assert, int assume)
        {
            this.Assert = assert;
            this.Assume = assume;
        }

        #endregion
    }
}
