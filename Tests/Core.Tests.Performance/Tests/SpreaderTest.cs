//-----------------------------------------------------------------------
// <copyright file="CreateMachinesTest.cs">
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
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Attributes.Jobs;

namespace Microsoft.PSharp.Core.Tests.Performance
{
    /// <summary>
    /// Tests P# performance when creating a lot of machines
    /// Here, every machine creates 2 child machines and so on
    /// Creates 2^x - 1 machines, where x is the count passed into Spreader.Config
    /// This benchmark is adapted from https://github.com/ponylang/ponyc/tree/master/examples/spreader
    /// </summary>
    [Config(typeof(Configuration))]
    [SimpleJob(RunStrategy.Monitoring, launchCount: 10, warmupCount: 2, targetCount: 10)]
    public class SpreaderTest
    {
        class Spreader : Machine
        {
            long _count;
            MachineId _parent;
            long _result;
            long _received;
            TaskCompletionSource<bool> hasCompleted;

            internal class Config : Event
            {
                public Config(MachineId parent, long count, TaskCompletionSource<bool> hasCompleted)
                {
                    this.Parent = parent;
                    this.Count = count;
                    this.hasCompleted = hasCompleted;
                }

                public MachineId Parent { get; private set; }
                public long Count { get; private set; }
                public TaskCompletionSource<bool> hasCompleted { get; private set; }
            }

            internal class ResultEvent : Event
            {
                public ResultEvent(long count)
                {
                    this.Count = count;
                }

                public long Count { get; private set; }
            }


            void SpawnChild()
            {
                this.CreateMachine(typeof(Spreader), new Config(this.Id, _count - 1, null));
            }

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(ResultEvent), nameof(Result))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var e = this.ReceivedEvent as Config;
                this._parent = e.Parent;
                this._count = e.Count;
                this.hasCompleted = e.hasCompleted;
                if (_count == 1)
                {
                    this.Send(_parent, new ResultEvent(1L));
                }
                else
                {
                    SpawnChild();
                    SpawnChild();
                }
            }

            void Result()
            {
                var e = this.ReceivedEvent as ResultEvent;
                _received = _received + 1;
                _result = _result + e.Count;
                if (_received == 2)
                {
                    if (_parent != null)
                    {
                        this.Send(_parent, new ResultEvent(_result + 1));
                    }
                    else
                    {
                        // Console.WriteLine("{0} Machines", _result + 1);
                        this.hasCompleted.SetResult(true);
                    }
                }

            }
        }

        [Params(18, 19)]
        public int Size { get; set; }

        [Benchmark]
        public void CreateMachines()
        {
            TaskCompletionSource<bool> hasCompleted = new TaskCompletionSource<bool>();
            var runtime = new StateMachineRuntime();
            runtime.CreateMachine(typeof(Spreader), new Spreader.Config(null, Size, hasCompleted));
            hasCompleted.Task.Wait();                        
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
