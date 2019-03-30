// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Text;

namespace Microsoft.PSharp.TestingServices.RaceDetection.Util
{
    /// <summary>
    /// The class representing vector clocks.
    /// Adapted from the roadrunner tool:
    /// "The ROADRUNNER Dynamic Analysis Framework for Concurrent Programs" by Flanagan and Freund in PASTE '10.
    /// (See: https://github.com/stephenfreund/RoadRunner/blob/master/src/tools/util/VectorClock.java)
    /// Maps each machine(Id) to its clock. The clock is represented as an Epoch c@t.
    /// Here, c is the clock value and t the identifier for the machine
    /// We re-encode the mId into the value so that comparisons between epochs and VCs
    /// are direct. Currently used by the TestingRuntime which does not
    /// run machines concurrently. Future (multi-threaded) clients will need to call the APIs with
    /// exclusive access to certain parameters/this (as indicated in the comments)
    /// </summary>
    internal class VectorClock
    {
        private long[] Values;

        /// <summary>
        /// Use for all VCs that start with an empty array.
        /// </summary>
        private readonly long[] Empty = Array.Empty<long>();

        /// <summary>
        /// Initializes a new instance of the <see cref="VectorClock"/> class.
        /// </summary>
        protected VectorClock()
        {
            this.Values = this.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VectorClock"/> class.
        /// </summary>
        public VectorClock(VectorClock other)
            : this(other.Size())
        {
            this.Copy(other);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VectorClock"/> class.
        /// </summary>
        public VectorClock(long size)
        {
            if (size > 0)
            {
                this.MakeVC(size);
            }
            else
            {
                this.Values = this.Empty;
            }
        }

        /// <summary>
        /// size >= 0.
        /// Requires exclusive access to this.
        /// </summary>
        public void MakeVC(long size)
        {
            this.Values = new long[size];
            ClearFrom(this.Values, 0);
        }

        /// <summary>
        /// Copies the other vector clock into this.
        /// Requires: exclusive access to this and other.
        /// </summary>
        public void Copy(VectorClock other)
        {
            long[] otherValues = other.Values;
            this.EnsureCapacity(otherValues.Length);
            long[] thisValues = this.Values;

            // n = this.values.Length, m = other.values.Length
            // n >= m
            for (int i = 0; i < otherValues.Length; i++)
            {
                thisValues[i] = otherValues[i];
            }

            // Handle m..n-1
            for (int i = otherValues.Length; i < thisValues.Length; i++)
            {
                thisValues[i] = Epoch.MakeEpoch(i, 0);
            }
        }

        /// <summary>
        /// this = this ⨆ other.
        /// Requires: exclusive access to this and other.
        /// </summary>
        public void Max(VectorClock other)
        {
            long[] otherValues = other.Values;
            this.EnsureCapacity(otherValues.Length);
            long[] thisValues = this.Values;

            for (int i = 0; i < otherValues.Length; i++)
            {
                if (Epoch.Leq(thisValues[i], otherValues[i]))
                {
                    thisValues[i] = otherValues[i];
                }
            }
        }

        /// <summary>
        /// Returns true if this ≤ other. Requires: exclusive access to this and other.
        /// </summary>
        public bool Leq(VectorClock other)
        {
            return !this.AnyGt(other);
        }

        /// <summary>
        /// Returns true if any clock in this.Values is greater than in other.Values.
        /// Requires: exclusive access to this and other.
        /// </summary>
        public bool AnyGt(VectorClock other)
        {
            long[] thisValues = this.Values;
            long[] otherValues = other.Values;

            int thisLen = thisValues.Length;
            int otherLen = otherValues.Length;
            int min = Math.Min(thisLen, otherLen);

            for (int i = 0; i < min; i++)
            {
                if (!Epoch.Leq(thisValues[i], otherValues[i]))
                {
                    return true;
                }
            }

            // Handle min..thisLen
            for (int i = min; i < thisLen; i++)
            {
                if (thisValues[i] != Epoch.MakeEpoch(i, 0))
                {
                    return true;
                }
            }

            // Handle thisLen..otherLen
            // Our values are t@0 -> so never greater than other values.
            return false;
        }

        /// <summary>
        /// Returns the first index i >= start such that this.Values[i] > other.Values[i],
        /// -1 is no such index exists.
        /// Requires: exclusive access to this and other.
        /// </summary>
        public int NextGT(VectorClock other, int start)
        {
            long[] thisValues = this.Values;
            int thisLen = thisValues.Length;

            if (start >= thisLen)
            {
                // this.Values has no non-0 epochs left.
                return -1;
            }

            long[] otherValues = other.Values;
            int otherLen = otherValues.Length;

            int min = Math.Min(thisLen, otherLen);

            int i = start;

            // Handle start..min
            for (; i < min; i++)
            {
                if (!Epoch.Leq(thisValues[i], otherValues[i]))
                {
                    return i;
                }
            }

            // Handle i..thisLen
            for (; i < thisLen; i++)
            {
                if (thisValues[i] != Epoch.MakeEpoch(i, 0))
                {
                    return i;
                }
            }

            // Handle thisLen..otherLen
            // Our values are t@0 -> so never greater than other values.
            return -1;
        }

        /// <summary>
        /// Increments the clock component for mId.
        /// Requires: exclusive access to this.
        /// </summary>
        public long Tick(long mID)
        {
            this.EnsureCapacity((int)mID + 1);
            long incremented = Epoch.Tick(this.Values[mID]);
            this.Values[mID] = incremented;
            return incremented;
        }

        /// <summary>
        /// Sets the clock component for mId to v.
        /// Here, v is an epoch, not a simple long.
        /// Requires: exclusive access to this.
        /// </summary>
        public void SetComponent(long mID, long v)
        {
            Debug.Assert(mID == Epoch.MId(v), $"{mID} != {Epoch.MId(v)}");
            this.EnsureCapacity(mID + 1);
            this.Values[mID] = v;
        }

        /// <summary>
        /// Sets the clock component for mId to v.
        /// Here, v is an epoch, not a simple long.
        /// Requires: exclusive access to this.
        /// </summary>
        public void SetComponent(ulong mID, long v)
        {
            Debug.Assert((long)mID == Epoch.MId(v), $"{mID} != {Epoch.MId(v)}");
            this.EnsureCapacity((long)mID + 1);
            this.Values[mID] = v;
        }

        /// <summary>
        /// Requires: exclusive access to this.
        /// </summary>
        public override string ToString()
        {
            StringBuilder r = new StringBuilder();
            r.Append("[");

            for (int i = 0; i < this.Values.Length; i++)
            {
                r.Append((i > 0 ? " " : string.Empty) + Epoch.ToString(this.Values[i]));
            }

            return r.Append("]").ToString();
        }

        /// <summary>
        /// Gets the clock component for mId, as an epoch. Requires: exclusive access to this.
        /// </summary>
        public long GetComponent(long mID)
        {
            if ((int)mID < this.Values.Length)
            {
                return this.Values[mID];
            }
            else
            {
                return Epoch.MakeEpoch(mID, 0);
            }
        }

        /// <summary>
        /// Gets the size of our VC. Requires: exclusive access to this.
        /// </summary>
        public int Size()
        {
            return this.Values.Length;
        }

        /// <summary>
        /// Clears the clock values from pos to end.
        /// </summary>
        private static void ClearFrom(long[] values, int pos)
        {
            for (int i = pos; i < values.Length; i++)
            {
                values[i] = Epoch.MakeEpoch(i, 0);
            }
        }

        /// <summary>
        /// Grows our clock to be as long as len
        /// sets the added entries to 0@0.
        /// </summary>
        private void EnsureCapacity(long len)
        {
            int curLength = this.Values.Length;
            if (curLength < len)
            {
                long[] b = new long[len];
                for (int i = 0; i < curLength; i++)
                {
                    b[i] = this.Values[i];
                }

                ClearFrom(b, curLength);
                this.Values = b;
            }
        }
    }
}
