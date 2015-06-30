P#
====================
**P#** is an **actor-based** programming language for developing **highly-reliable asynchronous software**, such as web-services and distributed systems. P# has four key capabilities:

- Enables the development of efficient asynchronous applications using an **event-driven**, actor-based programming model. Because **all the asynchrony is explicitly exposed** at specific communication points in a P# program, the user does not need to explicity create and manage tasks; the P# runtime is instead responsible for handling all the underlying concurrency.
- Allows the environment to be **modelled** via language constructs. The compiler can then automatically substitute real code with modelled, and **systematically test** the compiled executable to discover bugs (such as assertion failures and uncaught exceptions).
- Provides strong **data race freedom** guarantees. The compiler is able to perform a **scalable static data race analysis** on the source code that can detect all potential data races in a program (based on certain assumptions, such as no use of non-P# threading or reflection).
- Fully interoperates with C#: the developer can **write any C# code** inside a P# program. P# is basically an extension of C#, build on top of the Roslyn and .NET frameworks, which not only makes P# easy to learn comparing with a completely new language, but also allows **easy integration with existing code**.

## Build instructions
1. Get Visual Studio 2015 Preview (required for Microsoft Roslyn).
2. Clone this project and compile using VS2015.
3. Get the [Visual Studio 2015 SDK](https://www.microsoft.com/en-us/download/details.aspx?id=46850) to be able to compile the P# visual studio extension (syntax highlighting).

## How to use
A good way to start is by reading the [manual](https://cdn.rawgit.com/p-org/PSharp/master/Docs/Manual/out/manual.pdf).

P# extends the C# language with state machines, states, state transitions and actions bindings. In P#, state machines are first class citizens and live in their own separate tasks. The only way they can communicate with each other is by explicitly sending and implicitly receiving events. As P# is based on C#, almost any valid C# code can be used in a P# method body (threading and code reflection APIs are not allowed).

The P# compiler can be used to parse a program, statically analyse it for data races and finally compile it to an executable. To invoke the compiler use the following command:

```
.\PSharpCompiler.exe /s:${SOLUTION_PATH}\${SOLUTION_NAME}.sln
```

Where ${SOLUTION\_PATH} is the path to your P# solution and ${SOLUTION\_NAME} is the name of your P# solution.

To specify an output path destination use the option `/o:${OUTPUT\_PATH}`.

To compile only a specific project in the solution use the option `/p:${PROJECT_NAME}`.

## Options

To see various available command line options use the option `/?`.

To statically analyze the program for data races use the option `/analyze`.

To systematically test the program for bugs (i.e. assertion failures and exceptions) use the option `/test`. You can optionally give the number of testing iterations to perform using `/i:value`.

## Publications
- **[Asynchronous Programming, Analysis and Testing with State Machines](https://dl.acm.org/citation.cfm?id=2737996)**. Pantazis Deligiannis, Alastair F. Donaldson, Jeroen Ketema, Akash Lal and Paul Thomson. In the *ACM SIGPLAN Conference on Programming Language Design and Implementation* (PLDI), 2015.
