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

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.PSharp.LanguageServices;
using Microsoft.PSharp.LanguageServices.Parsing;
using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.DynamicAnalysis.Tests.Unit
{
    [TestClass]
    public class SimpleAsyncAwaitFailTest : BasePSharpTest
    {
        [TestMethod]
        public void TestSimpleAsyncAwaitFail()
        {
            var test = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.PSharp;

namespace SystematicTesting
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
            this.Assert(this.Value < 3, ""Value is '{0}' (expected less than '3')."", this.Value);
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
        public static void Main(string[] args)
        {
            TestProgram.Execute();
            Console.ReadLine();
        }

        [Test]
        public static void Execute()
        {
            PSharpRuntime.CreateMachine(typeof(TaskCreator));
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
            sctConfig.SchedulingIterations = 2;
            sctConfig.SchedulingStrategy = SchedulingStrategy.DFS;
            sctConfig.ScheduleIntraMachineConcurrency = true;

            var assembly = base.GetAssembly(program.GetSyntaxTree());
            var context = AnalysisContext.Create(sctConfig, assembly);
            var sctEngine = SCTEngine.Create(context).Run();

            var bugReport = "Value is '3' (expected less than '3').";

            Assert.AreEqual(bugReport, sctEngine.BugReport);
        }
    }
}
