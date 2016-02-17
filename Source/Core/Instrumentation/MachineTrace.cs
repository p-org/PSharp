//-----------------------------------------------------------------------
// <copyright file="MachineTrace.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
//      EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
//      OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
//      The example companies, organizations, products, domain names,
//      e-mail addresses, logos, people, places, and events depicted
//      herein are fictitious.  No association with any real company,
//      organization, product, domain name, email address, logo, person,
//      places, or events is intended or should be inferred.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp
{
    [Serializable]
    public class MachineTrace
    {
        public int machineID;

        public bool isSend = false;

        /// <summary>
        /// Start action fields.
        /// </summary>
        public String actionName;
        public int actionID;
        public String eventName;
        public int eventID;

        /// <summary>
        /// Send event fields.
        /// </summary>
        public int sendID;
        public int toMachine;
        public String sendEventName;
        public int sendEventID;

        /// <summary>
        /// Trace when starting an action.
        /// </summary>
        /// <param name="machineID"></param>
        /// <param name="actionName"></param>
        /// <param name="actionID"></param>
        /// <param name="eventName"></param>
        /// <param name="eventID"></param>
        public MachineTrace(int machineID, String actionName, int actionID, String eventName, int eventID)
        {
            this.machineID = machineID;
            this.actionName = actionName;
            this.actionID = actionID;
            this.eventName = eventName;
            this.eventID = eventID;
            this.isSend = false;
        }

        /// <summary>
        /// Trace when sending an event.
        /// </summary>
        /// <param name="machineID"></param>
        /// <param name="sendID"></param>
        /// <param name="toMachine"></param>
        /// <param name="sendEventName"></param>
        /// <param name="sendEventID"></param>
        public MachineTrace(int machineID, int sendID, int toMachine, String sendEventName, int sendEventID)
        {
            this.machineID = machineID;
            this.sendID = sendID;
            this.toMachine = toMachine;
            this.sendEventName = sendEventName;
            this.sendEventID = sendEventID;
            this.isSend = true;
        }
    }

    /*[Serializable]
    public class ActionBegin : MachineTrace
    {
        public String actionName;
        public int actionID;
        public String eventName;
        public int eventID;

        public ActionBegin(int machineID, String actionName, int actionID, String eventName, int eventID)
        {
            this.machineID = machineID;
            this.actionName = actionName;
            this.actionID = actionID;
            this.eventName = eventName;
            this.eventID = eventID;
        }
    }

    [Serializable]
    public class SendEvent : MachineTrace
    {
        public int sendID;
        public int toMachine;
        public String sendEventName;
        public int sendEventID;

        public SendEvent(int machineID, int sendID, int toMachine, String sendEventName, int sendEventID)
        {
            this.machineID = machineID;
            this.sendID = sendID;
            this.toMachine = toMachine;
            this.sendEventName = sendEventName;
            this.sendEventID = sendEventID;
        }
    }*/
}
