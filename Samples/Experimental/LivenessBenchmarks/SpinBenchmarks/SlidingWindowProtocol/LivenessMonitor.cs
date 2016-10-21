using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlidingWindowProtocol
{
    class LivenessMonitor : Monitor
    {
        #region events
        public class NotifyMessageSent : Event
        {
            public int FrameSent;

            public NotifyMessageSent(int frameSent)
            {
                this.FrameSent = frameSent;
            }
        }
        public class NotifyMessageReceived : Event
        {
            public int FrameReceived;

            public NotifyMessageReceived(int frameReceived)
            {
                this.FrameReceived = frameReceived;
            }
        }
        public class Local : Event { }
        #endregion

        #region fields
        List<int> SentMesages;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Local), typeof(MessageReceived))]
        class Init : MonitorState { }

        [Cold]
        [OnEventDoAction(typeof(NotifyMessageSent), nameof(OnMessageSent))]
        [OnEventGotoState(typeof(NotifyMessageReceived), typeof(MessageReceived))]
        [OnEventGotoState(typeof(Local), typeof(MessageSent))]
        class MessageReceived : MonitorState { }

        [Hot]
        [OnEventDoAction(typeof(NotifyMessageSent), nameof(OnMessageSent))]
        [OnEventDoAction(typeof(NotifyMessageReceived), nameof(OnMessageReceived))]
        [OnEventGotoState(typeof(Local), typeof(MessageReceived))]
        class MessageSent : MonitorState { }
        #endregion

        #region actions
        void InitOnEntry()
        {
            SentMesages = new List<int>();
            Raise(new Local());
        }

        void OnMessageSent()
        {
            SentMesages.Add((ReceivedEvent as NotifyMessageSent).FrameSent);
            Raise(new Local());
        }

        void OnMessageReceived()
        {
            SentMesages.Remove((ReceivedEvent as NotifyMessageReceived).FrameReceived);
            if(SentMesages.Count == 0)
            {
                Raise(new Local());
            }
        }
        #endregion
    }
}
