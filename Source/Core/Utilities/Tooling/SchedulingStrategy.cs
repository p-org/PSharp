//-----------------------------------------------------------------------
// <copyright file="SchedulingStrategy.cs">
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

using System.Runtime.Serialization;

namespace Microsoft.PSharp.Utilities
{
    /// <summary>
    /// P# runtime scheduling strategy.
    /// </summary>
    [DataContract]
    public enum SchedulingStrategy
    {
        /// <summary>
        /// Interactive scheduling.
        /// </summary>
        [EnumMember(Value = "Interactive")]
        Interactive = 0,
        /// <summary>
        /// Replay scheduling.
        /// </summary>
        [EnumMember(Value = "Replay")]
        Replay,
        /// <summary>
        /// Portfolio scheduling.
        /// </summary>
        [EnumMember(Value = "Portfolio")]
        Portfolio,
        /// <summary>
        /// Random scheduling.
        /// </summary>
        [EnumMember(Value = "Random")]
        Random,
        /// <summary>
        /// Probabilistic random-walk scheduling.
        /// </summary>
        [EnumMember(Value = "ProbabilisticRandom")]
        ProbabilisticRandom,
        /// <summary>
        /// Prioritized scheduling.
        /// </summary>
        [EnumMember(Value = "PCT")]
        PCT,
        /// <summary>
        /// Prioritized scheduling with Random tail.
        /// </summary>
        [EnumMember(Value = "FairPCT")]
        FairPCT,
        /// <summary>
        /// Depth-first search scheduling.
        /// </summary>
        [EnumMember(Value = "DFS")]
        DFS,
        /// <summary>
        /// Depth-first search scheduling with
        /// iterative deepening.
        /// </summary>
        [EnumMember(Value = "IDDFS")]
        IDDFS,
        /// <summary>
        /// Dynamic partial-order reduction (DPOR) scheduling.
        /// </summary>
        [EnumMember(Value = "DPOR")]
        DPOR,
        /// <summary>
        /// Randomized dynamic partial-order reduction (rDPOR) scheduling.
        /// </summary>
        [EnumMember(Value = "rDPOR")]
        RDPOR,
        /// <summary>
        /// Delay-bounding scheduling.
        /// </summary>
        [EnumMember(Value = "DelayBounding")]
        DelayBounding,
        /// <summary>
        /// Random delay-bounding scheduling.
        /// </summary>
        [EnumMember(Value = "RandomDelayBounding")]
        RandomDelayBounding
    }
}
