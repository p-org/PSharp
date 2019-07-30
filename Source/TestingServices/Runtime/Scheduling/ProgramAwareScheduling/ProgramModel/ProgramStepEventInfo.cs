// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel
{
    /// <summary>
    /// TODO: Eventually replace this with just a signature
    /// </summary>
    public class ProgramStepEventInfo
    {
        /// <summary>
        /// The event
        /// </summary>
        public Event Event;

        /// <summary>
        /// The sender
        /// </summary>
        public ulong SrcId;

        /// <summary>
        /// The step this was sent at
        /// </summary>
        public int SendStep;

        /// <summary>
        /// A hash representing the EventInfo.
        /// </summary>
        public int HashedState;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgramStepEventInfo"/> class.
        /// </summary>
        /// <param name="evt">The event</param>
        /// <param name="srcId">The Id of the sender</param>
        /// <param name="sendStep">The stepIndex at which this send was done</param>
        public ProgramStepEventInfo(Event evt, ulong srcId, int sendStep)
        {
            this.Event = evt;
            this.SrcId = srcId;
            this.SendStep = sendStep;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return this.Event.GetHashCode() + this.SendStep;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            ProgramStepEventInfo other = obj as ProgramStepEventInfo;
            if ( other != null )
            {
                return other.Event == this.Event && other.SendStep == this.SendStep;
            }
            else
            {
                return false;
            }
        }
    }
}
