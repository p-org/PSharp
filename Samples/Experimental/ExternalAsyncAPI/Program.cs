using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;

namespace ExternalAsyncAPI
{
    static class External
    {
        public static async Task<int> Ext(int arg)
        {
            await Task.Delay(10);
            return arg + 1;
        }
    }


    class Request : Event
    {
        public int value;
        public MachineId Id;

        public Request(int value, MachineId Id)
        {
            this.value = value;
            this.Id = Id;
        }
    }
    class Response : Event
    {
        public int value;
        public MachineId Id;

        public Response(int value, MachineId Id)
        {
            this.value = value;
            this.Id = Id;
        }
    }

    class M : Machine
    {
        [Start]
        [OnEventDoAction(typeof(Request), nameof(Act))]
        class Init : MachineState { }

        void Act()
        {
            var value = (this.ReceivedEvent as Request).value;
            var sender = (this.ReceivedEvent as Request).Id;
          
            Task.Run(() =>
            {
                var r = External.Ext(value).Result;
                sender.Runtime.SendEvent(sender, new Response(r, this.Id));
            });
        }

    }

    class Harness : Machine
    {
        MachineId m;

        [OnEntry(nameof(Start))]
        [OnEventDoAction(typeof(Response), nameof(Complete))]
        [Start]
        class Init : MachineState { }

        void Start()
        {
            m = this.CreateMachine(typeof(M));
            this.Send(m, new Request(3, this.Id));
            this.Send(m, new Request(5, this.Id));
            this.Send(m, new Request(7, this.Id));
            this.Send(m, new Request(9, this.Id));
        }

        void Complete()
        {
            var e = (this.ReceivedEvent as Response);
            Console.WriteLine("Got result {0} from machine {1}", e.value, e.Id.Name);
        }
    }


    class Program
    {
        static void Main(string[] args)
        {
            var runtime = PSharpRuntime.Create();
            runtime.CreateMachine(typeof(Harness));
            //runtime.Wait();
            Console.ReadLine();
        }
    }
}
