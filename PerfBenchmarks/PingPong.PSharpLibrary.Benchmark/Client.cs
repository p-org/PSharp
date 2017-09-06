using Microsoft.PSharp;
using System;
using System.Threading.Tasks;

namespace PingPong.PSharpLibrary
{    
    internal class Client : Machine
    {
        long received = 0L;
        long sent = 0L;
        TaskCompletionSource<bool> hasCompleted;
        TaskCompletionSource<bool> hasInitialized;
        long repeat;
 
        /// <summary>
        /// Reference to the server machine.
        /// </summary>
        MachineId Server;
       
        /// <summary>
        /// Event declaration of a 'Config' event that contains payload.
        /// </summary>
        internal class Config : Event
        {
            /// <summary>
            /// The payload of the event. It is a reference to the server machine
            /// (sent by the environment upon creation of the client).
            /// </summary>
            public MachineId Server;
            public TaskCompletionSource<bool> hasCompleted;
            public TaskCompletionSource<bool> hasInitialized;
            public long repeatCount;

            public Config(MachineId server, TaskCompletionSource<bool> hasCompleted, TaskCompletionSource<bool> hasInitialized, long repeat)
            {
                this.Server = server;
                this.hasCompleted = hasCompleted;
                this.hasInitialized = hasInitialized;
                this.repeatCount = repeat;
            }
        }
       
        internal class Register : Event
        {
            /// <summary>
            /// The payload of the event. It is a reference to the client machine.
            /// </summary>
            public MachineId Client;

            public Register(MachineId client)
            {
                this.Client = client;
            }
        }

        [Start]
        [OnEntry(nameof(InitOnEntry))]       
        class Init : MachineState { }

        async Task InitOnEntry()
        {            
            var e = this.ReceivedEvent as Config;
            this.Server = e.Server;
            this.hasCompleted = e.hasCompleted;
            this.repeat = e.repeatCount;
            this.hasInitialized = e.hasInitialized;
            this.Send(this.Server, new Register(this.Id));
            await Receive(typeof(Server.Ack));
            this.Goto<Active>();
        }

        [OnEntry(nameof(ActiveOnEntry))]
        [OnEventDoAction(typeof(Messages.Msg), nameof(HandleMsg))]
        [OnEventDoAction(typeof(Messages.Started), nameof(HandleStarted))]
        [OnEventDoAction(typeof(Messages.Run), nameof(HandleRun))]
        /// </summary>
        class Active : MachineState { }

        void ActiveOnEntry()
        {
            this.hasInitialized.SetResult(true);
        }

        void HandleMsg()
        {
            var e = this.ReceivedEvent;
            received++;
            if (sent < repeat)
            {
                this.Send(Server, e);
                sent++;
            }
            else if (received >= repeat)
            {
                //Console.WriteLine("Sent/Received/repeat in Client: {0}/{1}/{2}", sent, received, repeat);
                this.Send(Server, new Halt());                
                Raise(new Halt());
                hasCompleted.SetResult(true);
            }
        }

        void HandleRun()
        {
            var msg = new Messages.Msg();

            for (int i = 0; i < Math.Min(1000, repeat); i++)
            {
                this.Send(Server, msg);
                sent++;
            }
        }

        void HandleStarted()
        {
            throw new NotImplementedException();
        }
    }
}