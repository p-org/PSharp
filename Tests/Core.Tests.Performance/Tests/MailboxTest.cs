//-----------------------------------------------------------------------
// <copyright file="MailboxTest.cs">
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

using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Engines;

namespace Microsoft.PSharp.Core.Tests.Performance
{
    [Config(typeof(Configuration))]
    [SimpleJob(RunStrategy.Monitoring)]
    public class MailboxTest
    {
        class Client : Machine
        {
            internal class Configure : Event
            {
                internal MachineId Server;
                internal long NumberOfSends;

                internal Configure(MachineId server, long numberOfSends)
                {
                    this.Server = server;
                    this.NumberOfSends = numberOfSends;
                }
            }

            internal class Ping : Event { }

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var server = (this.ReceivedEvent as Configure).Server;
                var numberOfSends = (this.ReceivedEvent as Configure).NumberOfSends;
                for (int i = 0; i < numberOfSends; i++)
                {
                    this.Send(server, new Ping());
                }
            }
        }

        class Server : Machine
        {
            internal class Configure : Event
            {
                public TaskCompletionSource<bool> TCS;
                internal long NumberOfClients;
                internal long NumberOfSends;

                internal Configure(TaskCompletionSource<bool> tcs, long numberOfClients, long numberOfSends)
                {
                    this.TCS = tcs;
                    this.NumberOfClients = numberOfClients;
                    this.NumberOfSends = numberOfSends;
                }
            }

            TaskCompletionSource<bool> TCS;
            long NumberOfClients;
            long NumberOfSends;
            long MaxValue = 0;
            long Counter = 0;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(Client.Ping), nameof(Pong))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.TCS = (this.ReceivedEvent as Configure).TCS;
                this.NumberOfClients = (this.ReceivedEvent as Configure).NumberOfClients;
                this.NumberOfSends = (this.ReceivedEvent as Configure).NumberOfSends;
                this.MaxValue = this.NumberOfClients * this.NumberOfSends;
                for (int i = 0; i < this.NumberOfClients; i++)
                {
                    this.CreateMachine(typeof(Client), new Client.Configure(this.Id, this.NumberOfSends));
                }
            }

            void Pong()
            {
                this.Counter++;
                if (this.Counter == this.MaxValue)
                {
                    this.TCS.SetResult(true);
                }
            }
        }

        [Fast]
        class FastClient : Machine
        {
            internal class Configure : Event
            {
                internal MachineId Server;
                internal long NumberOfSends;

                internal Configure(MachineId server, long numberOfSends)
                {
                    this.Server = server;
                    this.NumberOfSends = numberOfSends;
                }
            }

            internal class Ping : Event { }

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var server = (this.ReceivedEvent as Configure).Server;
                var numberOfSends = (this.ReceivedEvent as Configure).NumberOfSends;
                for (int i = 0; i < numberOfSends; i++)
                {
                    this.Send(server, new Ping());
                }
            }
        }

        [Fast]
        class FastServer : Machine
        {
            internal class Configure : Event
            {
                public TaskCompletionSource<bool> TCS;
                internal long NumberOfClients;
                internal long NumberOfSends;

                internal Configure(TaskCompletionSource<bool> tcs, long numberOfClients, long numberOfSends)
                {
                    this.TCS = tcs;
                    this.NumberOfClients = numberOfClients;
                    this.NumberOfSends = numberOfSends;
                }
            }

            TaskCompletionSource<bool> TCS;
            long NumberOfClients;
            long NumberOfSends;
            long MaxValue = 0;
            long Counter = 0;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(FastClient.Ping), nameof(Pong))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.TCS = (this.ReceivedEvent as Configure).TCS;
                this.NumberOfClients = (this.ReceivedEvent as Configure).NumberOfClients;
                this.NumberOfSends = (this.ReceivedEvent as Configure).NumberOfSends;
                this.MaxValue = this.NumberOfClients * this.NumberOfSends;
                for (int i = 0; i < this.NumberOfClients; i++)
                {
                    this.CreateMachine(typeof(FastClient), new FastClient.Configure(this.Id, this.NumberOfSends));
                }
            }

            void Pong()
            {
                this.Counter++;
                if (this.Counter == this.MaxValue)
                {
                    this.TCS.SetResult(true);
                }
            }
        }

        [Params(1, 2, 4, 8, 16)]
        public int Clients { get; set; }

        [Params(100, 1000, 10000, 100000)]
        public int EventsPerClient { get; set; }

        [Benchmark(Baseline = true)]
        public void SendMessages()
        {
            var tcs = new TaskCompletionSource<bool>();

            var runtime = new StateMachineRuntime();
            runtime.CreateMachine(typeof(Server), null,
                new Server.Configure(tcs, this.Clients, this.EventsPerClient),
                null);

            tcs.Task.Wait();
        }

        [Benchmark]
        public void SendMessagesFast()
        {
            var tcs = new TaskCompletionSource<bool>();

            var runtime = new StateMachineRuntime();
            runtime.CreateMachine(typeof(FastServer), null,
                new FastServer.Configure(tcs, this.Clients, this.EventsPerClient),
                null);

            tcs.Task.Wait();
        }

        //[Benchmark(Baseline = true)]
        //public void CreateTasks()
        //{
        //    Task[] tasks = new Task[Size];
        //    for (int idx = 0; idx < Size; idx++)
        //    {
        //        var task = new Task(() => { return; });
        //        task.Start();
        //        tasks[idx] = task;
        //    }

        //    Task.WaitAll(tasks);
        //}
    }
}
