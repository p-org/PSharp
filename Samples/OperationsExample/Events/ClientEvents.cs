
using Microsoft.PSharp;

namespace OperationsExample.Events
{
    internal class ClientEvents
    {
        internal class local : Event { }

        internal class Config : Event
        {
            public MachineId Master;

            public Config(MachineId master)
                : base(-1, -1)
            {
                this.Master = master;
            }
        }
    }
}
