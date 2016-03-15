//-----------------------------------------------------------------------
// <copyright file="BugRepro1Test.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
//      EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
//      OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
//      The example companies, organizations, products, domain names,
//      e-mail addresses, logos, people, places, and events depicted
//      herein are fictitious.  No association with any real company,
//      organization, product, domain name, email address, logo, person,
//      places, or events is intended or should be inferred.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.PSharp.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.PSharp.SystematicTesting.Tests.Unit
{
    [TestClass]
    public class BugRepro1Test
    {
        class Ping : Event
        {
            public int V;
            public Ping(int v) : base(1, -1) { this.V = v; }
        }

        class Success : Event { }

        class PING : Machine
        {
            int x;
            int y;

            [Start]
            [OnEntry(nameof(EntryPingInit))]
            [OnEventDoAction(typeof(Success), nameof(SuccessAction))]
            [OnEventDoAction(typeof(Ping), nameof(PingAction))]
            class PingInit : MachineState { }

            void EntryPingInit()
            {
                this.Raise(new Success());
            }

            void SuccessAction()
            {
                x = Func1(1, 1);
                this.Assert(x == 2);
                y = Func2(x); // x == 2
            }

            void PingAction()
            {
                this.Assert(x == 4);
                x = x + 1;
                this.Assert(x == 5);
            }

            // i: value passed; j: identifies caller (1: Success handler;  2: Func2)
            int Func1(int i, int j)
            {
                if (j == 1)
                {
                    i = i + 1; // i: 2
                }

                if (j == 2)
                {
                    this.Assert(i == 3);
                    i = i + 1;
                    this.Assert(i == 4);
                    this.Send(this.Id, new Ping(i));
                    this.Assert(i == 4);
                }

                return i;
            }

            int Func2(int v)
            {
                v = v + 1;
                this.Assert(v == 3);
                x = Func1(v, 2);
                this.Assert(x == 4);
                return v;
            }
        }

        public static class TestProgram
        {
            [Test]
            public static void Execute(PSharpRuntime runtime)
            {
                runtime.CreateMachine(typeof(PING));
            }
        }

        [TestMethod]
        public void TestBugRepro1()
        {
            var configuration = Configuration.Create();
            configuration.SuppressTrace = true;
            configuration.Verbose = 2;

            var engine = TestingEngine.Create(configuration, TestProgram.Execute).Run();
            Assert.AreEqual(0, engine.NumOfFoundBugs);
        }
    }
}
