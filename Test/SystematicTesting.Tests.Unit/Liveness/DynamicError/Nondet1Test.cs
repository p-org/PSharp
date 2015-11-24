//-----------------------------------------------------------------------
// <copyright file="Nondet1Test.cs" company="Microsoft">
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
using System.Collections.Generic;

namespace Microsoft.PSharp.SystematicTesting.Tests.Unit
{
    [TestClass]
    public class Nondet1Test : BasePSharpTest
    {
        class Unit : Event { }
        class UserEvent : Event { }
        class Done : Event { }
        class Loop : Event { }
        class Waiting : Event { }
        class Computing : Event { }

        class EventHandler : Machine
        {
            List<MachineId> Workers;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Unit), typeof(WaitForUser))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.CreateMonitor(typeof(WatchDog));
                this.Raise(new Unit());
            }

            [OnEntry(nameof(WaitForUserOnEntry))]
            [OnEventGotoState(typeof(UserEvent), typeof(HandleEvent))]
            class WaitForUser : MachineState { }

            void WaitForUserOnEntry()
            {
                this.Monitor<WatchDog>(new Waiting());
                this.Send(this.Id, new UserEvent());
            }

            [OnEntry(nameof(HandleEventOnEntry))]
            [OnEventGotoState(typeof(Done), typeof(WaitForUser))]
            [OnEventGotoState(typeof(Loop), typeof(HandleEvent))]
            class HandleEvent : MachineState { }

            void HandleEventOnEntry()
            {
                this.Monitor<WatchDog>(new Computing());
                if (this.Random())
                {
                    this.Send(this.Id, new Done());
                }
                else
                {
                    this.Send(this.Id, new Loop());
                }
            }
        }

        class WatchDog : Monitor
        {
            List<MachineId> Workers;

            [Start]
            [Cold]
            [OnEventGotoState(typeof(Waiting), typeof(CanGetUserInput))]
            [OnEventGotoState(typeof(Computing), typeof(CannotGetUserInput))]
            class CanGetUserInput : MonitorState { }

            [Hot]
            [OnEventGotoState(typeof(Waiting), typeof(CanGetUserInput))]
            [OnEventGotoState(typeof(Computing), typeof(CannotGetUserInput))]
            class CannotGetUserInput : MonitorState { }
        }

        public static class TestProgram
        {
            [Test]
            public static void Execute(PSharpRuntime runtime)
            {
                runtime.CreateMachine(typeof(EventHandler));
            }
        }

        [TestMethod]
        public void TestNondet1()
        {
            var configuration = Configuration.Create();
            configuration.SuppressTrace = true;
            configuration.Verbose = 3;
            configuration.CheckLiveness = true;
            configuration.CacheProgramState = true;
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;

            Output.Debugging = true;

            var engine = SCTEngine.Create(configuration, TestProgram.Execute).Run();
            var bugReport = "Monitor 'WatchDog' detected infinite execution that violates a liveness property.";
            Assert.AreEqual(bugReport, engine.BugReport);
        }
    }
}
