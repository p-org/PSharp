using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ReliableServices;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace WordCount
{
    /// <summary>
    /// GatherResultsMachine
    /// </summary>
    class SimpleGatherResultsMachine : ReliableStateMachine
    {
        /// <summary>
        /// Highest frequency 
        /// </summary>
        ReliableRegister<int> HighestFrequency;

        /// <param name="stateManager"></param>
        public SimpleGatherResultsMachine(IReliableStateManager stateManager)
            : base(stateManager) { }

        [Start]
        [OnEventDoAction(typeof(WordFreqEvent), nameof(Update))]
        class Init : MachineState { }

        async Task Update()
        {
            var ev = (this.ReceivedEvent as WordFreqEvent);
            this.Logger.WriteLine("Got: word = {0}, freq = {1}, ts = {2}", ev.word, ev.freq, ev.timestamp);

            this.Assert(ev.freq > await HighestFrequency.Get(CurrentTransaction), "Frequency must be monotonically increasing");
            this.Logger.WriteLine("Highest Freq word = {0}, with freq {1}", ev.word, ev.freq);
            await HighestFrequency.Set(CurrentTransaction, ev.freq);
        }

        public override Task OnActivate()
        {
            HighestFrequency = new ReliableRegister<int>(QualifyWithMachineName("HighestFrequency"), this.StateManager, 0);
            return Task.CompletedTask;
        }

        private string QualifyWithMachineName(string name)
        {
            return name + "_" + this.Id.Name;
        }
    }
}
