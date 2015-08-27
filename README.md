P#
====================
An **actor-based** programming language and **concurrency unit testing** framework for developing **highly-reliable asynchronous software**, such as web-services and distributed systems.

## Features
- Enables the development of efficient asynchronous applications using an **event-driven**, actor-based programming model. Because **all the asynchrony is explicitly exposed** at specific communication points in a P# program, the user does not need to explicitly create and manage tasks; the P# runtime (built on top of TPL) is instead responsible for handling all the underlying concurrency, allowing the developer to focus on application logic.
- Allows the environment to be **modeled** via simple language features. The P# compiler can then automatically substitute real code with modeled, and the P# tester can **systematically test** the compiled executable to discover bugs (such as assertion failures and uncaught exceptions).
- Fully interoperates with C#: the developer can **write any C# code** inside a P# program. P# is basically an extension of C#, build on top of the Roslyn, TPL and .NET frameworks, which not only makes P# easy to learn comparing with a completely new language, but also allows **easy integration with existing code**.

## Build instructions
1. Get Visual Studio 2015 Preview (required for Microsoft Roslyn).
2. Clone this project and compile using VS2015.

Optional: Get the [Visual Studio 2015 SDK](https://www.microsoft.com/en-us/download/details.aspx?id=46850) to be able to compile the P# visual studio extension (syntax highlighting). Only for the high-level P# language.

## How to use
A good way to start is by reading the [manual](https://cdn.rawgit.com/p-org/PSharp/master/Docs/Manual/out/manual.pdf).

## Compilation
The P# compiler can be used to parse a P# program, rewrite it to C# and finally compile it to an executable. To invoke the compiler use the following command:

```
.\PSharpCompiler.exe /s:${SOLUTION_PATH}\${SOLUTION_NAME}.sln
```

Where ${SOLUTION\_PATH} is the path to your P# solution and ${SOLUTION\_NAME} is the name of your P# solution.

To specify an output path destination use the option `/o:${OUTPUT\_PATH}`.

To compile only a specific project in the solution use the option `/p:${PROJECT_NAME}`.

To only compile for testing use the option `/t:testing`.

## Systematic testing
The P# tester can be used to systematically test a P# program to find safety property and liveness property violations. It can be invoked on a P# program (dll) that was previously compiled using the P# compiler (or some other custom build system). To invoke the tester use the following command:

```
.\PSharpTester.exe /test:${DLL_PATH}\${DLL_NAME}.dll
```

Where ${DLL\_PATH} is the path to your P# program and ${DLL\_NAME} is the name of your P# program.

You can optionally give the number of testing iterations to perform using `/i:value`.

To enable liveness checking use the option `/liveness`.

## Options

To see various available command line options for the P# tools use the option `/?`.

## Publications
- **[Asynchronous Programming, Analysis and Testing with State Machines](https://dl.acm.org/citation.cfm?id=2737996)**. Pantazis Deligiannis, Alastair F. Donaldson, Jeroen Ketema, Akash Lal and Paul Thomson. In the *ACM SIGPLAN Conference on Programming Language Design and Implementation* (PLDI), 2015.
