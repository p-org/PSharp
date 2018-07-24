using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ServiceFabric;
using Microsoft.PSharp.ServiceFabric.Utilities;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace WordCount
{
    /// <summary>
    /// GatherResultsMachine
    /// </summary>
    class SimpleGatherResultsMachine : ReliableMachine
    {
        public SimpleGatherResultsMachine(IReliableStateManager stateManager)
            : base(stateManager)
        { }

        /// <summary>
        /// Highest frequency 
        /// </summary>
        ReliableRegister<int> HighestFrequency;

        [Start]
        [OnEventDoAction(typeof(WordFreqEvent), nameof(Update))]
        class Init : MachineState { }

        async Task Update()
        {
            var ev = (this.ReceivedEvent as WordFreqEvent);

            if (ev.freq > await HighestFrequency.Get())
            {
                this.Logger.WriteLine("Highest Freq word = {0}, with freq {1}", ev.word, ev.freq);
                this.Monitor<SafetyMonitor>(ev); // assert safety
                await HighestFrequency.Set(ev.freq);
            }
        }

        protected override Task OnActivate()
        {
            HighestFrequency = this.GetOrAddRegister<int>("HighestFrequency", 0);
            return Task.CompletedTask;
        }

    }
}