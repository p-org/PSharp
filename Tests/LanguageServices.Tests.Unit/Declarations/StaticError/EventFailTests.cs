//-----------------------------------------------------------------------
// <copyright file="EventFailTests.cs">
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

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp.LanguageServices.Tests.Unit
{
    [TestClass]
    public class EventFailTests
    {
        [TestMethod, Timeout(10000)]
        public void TestProtectedEventDeclaration()
        {
            var test = @"
namespace Foo {
protected event e;
}";
            LanguageTestUtilities.AssertFailedTestLog("An event cannot be declared as protected.", test);
        }

        [TestMethod, Timeout(10000)]
        public void TestPrivateEventDeclaration()
        {
            var test = @"
namespace Foo {
private event e;
}";
            LanguageTestUtilities.AssertFailedTestLog("An event declared in the scope of a namespace cannot be private.", test);
        }

        [TestMethod, Timeout(10000)]
        public void TestEventDeclarationWithoutNamespace()
        {
            var test = "event e;";
            LanguageTestUtilities.AssertFailedTestLog("Must be declared inside a namespace.", test);
        }

        [TestMethod, Timeout(10000)]
        public void TestEventDeclarationWithGenericError1()
        {
            var test = @"
namespace Foo {
public event e>;
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \"(\" or \";\".", test);
        }

        [TestMethod, Timeout(10000)]
        public void TestEventDeclarationWithGenericError2()
        {
            var test = @"
namespace Foo {
public event e<;
}";
            LanguageTestUtilities.AssertFailedTestLog("Invalid generic expression.", test);
        }

        [TestMethod, Timeout(10000)]
        public void TestAbstractEventDeclarationInMachine()
        {
            var test = @"
namespace Foo {
machine M {
abstract event e;
}
{
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Unexpected token 'abstract'.", test);
        }

        [TestMethod, Timeout(10000)]
        public void TestProtectedEventDeclarationInMachine()
        {
            var test = @"
namespace Foo {
machine M {
protected event e;
}
{
}
}";
            LanguageTestUtilities.AssertFailedTestLog("An event cannot be declared as protected.", test);
        }
    }
}
