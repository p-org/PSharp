//-----------------------------------------------------------------------
// <copyright file="EventInheritanceTests.cs">
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
    public class EventInheritanceTests
    {
        #region correct tests

        [Fact]
        public void NoPayloadOnEither()
        {
            var test = @"
namespace Foo {
    extern event E1x;
    internal event E1;
    internal event E2 : E1;
    internal event E2x : E1x;
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    internal class E1 : Event
    {
        public E1()
            : base()
        {
        }
    }

    internal class E2 : E1
    {
        public E2()
            : base()
        {
            base.SetCardinalityConstraints(-1, -1);
        }
    }

    internal class E2x : E1x
    {
        public E2x()
            : base()
        {
            base.SetCardinalityConstraints(-1, -1);
        }
    }
}
";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void NoPayload_InMachine()
        {
            var test = @"
namespace Foo {
    machine m1 {
        extern event E1x;
        internal event E1;
        internal event E2 : E1;
        internal event E2x : E1x;
        
        start state Init {}
    }
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class m1 : Machine
    {
        internal class E1 : Event
        {
            public E1()
                : base()
            {
            }
        }

        internal class E2 : E1
        {
            public E2()
                : base()
            {
                base.SetCardinalityConstraints(-1, -1);
            }
        }

        internal class E2x : E1x
        {
            public E2x()
                : base()
            {
                base.SetCardinalityConstraints(-1, -1);
            }
        }

        [Microsoft.PSharp.Start]
        class Init : MachineState
        {
        }
    }
}
";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void NoPayloadOnOtherNamespace()
        {
            var test = @"
namespace Foo {
    internal event E1;
}
namespace Bar {
    extern event Foo.E1;
    internal event E2 : Foo.E1;
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    internal class E1 : Event
    {
        public E1()
            : base()
        {
        }
    }
}

namespace Bar
{
    internal class E2 : Foo.E1
    {
        public E2()
            : base()
        {
            base.SetCardinalityConstraints(-1, -1);
        }
    }
}
";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void NoPayloadOnEither_Generic()
        {
            var test = @"
namespace Foo {
    extern event E1x<T>;
    internal event E1<T>;
    internal event E2<T> : E1<T>;
    internal event E2x<T> : E1x<T>;
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    internal class E1<T> : Event
    {
        public E1()
            : base()
        {
        }
    }

    internal class E2<T> : E1<T>
    {
        public E2()
            : base()
        {
            base.SetCardinalityConstraints(-1, -1);
        }
    }

    internal class E2x<T> : E1x<T>
    {
        public E2x()
            : base()
        {
            base.SetCardinalityConstraints(-1, -1);
        }
    }
}
";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void NoPayloadOnBase()
        {
            var test = @"
namespace Foo {
    extern event E1x;
    internal event E1;
    internal event E2 : E1 (b:int);
    internal event E2x : E1x (b:int);
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    internal class E1 : Event
    {
        public E1()
            : base()
        {
        }
    }

    internal class E2 : E1
    {
        public int b;

        public E2(int b)
            : base()
        {
            this.b = b;
            base.SetCardinalityConstraints(-1, -1);
        }
    }

    internal class E2x : E1x
    {
        public int b;

        public E2x(int b)
            : base()
        {
            this.b = b;
            base.SetCardinalityConstraints(-1, -1);
        }
    }
}
";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void NoPayloadOnDerived()
        {
            var test = @"
namespace Foo {
    extern event E1x (a:string);
    internal event E1 (a:string);
    internal event E2 : E1;
    internal event E2x : E1x;
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    internal class E1 : Event
    {
        public string a;

        public E1(string a)
            : base()
        {
            this.a = a;
        }
    }

    internal class E2 : E1
    {
        public E2(string a)
            : base(a)
        {
            base.SetCardinalityConstraints(-1, -1);
        }
    }

    internal class E2x : E1x
    {
        public E2x(string a)
            : base(a)
        {
            base.SetCardinalityConstraints(-1, -1);
        }
    }
}
";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void PayloadOnBoth()
        {
            var test = @"
namespace Foo {
    extern event E1x (a:string);
    internal event E1 (a:string);
    internal event E2 : E1 (b:int);
    internal event E2x : E1x (b:int);
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    internal class E1 : Event
    {
        public string a;

        public E1(string a)
            : base()
        {
            this.a = a;
        }
    }

    internal class E2 : E1
    {
        public int b;

        public E2(string a, int b)
            : base(a)
        {
            this.b = b;
            base.SetCardinalityConstraints(-1, -1);
        }
    }

    internal class E2x : E1x
    {
        public int b;

        public E2x(string a, int b)
            : base(a)
        {
            this.b = b;
            base.SetCardinalityConstraints(-1, -1);
        }
    }
}
";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void MultiPayloadMultiLevel()
        {
            var test = @"
namespace Foo {
    extern event E1x0 (ax0:char, bx0:string);
    extern event E1x : E1x0 (a1x:float, b1x:double);
    internal event E10 (a10:short, b10:ushort);
    internal event E1 : E10 (a1:byte, b1:bool);
    internal event E2 : E1 (a2:int, b2:uint);
    internal event E2x : E1x (a2x:long, b2x:ulong);
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    internal class E10 : Event
    {
        public short a10;
        public ushort b10;

        public E10(short a10, ushort b10)
            : base()
        {
            this.a10 = a10;
            this.b10 = b10;
        }
    }

    internal class E1 : E10
    {
        public byte a1;
        public bool b1;

        public E1(short a10, ushort b10, byte a1, bool b1)
            : base(a10, b10)
        {
            this.a1 = a1;
            this.b1 = b1;
            base.SetCardinalityConstraints(-1, -1);
        }
    }

    internal class E2 : E1
    {
        public int a2;
        public uint b2;

        public E2(short a10, ushort b10, byte a1, bool b1, int a2, uint b2)
            : base(a10, b10, a1, b1)
        {
            this.a2 = a2;
            this.b2 = b2;
            base.SetCardinalityConstraints(-1, -1);
        }
    }

    internal class E2x : E1x
    {
        public long a2x;
        public ulong b2x;

        public E2x(char ax0, string bx0, float a1x, double b1x, long a2x, ulong b2x)
            : base(ax0, bx0, a1x, b1x)
        {
            this.a2x = a2x;
            this.b2x = b2x;
            base.SetCardinalityConstraints(-1, -1);
        }
    }
}
";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void MultiPayloadMultiLevel_Generic()
        {
            // Some generic type params in derived classes are not specified in the same order
            // as in the base class, to verify correct handling.
            var test = @"
namespace Foo {
    extern event E1x0<Te1x0> (ax0:char, bx0:string);
    extern event E1x<Te1x0, Te1x> : E1x0<Te1x0> (a1x:float, b1x:double);
    internal event E10 <Te10> (a10:short, b10:ushort);
    internal event E1 <Te10, Te1>: E10<Te10> (a1:byte, b1:bool);
    internal event E2 <Te2, Te1, Te10>: E1<Te10, Te1> (a2:int, b2:uint);
    internal event E2x <Te1x0, Te1x, Te2x> : E1x<Te1x0, Te1x> (a2x:long, b2x:ulong);
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    internal class E10<Te10> : Event
    {
        public short a10;
        public ushort b10;

        public E10(short a10, ushort b10)
            : base()
        {
            this.a10 = a10;
            this.b10 = b10;
        }
    }

    internal class E1<Te10, Te1> : E10<Te10>
    {
        public byte a1;
        public bool b1;

        public E1(short a10, ushort b10, byte a1, bool b1)
            : base(a10, b10)
        {
            this.a1 = a1;
            this.b1 = b1;
            base.SetCardinalityConstraints(-1, -1);
        }
    }

    internal class E2<Te2, Te1, Te10> : E1<Te10, Te1>
    {
        public int a2;
        public uint b2;

        public E2(short a10, ushort b10, byte a1, bool b1, int a2, uint b2)
            : base(a10, b10, a1, b1)
        {
            this.a2 = a2;
            this.b2 = b2;
            base.SetCardinalityConstraints(-1, -1);
        }
    }

    internal class E2x<Te1x0, Te1x, Te2x> : E1x<Te1x0, Te1x>
    {
        public long a2x;
        public ulong b2x;

        public E2x(char ax0, string bx0, float a1x, double b1x, long a2x, ulong b2x)
            : base(ax0, bx0, a1x, b1x)
        {
            this.a2x = a2x;
            this.b2x = b2x;
            base.SetCardinalityConstraints(-1, -1);
        }
    }
}
";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void AssumeAssertInheritance()
        {
            var test = @"
namespace Foo {
    internal event E1 assert 1;
    internal event E2: E1 assert 2;
    internal event E3 assume 3;
    internal event E4: E3 assume 4;
    internal event E5 assert 5;
    internal event E6: E5 assume 6;
    internal event E7: E6;
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    internal class E1 : Event
    {
        public E1()
            : base(1, -1)
        {
        }
    }

    internal class E2 : E1
    {
        public E2()
            : base()
        {
            base.SetCardinalityConstraints(2, -1);
        }
    }

    internal class E3 : Event
    {
        public E3()
            : base(-1, 3)
        {
        }
    }

    internal class E4 : E3
    {
        public E4()
            : base()
        {
            base.SetCardinalityConstraints(-1, 4);
        }
    }

    internal class E5 : Event
    {
        public E5()
            : base(5, -1)
        {
        }
    }

    internal class E6 : E5
    {
        public E6()
            : base()
        {
            base.SetCardinalityConstraints(-1, 6);
        }
    }

    internal class E7 : E6
    {
        public E7()
            : base()
        {
            base.SetCardinalityConstraints(-1, -1);
        }
    }
}
";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        #endregion correct tests

        #region failure tests

        [Fact]
        public void BaseEventClassNotDeclared()
        {
            var test = @"
namespace Foo {
    internal event E2 : E1;
}";
            LanguageTestUtilities.AssertFailedTestLog("Could not find definition or extern declaration of base event E1.", test);
        }

        [Fact]
        public void ExternOutsideNamespace()
        {
            var test = @"
extern event E1;
namespace Foo {
    internal event E2 : E1;
}";
            LanguageTestUtilities.AssertFailedTestLog("Must be declared inside a namespace or machine.", test);
        }

        [Fact]
        public void ExternAfterDefinition()
        {
            var test = @"
namespace Foo {
    event E1;
    extern event E1;
    internal event E2 : E1;
}";
            LanguageTestUtilities.AssertFailedTestLog("Event E1 has already been defined earlier in this file.", test);
        }

        [Fact]
        public void DefinitionAfterExtern()
        {
            var test = @"
namespace Foo {
    extern event E1;
    event E1;
    internal event E2 : E1;
}";
            LanguageTestUtilities.AssertFailedTestLog("Event E1 has already been declared \"extern\" earlier in this file.", test);
        }

        [Fact]
        public void ExternWithModifiers()
        {
            var test = @"
namespace Foo {
    extern internal event E1;
    internal event E2 : E1;
}";
            LanguageTestUtilities.AssertFailedTestLog("\"extern\" applies only to events and can have no access modifiers.", test);
        }

        [Fact]
        public void ExternWithAssert()
        {
            var test = @"
namespace Foo {
    extern event E1 assert 42;
    internal event E2 : E1;
}";
            LanguageTestUtilities.AssertFailedTestLog("\"extern\" cannot have an Assert or Assume specification.", test);
        }

        [Fact]
        public void ExternWithAssume()
        {
            var test = @"
namespace Foo {
    extern event E1 assume 42;
    internal event E2 : E1;
}";
            LanguageTestUtilities.AssertFailedTestLog("\"extern\" cannot have an Assert or Assume specification.", test);
        }

        [Fact]
        public void BaseGenericButNotReference()
        {
            var test = @"
namespace Foo {
    extern event E1<T>;
    internal event E2 : E1;
}";
            LanguageTestUtilities.AssertFailedTestLog("Mismatch in number of generic type arguments for base event E1.", test);
        }

        [Fact]
        public void ReferenceGenericButNotBase()
        {
            var test = @"
namespace Foo {
    extern event E1;
    internal event E2 : E1<T>;
}";
            LanguageTestUtilities.AssertFailedTestLog("Mismatch in number of generic type arguments for base event E1.", test);
        }

        [Fact]
        public void GenericTypeCountMismatch()
        {
            var test = @"
namespace Foo {
    extern event E1<T>;
    internal event E2 : E1<T1, T2>;
}";
            LanguageTestUtilities.AssertFailedTestLog("Mismatch in number of generic type arguments for base event E1.", test);
        }

        #endregion failure tests
    }
}
