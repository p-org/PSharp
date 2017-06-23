//-----------------------------------------------------------------------
// <copyright file="CActBegin.cs">
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

using System.Collections.Generic;

namespace Microsoft.PSharp.DynamicRaceDetection
{
    internal class CActBegin : Node
    {
        public string ActionName;
        public int ActionId = -1;
        public string EventName;
        public int EventId;

        public int TaskId = -1;
        public bool IsTask;

        public List<MemAccess> Addresses;

        public bool IsStart = false;

        /// <summary>
        /// Constructor.
        /// </summary>
        public CActBegin(int machineId, string actionName, int actionId, string eventName, int eventID)
        {
            this.MachineId = machineId;
            this.ActionName = actionName;
            this.ActionId = actionId;
            this.EventName = eventName;
            this.EventId = eventID;
            this.IsTask = false;

            this.Addresses = new List<MemAccess>();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public CActBegin(int machineId, int taskId)
        {
            this.MachineId = machineId;
            this.TaskId = taskId;
            this.IsTask = true;

            this.Addresses = new List<MemAccess>();
        }
    }
}
