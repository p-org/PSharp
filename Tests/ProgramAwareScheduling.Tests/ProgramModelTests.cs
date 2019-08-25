// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.PSharp.TestingClientInterface;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware;
using Microsoft.PSharp.TestingServices.Scheduling;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;

using Xunit;

namespace Microsoft.PSharp.ProgramAwareScheduling.Tests
{
    public class ProgramModelTests
    {
        [Fact(Timeout = 5000)]
        public void TestCreateMachineWithEvent()
        {
            Action<IMachineRuntime> testAction = r =>
            {
                var aId = r.CreateMachine(typeof(ForwarderMachine), new ForwarderEvent());
            };

            AbstractBaseProgramModelStrategy strategy = new BasicProgramModelBasedStrategy(new RandomStrategy(0), false);
            TestingReporter reporter = new TestingReporter(strategy);

            Assert.True(SimpleTesterController.RunTest(testAction, strategy, reporter, 1, 0, true, 2), "The test encountered an unexpected error:\n" + SimpleTesterController.CaughtException);
        }

        [Fact(Timeout = 5000)]
        public void TestExplicitReceiveWithoutWaiting()
        {
            Action<IMachineRuntime> testActionReceiveWithoutWaiting = r =>
            {
                var blockReceiveMachine = r.CreateMachine(typeof(ExplicitReceiverMachine));
                r.SendEvent(blockReceiveMachine, new ForwarderEvent(blockReceiveMachine, new BreakLoopEvent())); // Signal to end
                r.SendEvent(blockReceiveMachine, new NaturallyReceivedEvent());
            };

            AbstractBaseProgramModelStrategy strategy = new BasicProgramModelBasedStrategy(new RandomStrategy(0), false);
            TestingReporter reporter = new TestingReporter(strategy);

            Assert.True(SimpleTesterController.RunTest(testActionReceiveWithoutWaiting, strategy, reporter, 1, 0, true, 2), "The test encountered an unexpected error:\n" + SimpleTesterController.CaughtException);

            ProgramStep root = reporter.ProgramSummary.PartialOrderRoot;
            // Verify the sequence of steps which the LoopedBlockingReceiveMachine takes.
            // Special -nm-> create -nm-> create -c-> start -nm-> receive -nm-> send -nm-> receive -nm-> start

            Assert.True(
                CheckTreePath(root, new Tuple<TreeEdgeType, ProgramStepType, AsyncOperationType>[]
                {
                                Tuple.Create(TreeEdgeType.MachineThread, ProgramStepType.SchedulableStep, AsyncOperationType.Create),
                                Tuple.Create(TreeEdgeType.MachineThread, ProgramStepType.SchedulableStep, AsyncOperationType.Send),
                                Tuple.Create(TreeEdgeType.Created, ProgramStepType.ExplicitReceiveComplete, AsyncOperationType.Receive)
                }),
                "The tree was not as expected - CreatedStep between send and explicit receive");

            Assert.True(
                CheckTreePath(root, new Tuple<TreeEdgeType, ProgramStepType, AsyncOperationType>[]
                {
                    Tuple.Create(TreeEdgeType.MachineThread, ProgramStepType.SchedulableStep, AsyncOperationType.Create),
                    Tuple.Create(TreeEdgeType.MachineThread, ProgramStepType.SchedulableStep, AsyncOperationType.Send),
                    Tuple.Create(TreeEdgeType.MachineThread, ProgramStepType.SchedulableStep, AsyncOperationType.Send),
                    Tuple.Create(TreeEdgeType.Created, ProgramStepType.SchedulableStep, AsyncOperationType.Receive),
                    Tuple.Create(TreeEdgeType.Inbox, ProgramStepType.ExplicitReceiveComplete, AsyncOperationType.Receive),
                }),
                "The tree was not as expected - InboxOrdering between natural & explicit receives is not set");
        }

