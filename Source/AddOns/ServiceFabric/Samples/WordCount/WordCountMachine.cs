﻿using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ServiceFabric;
using Microsoft.PSharp.ServiceFabric.Utilities;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace WordCount
{
    /// <summary>
    /// WordCount state machine.
    /// </summary>
    class WordCountMachine : ReliableMachine
    {
        public WordCountMachine(IReliableStateManager stateManager)
            : base(stateManager)
        { }

        /// <summary>
        /// Word dictionary.
        /// </summary>
        IReliableDictionary<string, int> WordFrequency;

        /// <summary>
        /// Latest timestamp seen so far.
        /// </summary>
        ReliableRegister<int> LatestTimeStamp;

        /// <summary>
        /// Highest frequency.
        /// </summary>
        ReliableRegister<int> HighestFrequency;

        /// <summary>
        /// Target machine.
        /// </summary>
        ReliableRegister<MachineId> TargetMachine;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventDoAction(typeof(WordEvent), nameof(IncludeWord))]
        class Init : MachineState { }

        /// <summary>
        /// Includes a new word.
        /// </summary>
        async Task IncludeWord()
        {
            // grab the word
            var word = (this.ReceivedEvent as WordEvent).word;
            var ts = (this.ReceivedEvent as WordEvent).timestamp;

            this.Logger.WriteLine("Machine {0}: Including word {1} at timestamp {2}", this.Id.Name, word, ts);

            // increment timestamp
            await LatestTimeStamp.Set(ts);

            // increment frequency
            var freq = await WordFrequency.AddOrUpdateAsync(this.CurrentTransaction, word, 1, (k, v) => v + 1);

            if (freq > await HighestFrequency.Get())
            {
                await HighestFrequency.Set(freq);

                // report
                Send(await TargetMachine.Get(), new WordFreqEvent(word, ts, freq, this.Id));
            }
        }

        /// <summary>
        /// Machine construction.
        /// </summary>
        public async Task InitOnEntry()
        {
            // called after OnActivate
            var ev = (this.ReceivedEvent as WordCountInitEvent);
            await TargetMachine.Set(ev.TargetMachine);
        }

        /// <summary>
        /// (Re-)Initialize.
        /// </summary>
        protected override async Task OnActivate()
        {
            this.Logger.WriteLine("Machine {0}: starting", this.Id.Name);

            WordFrequency = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, int>>("WordFrequency" + this.Id);
            LatestTimeStamp = this.GetOrAddRegister<int>("LatestTimeStamp", 0);
            HighestFrequency = this.GetOrAddRegister<int>("HighestFrequency", 0);
            TargetMachine = this.GetOrAddRegister<MachineId>("TargetMachine", null);
        }
    }
}