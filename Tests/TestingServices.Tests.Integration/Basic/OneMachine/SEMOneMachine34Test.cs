//-----------------------------------------------------------------------
// <copyright file="SEMOneMachine34Test.cs">
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

using System;
using System.Collections.Generic;

using Microsoft.PSharp.Utilities;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Integration
{
    public class SEMOneMachine34Test : BaseTest
    {
        class E1 : Event
        {
            public Tuple<int, bool> T;
            public E1(Tuple<int, bool> t) : base(-1, -1) { this.T = t; }
        }

        class E2 : Event
        {
            public int V;
            public bool B;
            public E2(int v, bool b) : base(-1, -1) { this.V = v; this.B = b; }
        }

        class E3 : Event
        {
            public int V;
            public E3(int v) : base(-1, -1) { this.V = v; }
        }

        class E4 : Event
        {
            public Dictionary<int, int> D;
            public List<bool> L;
            public E4(Dictionary<int, int> d, List<bool> l) : base(-1, -1) { this.D = d; this.L = l; }
        }

        class MachOS : Machine
        {
            int Int;
            bool Bool;
            Dictionary<int, int> m;
            List<bool> s;

            [Start]
            [OnEntry(nameof(EntryInit))]
            [OnEventDoAction(typeof(E1), nameof(Foo1))]
            [OnEventDoAction(typeof(E2), nameof(Foo2))]
            [OnEventDoAction(typeof(E3), nameof(Foo3))]
            [OnEventDoAction(typeof(E4), nameof(Foo4))]
            class Init : MachineState { }

            void EntryInit()
            {
                m = new Dictionary<int, int>();
                s = new List<bool>();
                m.Add(0, 1);
                m.Add(1, 2);
                s.Add(true);
                s.Add(false);
                s.Add(true);
                this.Send(this.Id, new E1(Tuple.Create(1, true)));
                this.Send(this.Id, new E2(0, false));
                this.Send(this.Id, new E3(1));
                this.Send(this.Id, new E4(m, s));
            }

            void Foo1()
            {
                Int = (this.ReceivedEvent as E1).T.Item1;
                this.Assert(Int == 1);
                Bool = (this.ReceivedEvent as E1).T.Item2;
                this.Assert(Bool == true);
            }

            void Foo2()
            {
                Int = (this.ReceivedEvent as E2).V;
                this.Assert(Int == 0);
                Bool = (this.ReceivedEvent as E2).B;
                this.Assert(Bool == false);
            }

            void Foo3()
            {
                Int = (this.ReceivedEvent as E3).V;
                this.Assert(Int == 1);
            }

            void Foo4()
            {
                Int = (this.ReceivedEvent as E4).D[0];
                this.Assert(Int == 1);
                Bool = (this.ReceivedEvent as E4).L[2];
                this.Assert(Bool == true);
            }
        }

        [Fact]
        public void TestSEMOneMachine34()
        {
            var configuration = base.GetConfiguration();
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;
            configuration.SchedulingIterations = 100;

            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(MachOS));
            });

            base.AssertSucceeded(configuration, test);
        }
    }
}
