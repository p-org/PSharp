//-----------------------------------------------------------------------
// <copyright file="DefaultRandomNumberGenerator.cs">
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

using System;

namespace Microsoft.PSharp.Utilities
{
    /// <summary>
    /// Default random number generator that uses the System.Random generator.
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
        public int Seed { get; private set; }

        /// <summary>
        /// Initializes with a time-dependent seed.
        /// </summary>
        public DefaultRandomNumberGenerator()
        {
            Seed = DateTime.Now.Millisecond;
            Random = new Random(Seed);
        }

        /// <summary>
        /// Initializes with the given seed.
        /// </summary>
        /// <param name="seed">Seed value</param>
        public DefaultRandomNumberGenerator(int seed)
        {
            Seed = seed;
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

        /// <summary>
        /// Increments the seed used by the generator.
        /// </summary>
        public void IncrementSeed()
        {
            Seed++;
            Random = new Random(Seed);
        }
    }
}
