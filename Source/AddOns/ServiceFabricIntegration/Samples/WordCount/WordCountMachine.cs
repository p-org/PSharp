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
    /// WordCount state machine
    /// </summary>
    class WordCountMachine : ReliableStateMachine
    {
        /// <summary>
        /// Word dictionary
        /// </summary>
        IReliableDictionary<string, int> WordFrequency;

        /// <summary>
        /// Latest timestamp seen so far 
        /// </summary>
        ReliableRegister<int> LatestTimeStamp;

        /// <summary>
        /// Highest frequency 
        /// </summary>
        ReliableRegister<int> HighestFrequency;

        /// <summary>
        /// Target machine
        /// </summary>
        ReliableRegister<MachineId> TargetMachine;

        /// <param name="stateManager"></param>
        public WordCountMachine(IReliableStateManager stateManager)
            : base(stateManager) { }

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventDoAction(typeof(WordEvent), nameof(IncludeWord))]
        class Init : MachineState { }

        /// <summary>
        /// Includes a new word
        /// </summary>
        /// <returns></returns>
        async Task IncludeWord()
        {
            // grab the word
            var word = (this.ReceivedEvent as WordEvent).word;
            var ts = (this.ReceivedEvent as WordEvent).timestamp;

            this.Logger.WriteLine("Machine {0}: Including word {1} at timestamp {2}", this.Id.Name, word, ts);

            // increment timestamp
            await LatestTimeStamp.Set(CurrentTransaction, ts);

            // increment frequency
            var freq = await WordFrequency.AddOrUpdateAsync(CurrentTransaction, word, 1, (k, v) => v + 1);

            if (freq > await HighestFrequency.Get(CurrentTransaction))
            {
                await HighestFrequency.Set(CurrentTransaction, freq);
                
                // report
                await this.ReliableSend(await TargetMachine.Get(CurrentTransaction), new WordFreqEvent(word, ts, freq, this.Id));
            }
        }

        /// <summary>
        /// Machine construction 
        /// </summary>
        /// <returns></returns>
        public async Task InitOnEntry()
        {
            // called after OnActivate
            var ev = (this.ReceivedEvent as WordCountInitEvent);
            await TargetMachine.Set(CurrentTransaction, ev.TargetMachine);
        }

        /// <summary>
        /// (Re-)Initialize
        /// </summary>
        /// <returns></returns>
        public override async Task OnActivate()
        {
            this.Logger.WriteLine("Machine {0}: starting", this.Id.Name);

            WordFrequency = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, int>>(QualifyWithMachineName("WordFrequency"));
            LatestTimeStamp = new ReliableRegister<int>(QualifyWithMachineName("LatestTimeStamp"), this.StateManager, 0);
            HighestFrequency = new ReliableRegister<int>(QualifyWithMachineName("HighestFrequency"), this.StateManager, 0);
            TargetMachine = new ReliableRegister<MachineId>(QualifyWithMachineName("TargetMachine"), this.StateManager, null);
        }

        private string QualifyWithMachineName(string name)
        {
            return name + "_" + this.Id.Name;
        }
    }

}