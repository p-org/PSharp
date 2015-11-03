using System;
using Microsoft.PSharp;

namespace Raft
{
    internal class Environment : Machine
    {
		[Start]
        class Init : MachineState { }
    }
}
