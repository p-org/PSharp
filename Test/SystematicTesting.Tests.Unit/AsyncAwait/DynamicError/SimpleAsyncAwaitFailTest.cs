//-----------------------------------------------------------------------
// <copyright file="SimpleAsyncAwaitFailTest.cs" company="Microsoft">
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
using System.Threading.Tasks;

namespace Microsoft.PSharp.SystematicTesting.Tests.Unit
{
    [TestClass]
    public class SimpleAsyncAwaitFailTest : BasePSharpTest
    {
        class Unit : Event { }

        internal class TaskCreator : Machine
        {
            int Value;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Unit), typeof(Active))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Value = 0;
                this.Raise(new Unit());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            class Active : MachineState { }

            void ActiveOnEntry()
            {
                Process();
                this.Assert(this.Value < 3, "Value is '{0}' (expected less than '3').", this.Value);
            }

            async void Process()
            {
                Task t = Increment();
                this.Value++;
                await t;
                this.Value++;
            }

            Task Increment()
            {
                Task t = new Task(() => {
                    this.Value++;
                });

                t.Start();
                return t;
            }
        }

        public static class TestProgram
        {
            [Test]
            public static void Execute(PSharpRuntime runtime)
            {
                runtime.CreateMachine(typeof(TaskCreator));
            }
        }

        [TestMethod]
        public void TestSimpleAsyncAwaitFail()
        {
            var configuration = Configuration.Create();
            configuration.SuppressTrace = true;
            configuration.Verbose = 2;
            configuration.SchedulingIterations = 2;
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;
            configuration.ScheduleIntraMachineConcurrency = true;

            var engine = TestingEngine.Create(configuration, TestProgram.Execute).Run();
            var bugReport = "Value is '3' (expected less than '3').";
            Assert.AreEqual(bugReport, engine.BugReport);
        }
    }
}
