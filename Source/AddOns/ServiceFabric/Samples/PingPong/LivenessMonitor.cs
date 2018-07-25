using Microsoft.PSharp;

namespace PingPong
{
    /// <summary>
    /// Asserts liveness of an execution.
    /// </summary>
    class LivenessMonitor : Monitor
    {
        public class CheckPingEvent : Event { }
        public class CheckPongEvent : Event { }

        int PingCount;
        int PongCount;

        [Start]
        [OnEntry(nameof(Initialize))]
        class Init : MonitorState { }

        void Initialize()
        {
            this.PingCount = 0;
            this.PongCount = 0;
            this.Goto<Balanced>();
        }

        [Cold]
        [OnEventDoAction(typeof(CheckPingEvent), nameof(CheckForUnbalancedCount))]
        [OnEventDoAction(typeof(CheckPongEvent), nameof(CheckForUnbalancedCount))]
        class Balanced : MonitorState { }

        [Hot]
        [OnEventDoAction(typeof(CheckPingEvent), nameof(CheckForBalancedCount))]
        [OnEventDoAction(typeof(CheckPongEvent), nameof(CheckForBalancedCount))]
        class Unbalanced : MonitorState { }

        void CheckForUnbalancedCount()
        {
            if (this.ReceivedEvent is CheckPingEvent)
            {
                this.PingCount++;
            }
            else if (this.ReceivedEvent is CheckPongEvent)
            {
                this.PongCount++;
            }

            if (this.PingCount != this.PongCount)
            {
                this.Goto<Unbalanced>();
            }
        }

        void CheckForBalancedCount()
        {
            if (this.ReceivedEvent is CheckPingEvent)
            {
                this.PingCount++;
            }
            else if (this.ReceivedEvent is CheckPongEvent)
            {
                this.PongCount++;
            }

            if (this.PingCount == this.PongCount)
            {
                this.Goto<Balanced>();
            }
        }
    }
}
