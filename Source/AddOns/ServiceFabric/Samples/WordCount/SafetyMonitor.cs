using Microsoft.PSharp;

namespace WordCount
{
    /// <summary>
    /// Asserts safety of an execution.
    /// </summary>
    class SafetyMonitor : Monitor
    {
        string Word = null;
        int Freq = 0;
        int Timestamp = 0;

        [Start]
        [OnEventDoAction(typeof(WordFreqEvent), nameof(CheckSafety))]
        class Init : MonitorState { }

        void CheckSafety()
        {
            var ev = (this.ReceivedEvent as WordFreqEvent);

            // either same (duplication) or monotonically increasing
            if (Word != null && !(ev.Freq == Freq && ev.Word == Word && ev.Timestamp == Timestamp))
            {
                this.Assert(ev.Freq > Freq, "Frequencies must be monotonically increasing");
                //this.Assert(ev.timestamp > timestamp, "Timestamps must be monotonically increasing");
            }

            Word = ev.Word;
            Freq = ev.Freq;
            Timestamp = ev.Timestamp;
        }
    }
}