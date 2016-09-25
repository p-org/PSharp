using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeaderElection
{
    class Environment : Machine
    {
        #region states
        [Start]
        [OnEntry(nameof(OnInitEntry))]
        class Init : MachineState { }
        #endregion

        #region actions
        void OnInitEntry()
        {
            var leaderCountMachine = CreateMachine(typeof(LeaderCount_Machine));
            var node1 = CreateMachine(typeof(Node), new Node.Initialize(leaderCountMachine, 1));
            var node2 = CreateMachine(typeof(Node), new Node.Initialize(leaderCountMachine, 2));
            var node3 = CreateMachine(typeof(Node), new Node.Initialize(leaderCountMachine, 3));
            var node4 = CreateMachine(typeof(Node), new Node.Initialize(leaderCountMachine, 4));
            var node5 = CreateMachine(typeof(Node), new Node.Initialize(leaderCountMachine, 5));

            Send(node1, new Node.SetNeighbours(node2, node5));
            Send(node2, new Node.SetNeighbours(node3, node1));
            Send(node3, new Node.SetNeighbours(node4, node2));
            Send(node4, new Node.SetNeighbours(node5, node3));
            Send(node5, new Node.SetNeighbours(node1, node4));
        }
            #endregion
        }
    }
