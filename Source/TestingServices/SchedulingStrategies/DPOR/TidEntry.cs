//-----------------------------------------------------------------------
// <copyright file="TidEntry.cs">
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

using System.Collections.Generic;

namespace Microsoft.PSharp.TestingServices.Scheduling
{
    /// <summary>
    /// Thread entry stored on the stack of a depth-first search to track which threads existed
    /// and whether they have been executed already, etc.
    /// </summary>
    public class TidEntry
    {
        /// <summary>
        /// The id/index of this thread in the original thread creation order list of threads.
        /// </summary>
        public int Id;

        /// <summary>
        /// Is the thread enabled?
        /// </summary>
        public bool Enabled;

        /// <summary>
        /// Skip exploring this thread from here.
        /// </summary>
        public bool Sleep;

        /// <summary>
        /// Backtrack to this transition?
        /// </summary>
        public bool Backtrack;

        /// <summary>
        /// Operation type.
        /// </summary>
        public OperationType OpType;

        /// <summary>
        /// Target type. E.g. thread, queue, mutex, variable.
        /// </summary>
        public OperationTargetType TargetType;

        /// <summary>
        /// Target of the operation.
        /// </summary>
        public int TargetId;

        /// <summary>
        /// For a receive operation: the step of the corresponding send.
        /// </summary>
        public int SendStepIndex;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="enabled"></param>
        /// <param name="opType"></param>
        /// <param name="targetType"></param>
        /// <param name="targetId"></param>
        /// <param name="sendStepIndex"></param>
        public TidEntry(int id, bool enabled, OperationType opType, OperationTargetType targetType, int targetId, int sendStepIndex)
        {
            Id = id;
            Enabled = enabled;
            Sleep = false;
            Backtrack = false;
            OpType = opType;
            TargetType = targetType;
            TargetId = targetId;
            SendStepIndex = sendStepIndex;


        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly Comparer ComparerSingleton = new Comparer();

        /// <summary>
        /// 
        /// </summary>
        public class Comparer : IEqualityComparer<TidEntry>
        {
            #region Implementation of IEqualityComparer<in Comparer>

            /// <summary>Determines whether the specified objects are equal.</summary>
            /// <returns>true if the specified objects are equal; otherwise, false.</returns>
            /// <param name="x">The first object to compare.</param>
            /// <param name="y">The second object to compare.</param>
            public bool Equals(TidEntry x, TidEntry y)
            {
                return
                    x.OpType == y.OpType &&
                    (x.OpType == OperationType.Yield || x.Enabled == y.Enabled) &&
                    x.Id == y.Id &&
                    x.TargetId == y.TargetId &&
                    x.TargetType == y.TargetType;
            }

            /// <summary>Returns a hash code for the specified object.</summary>
            /// <returns>A hash code for the specified object.</returns>
            /// <param name="obj">The <see cref="T:System.Object" /> for which a hash code is to be returned.</param>
            /// <exception cref="T:System.ArgumentNullException">The type of <paramref name="obj" /> is a reference type and <paramref name="obj" /> is null.</exception>
            public int GetHashCode(TidEntry obj)
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 23 + obj.Id.GetHashCode();
                    hash = hash * 23 + obj.OpType.GetHashCode();
                    hash = hash * 23 + obj.TargetId.GetHashCode();
                    hash = hash * 23 + obj.TargetType.GetHashCode();
                    hash = hash * 23 + (obj.OpType == OperationType.Yield ? true.GetHashCode() : obj.Enabled.GetHashCode());
                    return hash;
                }
            }

            #endregion
        }

    }
}