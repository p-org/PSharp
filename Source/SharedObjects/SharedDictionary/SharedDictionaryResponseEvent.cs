//-----------------------------------------------------------------------
// <copyright file="SharedDictionaryResponseEvent.cs">
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

namespace Microsoft.PSharp.SharedObjects
{
    /// <summary>
    /// Event containing the value of a shared dictionary.
    /// </summary>
    internal class SharedDictionaryResponseEvent<T> : Event
    {
        /// <summary>
        /// Value.
        /// </summary>
        public T Value;

        /// <summary>
        /// Creates a new response event.
        /// </summary>
        /// <param name="value">Value</param>
        public SharedDictionaryResponseEvent(T value)
        {
            Value = value;
        }
    }
}
