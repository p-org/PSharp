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
        #region fields

        /// <summary>
        /// Is a send.
        /// </summary>
        public bool IsSend;

        /// <summary>
        /// Is a create.
        /// </summary>
        public bool IsCreate;

        /// <summary>
        /// Is a task.
        /// </summary>
        public bool IsTask;

        /// <summary>
        /// Is a write memory access.
        /// </summary>
        public bool IsWrite;

        /// <summary>
        /// The location.
        /// </summary>
        public UIntPtr Location;

        /// <summary>
        /// The object handle.
        /// </summary>
        public UIntPtr ObjHandle;

        /// <summary>
        /// The offset.
        /// </summary>
        public UIntPtr Offset;

        /// <summary>
        /// The source location.
        /// </summary>
        public string SrcLocation;

        /// <summary>
        /// The send id.
        /// </summary>
        public int SendId;

        /// <summary>
        /// Create machine id.
        /// </summary>
        public int CreateMachineId;

        /// <summary>
        /// The task id.
        /// </summary>
        public int TaskId;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        public ActionInstr(bool isWrite, UIntPtr location, UIntPtr objHandle,
            UIntPtr offset, string srcLocation)
        {
            this.IsWrite = isWrite;
            this.Location = location;
            this.ObjHandle = objHandle;
            this.Offset = offset;
            this.IsSend = false;
            this.SrcLocation = srcLocation;
            this.IsTask = false;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ActionInstr(int sendId)
        {
            this.SendId = sendId;
            this.IsSend = true;
            this.IsTask = false;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ActionInstr(int createMachineId, bool isCreate)
        {
            this.CreateMachineId = createMachineId;
            this.IsCreate = isCreate;
            this.IsSend = false;
            this.IsTask = false;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ActionInstr(bool isTask, int taskId)
        {
            this.IsTask = isTask;
            this.TaskId = taskId;
        }

        #endregion
    }
}
