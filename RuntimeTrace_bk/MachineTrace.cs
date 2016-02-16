using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuntimeTrace
{
    [Serializable]
    public class MachineTrace
    {
        public int machineID;

        public bool isSend = false;

        //begin action fields
        public String actionName;
        public int actionID;
        public String eventName;
        public int eventID;

        //send event fields
        public int sendID;
        public int toMachine;
        public String sendEventName;
        public int sendEventID;

        //action begin
        public MachineTrace(int machineID, String actionName, int actionID, String eventName, int eventID)
        {
            this.machineID = machineID;
            this.actionName = actionName;
            this.actionID = actionID;
            this.eventName = eventName;
            this.eventID = eventID;
            this.isSend = false;
        }

        //send event
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
