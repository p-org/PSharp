using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlidingWindowProtocol
{
    public enum mtype
    {
        red,
        white,
        blue
    }

    class SourceMachine : Machine
    {
        #region events
        public class Initialize : Event
        {
            public MachineId Target;

            public Initialize(MachineId target)
            {
                this.Target = target;
            }
        }
        public class StartSending : Event { }
        public class ColoredMessage : Event
        {
            public mtype ColoredMsg;

            public ColoredMessage(mtype coloredMsg)
            {
                this.ColoredMsg = coloredMsg;
            }
        }
        #endregion

        #region fields
        private MachineId TargetMachine;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInitEntry))]
        [OnEventDoAction(typeof(StartSending), nameof(OnStartSending))]
        class Init: MachineState { }
        #endregion

        #region actions
        void OnInitEntry()
        {
            this.TargetMachine = (ReceivedEvent as Initialize).Target;
            Raise(new StartSending());
        }

        void OnStartSending()
        {
            while (true)
            {
                if (Random())
                {
                    Send(TargetMachine, new ColoredMessage(mtype.white));
                }
                else
                {
                    Send(TargetMachine, new ColoredMessage(mtype.red));
                    break;
                }
            }
            while (true)
            {
                if (Random())
                {
                    Send(TargetMachine, new ColoredMessage(mtype.white));
                }
                else
                {
                    Send(TargetMachine, new ColoredMessage(mtype.blue));
                    break;
                }
            }
            while (true)
            {
                Send(TargetMachine, new ColoredMessage(mtype.white));
            }
        }
        #endregion
    }
}
