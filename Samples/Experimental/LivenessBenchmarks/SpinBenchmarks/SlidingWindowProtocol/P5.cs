using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlidingWindowProtocol
{

    public static class Globals
    {
        public static int MaxSeq = 5;
    }
    class P5 : Machine
    {
        #region events
        public class SetInputOutput : Event
        {
            public MachineId InputMachineId;
            public MachineId OutputMachineId;

            public SetInputOutput(MachineId inputMachineId, MachineId outputMachineId)
            {
                this.InputMachineId = inputMachineId;
                this.OutputMachineId = outputMachineId;
            }
        }
        public class Message : Event
        {
            public int NextFrame;
            public int FrameExp;

            public Message(int nextFrame, int frameExp)
            {
                this.NextFrame = nextFrame;
                this.FrameExp = frameExp;
            }
        }
        public class ColoredMessage : Event
        {
            public mtype ColoredMsg;

            public ColoredMessage(mtype coloredMsg)
            {
                this.ColoredMsg = coloredMsg;
            }
        }
        public class StartSliding : Event { }
        #endregion

        #region fields
        private int NextFrame;
        private int AckExp;
        private int FrameExp;
        private int nbuf, i;

        private bool timeout;

        private MachineId OutputMachineId;
        private MachineId InputMachineId;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInitEntry))]
        [OnEventDoAction(typeof(SetInputOutput), nameof(OnSetInputOutput))]
        [OnEventGotoState(typeof(StartSliding), typeof(SlidingWindow))]
        [OnEventDoAction(typeof(SourceMachine.ColoredMessage), nameof(OnColoredMsgFromSrc))]
        [OnEventDoAction(typeof(ColoredMessage), nameof(OnColoredMsgFrmProcess))]
        class Init : MachineState { }

        [OnEntry(nameof(OnSlideWindow))]
        class SlidingWindow : MachineState { }
        #endregion

        #region actions
        void OnInitEntry()
        {
            NextFrame = 0;
            AckExp = 0;
            FrameExp = 0;
            nbuf = 0; i = 0;

            timeout = false;
        }

        void OnSlideWindow()
        {
            while (true)
            {
                if(nbuf < Globals.MaxSeq)
                {
                    nbuf++;
                    Send(OutputMachineId, new Message(NextFrame, (FrameExp + Globals.MaxSeq)%(Globals.MaxSeq + 1)));
                    NextFrame = (NextFrame + 1) % (Globals.MaxSeq + 1);
                }
                else
                {
                    Send(InputMachineId, new ChannelMachine.GetMessageReq(this.Id));
                    var receivedEvent = Receive(typeof(ChannelMachine.GetMessageResp));
                    var e = (receivedEvent as ChannelMachine.GetMessageResp);
                    if (e.Value != null)
                    {
                        var r = e.Value.NextFrame;
                        var s = e.Value.FrameExp;
                        if(r == this.FrameExp)
                        {
                            Console.WriteLine("[MSG] accept: " + r);
                            FrameExp = (FrameExp + 1) % (Globals.MaxSeq + 1);
                        }
                        else
                        {

                        }
                        while (true)
                        {
                            if(((AckExp <= s) && (s < NextFrame))
                                || ((AckExp <= s) && (NextFrame < AckExp))
                                || ((s < NextFrame) && (NextFrame < AckExp)))
                            {
                                nbuf--;
                                AckExp = (AckExp + 1) % (Globals.MaxSeq + 1);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    else if(timeout)
                    {
                        NextFrame = AckExp;
                        Console.WriteLine("[MSG] Timeout");
                        i = 1;
                        while (true)
                        {
                            if(i <= nbuf)
                            {
                                Send(OutputMachineId, new Message(NextFrame, (FrameExp + Globals.MaxSeq)%(Globals.MaxSeq + 1)));
                                NextFrame = (NextFrame + 1) % (Globals.MaxSeq + 1);
                                i++;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

        void OnSetInputOutput()
        {
            var e = ReceivedEvent as SetInputOutput;
            this.InputMachineId = e.InputMachineId;
            this.OutputMachineId = e.OutputMachineId;
            Raise(new StartSliding());
        }

        void OnColoredMsgFromSrc()
        {
            var e = ReceivedEvent as SourceMachine.ColoredMessage;
            Send(OutputMachineId, new ColoredMessage(e.ColoredMsg));
            if(e.ColoredMsg == mtype.red)
            {
                this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyRedMessageSent());
            }
        }

        void OnColoredMsgFrmProcess()
        {
            if ((ReceivedEvent as ColoredMessage).ColoredMsg == mtype.red)
            {
                this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyRedMessageReceived());
            }
        }
        #endregion
    }
}
