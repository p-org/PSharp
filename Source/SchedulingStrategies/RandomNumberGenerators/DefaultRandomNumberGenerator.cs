// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.PSharp.TestingServices.SchedulingStrategies
{
    /// <summary>
    /// Default random number generator that uses the
    /// <see cref="System.Random"/> generator.
    /// </summary>
    public class DefaultRandomNumberGenerator : IRandomNumberGenerator
    {
        /// <summary>
        /// Device for generating random numbers.
        /// </summary>
        private Random Random;

        /// <summary>
        /// The seed currently used by the generator.
        /// </summary>
        private int RandomSeed;

        /// <summary>
        /// The seed currently used by the generator.
        /// </summary>
        public int Seed
        {
            get => RandomSeed;

            set
            {
                RandomSeed = value;
                Random = new Random(RandomSeed);
            }
        }

        /// <summary>
        /// Initializes with a time-dependent seed.
        /// </summary>
        public DefaultRandomNumberGenerator()
        {
            RandomSeed = DateTime.Now.Millisecond;
            Random = new Random(RandomSeed);
        }

        /// <summary>
        /// Initializes with the given seed.
        /// </summary>
        /// <param name="seed">Seed value</param>
        public DefaultRandomNumberGenerator(int seed)
        {
            RandomSeed = seed;
            Random = new Random(seed);
        }

        /// <summary>
        /// Returns a non-negative random number.
        /// </summary>
        public int Next()
        {
            return Random.Next();
        }

        /// <summary>
        /// Returns a non-negative random number less than maxValue.
        /// </summary>
        /// <param name="maxValue">Exclusive upper bound</param>
        public int Next(int maxValue)
        {
            return Random.Next(maxValue);
        }
    }
}
