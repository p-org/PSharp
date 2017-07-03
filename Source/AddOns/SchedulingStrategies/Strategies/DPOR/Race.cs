//-----------------------------------------------------------------------
// <copyright file="Race.cs">
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
    /// Represents a race (two visible operation that are concurrent 
    /// but dependent) that can be reversed to reach a different terminal state.
    /// </summary>
    public class Race
    {
        /// <summary>
        /// The index of the first racing visible operation.
        /// </summary>
        public int A;

        /// <summary>
        /// The index of the second racing visible operation.
        /// </summary>
        public int B;

        /// <summary>
        /// Construct a race.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public Race(int a, int b)
        {
            A = a;
            B = b;
        }
    }
}