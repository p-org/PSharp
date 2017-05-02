//-----------------------------------------------------------------------
// <copyright file="SendEvent.cs">
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

namespace Microsoft.PSharp.DynamicRaceDetection
{
    internal class SendEvent : Node
    {
        public int SendId;
        public int ToMachine;
        public string SendEventName;
        public int SendEventId;

        /// <summary>
        /// Constructor.
        /// </summary>
        public SendEvent(int machineId, int sendId, int toMachine,
            string sendEventName, int sendEventID)
        {
            this.MachineId = machineId;
            this.SendId = sendId;
            this.ToMachine = toMachine;
            this.SendEventName = sendEventName;
            this.SendEventId = sendEventID;
        }
    }
}
