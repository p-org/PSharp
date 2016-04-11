//-----------------------------------------------------------------------
// <copyright file="Profiler.cs">
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

using System.Diagnostics;

namespace Microsoft.PSharp.Utilities
{
    public sealed class Profiler
    {
        private Stopwatch StopWatch = null;
        
        /// <summary>
        /// Starts measuring execution time.
        /// </summary>
        public void StartMeasuringExecutionTime()
        {
            this.StopWatch = new Stopwatch();
            this.StopWatch.Start();
        }

        /// <summary>
        /// Stops measuring execution time.
        /// </summary>
        public void StopMeasuringExecutionTime()
        {
            this.StopWatch.Stop();
        }

        /// <summary>
        /// Returns profilling results.
        /// </summary>
        /// <returns>Seconds</returns>
        public double Results()
        {
            return this.StopWatch.Elapsed.TotalSeconds;
        }
    }
}
