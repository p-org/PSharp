using Microsoft.PSharp;

namespace PingPong
{
    /// <summary>
    /// Asserts safety of an execution.
    /// </summary>
    class SafetyMonitor : Monitor
    {
        public class CheckReplyCount : Event
        {
            public int Count;

            public CheckReplyCount(int count)
            {
                this.Count = count;
            }
        }

        [Start]
        [OnEventDoAction(typeof(CheckReplyCount), nameof(CheckSafety))]
        class Init : MonitorState { }

        void CheckSafety()
        {
            int count = (this.ReceivedEvent as CheckReplyCount).Count;
            this.Assert(count <= 5, "Count should not be more than 5.");
        }
    }
}