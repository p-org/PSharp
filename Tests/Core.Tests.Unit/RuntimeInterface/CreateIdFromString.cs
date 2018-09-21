// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.Core.Tests.Unit
{
    public class CreateIdFromString 
    {
        class E : Event { }

        class Conf : Event
        {
            public TaskCompletionSource<bool> tcs;

            public Conf(TaskCompletionSource<bool> tcs)
            {
                this.tcs = tcs;
            }
        }

        class M : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                if(this.ReceivedEvent is Conf)
                {
                    (this.ReceivedEvent as Conf).tcs.SetResult(true);
                }
            }
        }

        [Fact]
        public void TestCreateWithId1()
        {
            var runtime = PSharpRuntime.Create();
            var failed = false;
            var tcs = new TaskCompletionSource<bool>();
            runtime.OnFailure += delegate
            {
                failed = true;
                tcs.SetResult(false);
            };

            var m1 = runtime.CreateMachine(typeof(M));
            var m2 = runtime.CreateMachineIdFromString(typeof(M), "M");
            runtime.Assert(!m1.Equals(m2));
            runtime.CreateMachine(m2, typeof(M), new Conf(tcs));

            tcs.Task.Wait(5000);
            Assert.False(failed);
        }

        [Fact]
        public void TestCreateWithId2()
        {
            var runtime = PSharpRuntime.Create();
            var failed = false;
            var tcs = new TaskCompletionSource<bool>();
            runtime.OnFailure += delegate
            {
                failed = true;
                tcs.SetResult(false);
            };

            var m1 = runtime.CreateMachineIdFromString(typeof(M), "M1");
            var m2 = runtime.CreateMachineIdFromString(typeof(M), "M2");
            runtime.Assert(!m1.Equals(m2));
            runtime.CreateMachine(m1, typeof(M));
            runtime.CreateMachine(m2, typeof(M), new Conf(tcs));

            tcs.Task.Wait(5000);
            Assert.False(failed);

        }

        class M2 : Machine
        {
            [Start]
            class S : MachineState { }
        }

        class M3 : Machine
        {
            [Start]
            class S : MachineState { }
        }

        [Fact]
        public void TestCreateWithId4()
        {
            var runtime = PSharpRuntime.Create();
            var failed = false;
            var tcs = new TaskCompletionSource<bool>();
            runtime.OnFailure += delegate
            {
                failed = true;
                tcs.SetResult(false);
            };

            try
            {
                var m3 = runtime.CreateMachineIdFromString(typeof(M3), "M3");
                runtime.CreateMachine(m3, typeof(M2));
            }
            catch(Exception)
            {
                failed = true;
                tcs.SetResult(false);
            }

            tcs.Task.Wait();
            Assert.True(failed);
        }

        [Fact]
        public void TestCreateWithId5()
        {
            var r = PSharpRuntime.Create();
            var failed = false;
            var tcs = new TaskCompletionSource<bool>();
            r.OnFailure += delegate
            {
                failed = true;
                tcs.SetResult(false);
            };

            try
            {
                var m1 = r.CreateMachineIdFromString(typeof(M2), "M2");
                r.CreateMachine(m1, typeof(M2));
                r.CreateMachine(m1, typeof(M2));
            }
            catch (Exception)
            {
                failed = true;
                tcs.SetResult(false);
            }

            tcs.Task.Wait();
            Assert.True(failed);
        }

        class E2 : Event
        {
            public MachineId mid;

            public E2(MachineId mid)
            {
                this.mid = mid;
            }
        }

        class M4 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(Conf), nameof(Process))]
            class S : MachineState { }

            void Process()
            {
                (this.ReceivedEvent as Conf).tcs.SetResult(true);
            }
        }

        [Fact]
        public void TestCreateWithId9()
        {
            var r = PSharpRuntime.Create();
            var m1 = r.CreateMachineIdFromString(typeof(M4), "M4");
            var m2 = r.CreateMachineIdFromString(typeof(M4), "M4");
            Assert.True(m1.Equals(m2));
        }

        class M6 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var m = this.Runtime.CreateMachineIdFromString(typeof(M4), "M4");
                this.CreateMachine(m, typeof(M4), "friendly");
            }
        }

        [Fact]
        public void TestCreateWithId10()
        {
            var r = PSharpRuntime.Create();
            var failed = false;
            var tcs = new TaskCompletionSource<bool>();
            r.OnFailure += delegate
            {
                failed = true;
                tcs.SetResult(false);
            };

            r.CreateMachine(typeof(M6));
            r.CreateMachine(typeof(M6));

            tcs.Task.Wait();
            Assert.True(failed);
        }

        class M7 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                await this.Runtime.CreateMachineAndExecute(typeof(M6));
                var m = this.Runtime.CreateMachineIdFromString(typeof(M4), "M4");
                this.Runtime.SendEvent(m, this.ReceivedEvent);
            }
        }

        [Fact]
        public void TestCreateWithId11()
        {
            var r = PSharpRuntime.Create();
            var failed = false;
            var tcs = new TaskCompletionSource<bool>();
            r.OnFailure += delegate
            {
                failed = true;
                tcs.SetResult(false);
            };

            r.CreateMachine(typeof(M7), new Conf(tcs));

            tcs.Task.Wait();
            Assert.False(failed);
        }
    }
}
