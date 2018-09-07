// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.PSharp.Runtime;
using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    // Sub-namespaces copy the expected result from Microsoft.PSharp.LanguageServices.Tests.Unit.EventInheritanceTests
    // to verify the rewriting is correct.
    namespace MultiPayloadMultiLevel
    {
        internal class E10 : Event
        {
            public short a10;
            public ushort b10;

            public E10(short a10, ushort b10)
                : base()
            {
                Xunit.Assert.True(a10 == 1);
                Xunit.Assert.True(b10 == 2);
                this.a10 = a10;
                this.b10 = b10;
            }
        }

        internal class E1 : E10
        {
            public byte a1;
            public bool b1;

            public E1(short a10, ushort b10, byte a1, bool b1)
                : base(a10, b10)
            {
                Xunit.Assert.True(a1 == 30);
                Xunit.Assert.True(b1 == true);
                this.a1 = a1;
                this.b1 = b1;
            }
        }

        internal class E2 : E1
        {
            public int a2;
            public uint b2;

            public E2(short a10, ushort b10, byte a1, bool b1, int a2, uint b2)
                : base(a10, b10, a1, b1)
            {
                Xunit.Assert.True(a2 == 100);
                Xunit.Assert.True(b2 == 101);
                this.a2 = a2;
                this.b2 = b2;
            }
        }

        public class Tester
        {
            public static void Test()
            {
                Assert.True(new E2(1, 2, 30, true, 100, 101) is E1);
            }
        }
    }

    namespace MultiPayloadMultiLevel_Generic
    {
        internal class E10<Te10> : Event
        {
            public short a10;
            public ushort b10;

            public E10(short a10, ushort b10)
                : base()
            {
                Xunit.Assert.True(a10 == 1);
                Xunit.Assert.True(b10 == 2);
                this.a10 = a10;
                this.b10 = b10;
            }
        }

        internal class E1<Te10, Te1> : E10<Te10>
        {
            public byte a1;
            public bool b1;

            public E1(short a10, ushort b10, byte a1, bool b1)
                : base(a10, b10)
            {
                Xunit.Assert.True(a1 == 30);
                Xunit.Assert.True(b1 == true);
                this.a1 = a1;
                this.b1 = b1;
            }
        }

        internal class E2<Te2, Te1, Te10> : E1<Te10, Te1>
        {
            public int a2;
            public uint b2;

            public E2(short a10, ushort b10, byte a1, bool b1, int a2, uint b2)
                : base(a10, b10, a1, b1)
            {
                Xunit.Assert.True(a2 == 100);
                Xunit.Assert.True(b2 == 101);
                this.a2 = a2;
                this.b2 = b2;
            }
        }

        public class Tester
        {
            public static void Test()
            {
                var e2 = new E2<string, int, bool>(1, 2, 30, true, 100, 101);
                Assert.True(e2 is E1<bool, int>);
                Assert.True(e2 is E10<bool>);
            }
        }
    }

    namespace AssertAssume
    {
        internal class E1 : Event
        {
            public E1() : base(1, -1) { }
        }

        internal class E2 : E1
        {
            public E2() : base()
            {
                base.SetCardinalityConstraints(2, -1);
            }
        }

        internal class E3 : Event
        {
            public E3() : base(-1, 3) { }
        }

        internal class E4 : E3
        {
            public E4() : base()
            {
                base.SetCardinalityConstraints(-1, 4);
            }
        }

        internal class E5 : Event
        {
            public E5() : base(5, -1) { }
        }

        internal class E6 : E5
        {
            public E6() : base()
            {
                base.SetCardinalityConstraints(-1, 6);
            }
        }

        internal class E7 : E6
        {
            public E7()
            : base()
        {
                base.SetCardinalityConstraints(-1, -1);
            }
        }

        public class Tester
        {
            public static void Test()
            {
                bool verify(Event ev, int expectedAssert, int expectedAssume) => ev.Assert == expectedAssert && ev.Assume == expectedAssume;
                Assert.True(verify(new E1(), 1, -1));
                Assert.True(verify(new E2(), 2, -1));
                Assert.True(verify(new E3(), -1, 3));
                Assert.True(verify(new E4(), -1, 4));
                Assert.True(verify(new E5(), 5, -1));
                Assert.True(verify(new E6(), -1, 6));
                Assert.True(verify(new E7(), -1, -1));
            }
        }
    }

    public class EventInheritanceTest : BaseTest
    {
        [Fact]
        public void Test_MultiPayloadMultiLevel()
        {
            MultiPayloadMultiLevel.Tester.Test();
        }

        [Fact]
        public void Test_MultiPayloadMultiLevel_Generic()
        {
            MultiPayloadMultiLevel_Generic.Tester.Test();
        }

        [Fact]
        public void Test_AssertAssume()
        {
            AssertAssume.Tester.Test();
        }

        class A : Machine
        {
            internal class Configure : Event
            {
                public TaskCompletionSource<bool> TCS;

                public Configure(TaskCompletionSource<bool> tcs)
                {
                    this.TCS = tcs;
                }
            }

            public static int E1count;
            public static int E2count;
            public static int E3count;

            private TaskCompletionSource<bool> TCS;

            public class E3 : E2
            { }

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E1), nameof(E1_handler))]
            [OnEventDoAction(typeof(E2), nameof(E2_handler))]
            [OnEventDoAction(typeof(E3), nameof(E3_handler))]
            class S0 : MachineState { }

            void InitOnEntry()
            {
                this.TCS = (this.ReceivedEvent as Configure).TCS;
            }

            void E1_handler()
            {
                ++E1count;
                Xunit.Assert.True(ReceivedEvent is E1);
                CheckComplete();
            }

            void E2_handler()
            {
                ++E2count;
                Xunit.Assert.True(ReceivedEvent is E1);
                Xunit.Assert.True(ReceivedEvent is E2);
                CheckComplete();
            }

            void E3_handler()
            {
                ++E3count;
                Xunit.Assert.True(ReceivedEvent is E1);
                Xunit.Assert.True(ReceivedEvent is E2);
                Xunit.Assert.True(ReceivedEvent is E3);
                CheckComplete();
            }

            void CheckComplete()
            {
                if (E1count == 1 && E2count == 1 && E3count == 1)
                {
                    this.TCS.SetResult(true);
                }
            }
        }

        class E1 : Event
        { }
        class E2 : E1
        { }

        [Fact]
        public void TestEventInheritanceRun()
        {
            var tcs = new TaskCompletionSource<bool>();
            var runtime = new ProductionRuntime();
            var a = runtime.CreateMachine(typeof(A), null, new A.Configure(tcs), null);
            runtime.SendEvent(a, new A.E3());
            runtime.SendEvent(a, new E1());
            runtime.SendEvent(a, new E2());
            Assert.True(tcs.Task.Wait(2000), "Test timed out");
        }
    }
}
