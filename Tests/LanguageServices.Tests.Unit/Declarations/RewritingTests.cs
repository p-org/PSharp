//-----------------------------------------------------------------------
// <copyright file="RewritingTests.cs">
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

using Microsoft.PSharp.LanguageServices.Parsing;

using Xunit;

namespace Microsoft.PSharp.LanguageServices.Tests.Unit
{
    public class RewritingTests
    {
        #region failure tests

        [Fact]
        public void TestDuplicateStatesAndJump()
        {
            var test = @"
namespace Foo {
public event e;
machine M {
start state S1 { }
state S2 { 
  on e do { jump(S1); }
}
state S2 { 
  on e do { jump(S1); }
}
}
}";

            var exception_hit = false;
            try
            {
                LanguageTestUtilities.RunRewriter(test);
            }
            catch (RewritingException ex)
            {
                exception_hit = true;
                Assert.Equal(ex.Message.Replace(Environment.NewLine, string.Empty), 
                    "Multiple declarations of the state 'S2'" + 
                    "File: Program.psharp" +
                    "Lines: 5 and 8");
            }

            Assert.True(exception_hit, "expected exception was not hit");
        }

        #endregion
    }
}
