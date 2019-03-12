﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.SharedObjects.Tests
{
    public class MockSharedRegisterTest : BaseTest
    {
        public MockSharedRegisterTest(ITestOutputHelper output)
            : base(output)
        { }

        struct S
        {
            public int Value1;
            public int Value2;

            public S(int value1, int value2)
            {
                this.Value1 = value1;
                this.Value2 = value2;
            }
        };

        class E<T> : Event where T : struct
        {
            public ISharedRegister<T> Counter;

            public E(ISharedRegister<T> counter)
            {
                this.Counter = counter;
            }
        }

        class Setup : Event
        {
            public bool Flag;

            public Setup(bool flag)
            {
                this.Flag = flag;
            }
        }

        class M1 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var flag = (this.ReceivedEvent as Setup).Flag;

                var counter = SharedRegister.Create<int>(this.Id.Runtime, 0);
                counter.SetValue(5);

                this.CreateMachine(typeof(N1), new E<int>(counter));

                counter.Update(x =>
                {
                    if (x == 5)
                    {
                        return 6;
                    }
                    return x;
                });

                var v = counter.GetValue();

                if (flag)
                {
                    // Succeeds.
                    this.Assert(v == 2 || v == 6);
                }
                else
                {
                    // Fails.
                    this.Assert(v == 6);
                }
            }
        }

        class N1 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var counter = (this.ReceivedEvent as E<int>).Counter;
                counter.SetValue(2);
            }
        }

        class M2 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var flag = (this.ReceivedEvent as Setup).Flag;

                var counter = SharedRegister.Create<S>(this.Id.Runtime);
                counter.SetValue(new S(1, 1));

                this.CreateMachine(typeof(N2), new E<S>(counter));

                counter.Update(x =>
                {
                    return new S(x.Value1 + 1, x.Value2 + 1);
                });

                var v = counter.GetValue();

                // Succeeds.
                this.Assert(v.Value1 == v.Value2);

                if (flag)
                {
                    // Succeeds.
                    this.Assert(v.Value1 == 2 || v.Value1 == 5 || v.Value1 == 6);
                }
                else
                {
                    // Fails.
                    this.Assert(v.Value1 == 2 || v.Value1 == 6);
                }
            }
        }

        class N2 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var counter = (this.ReceivedEvent as E<S>).Counter;
                counter.SetValue(new S(5, 5));
            }
        }

        [Fact]
        public void TestMockSharedRegister1()
        {
            var config = Configuration.Create().WithNumberOfIterations(100);
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M1), new Setup(true));
            });

            base.AssertSucceeded(config, test);
        }

        [Fact]
        public void TestMockSharedRegister2()
        {
            var config = Configuration.Create().WithNumberOfIterations(100);
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M1), new Setup(false));
            });

            base.AssertFailed(config, test, "Detected an assertion failure.");
        }

        [Fact]
        public void TestMockSharedRegister3()
        {
            var config = Configuration.Create().WithNumberOfIterations(100);
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M2), new Setup(true));
            });

            base.AssertSucceeded(config, test);
        }

        [Fact]
        public void TestMockSharedRegister4()
        {
            var config = Configuration.Create().WithNumberOfIterations(100);
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M2), new Setup(false));
            });

            base.AssertFailed(config, test, "Detected an assertion failure.");
        }
    }
}
