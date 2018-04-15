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
    /// Asserts safety of an execution
    /// </summary>
    class SafetyMonitor : Monitor
    {
        string word = null;
        int freq = 0;
        int timestamp = 0;

        [Start]
        [OnEventDoAction(typeof(WordFreqEvent), nameof(CheckSafety))]
        class Init : MonitorState { }

        void CheckSafety()
        {
            var ev = (this.ReceivedEvent as WordFreqEvent);

            // either same (duplication) or monotonically increasing
            if (word != null && !(ev.freq == freq && ev.word == word && ev.timestamp == timestamp))
            {
                this.Assert(ev.freq > freq, "Frequencies must be monotonically increasing");
                //this.Assert(ev.timestamp > timestamp, "Timestamps must be monotonically increasing");
            }

            word = ev.word;
            freq = ev.freq;
            timestamp = ev.timestamp;
        }

    }
}