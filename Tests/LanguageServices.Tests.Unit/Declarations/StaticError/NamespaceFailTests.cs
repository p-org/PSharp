//-----------------------------------------------------------------------
// <copyright file="NamespaceFailTests.cs">
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
    public class NamespaceFailTests
    {
        [TestMethod, Timeout(10000)]
        public void TestUnexpectedTokenWithoutNamespace()
        {
            var test = "private";
            LanguageTestUtilities.AssertFailedTestLog("Unexpected token.", test);
        }

        [TestMethod, Timeout(10000)]
        public void TestNamespaceDeclarationWithoutIdentifier()
        {
            var test = "namespace { }";
            LanguageTestUtilities.AssertFailedTestLog("Expected namespace identifier.", test);
        }
    }
}
