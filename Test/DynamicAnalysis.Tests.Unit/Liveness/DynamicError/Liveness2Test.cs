//-----------------------------------------------------------------------
// <copyright file="Liveness2Test.cs" company="Microsoft">
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

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.PSharp.LanguageServices;
using Microsoft.PSharp.LanguageServices.Parsing;
using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.DynamicAnalysis.Tests.Unit
{
    [TestClass]
    public class Liveness2Test : BasePSharpTest
    {
        [TestMethod]
        public void TestLiveness2()
        {
            var test = @"
using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace SystematicTesting
{
    class Unit : Event { }
    class UserEvent : Event { }
    class Done : Event { }
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
        [OnEventGotoState(typeof(Done), typeof(HandleEvent))]
        class HandleEvent : MachineState { }

        void HandleEventOnEntry()
        {
            this.Monitor<WatchDog>(new Computing());
            this.Send(this.Id, new Done());
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
        public static void Main(string[] args)
        {
            TestProgram.Execute();
            Console.ReadLine();
        }

        [Test]
        public static void Execute()
        {
            PSharpRuntime.CreateMachine(typeof(EventHandler));
        }
    }
}";
            
            var parserConfig = new LanguageServicesConfiguration();
            var parser = new CSharpParser(new PSharpProject(parserConfig),
                SyntaxFactory.ParseSyntaxTree(test), true);
            var program = parser.Parse();
            program.Rewrite();

            var sctConfig = new DynamicAnalysisConfiguration();
            sctConfig.SuppressTrace = true;
            sctConfig.Verbose = 2;
            sctConfig.CheckLiveness = true;
            sctConfig.SchedulingStrategy = SchedulingStrategy.DFS;

            var assembly = base.GetAssembly(program.GetSyntaxTree());
            var context = AnalysisContext.Create(sctConfig, assembly);
            var sctEngine = SCTEngine.Create(context).Run();

            var bugReport = "Monitor 'WatchDog' detected infinite execution that violates a liveness property.";

            Assert.AreEqual(bugReport, sctEngine.BugReport);
        }
    }
}