        [Fact(Timeout = 5000)]
        public void TestExplicitReceiveWithWaiting()
        {
            Action<IMachineRuntime> testActionReceiveWithWaiting = r =>
            {
                var forwarderMachine = r.CreateMachine(typeof(ForwarderMachine));
                var blockReceiveMachine = r.CreateMachine(typeof(ExplicitReceiverMachine));

                r.SendEvent(blockReceiveMachine,
                    new ForwarderEvent(forwarderMachine, new ForwarderEvent(
                        blockReceiveMachine, new ForwarderEvent(blockReceiveMachine, new BreakLoopEvent()))));

                r.SendEvent(blockReceiveMachine, new NaturallyReceivedEvent());
            };

            AbstractBaseProgramModelStrategy strategy = new BasicProgramModelBasedStrategy(new RandomStrategy(0), false);
            TestingReporter reporter = new TestingReporter(strategy);

            Assert.True(SimpleTesterController.RunTest(testActionReceiveWithWaiting, strategy, reporter, 1, 0, true, 2), "The test encountered an unexpected error:\n" + SimpleTesterController.CaughtException);

            ProgramStep root = reporter.ProgramSummary.PartialOrderRoot;
            // Verify the sequence of steps which the LoopedBlockingReceiveMachine takes.
            // Special -nm-> create -nm-> create -c-> start -nm-> receive -nm-> send -nm-> receive -nm-> start

            Assert.True(
                CheckTreePath(root, new Tuple<TreeEdgeType, ProgramStepType, AsyncOperationType>[]
                {
                    Tuple.Create(TreeEdgeType.MachineThread, ProgramStepType.SchedulableStep, AsyncOperationType.Create),
                    Tuple.Create(TreeEdgeType.MachineThread, ProgramStepType.SchedulableStep, AsyncOperationType.Create),
                    Tuple.Create(TreeEdgeType.MachineThread, ProgramStepType.SchedulableStep, AsyncOperationType.Send), // runtime.send(receiver, fwdEvent)
                    Tuple.Create(TreeEdgeType.Created, ProgramStepType.ExplicitReceiveComplete, AsyncOperationType.Receive), // receiver.receive(fwdEvent)
                    Tuple.Create(TreeEdgeType.MachineThread, ProgramStepType.SchedulableStep, AsyncOperationType.Send), // receiver.send(forwarder, fwdEvent)
                    Tuple.Create(TreeEdgeType.Created, ProgramStepType.SchedulableStep, AsyncOperationType.Receive), // forwarder.receive(fwdEvent)
                    Tuple.Create(TreeEdgeType.MachineThread, ProgramStepType.SchedulableStep, AsyncOperationType.Send), // forwarder.send(receiver, fwdEvent)
                    Tuple.Create(TreeEdgeType.Created, ProgramStepType.ExplicitReceiveComplete, AsyncOperationType.Receive), // receiver.receive(fwdEvent) - This is with waiting
                }),
                "The tree was not as expected - Created step for explicit receive is not set");
        }

        private enum TreeEdgeType
        {
            MachineThread,
            Created,
            Inbox
        }

        private static bool CheckTreePath(ProgramStep root, Tuple<TreeEdgeType, ProgramStepType, AsyncOperationType>[] steps)
        {
            ProgramStep at = root;
            foreach (Tuple<TreeEdgeType, ProgramStepType, AsyncOperationType> step in steps)
            {
                ProgramStep nextNode = null;
                switch (step.Item1)
                {
                    case TreeEdgeType.MachineThread:
                        nextNode = at.NextMachineStep;
                        break;
                    case TreeEdgeType.Created:
                        nextNode = at.CreatedStep;
                        break;
                    case TreeEdgeType.Inbox:
                        nextNode = at.NextInboxOrderingStep;
                        break;
                }

                if (nextNode == null ||
                    nextNode.ProgramStepType != step.Item2 ||
                    ( nextNode.ProgramStepType == ProgramStepType.SchedulableStep && nextNode.OpType != step.Item3))
                {
                    return false;
                }

                at = nextNode;
            }

            return true;
        }
    }
}
