﻿// ------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Microsoft.PSharp.Utilities
{
    /// <summary>
    /// The P# profiler.
    /// </summary>
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
            if (this.StopWatch != null)
            {
                this.StopWatch.Stop();
            }
        }

        /// <summary>
        /// Returns profilling results.
        /// </summary>
        public double Results() =>
            this.StopWatch != null ? this.StopWatch.Elapsed.TotalSeconds : 0;
    }
}
