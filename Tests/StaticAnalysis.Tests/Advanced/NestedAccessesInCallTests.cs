﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.StaticAnalysis.Tests
{
    public class NestedAccessesInCallTests : BaseTest
    {
        public NestedAccessesInCallTests(ITestOutputHelper output)
            : base(output)
        { }

        [Fact]
        public void TestNestedAccessesInCall1Fail()
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
  this.Foo(letter);
 }

 void Foo(Letter letter)
 {
  this.Bar(letter);
 }

 void Bar(Letter letter)
 {
  letter.Text = ""text2"";  // ERROR
 }
}
}";
            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' " +
                "accesses 'letter' after giving up its ownership.";
            base.AssertFailed(test, 1, error, isPSharpProgram: false);
        }

        [Fact]
        public void TestNestedAccessesInCall2Fail()
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

struct Envelope
{
 public Envelope(int id, Letter letter)
 {
  letter.Text = ""text2"";  // ERROR
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
  this.Foo(letter);
 }

 void Foo(Letter letter)
 {
  this.Bar(letter);
 }

 void Bar(Letter letter)
 {
  new Envelope(0, letter);
 }
}
}";
            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' " +
                "accesses 'letter' after giving up its ownership.";
            base.AssertFailed(test, 1, error, isPSharpProgram: false);
        }

        [Fact]
        public void TestNestedAccessesInCall3Fail()
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

struct Envelope
{
 public Envelope(int id, Letter letter)
 {
  new AnotherEnvelope(letter, id);
 }
}

struct AnotherEnvelope
{
 public AnotherEnvelope(Letter letter, int id)
 {
  letter.Text = ""text2"";  // ERROR
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
  this.Foo(letter);
 }

 void Foo(Letter letter)
 {
  this.Bar(letter);
 }

 void Bar(Letter letter)
 {
  new Envelope(0, letter);
 }
}
}";
            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' " +
                "accesses 'letter' after giving up its ownership.";
            base.AssertFailed(test, 1, error, isPSharpProgram: false);
        }
    }
}
