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

namespace Microsoft.PSharp.Monitoring
{
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
