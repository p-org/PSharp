﻿//-----------------------------------------------------------------------
// <copyright file="MemberVisibilityFailTests.cs">
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

using Microsoft.PSharp.LanguageServices.Compilation;
using Microsoft.PSharp.LanguageServices.Parsing;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.StaticAnalysis.Tests.Unit
{
    [TestClass]
    public class MemberVisibilityFailTests : BaseTest
    {
        [TestMethod, Timeout(10000)]
        public void TestPublicFieldVisibility()
        {
            var test = @"
using Microsoft.PSharp;

namespace Foo {
class M : Machine
{
 public int Num;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  this.Num = 1;
 }
}
}";
            var error = "Warning: Field 'int Num' of machine 'Foo.M' is declared as " +
                "'public'.   at 'public int Num;' in Program.cs:line 7";
            base.AssertWarning(test, 1, error, isPSharpProgram: false);
        }

        [TestMethod, Timeout(10000)]
        public void TestPublicMethodVisibility()
        {
            var test = @"
using Microsoft.PSharp;

namespace Foo {
class M : Machine
{
 int Num;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 public void FirstOnEntryAction()
 {
  this.Num = 1;
 }
}
}";
            var error = "Warning: Method 'FirstOnEntryAction' of machine 'Foo.M' is " +
                "declared as 'public'.   at 'FirstOnEntryAction' in Program.cs:line 13";
            base.AssertWarning(test, 1, error, isPSharpProgram: false);
        }
    }
}
