//-----------------------------------------------------------------------
// <copyright file="SendInterleavingsTest.cs" company="Microsoft">
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
    public class SendInterleavingsTest : BasePSharpTest
    {
        class Config : Event
        {
            public MachineId Id;
            public Config(MachineId id) : base(-1, -1) { this.Id = id; }
        }

        class Event1 : Event { }
        class Event2 : Event { }

        class Receiver : Machine
        {
            [Start]
            [OnEntry(nameof(Initialize))]
            [OnEventDoAction(typeof(Event1), nameof(OnEvent1))]
            [OnEventDoAction(typeof(Event2), nameof(OnEvent2))]
            class Init : MachineState { }

            int count1 = 0;
            void Initialize()
            {
                var s1 = CreateMachine(typeof(Sender1));
                this.Send(s1, new Config(this.Id));
                var s2 = CreateMachine(typeof(Sender2));
                this.Send(s2, new Config(this.Id));
            }

            void OnEvent1()
            {
                count1++;
            }
            void OnEvent2()
            {
                Assert(count1 != 1);
            }
        }

        class Sender1 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(Config), nameof(Run))]
            class State : MachineState { }

            void Run()
            {
                Send((this.ReceivedEvent as Config).Id, new Event1());
                Send((this.ReceivedEvent as Config).Id, new Event1());
            }
        }

        class Sender2 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(Config), nameof(Run))]
            class State : MachineState { }

            void Run()
            {
                Send((this.ReceivedEvent as Config).Id, new Event2());
            }
        }

        public static class TestProgram
        {
            [Test]
            public static void Execute(PSharpRuntime runtime)
            {
                runtime.CreateMachine(typeof(Receiver));
            }
        }

        [TestMethod]
        public void TestSendInterleavingsAssertionFailure()
        {
            var configuration = Configuration.Create();
            configuration.SuppressTrace = true;
            configuration.Verbose = 2;
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;
            configuration.SchedulingIterations = 19;

            var engine = TestingEngine.Create(configuration, TestProgram.Execute).Run();
            Assert.AreEqual(1, engine.NumOfFoundBugs);
        }
    }
}
