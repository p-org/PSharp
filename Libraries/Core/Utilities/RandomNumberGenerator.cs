//-----------------------------------------------------------------------
// <copyright file="RandomNumberGenerator.cs">
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
    /// Random number generator interface
    /// </summary>
    public interface IRandomNumberGenerator
    {
        /// <summary>
        /// Returns a non-negative random number
        /// </summary>
        int Next();

        /// <summary>
        /// Returns a non-negative random number less than maxValue
        /// </summary>
        /// <param name="maxValue">Exclusive upper bound</param>
        int Next(int maxValue);
    }

    /// <summary>
    /// Wrapper around System.Random
    /// </summary>
    public class RandomWrapper : IRandomNumberGenerator
    {
        Random random;

        /// <summary>
        /// Initialize with time-dependent seed
        /// </summary>
        public RandomWrapper()
        {
            random = new Random();
        }

        /// <summary>
        /// Initialize with the given seed
        /// </summary>
        /// <param name="seed">Seed value</param>
        public RandomWrapper(int seed)
        {
            random = new Random(seed);
        }

        /// <summary>
        /// Returns a non-negative random number
        /// </summary>
        public int Next()
        {
            return random.Next();
        }

        /// <summary>
        /// Returns a non-negative random number less than maxValue
        /// </summary>
        /// <param name="maxValue">Exclusive upper bound</param>
        public int Next(int maxValue)
        {
            return random.Next(maxValue);
        }
    }
}
