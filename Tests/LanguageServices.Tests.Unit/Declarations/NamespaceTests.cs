//-----------------------------------------------------------------------
// <copyright file="NamespaceTests.cs">
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
    public class NamespaceTests
    {
        #region correct tests

        [Fact]
        public void TestNamespaceDeclaration()
        {
            var test = @"
namespace Foo { }";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestNamespaceDeclaration2()
        {
            var test = @"
namespace Foo { }
namespace Bar { }";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
}

namespace Bar
{
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestNamespaceDeclarationCompact()
        {
            var test = @"
namespace Foo{}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        #endregion

        #region failure tests

        [Fact]
        public void TestUnexpectedTokenWithoutNamespace()
        {
            var test = "private";
            LanguageTestUtilities.AssertFailedTestLog("Unexpected token.", test);
        }

        [Fact]
        public void TestNamespaceDeclarationWithoutIdentifier()
        {
            var test = "namespace { }";
            LanguageTestUtilities.AssertFailedTestLog("Expected namespace identifier.", test);
        }

        #endregion
    }
}
