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
    class Program
    {
        static void Main(string[] args)
        {
            //System.Diagnostics.Debugger.Launch();

            var config = Configuration.Create(); //.WithVerbosityEnabled(2);
            var runtime = PSharpRuntime.Create(config);
            var stateManager = new StateManagerMock(runtime);
            runtime.AddMachineFactory(new ReliableStateMachineFactory(stateManager));
            var mid = runtime.CreateMachine(typeof(WordCount));
            
            var rand = new Random();
            var cnt = 100;
            while(cnt > 0)
            {
                cnt--;
                var next = rand.Next('a', 'z' + 1);
                ReliableStateMachine.ReliableSend(stateManager, mid, new WordEvent(((char)next).ToString())).Wait();
            }
            Console.ReadLine();
        }
    }

    /// <summary>
    /// Event storing a word
    /// </summary>
    [DataContract]
    class WordEvent : Event
    {
        public string word;

        public WordEvent(string word)
        {
            this.word = word;
        }
    }

    /// <summary>
    /// WordCount state machine
    /// </summary>
    class WordCount : ReliableStateMachine
    {
        /// <summary>
        /// Word dictionary
        /// </summary>
        IReliableDictionary<string, int> WordFrequency;

        /// <summary>
        /// Number of words seen so far (stored at index 0)
        /// </summary>
        IReliableDictionary<int, int> NumWords;

        /// <summary>
        /// Number of words seen so far (cache)
        /// </summary>
        int NumWordsCache;

        /// <param name="stateManager"></param>
        public WordCount(IReliableStateManager stateManager)
            : base(stateManager) { }

        [Start]
        [OnEventDoAction(typeof(WordEvent), nameof(IncludeWord))]
        class Init : MachineState { }

        async Task IncludeWord()
        {
            // grab the word
            var word = (this.ReceivedEvent as WordEvent).word;
            this.Logger.WriteLine("Including word: {0}", word);

            // increment frequency
            await WordFrequency.AddOrUpdateAsync(CurrentTransaction, word, 1, (k, v) => v + 1);

            // increment word count
            await NumWords.AddOrUpdateAsync(CurrentTransaction, 0, 1, (k, v) => v + 1);
        }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <returns></returns>
        public override async Task OnActivate()
        {
            this.Logger.WriteLine("Starting machine {0}", this.Id.Name);

            WordFrequency = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, int>>("WordFrequency");
            NumWords = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, int>>("NumWords");
            NumWordsCache = await NumWords.GetOrAddAsync(CurrentTransaction, 0, 0);
        }
    }



}
