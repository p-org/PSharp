// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.StaticAnalysis.Tests
{
    public class ExternalLibraryCallTests : BaseTest
    {
        public ExternalLibraryCallTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact]
        public void TestExternalLibraryCallFail()
        {
            var test = @"
using Microsoft.PSharp;

namespace Foo {
class eUnit : Event
{
 public Letter Letter;
 
 public eUnit(Letter letter)
  : base()
 {
  this.Letter = letter;
 }
}

struct Letter
{
 public string Text;

 public Letter(string text)
 {
  this.Text = text;
 }
}

class M : Machine
{
 MachineId Target;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  this.Target = this.CreateMachine(typeof(M));
  Letter letter = new Letter(""London"");
  this.Send(this.Target, new eUnit(letter));
  System.Console.WriteLine(letter.Text);
 }
}
}";

            var warning = "Warning: Method 'FirstOnEntryAction' of machine 'Foo.M' calls a " +
                "method with unavailable source code, which might be a source of errors." +
                "   at 'System.Console.WriteLine(letter.Text)' in Program.cs:line 39" +
                "   --- Source of giving up ownership ---" +
                "   at 'this.Send(this.Target, new eUnit(letter));' in Program.cs:line 38";
            this.AssertWarning(test, 1, warning, isPSharpProgram: false);
        }
    }
}
