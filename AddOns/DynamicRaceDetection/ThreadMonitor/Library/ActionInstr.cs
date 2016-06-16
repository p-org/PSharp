//-----------------------------------------------------------------------
// <copyright file="ActionInstr.cs">
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

namespace Microsoft.PSharp.Monitoring
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
}
