//-----------------------------------------------------------------------
// <copyright file="EventInfo.cs">
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
using System.Runtime.Serialization;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Class that contains a P# event, and its
    /// associated information.
    /// </summary>
    [DataContract]
    internal class EventInfo
    {
        /// <summary>
        /// Contained event.
        /// </summary>
        internal Event Event { get; private set; }

        /// <summary>
        /// Event type.
        /// </summary>
        internal Type EventType { get; private set; }

        /// <summary>
        /// Event name.
        /// </summary>
        [DataMember]
        internal string EventName { get; private set; }

        /// <summary>
        /// The step from which this event was sent.
        /// </summary>
        internal int SendStep { get; }

        /// <summary>
        /// Information regarding the event origin.
        /// </summary>
        [DataMember]
        internal EventOriginInfo OriginInfo { get; private set; }

        /// <summary>
        /// The operation group id associated with this event.
        /// </summary>
        internal Guid OperationGroupId { get; private set; }

        /// <summary>
        /// Creates a new <see cref="EventInfo"/>.
        /// </summary>
        /// <param name="e">Event</param>
        internal EventInfo(Event e)
        {
            Event = e;
            EventType = e.GetType();
            EventName = EventType.FullName;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="e">Event</param>
        /// <param name="originInfo">EventOriginInfo</param>
        internal EventInfo(Event e, EventOriginInfo originInfo) : this(e)
        {
            OriginInfo = originInfo;
        }

        /// <summary>
        /// Sets the operation group id associated with this event.
        /// </summary>
        /// <param name="operationGroupId">Operation group id.</param>
        internal void SetOperationGroupId(Guid operationGroupId)
        {
            this.OperationGroupId = operationGroupId;
        }

        /// <summary>
        /// Construtor.
        /// </summary>
        /// <param name="e">Event</param>
        /// <param name="originInfo">EventOriginInfo</param>
        /// <param name="sendStep">int</param>
        internal EventInfo(Event e, EventOriginInfo originInfo, int sendStep)
            : this(e, originInfo)
        {
            SendStep = sendStep;
        }
    }
}
