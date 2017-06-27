//-----------------------------------------------------------------------
// <copyright file="SleepSets.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------


namespace Microsoft.PSharp.TestingServices.Scheduling
{
    /// <summary>
    /// Sleep sets is a reduction technique that can be in addition to DPOR
    /// or on its own.
    /// </summary>
    public static class SleepSets
    {
        /// <summary>
        /// Update the sleep sets for the top operation on the stack.
        /// This will look at the second from top element in the stack
        /// and copy forward the sleep set, excluding threads that are dependent
        /// with the executed operation.
        /// </summary>
        /// <param name="stack"></param>
        /// <param name="asserter">IAsserter</param>
        public static void UpdateSleepSets(Stack stack, IAsserter asserter)
        {
            if (stack.GetNumSteps() <= 1)
            {
                return;
            }

            TidEntryList prevTop = stack.GetSecondFromTop();
            TidEntry prevSelected = prevTop.List[prevTop.GetSelected(asserter)];

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
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
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