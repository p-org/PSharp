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

namespace Microsoft.PSharp.Core.Tests.Performance
{
    [Config(typeof(Configuration))]
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
                internal long NumberOfClients;
                internal long NumberOfSends;

                internal Configure(long numberOfClients, long numberOfSends)
                {
                    this.NumberOfClients = numberOfClients;
                    this.NumberOfSends = numberOfSends;
                }
            }

            long Counter = 0;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(Client.Ping), nameof(Pong))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var numberOfClients = (this.ReceivedEvent as Configure).NumberOfClients;
                var numberOfSends = (this.ReceivedEvent as Configure).NumberOfSends;
                for (int i = 0; i < numberOfClients; i++)
                {
                    this.CreateMachine(typeof(Client), new Client.Configure(this.Id, numberOfSends));
                }
            }

            void Pong()
            {
                this.Counter++;
            }
        }

        [Params(1, 2, 4)]
        public int Clients { get; set; }

        [Params(100, 1000, 5000, 10000)]
        public int EventsPerClient { get; set; }

        [Benchmark]
        public void SendMessages()
        {
            var runtime = new StateMachineRuntime();
            runtime.TryCreateMachine(typeof(Server), null,
                new Server.Configure(this.Clients, this.EventsPerClient),
                null, false);
            runtime.Wait();
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
