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
    /// Event storing a word
    /// </summary>
    [DataContract]
    class WordEvent : Event
    {
        /// <summary>
        ///  The word
        /// </summary>
        [DataMember]
        public string word;

        /// <summary>
        /// Timestamp at which the word occurred
        /// </summary>
        [DataMember]
        public int timestamp;

        public WordEvent(string word, int ts)
        {
            this.word = word;
            this.timestamp = ts;
        }
    }

    /// <summary>
    /// Initialization event for word-count machine
    /// </summary>
    [DataContract]
    class WordCountInitEvent : Event
    {
        [DataMember]
        public MachineId TargetMachine;

        public WordCountInitEvent(MachineId targetMachine)
        {
            this.TargetMachine = targetMachine;
        }
    }

    /// <summary>
    /// Event storing a word and its frequency
    /// </summary>
    [DataContract]
    class WordFreqEvent : Event
    {
        /// <summary>
        ///  The word
        /// </summary>
        [DataMember]
        public string word;

        /// <summary>
        /// Timestamp at which the word occurred
        /// </summary>
        [DataMember]
        public int timestamp;

        /// <summary>
        /// Word frequency
        /// </summary>
        [DataMember]
        public int freq;

        /// <summary>
        /// Sender
        /// </summary>
        [DataMember]
        public MachineId mid;


        public WordFreqEvent(string word, int ts, int freq, MachineId mid)
        {
            this.word = word;
            this.timestamp = ts;
            this.freq = freq;
            this.mid = mid;
        }
    }
}