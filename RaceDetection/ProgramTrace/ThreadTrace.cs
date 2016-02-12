using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ProgramTrace
{
    [Serializable]
    public class ActionInstr
    {
        public bool isSend;
        public bool isCreate;

        //memory access fields
        public bool isWrite;
        public UIntPtr location;
        public UIntPtr objHandle;
        public UIntPtr offset;
        public string srcLocation;

        //send event fields
        public int sendID;

        //create machine fields
        public int createMachineID;

        public ActionInstr(bool isWrite, UIntPtr location, UIntPtr objHandle, UIntPtr offset, string srcLocation)
        {
            this.isWrite = isWrite;
            this.location = location;
            this.objHandle = objHandle;
            this.offset = offset;
            this.isSend = false;
            this.srcLocation = srcLocation;
        }

        public ActionInstr(int sendID)
        {
            this.sendID = sendID;
            this.isSend = true;
        }

        public ActionInstr(int createMachineID, bool isCreate)
        {
            this.createMachineID = createMachineID;
            this.isCreate = isCreate;
            this.isSend = false;
        }
    }

    [Serializable]
    public class ThreadTrace
    {
        public int machineID;
        public String actionName;
        public int actionID;
        public List<ActionInstr> accesses = new List<ActionInstr>();

        public ThreadTrace(int machineID)
        {
            this.machineID = machineID;
        }

        public void set(String actionName)
        {
            this.actionName = actionName;
        }

        public void set(int actionID)
        {
            this.actionID = actionID;
        }
    }
}
