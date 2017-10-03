using Microsoft.PSharp;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Core.Utilities.Profiling
{
    /// <summary>
    /// TBD
    /// </summary>
    public class CriticalPathProfiler : ICriticalPathProfiler
    {
        /// <summary>
        /// Record when we started the critical path profiler
        /// </summary>
        internal long StartTime;

        static ConcurrentDictionary<long, long> LongestPathTimeAtSend;

        /// <summary>
        /// Stopwatch.GetTimestamp gets the current number of ticks in the timer mechanism.
        /// The scaling factor helps convert this to elapsed milliseconds
        /// </summary>
        static long ScalingFactor;

        static CriticalPathProfiler()
        {
            ScalingFactor = Stopwatch.Frequency / 1000L;
            LongestPathTimeAtSend = new ConcurrentDictionary<long, long>();
        }

        /// <summary>
        /// TBD
        /// </summary>
        public Configuration Configuration { get; set; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="machine"></param>
        /// <param name="currentStateName"></param>
        /// <param name="actionName"></param>
        public void OnActionEnter(Machine machine, string currentStateName, string actionName)
        {
            if (Configuration.EnableCriticalPathProfiling)
            {
                machine.LocalWatch.Start();
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="machine"></param>
        /// <param name="currentStateName"></param>
        /// <param name="actionName"></param>
        public void OnActionExit(Machine machine, string currentStateName, string actionName)
        {
            if (Configuration.EnableCriticalPathProfiling)
            {
                machine.LocalWatch.Stop();
                machine.LongestPathTime += (machine.LocalWatch.ElapsedMilliseconds);
                machine.LocalWatch.Reset();
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="child"></param>
        public void OnCreateMachine(Machine parent, Machine child)
        {
            if (Configuration.EnableCriticalPathProfiling)
            {
                if (parent == null)
                {
                    child.LongestPathTime = (this.StartTime - Stopwatch.GetTimestamp()) / ScalingFactor;
                }
                else
                {
                    child.LongestPathTime = parent.LongestPathTime;
                }
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="machine"></param>
        /// <param name="eventSequenceNumber"></param>
        public void OnDequeue(Machine machine, long eventSequenceNumber)
        {
            if (Configuration.EnableCriticalPathProfiling)
            {
                machine.LongestPathTime += machine.LocalWatch.ElapsedMilliseconds;
                long senderLongestPathTime = 0;
                if (!LongestPathTimeAtSend.TryGetValue(eventSequenceNumber, out senderLongestPathTime))
                {
                    throw new Exception("Failed to retrieve key");
                }
                machine.LongestPathTime = Math.Max(senderLongestPathTime, machine.LongestPathTime);
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="machine"></param>
        public void OnHalt(Machine machine)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="machine"></param>
        /// <param name="currentStateName"></param>
        /// <param name="eventName"></param>
        public void OnReceiveBegin(Machine machine, string currentStateName, string eventName)
        {
            if (Configuration.EnableCriticalPathProfiling)
            {
                machine.IdleTimeStart = machine.LocalWatch.ElapsedMilliseconds;
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="machine"></param>
        /// <param name="currentStateName"></param>
        /// <param name="eventNames"></param>
        /// <param name="wasBlocked"></param>
        /// <param name="eventSequenceNumber"></param>
        public void OnReceiveEnd(Machine machine, string currentStateName, string eventNames, bool wasBlocked, long eventSequenceNumber)
        {
            if (Configuration.EnableCriticalPathProfiling)
            {
                machine.IdleTime += (machine.LocalWatch.ElapsedMilliseconds - machine.IdleTimeStart);
                machine.LongestPathTime += machine.LocalWatch.ElapsedMilliseconds;
                long senderLongestPathTime = 0;
                if(!LongestPathTimeAtSend.TryGetValue(eventSequenceNumber, out senderLongestPathTime))
                {
                    throw new Exception("Failed to retrieve key");
                }
                machine.LongestPathTime = Math.Max(senderLongestPathTime, machine.LongestPathTime);
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="source"></param>
        /// <param name="eventSequenceNumber"></param>
        public void OnSend(Machine source, long eventSequenceNumber)
        {
            if (Configuration.EnableCriticalPathProfiling)
            {
                var currentTimeStamp = source.LongestPathTime + source.LocalWatch.ElapsedMilliseconds;
                LongestPathTimeAtSend.TryAdd(eventSequenceNumber, currentTimeStamp);
            }
        }
       
        /// <summary>
        /// TBD
        /// </summary>
        public void StartCriticalPathProfiling()
        {
            this.StartTime = Stopwatch.GetTimestamp();
        }

        /// <summary>
        /// TBD
        /// </summary>
        public void StopCriticalPathProfiling()
        {
            throw new NotImplementedException();
        }

    }
}