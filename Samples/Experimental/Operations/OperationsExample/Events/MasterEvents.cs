using Microsoft.PSharp;

namespace OperationsExample.Events
{
    internal class MasterEvents
    {
        internal class RegisterClient : Event
        {
            public MachineId client;

            public RegisterClient(MachineId client)
            {
                this.client = client;
            }
        }

        internal class masterlocal : Event { }

        internal class Response : Event
        {
            public string V;

            public Response(string v)
            {
                this.V = v;
            }
        }

        internal class Read : Event
        {
            public string K;

            public Read(string k)
            {
                this.K = k;
            }
        }

        internal class Write : Event
        {
            public string K;
            public string V;

            public Write(string k, string v)
            {
                this.K = k;
                this.V = v;
            }
        }
    }
}
