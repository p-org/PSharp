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

using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;

namespace Microsoft.PSharp.Core.Tests.Performance
{
    [Config(typeof(Configuration))]
    public class CreateMachinesTest
    {
        class Node : Machine
        {
            [Start]
            class Init : MachineState { }
        }

        [Params(100, 1000, 10000)]
        public int Size { get; set; }

        [Benchmark]
        public void CreateMachines()
        {
            var runtime = new StateMachineRuntime();

            for (int idx = 0; idx < Size; idx++)
            {
                runtime.TryCreateMachine(typeof(Node), null, null, null, false);
            }

            runtime.Wait();
        }

        [Benchmark(Baseline = true)]
        public void CreateTasks()
        {
            Task[] tasks = new Task[Size];
            for (int idx = 0; idx < Size; idx++)
            {
                var task = new Task(() => { return; });
                task.Start();
                tasks[idx] = task;
            }

            Task.WaitAll(tasks);
        }
    }
}
