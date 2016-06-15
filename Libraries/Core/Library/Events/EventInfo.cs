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

namespace Microsoft.PSharp
{
    /// <summary>
    /// Class that contains a P# event, and its
    /// associated information.
    /// </summary>
    internal class EventInfo
    {
        #region fields

        /// <summary>
        /// Contained event.
        /// </summary>
        internal Event Event { get; private set; }

        /// <summary>
        /// Event type.
        /// </summary>
        internal Type Type { get; private set; }

        /// <summary>
        /// Event name.
        /// </summary>
        internal string Name { get; private set; }

        /// <summary>
        /// Information regarding the event origin.
        /// </summary>
        internal EventOriginInfo OriginInfo { get; private set; }

        /// <summary>
        /// The operation id of the event.
        /// </summary>
        internal int OperationId { get; private set; }

        #endregion

        #region constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="e">Event</param>
        internal EventInfo(Event e)
        {
            this.Event = e;
            this.Type = e.GetType();
            this.Name = this.Type.FullName;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="e">Event</param>
        /// <param name="originInfo">EventOriginInfo</param>
        internal EventInfo(Event e, EventOriginInfo originInfo)
        {
            this.Event = e;
            this.Type = e.GetType();
            this.OriginInfo = originInfo;
        }

        #endregion

        #region methods

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
