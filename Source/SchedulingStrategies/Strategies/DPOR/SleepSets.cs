// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.PSharp.TestingServices.SchedulingStrategies.DPOR
{
    /// <summary>
    /// Sleep sets is a reduction technique that can be in addition to DPOR
    /// or on its own.
    /// </summary>
    internal static class SleepSets
    {
        /// <summary>
        /// Update the sleep sets for the top operation on the stack.
        /// This will look at the second from top element in the stack
        /// and copy forward the sleep set, excluding threads that are dependent
        /// with the executed operation.
        /// </summary>
        /// <param name="stack">Stack</param>
        /// <param name="contract">IContract</param>
        public static void UpdateSleepSets(Stack stack, IContract contract)
        {
            if (stack.GetNumSteps() <= 1)
            {
                return;
            }

            TidEntryList prevTop = stack.GetSecondFromTop();
            TidEntry prevSelected = prevTop.List[prevTop.GetSelected(contract)];

            TidEntryList currTop = stack.GetTop();

            // For each thread on the top of stack (except previously selected thread and new threads):
            //   if thread was slept previously 
            //   and thread's op was independent with selected op then:
            //     the thread is still slept.
            //   else: not slept.

            for (int i = 0; i < prevTop.List.Count; i++)
            {
                if (i == prevSelected.Id)
                {
                    continue;
                }
                if (prevTop.List[i].Sleep && !IsDependent(prevTop.List[i], prevSelected))
                {
                    currTop.List[i].Sleep = true;
                }
            }
        }

        /// <summary>
        /// Used to test if two operations are dependent.
        /// However, it is not perfect and it assumes we are only checking
        /// co-enabled operations from the same scheduling point.
        /// Thus, the following will always appear to be independent,
        /// even though this is not always the case:
        /// Create and Start, Send and Receive.
        /// </summary>
        public static bool IsDependent(TidEntry a, TidEntry b)
        {
            // This method will not detect the dependency between 
            // Create and Start (because Create's target id is always -1), 
            // but this is probably fine because we will never be checking that;
            // we only check enabled ops against other enabled ops.
            // Similarly, we assume that Send and Receive will always be independent
            // because the Send would need to enable the Receive to be dependent.
            // Receives are independent as they will always be from different threads,
            // but they should always have different target ids anyway.

            if (
                a.TargetId != b.TargetId || 
                a.TargetType != b.TargetType || 
                a.TargetId == -1 || 
                b.TargetId == -1)
            {
                return false;
            }

            // Same target:

            if (a.TargetType == OperationTargetType.Inbox)
            {
                return a.OpType == OperationType.Send && b.OpType == OperationType.Send;
            }

            return true;
        }
    }
}