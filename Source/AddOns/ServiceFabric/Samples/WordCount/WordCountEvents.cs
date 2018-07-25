using System.Runtime.Serialization;
using Microsoft.PSharp;

namespace WordCount
{
    class WordEvent : Event
    {
        /// <summary>
        ///  The word
        /// </summary>
        public string word;

        /// <summary>
        /// Timestamp at which the word occurred
        /// </summary>
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
    /// Event storing a word and its frequency.
    /// </summary>
    public class WordFreqEvent : Event
    {
        /// <summary>
        ///  The word
        /// </summary>
        public string Word;

        /// <summary>
        /// Timestamp at which the word occurred.
        /// </summary>
        public int Timestamp;

        /// <summary>
        /// Word frequency.
        /// </summary>
        public int Freq;

        /// <summary>
        /// Sender.
        /// </summary>
        public MachineId Mid;
        
        public WordFreqEvent(string word, int ts, int freq, MachineId mid)
        {
            this.Word = word;
            this.Timestamp = ts;
            this.Freq = freq;
            this.Mid = mid;
        }
    }
}