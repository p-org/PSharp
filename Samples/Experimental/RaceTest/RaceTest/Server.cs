using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaceTest
{
    public class Server : Machine
    {
        [Start]
        [OnEntry(nameof(OnInint))]
        class Init : MachineState { }

        void OnInint()
        {
            var client = CreateMachine(typeof(Client));
            var ev = new Client.Ping(5);
            Send(client, ev);
            ev.Count++;
        }
    }
}
