﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Xunit;

namespace Microsoft.PSharp.StaticAnalysis.Tests.Unit
{
    public class FieldAliasAccessAfterSendTests : BaseTest
    {
        #region failure tests

        [Fact]
        public void TestFieldAliasAccessAfterSend1Fail()
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
 public int Num;

 public Letter(string text, int num)
 {
  this.Text = text;
  this.Num = num;
 }
}

class M : Machine
{
 MachineId Target;
 Letter Letter;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  this.Letter = new Letter(""test"", 0);
  this.Target = this.CreateMachine(typeof(M));
  var otherLetter = this.Letter;
  this.Send(this.Target, new eUnit(this.Letter));
  otherLetter.Text = ""changed"";
  otherLetter.Num = 1;
 }
}
}";
            
            var configuration = base.GetConfiguration();
            configuration.DoStateTransitionAnalysis = false;
            base.AssertFailed(configuration, test, 3, isPSharpProgram: false);
        }

        [Fact]
        public void TestFieldAliasAccessAfterSend2Fail()
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
 Letter Letter;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  this.Target = this.CreateMachine(typeof(M));
  this.Letter = new Letter(""London"");
  this.Send(this.Target, new eUnit(this.Letter));
  this.Foo();
 }

 void Foo()
 {
  this.Bar();
 }

 void Bar()
 {
  this.Letter.Text = ""text2""; // ERROR
 }
}
}";
            
            var configuration = base.GetConfiguration();
            configuration.DoStateTransitionAnalysis = false;
            base.AssertFailed(configuration, test, 2, isPSharpProgram: false);
        }

        #endregion
    }
}
