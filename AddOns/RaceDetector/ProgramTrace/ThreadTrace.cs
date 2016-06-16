using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgramTrace
{
    [Serializable]
    public class ActionInstr
    {
        public bool isSend;
        public bool isCreate;
        public bool isTask;

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

        //task Id
        public int taskId;

        public ActionInstr(bool isWrite, UIntPtr location, UIntPtr objHandle, UIntPtr offset, string srcLocation)
        {
            this.isWrite = isWrite;
            this.location = location;
            this.objHandle = objHandle;
            this.offset = offset;
            this.isSend = false;
            this.srcLocation = srcLocation;
            this.isTask = false;
        }

        public ActionInstr(int sendID)
        {
            this.sendID = sendID;
            this.isSend = true;
            this.isTask = false;
        }

        public ActionInstr(int createMachineID, bool isCreate)
        {
            this.createMachineID = createMachineID;
            this.isCreate = isCreate;
            this.isSend = false;
            this.isTask = false;
        }

        public ActionInstr(bool isTask, int taskId)
        {
            this.isTask = isTask;
            this.taskId = taskId;
        }
    }

    [Serializable]
    public class ThreadTrace
    {
        public int machineID;
        public String actionName;
        public int actionID;
        public List<ActionInstr> accesses = new List<ActionInstr>();

        public string iteration = null;

        public bool isTask;
        public int taskId;

        public ThreadTrace(int machineID)
        {
            this.machineID = machineID;
            this.isTask = false;
            this.taskId = -1;
        }

        public void set(String actionName)
        {
            this.actionName = actionName;
        }

        public void set(int actionID)
        {
            this.actionID = actionID;
        }

        public ThreadTrace(int taskId, string actionName)
        {
            this.isTask = true;
            this.taskId = taskId;
        }
    }

}
