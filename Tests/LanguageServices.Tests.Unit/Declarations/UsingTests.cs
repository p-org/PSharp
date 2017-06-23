//-----------------------------------------------------------------------
// <copyright file="UsingTests.cs">
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

using Xunit;

namespace Microsoft.PSharp.LanguageServices.Tests.Unit
{
    public class UsingTests
    {
        #region correct tests

        [Fact]
        public void TestUsingDeclaration()
        {
            var test = @"
using System.Text;";
            var expected = @"
using Microsoft.PSharp;
using System.Text;
";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        #endregion

        #region failure tests

        [Fact]
        public void TestIncorrectUsingDeclaration()
        {
            var test = "using System.Text";
            LanguageTestUtilities.AssertFailedTestLog("Expected \";\".", test);
        }

        [Fact]
        public void TestUsingDeclarationWithoutIdentifier()
        {
            var test = "using;";
            LanguageTestUtilities.AssertFailedTestLog("Expected identifier.", test);
        }

        #endregion
    }
}
