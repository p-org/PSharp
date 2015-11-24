//-----------------------------------------------------------------------
// <copyright file="SEMTwoMachines15Test.cs" company="Microsoft">
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
    public class SEMTwoMachines15Test : BasePSharpTest
    {
        class Config : Event
        {
            public bool Value;
            public Config(bool v) : base(1, -1) { this.Value = v; }
        }

        class Real1 : Machine
        {
            bool test = false;

            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                this.CreateMonitor(typeof(M));
                this.Monitor<M>(new Config(test));
            }
        }

        class M : Monitor
        {
            [Start]
            [OnEntry(nameof(EntryX))]
            [OnEventDoAction(typeof(Config), nameof(Configure))]
            class X : MonitorState { }

            void EntryX() { }

            void Configure()
            {
                this.Assert((this.ReceivedEvent as Config).Value == false); // passes
            }
        }

        public static class TestProgram
        {
            [Test]
            public static void Execute(PSharpRuntime runtime)
            {
                runtime.CreateMachine(typeof(Real1));
            }
        }

        [TestMethod]
        public void TestNewMonitor3()
        {
            var configuration = Configuration.Create();
            configuration.SuppressTrace = true;
            configuration.Verbose = 2;
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;

            var engine = TestingEngine.Create(configuration, TestProgram.Execute).Run();
            Assert.AreEqual(0, engine.NumOfFoundBugs);
        }
    }
}
