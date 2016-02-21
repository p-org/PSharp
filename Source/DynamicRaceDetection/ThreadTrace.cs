//-----------------------------------------------------------------------
// <copyright file="ThreadTrace.cs">
//      Copyright (c) 2016 Microsoft Corporation. All rights reserved.
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
using System.Collections.Generic;

namespace Microsoft.PSharp.DynamicRaceDetection
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
