namespace Microsoft.PSharp.ServiceFabric
{
    using Microsoft.PSharp.IO;

    public class EventSourcePSharpLogger : StateMachineLogger
    {
        private readonly IPSharpEventSourceLogger eventSource;

        public EventSourcePSharpLogger(IPSharpEventSourceLogger eventSource)
        {
            this.eventSource = eventSource;
        }

        public override void Dispose()
        {
            // no - op
        }

        public override void Write(string value)
        {
            this.eventSource.Message(value);
        }

        public override void Write(string format, params object[] args)
        {
            this.eventSource.Message(format, args);
        }

        public override void WriteLine(string value)
        {
            this.eventSource.Message(value);
        }

        public override void WriteLine(string format, params object[] args)
        {
            this.eventSource.Message(format, args);
        }
    }
}
