using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlidingWindowProtocol
{
    class ChannelMachine : Machine
    {
        #region events
        public class GetMessageReq : Event
        {
            public MachineId Target;

            public GetMessageReq(MachineId target)
            {
                this.Target = target;
            }
        }
        public class GetMessageResp : Event
        {
            public P5.Message Value;

            public GetMessageResp(P5.Message value)
            {
                this.Value = value;
            }
        }
        #endregion

        #region fields
        List<P5.Message> MessageQueue;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInitEntry))]
        [OnEventDoAction(typeof(GetMessageReq), nameof(OnGetMessageReq))]
        [OnEventDoAction(typeof(P5.Message), nameof(OnMessage))]
        class Init : MachineState { }
        #endregion

        #region actions
        void OnInitEntry()
        {
            MessageQueue = new List<P5.Message>();
        }
        void OnGetMessageReq()
        {
            P5.Message returnMsg;
            if (MessageQueue.Count == 0)
            {
                returnMsg = null;
            }
            else
            {
                returnMsg = MessageQueue[0];
                MessageQueue.RemoveAt(0);
            }
            Send((ReceivedEvent as GetMessageReq).Target, new GetMessageResp(returnMsg));
        }

        void OnMessage()
        {
            MessageQueue.Add(ReceivedEvent as P5.Message);
        }
        #endregion
    }
}
