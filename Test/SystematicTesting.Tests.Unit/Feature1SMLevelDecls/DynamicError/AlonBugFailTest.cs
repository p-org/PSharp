//-----------------------------------------------------------------------
// <copyright file="AlonBugFailTest.cs" company="Microsoft">
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
    public class AlonBugFailTest
    {
        class E : Event { }

        class Program : Machine
        {
            int i;

            [Start]
            [OnEntry(nameof(EntryInit))]
            [OnExit(nameof(ExitInit))]
            [OnEventGotoState(typeof(E), typeof(Call))] // Exit executes before this transition.
            class Init : MachineState { }

            void EntryInit()
            {
                i = 0;
                this.Raise(new E());
            }

            void ExitInit()
            {
                // This assert is reachable.
                this.Assert(false, "Bug found.");
            }

            [OnEntry(nameof(EntryCall))]
            class Call : MachineState { }

            void EntryCall()
            {
                if (i == 3)
                {
                    this.Pop();
                    return; // important if not compiling
                }
                else
                {
                    i = i + 1;
                }
            }
        }

        public static class TestProgram
        {
            [Test]
            public static void Execute(PSharpRuntime runtime)
            {
                runtime.CreateMachine(typeof(Program));
            }
        }

        [TestMethod]
        public void TestAlonBugAssertionFailure()
        {
            var configuration = Configuration.Create();
            configuration.SuppressTrace = true;
            configuration.Verbose = 2;

            var engine = TestingEngine.Create(configuration, TestProgram.Execute).Run();
            Assert.AreEqual(1, engine.NumOfFoundBugs);
        }
    }
}
