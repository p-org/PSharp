﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Xunit;

namespace Microsoft.PSharp.StaticAnalysis.Tests.Unit
{
    public class DuplicateSendsTests : BaseTest
    {
        #region failure tests

        [Fact]
        public void TestDuplicateSends1Fail()
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
  this.Send(this.Target, new eUnit(letter));
 }
}
}";
            
            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' sends " +
                "'letter', the ownership of which has already been given up.";
            base.AssertFailed(test, 1, error, isPSharpProgram: false);
        }

        [Fact]
        public void TestDuplicateSends2Fail()
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
  this.Send(this.Target, new eUnit(letter));
 }
}
}";
            
            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' sends " +
                "'letter', the ownership of which has already been given up.";
            base.AssertFailed(test, 1, error, isPSharpProgram: false);
        }

        [Fact]
        public void TestDuplicateSends3Fail()
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
  Letter letter = new Letter(""London"");
  this.Target = this.CreateMachine(typeof(M), new eUnit(letter));
  this.Send(this.Target, new eUnit(letter));
 }
}
}";
            
            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' sends " +
                "'letter', the ownership of which has already been given up.";
            base.AssertFailed(test, 1, error, isPSharpProgram: false);
        }

        [Fact]
        public void TestDuplicateSends4Fail()
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
  this.Target = this.CreateMachine(typeof(M), new eUnit(letter));
 }
}
}";
            
            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' sends " +
                "'letter', the ownership of which has already been given up.";
            base.AssertFailed(test, 1, error, isPSharpProgram: false);
        }

        [Fact]
        public void TestDuplicateSends5Fail()
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
  this.Target = this.CreateMachine(typeof(M), new eUnit(letter));
 }
}
}";
            
            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' sends " +
                "'letter', the ownership of which has already been given up.";
            base.AssertFailed(test, 1, error, isPSharpProgram: false);
        }

        #endregion
    }
}
