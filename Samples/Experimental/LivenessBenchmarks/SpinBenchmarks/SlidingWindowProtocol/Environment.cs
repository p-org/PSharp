using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlidingWindowProtocol
{
    class Environment : Machine
    {
        #region states
        [Start]
        [OnEntry(nameof(OnInitEntry))]
        class init : MachineState { }
        #endregion

        #region actions
        void OnInitEntry()
        {
            var channel1 = CreateMachine(typeof(ChannelMachine));
            var channel2 = CreateMachine(typeof(ChannelMachine));

            var machine1 = CreateMachine(typeof(P5));
            var machine2 = CreateMachine(typeof(P5));

            Send(machine1, new P5.SetInputOutput(channel2, channel1));
            Send(machine2, new P5.SetInputOutput(channel1, channel2));

            var sourceMachine = CreateMachine(typeof(SourceMachine), new SourceMachine.Initialize(machine1));
        }
        #endregion
    }
}
