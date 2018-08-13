using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;

namespace SendAndReceive
{
    /// <summary>
    /// Generic machine that helps fetch response
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class GetReponseMachine<T> : Machine where T : Event
    {
        /// <summary>
        /// Static method for safely getting a response from a machine
        /// </summary>
        /// <param name="runtime">The runtime</param>
        /// <param name="mid">Target machine Id</param>
        /// <param name="ev">Event to send whose respose we're interested in getting</param>
        /// <returns></returns>
        public static async Task<T> GetResponse(PSharpRuntime runtime, MachineId mid, Func<MachineId, Event> ev)
        {
            var conf = new Config(mid, ev);
            // This method awaits until the GetResponseMachine finishes its Execute method
            await runtime.CreateMachineAndExecute(typeof(GetReponseMachine<T>), conf);
            // Safety return the result back (no race condition here)
            return conf.ReceivedEvent;
        }

        /// <summary>
        /// Internal config event
        /// </summary>
        class Config : Event
        {
            public MachineId TargetMachineId;
            public Func<MachineId, Event> Ev;
            public T ReceivedEvent;

            public Config(MachineId targetMachineId, Func<MachineId, Event> ev)
            {
                this.TargetMachineId = targetMachineId;
                this.Ev = ev;
                this.ReceivedEvent = null;
            }
        }


        [Start]
        [OnEntry(nameof(Execute))]
        class Init : MachineState { }

        async Task Execute()
        {
            // grab the config event
            var config = this.ReceivedEvent as Config;
            // send event to target machine, adding self Id
            this.Send(config.TargetMachineId, config.Ev(this.Id));
            // wait for the response
            var rv = await this.Receive(typeof(T));
            // stash in the shared config event
            config.ReceivedEvent = rv as T;
            // halt
            this.Raise(new Halt());
        }
    }
}
