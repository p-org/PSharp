//-----------------------------------------------------------------------
// <copyright file="HotStateTest.cs" company="Microsoft">
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

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.PSharp.LanguageServices;
using Microsoft.PSharp.LanguageServices.Parsing;
using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.DynamicAnalysis.Tests.Unit
{
    [TestClass]
    public class HotStateTest : BasePSharpTest
    {
        [TestMethod]
        public void TestHotStateMonitor()
        {
            var test = @"
using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace SystematicTesting
{
    class Unit : Event { }
    class DoProcessing : Event { }
    class FinishedProcessing : Event { }
    class NotifyWorkerIsDone : Event { }

    class Master : Machine
    {
        List<MachineId> Workers;

		[Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(Active))]
        class Init : MachineState { }

		void InitOnEntry()
        {
            this.Workers = new List<MachineId>();

            for (int idx = 0; idx < 3; idx++)
            {
                var worker = this.CreateMachine(typeof(Worker), this.Id);
                this.Workers.Add(worker);
            }

            this.CreateMonitor(typeof(M), this.Workers);
            
            this.Raise(new Unit());
        }

        [OnEntry(nameof(ActiveOnEntry))]
        [OnEventDoAction(typeof(FinishedProcessing), nameof(ProcessWorkerIsDone))]
        class Active : MachineState { }

        void ActiveOnEntry()
        {
            foreach (var worker in this.Workers)
            {
                this.Send(worker, new DoProcessing());
            }
        }

        void ProcessWorkerIsDone()
        {
            this.Monitor<M>(new NotifyWorkerIsDone());
        }
    }

    class Worker : Machine
    {
        MachineId Master;

		[Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(Processing))]
        class Init : MachineState { }

		void InitOnEntry()
        {
            this.Master = (MachineId)this.Payload;
            this.Raise(new Unit());
        }
        
        [OnEventGotoState(typeof(DoProcessing), typeof(Done))]
        class Processing : MachineState { }

        [OnEntry(nameof(DoneOnEntry))]
        class Done : MachineState { }

        void DoneOnEntry()
        {
            if (this.Nondet())
            {
                this.Send(this.Master, new FinishedProcessing());
            }
            
            this.Raise(new Halt());
        }
    }

    class M : Monitor
    {
        List<MachineId> Workers;

        [Start]
        [Hot]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(Done))]
        [OnEventDoAction(typeof(NotifyWorkerIsDone), nameof(ProcessNotification))]
        class Init : MonitorState { }

        void InitOnEntry()
        {
            this.Workers = (List<MachineId>)this.Payload;
        }

        void ProcessNotification()
        {
            this.Workers.RemoveAt(0);

            if (this.Workers.Count == 0)
            {
                this.Raise(new Unit());
            }
        }
        
        class Done : MonitorState { }
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
            PSharpRuntime.CreateMachine(typeof(Master));
        }
    }
}";

            var parser = new CSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), true);
            var program = parser.Parse();
            program.Rewrite();

            var sctConfig = new DynamicAnalysisConfiguration();
            sctConfig.SuppressTrace = true;
            sctConfig.Verbose = 2;
            sctConfig.CheckLiveness = true;
            sctConfig.SchedulingStrategy = SchedulingStrategy.DFS;

            Output.Debugging = true;

            var assembly = base.GetAssembly(program.GetSyntaxTree());
            var context = AnalysisContext.Create(sctConfig, assembly);
            var sctEngine = SCTEngine.Create(context).Run();

            var bugReport = "Monitor 'M' detected liveness property violation in hot state 'Init'.";

            Assert.AreEqual(bugReport, sctEngine.BugReport);
        }
    }
}
