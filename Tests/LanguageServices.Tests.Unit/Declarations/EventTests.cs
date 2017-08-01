//-----------------------------------------------------------------------
// <copyright file="EventTests.cs">
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
    public class EventTests
    {
        #region correct tests

        [Fact]
        public void TestEventDeclaration()
        {
            var test = @"
namespace Foo {
event e1;
internal event e2;
public event e3;
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    public class e1 : Event
    {
        public e1()
            : base()
        {
        }
    }

    internal class e2 : Event
    {
        public e2()
            : base()
        {
        }
    }

    public class e3 : Event
    {
        public e3()
            : base()
        {
        }
    }
}
";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestSimpleGenericEventDeclaration()
        {
            var test = @"
namespace Foo {
event e1<T>;
internal event e2;
public event e3;
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    public class e1<T> : Event
    {
        public e1()
            : base()
        {
        }
    }

    internal class e2 : Event
    {
        public e2()
            : base()
        {
        }
    }

    public class e3 : Event
    {
        public e3()
            : base()
        {
        }
    }
}
";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestComplexGenericEventDeclaration()
        {
            var test = @"
namespace Foo {
internal event e1<T, K>;
internal event e2;
public event e3;
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    internal class e1<T,K> : Event
    {
        public e1()
            : base()
        {
        }
    }

    internal class e2 : Event
    {
        public e2()
            : base()
        {
        }
    }

    public class e3 : Event
    {
        public e3()
            : base()
        {
        }
    }
}
";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestEventDeclarationWithPayload()
        {
            var test = @"namespace Foo {
internal event e1 (m:string, n:int);
internal event e2 (m:string);
public event e3 (n:int);
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    internal class e1 : Event
    {
        public string m;
        public int n;

        public e1(string m, int n)
            : base()
        {
            this.m = m;
            this.n = n;
        }
    }

    internal class e2 : Event
    {
        public string m;

        public e2(string m)
            : base()
        {
            this.m = m;
        }
    }

    public class e3 : Event
    {
        public int n;

        public e3(int n)
            : base()
        {
            this.n = n;
        }
    }
}
";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestGenericEventDeclarationWithPayload()
        {
            var test = @"
namespace Foo {
internal event e1<T, K> (m:K, n:T);
internal event e2 (m:string);
public event e3 (n:int);
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    internal class e1<T,K> : Event
    {
        public K m;
        public T n;

        public e1(K m, T n)
            : base()
        {
            this.m = m;
            this.n = n;
        }
    }

    internal class e2 : Event
    {
        public string m;

        public e2(string m)
            : base()
        {
            this.m = m;
        }
    }

    public class e3 : Event
    {
        public int n;

        public e3(int n)
            : base()
        {
            this.n = n;
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestEventInMachineDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
public event e1<T>;
internal event e2;
public event e3<T, K> (m:K, n:T);
internal event e4 (m:string);
start state S { }
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        public class e1<T> : Event
        {
            public e1()
                : base()
            {
            }
        }

        internal class e2 : Event
        {
            public e2()
                : base()
            {
            }
        }

        public class e3<T,K> : Event
        {
            public K m;
            public T n;

            public e3(K m, T n)
                : base()
            {
                this.m = m;
                this.n = n;
            }
        }

        internal class e4 : Event
        {
            public string m;

            public e4(string m)
                : base()
            {
                this.m = m;
            }
        }

        [Microsoft.PSharp.Start]
        class S : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        #endregion

        #region failure tests

        [Fact]
        public void TestProtectedEventDeclaration()
        {
            var test = @"
namespace Foo {
protected event e;
}";
            LanguageTestUtilities.AssertFailedTestLog("An event cannot be declared as protected.", test);
        }

        [Fact]
        public void TestPrivateEventDeclaration()
        {
            var test = @"
namespace Foo {
private event e;
}";
            LanguageTestUtilities.AssertFailedTestLog("An event declared in the scope of a namespace cannot be private.", test);
        }

        [Fact]
        public void TestEventDeclarationWithoutNamespace()
        {
            var test = "event e;";
            LanguageTestUtilities.AssertFailedTestLog("Must be declared inside a namespace.", test);
        }

        [Fact]
        public void TestEventDeclarationWithGenericError1()
        {
            var test = @"
namespace Foo {
public event e>;
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \"(\" or \";\".", test);
        }

        [Fact]
        public void TestEventDeclarationWithGenericError2()
        {
            var test = @"
namespace Foo {
public event e<;
}";
            LanguageTestUtilities.AssertFailedTestLog("Invalid generic expression.", test);
        }

        [Fact]
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
            LanguageTestUtilities.AssertFailedTestLog("An event cannot be declared as abstract.", test);
        }

        [Fact]
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

        #endregion
    }
}
