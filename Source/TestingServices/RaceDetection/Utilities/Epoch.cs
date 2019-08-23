// ------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.PSharp.TestingServices.RaceDetection.Util
{
    /// <summary>
    /// The epochs used by the FastTrack algorithm.
    /// Adapted from https://github.com/stephenfreund/RoadRunner/blob/master/src/tools/util/Epoch.java
    /// </summary>
    public static class Epoch
    {
        private const int TidBits = 8;

        private const int ClockBits = 64 - TidBits;

        private const long MaxClock = (((long)1) << ClockBits) - 1;

        private const long MaxMId = ((long)1 << TidBits) - 1;

        /// <summary>
        /// The zero epoch.
        /// </summary>
        public const long Zero = 0;

        /// <summary>
        /// Denotes if the epoch represents a read shared variable,
        /// in which case we switch to full-width vector clocks.
        /// </summary>
        public const long ReadShared = -1;

        /// <summary>
        /// Returns the MId for an epoch.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long MId(long epoch) => epoch >> ClockBits;

        /// <summary>
        /// Returns the Clock for an epoch.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Clock(long epoch)
        {
            return epoch & MaxClock;
        }

        /// <summary>
        /// Obtains an epoch c@t from machine identifier t and clock c.
        /// </summary>
        public static long MakeEpoch(long mId, long clock)
        {
            Debug.Assert(mId <= MaxMId - 1, "Epoch tid overflow");
            return (mId << ClockBits) | clock;
        }

        /// <summary>
        /// Obtains an epoch c@t from machineId t = mId.Value and clock c.
        /// </summary>
        public static long MakeEpoch(MachineId mId, long clock)
        {
            return MakeEpoch((long)mId.Value, clock);
        }

        /// <summary>
        /// Increments the clock for this epoch by 1.
        /// </summary>
        public static long Tick(long epoch)
        {
            Debug.Assert(Clock(epoch) <= MaxClock - 1, "Epoch clock overflow");
            return epoch + 1;
        }

        /// <summary>
        /// Increments the clock for this epoch by amount.
        /// </summary>
        public static long Tick(long epoch, long amount)
        {
            Debug.Assert(Clock(epoch) <= MaxClock - amount, "Epoch clock overflow");
            return epoch + amount;
        }

        /// <summary>
        /// Checks if this epoch occurs earlier than the other.
        /// </summary>
        public static bool Leq(long e1, long e2)
        {
            Debug.Assert(MId(e1) == MId(e2), "Comparing epochs across different machines");
            return Clock(e1) <= Clock(e2);
        }

        /// <summary>
        /// Returns the epoch that is the join of the two passed in.
        /// </summary>
        public static long Max(long e1, long e2)
        {
            Debug.Assert(MId(e1) == MId(e2), "Joining epochs across different machines");
            return MakeEpoch(MId(e1), Math.Max(Clock(e1), Clock(e2)));
        }

        /// <summary>
        /// Following the FastTrack convention, represent an epoch as
        /// m:c where m is the machine Id and c is its clock.
        /// </summary>
        public static string ToString(long epoch)
        {
            if (epoch == ReadShared)
            {
                return "SHARED";
            }
            else
            {
                return string.Format("{0}:{1}", MId(epoch), Clock(epoch));
            }
        }
    }
}
