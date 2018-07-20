﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ServiceFabric;

namespace PingPong
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Diagnostics.Debugger.Launch();

            var stateManager = new StateManagerMock(null);
            stateManager.DisallowFailures();

            var config = Configuration.Create().WithVerbosityEnabled(2);
            var runtime = ServiceFabricRuntimeFactory.Create(stateManager, config);
            runtime.CreateMachine(typeof(PingMachine));

            Console.ReadLine();
        }

    }
}
