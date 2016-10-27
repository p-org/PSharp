using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaceTest
{
    public class Client : Machine
    {
        public class Ping : Event
        {
            public int Count;

            public Ping(int count)
            {
                this.Count = count;
            }
        }

        [Start]
        [OnEventDoAction(typeof(Ping), nameof(OnPing))]
        class Init : MachineState { }

        void OnPing()
        {
            var e = ReceivedEvent as Ping;
            Console.WriteLine(e.Count);
        }
    }
}
