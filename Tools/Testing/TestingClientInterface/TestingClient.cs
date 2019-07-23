﻿// ------------------------------------------------------------------------------------------------
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

        internal AbstractStrategyController Controller;

        public TestingClient(AbstractStrategyController controller)
        {
            this.Controller = controller;
        }

        public void Initialize()
        {
            this.TestingEngine = TestingEngineFactory.CreateBugFindingEngine(this.Controller.Configuration);
            ((this.TestingEngine as BugFindingEngine).Strategy as ControlUnitStrategy).Initialize(this.Controller);
        }

        public void Run()
        {
            this.Initialize();
            // Parses the command line options to get the configuration.
            this.TestingEngine.Run();
            IO.Output.WriteLine(this.Controller.GetReport());

            // k
        }
    }
}
