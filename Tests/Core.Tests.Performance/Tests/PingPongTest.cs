//-----------------------------------------------------------------------
// <copyright file="PingPongTest.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;
using System;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Engines;
using System.Collections.Generic;

namespace Microsoft.PSharp.Core.Tests.Performance
{
    /// <summary>
    /// A PingPong application written using P# as a C# library.
    /// This benchmark is adapted from the Akka.net benchmark that
    /// measures message processing throughput
    /// here: https://github.com/akkadotnet/akka.net/tree/dev/src/benchmark/PingPong
    /// </summary>    
    public class PingPongTest
    {

        public class Messages
        {
            public class Run : Event { };

            public class Started : Event { };

            public class Msg : Event { };
        }

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

        internal class Server : Machine
        {
            /// <summary>
            /// Event declaration of a 'Pong' event that does not contain any payload.
            /// </summary>
            internal class Pong : Event { }

            internal class Ack : Event { }

            public MachineId client;

            [Start]
            [OnEventDoAction(typeof(Messages.Started), nameof(Respond))]
            [OnEventDoAction(typeof(Messages.Msg), nameof(Respond))]
            [OnEventDoAction(typeof(Client.Register), nameof(Register))]
            /// </summary>
            class Active : MachineState { }

            void Respond()
            {
                this.Send(client, this.ReceivedEvent);
            }

            void Register()
            {
                var e = ReceivedEvent as Client.Register;
                this.client = e.Client;
                this.Send(e.Client, new Ack());
            }

        }

        [Config(typeof(Configuration))]
        [SimpleJob(RunStrategy.Monitoring)]
        public class PingPong {
            private List<MachineId> clients;
            private List<Task> initSignals;
            private List<Task> completionSignals;
            private PSharpRuntime runtime;            
            

            [Params(8)]
            public uint numberOfClients { get; set; }

            [Params(1000, 10000, 100000, 1000000)]
            public uint totalNumberOfMessages { get; set; }

            [GlobalSetup]
            public void GlobalSetup()
            {
                initSignals = new List<Task>();
                completionSignals = new List<Task>();
                clients = new List<MachineId>();
                runtime = new StateMachineRuntime();
                for (int i = 0; i < numberOfClients; i++)
                {
                    var hasCompleted = new TaskCompletionSource<bool>();
                    var hasInitialized = new TaskCompletionSource<bool>();
                    completionSignals.Add(hasCompleted.Task);
                    initSignals.Add(hasInitialized.Task);
                    var server = runtime.CreateMachine(typeof(Server));
                    var client = runtime.CreateMachine(typeof(Client), 
                        new Client.Config(server, hasCompleted, hasInitialized, totalNumberOfMessages/numberOfClients));
                    clients.Add(client);
                }
                Task.WhenAll(initSignals).Wait();
            }

            [Benchmark]
            public void CreateMachines()
            {
                var run = new Messages.Run();
                clients.ForEach(c => runtime.SendEvent(c, run));
                Task.WhenAll(completionSignals).Wait();
            }
        }
        //[Benchmark(Baseline = true)]
        //public void CreateTasks()
        //{
        //    var tcs = new TaskCompletionSource<bool>();
        //    int counter = 0;

        //    for (int idx = 0; idx < Size; idx++)
        //    {
        //        var task = new Task(() => {
        //            int value = Interlocked.Increment(ref counter);
        //            if (value == Size)
        //            {
        //                tcs.TrySetResult(true);
        //            }
        //        });

        //        task.Start();
        //    }

        //    tcs.Task.Wait();
        //}
    }
}
