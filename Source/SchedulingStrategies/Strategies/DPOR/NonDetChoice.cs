//-----------------------------------------------------------------------
// <copyright file="NonDetChoice.cs">
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

namespace Microsoft.PSharp.TestingServices.SchedulingStrategies.DPOR
{
    /// <summary>
    /// Stores the outcome of a nondetereminstic (nondet) choice.
    /// </summary>
    internal struct NonDetChoice
    {
        /// <summary>
        /// Is this nondet choice a boolean choice?
        /// If so, <see cref="Choice"/> is 0 or 1.
        /// Otherwise, it can be any int value.
        /// </summary>
        internal bool IsBoolChoice;

        /// <summary>
        /// The nondet choice; 0 or 1 if this is a bool choice;
        /// otherwise, any int.
        /// </summary>
        internal int Choice;
    }
}