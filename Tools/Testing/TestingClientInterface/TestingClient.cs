// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.PSharp.TestingServices;
using Microsoft.PSharp.TestingServices.Scheduling.ClientInterface;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;

namespace Microsoft.PSharp.TestingClientInterface
{
    public class TestingClient
    {
        private ITestingEngine TestingEngine;
        private readonly Action<IMachineRuntime> TestAction;

        internal AbstractStrategyController Controller;

        public int NumBugsFound => this.TestingEngine?.TestReport?.NumOfFoundBugs ?? -1;

        public TestingClient(AbstractStrategyController controller, Action<IMachineRuntime> testAction = null)
        {
            this.Controller = controller;
            this.TestAction = testAction;

            int cnt = 0;
            cnt += (this.TestAction != null) ? 1 : 0;
            cnt += (this.Controller.Configuration.AssemblyToBeAnalyzed.Length > 0) ? 1 : 0;

            if ( cnt != 1)
            {
                throw new ArgumentException("Exactly one of Configuration.AssemblyToBeAnalyzed or TestAction must be set");
            }
        }

        public void Initialize()
        {
            if (this.TestAction != null)
            {
                this.TestingEngine = TestingEngineFactory.CreateBugFindingEngine(this.Controller.Configuration, this.TestAction);
            }
            else if (this.Controller.Configuration.AssemblyToBeAnalyzed.Length > 0 )
            {
                this.TestingEngine = TestingEngineFactory.CreateBugFindingEngine(this.Controller.Configuration);
            }
            else
            {
                throw new ArgumentException("Exactly one of Configuration.AssemblyToBeAnalyzed or TestAction must be set");
            }

            ((this.TestingEngine as BugFindingEngine).Strategy as ControlUnitStrategy).Initialize(this.Controller);
        }

        public void Run()
        {
            this.Initialize();
            // Parses the command line options to get the configuration.
            this.TestingEngine.Run();
        }
    }
}
