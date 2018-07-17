Different ways to use P#
========================
P# is built on top of the [.NET](https://www.microsoft.com/net) framework and the [Roslyn](https://github.com/dotnet/roslyn) compiler.

P# is provided as both a _language extension_ of [C#](https://docs.microsoft.com/en-gb/dotnet/csharp/), as well as a set of _library_ and _runtime_ APIs that can be directly used from inside a C# program. This means that there are two main ways that someone can use P# to build highly-reliable systems:
- The _surface syntax_ of P# (i.e. C# language extension) can be used to build an entire system from scratch (see an example [here](https://github.com/p-org/PSharp/blob/master/Samples/PingPong/PingPong.PSharpLanguage/Server.psharp)). The surface P# syntax directly extends [C#](https://docs.microsoft.com/en-gb/dotnet/csharp/) with new language constructs, which allows for rapid prototyping. However, to use the surface syntax, a developer has to use the P# compiler, which is built on top of [Roslyn](https://github.com/dotnet/roslyn). The main disadvantage of this approach is that P# does not yet fully integrate with the Visual Studio integrated development environment (IDE), although we are actively working on this (see [here](https://github.com/p-org/PSharp/issues/128)), and thus does not support high-productivity features such as IntelliSense (e.g. for auto-completition and automated refactoring).
- The P# library and runtime APIs (available for C#) can be used to build an entire system from scratch (see an example [here](https://github.com/p-org/PSharp/blob/master/Samples/PingPong/PingPong.PSharpLibrary/Server.cs)). This approach is slightly more verbose than the above, but allows full integration with Visual Studio.

P# can be also used for thoroughly testing an _existing_ message-passing system, by modeling its environment (e.g. a client) and/or components of the system. However, this approach has the disadvantage that if nondeterminism in the system is not captured by (or expressed in) P#, then the P# testing engine might be unable to discover and reproduce bugs.

Note that many examples in our documentation will use the P# surface syntax, since it is less verbose.
