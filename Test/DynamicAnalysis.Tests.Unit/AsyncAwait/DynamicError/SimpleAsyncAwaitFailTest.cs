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

            Configuration.SuppressTrace = true;
            Configuration.Verbose = 2;
            Configuration.SchedulingIterations = 2;
            Configuration.SchedulingStrategy = "dfs";

            var parser = new CSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), true);
            var program = parser.Parse();
            program.Rewrite();

            var assembly = base.GetAssembly(program.GetSyntaxTree());
            AnalysisContext.Create(assembly);

            SCTEngine.Setup();
            SCTEngine.Run();

            var bugReport = "Value is '3' (expected less than '3').";

            Assert.AreEqual(bugReport, SCTEngine.BugReport);
        }
    }
}
