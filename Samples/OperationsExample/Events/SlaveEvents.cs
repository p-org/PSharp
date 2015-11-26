using Microsoft.PSharp;

namespace OperationsExample.Events
{
    class SlaveEvents
    {
        internal class slavelocal : Event { }

        internal class PerformSlaveOperation : MachineState { }

        internal class PerformResponse : Event
        {
            public string V;

            public PerformResponse(string v)
            {
                this.V = v;
            }
        }

        internal class PerformRead : Event
        {
            public string K;

            public PerformRead(string k)
            {
                this.K = k;
            }
        }

        internal class PerformWrite : Event
        {
            public string K;
            public string V;

            public PerformWrite(string k, string v)
            {
                this.K = k;
                this.V = v;
            }
        }
    }
}
