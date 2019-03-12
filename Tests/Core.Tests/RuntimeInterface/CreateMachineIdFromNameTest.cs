// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.Core.Tests
{
    public class CreateMachineIdFromNameTest : BaseTest
    {
        public CreateMachineIdFromNameTest(ITestOutputHelper output)
            : base(output)
        { }

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
            var config = base.GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) => {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += delegate
                {
                    failed = true;
                    tcs.SetResult(false);
                };

                var m1 = r.CreateMachine(typeof(M));
                var m2 = r.CreateMachineIdFromName(typeof(M), "M");
                r.Assert(!m1.Equals(m2));
                r.CreateMachine(m2, typeof(M), new Conf(tcs));

                tcs.Task.Wait(5000);
                Assert.False(failed);
            });

            base.Run(config, test);
        }

        [Fact]
        public void TestCreateWithId2()
        {
            var config = base.GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) => {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += delegate
                {
                    failed = true;
                    tcs.SetResult(false);
                };

                var m1 = r.CreateMachineIdFromName(typeof(M), "M1");
                var m2 = r.CreateMachineIdFromName(typeof(M), "M2");
                r.Assert(!m1.Equals(m2));
                r.CreateMachine(m1, typeof(M));
                r.CreateMachine(m2, typeof(M), new Conf(tcs));

                tcs.Task.Wait(5000);
                Assert.False(failed);
            });

            base.Run(config, test);
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
            var config = base.GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) => {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += delegate
                {
                    failed = true;
                    tcs.SetResult(false);
                };

                try
                {
                    var m3 = r.CreateMachineIdFromName(typeof(M3), "M3");
                    r.CreateMachine(m3, typeof(M2));
                }
                catch (Exception)
                {
                    failed = true;
                    tcs.SetResult(false);
                }

                tcs.Task.Wait();
                Assert.True(failed);
            });

            base.Run(config, test);
        }

        [Fact]
        public void TestCreateWithId5()
        {
            var config = base.GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) => {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += delegate
                {
                    failed = true;
                    tcs.SetResult(false);
                };

                try
                {
                    var m1 = r.CreateMachineIdFromName(typeof(M2), "M2");
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
            });

            base.Run(config, test);
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
            var config = base.GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) => {
                var m1 = r.CreateMachineIdFromName(typeof(M4), "M4");
                var m2 = r.CreateMachineIdFromName(typeof(M4), "M4");
                Assert.True(m1.Equals(m2));
            });

            base.Run(config, test);
        }

        class M6 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var m = this.Runtime.CreateMachineIdFromName(typeof(M4), "M4");
                this.CreateMachine(m, typeof(M4), "friendly");
            }
        }

        [Fact]
        public void TestCreateWithId10()
        {
            var config = base.GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) => {
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
            });

            base.Run(config, test);
        }

        class M7 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                await this.Runtime.CreateMachineAndExecute(typeof(M6));
                var m = this.Runtime.CreateMachineIdFromName(typeof(M4), "M4");
                this.Runtime.SendEvent(m, this.ReceivedEvent);
            }
        }

        [Fact]
        public void TestCreateWithId11()
        {
            var config = base.GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) => {
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
            });

            base.Run(config, test);
        }
    }
}
