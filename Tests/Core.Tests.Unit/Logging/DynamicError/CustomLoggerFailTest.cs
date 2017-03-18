//-----------------------------------------------------------------------
// <copyright file="CustomLoggerFailTest.cs">
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

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.PSharp.Core.Tests.Unit
{
    [TestClass]
    public class CustomLoggerFailTest
    {
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestNullCustomLogger()
        {
            PSharpRuntime runtime = PSharpRuntime.Create();

            try
            {
                runtime.SetLogger(null);
            }
            catch (InvalidOperationException ex)
            {
                Assert.AreEqual("Cannot install a null logger.", ex.Message);
                throw;
            }
        }
    }
}
