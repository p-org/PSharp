P#
====================
A toolkit for **building**, **analyzing**, **systematically testing** and **debugging** asynchronous reactive software, such as web-services and distributed systems.

## Features
The P# framework provides:
- Language extensions to C# for building **event-driven asynchronous** applications, writing **test harnesses**, and specifying **safety and liveness properties**.
- A **systematic testing engine** that can capture and control all specified nondeterminism in the system, systematically explore the actual executable code to discover bugs, and report bug traces. A P# bug trace provides a global order of all communication events, and thus is easier to debug.
- Support for **replaying** bug traces, and **debugging** them using the Visual Studio debugger.

Although P# primarily targets .NET, it has also experimental support for systematically testing native C++ code.

## Build instructions
1. Get Visual Studio 2015 (required for Microsoft Roslyn).
2. Clone this project and compile using VS2015.

Optional: Get the [Visual Studio 2015 SDK](https://www.microsoft.com/en-us/download/details.aspx?id=46850) to be able to compile the P# visual studio extension (syntax highlighting). Only for the high-level P# language.

## How to use
A good way to start is by reading the [manual](https://github.com/p-org/PSharp/blob/master/Docs/Manual/manual.pdf) (which is not feature complete yet, but please also feel free to contact the P# dev team with specific questions).

Next, feel free to check out the P# [samples](https://github.com/p-org/PSharp/tree/master/Samples).

## Compilation
The P# compiler can be used to parse a P# program, rewrite it to C# and finally compile it to an executable. To invoke the compiler use the following command:

```
.\PSharpCompiler.exe /s:${SOLUTION_PATH}\${SOLUTION_NAME}.sln
```

Where ${SOLUTION\_PATH} is the path to your P# solution and ${SOLUTION\_NAME} is the name of your P# solution.

To specify an output path destination use the option `/o:${OUTPUT\_PATH}`.

To compile only a specific project in the solution use the option `/p:${PROJECT_NAME}`.

To compile as a library (dll) use the option `/t:lib`.

To compile for testing use the option `/t:test`.

## Systematic testing
The P# tester can be used to systematically test a P# program to find safety property and liveness property violations. It can be invoked on a P# program (dll) that was previously compiled using the P# compiler (or some other custom build system). To invoke the tester use the following command:

```
.\PSharpTester.exe /test:${DLL_PATH}\${DLL_NAME}.dll
```

Where ${DLL\_PATH} is the path to your P# program and ${DLL\_NAME} is the name of your P# program.

You can optionally give the number of testing iterations to perform using `/i:value`.

## Replay and debug buggy executions
The P# replayer can be used to reproduce and debug buggy executions (found by `PSharpTester.exe`). To invoke the replayer use the following command:

```
.\PSharpReplayer.exe /test:${DLL_PATH}\${DLL_NAME}.dll /trace:${TRACE_PATH}\${TRACE_NAME}.pstrace
```

Where ${TRACE\_PATH} is the path to the bug trace (dumped by `PSharpTester.exe`) and ${TRACE\_NAME} is the name of the bug trace.

You can attach the Visual Studio debugger on this buggy execution, to get the familiar VS debugging experience, by using `/break`. When using this flag, P# will automatically instrument a breakpoint when the bug is found. You can also insert your own breakpoints in the source code.

## Options

To see various available command line options for the P# tools use the option `/?`.

## How to contribute

We welcome contributions to the P# project! Before you start contributing, though, please read carefully the development guidelines.

## Contact us

If you would like to use P# in your project, or have any specific questions, please feel free to contact one of the following members of the P# team:
- Pantazis Deligiannis (p.deligiannis@imperial.ac.uk) [Maintainer]
- Akash Lal (akashl@microsoft.com) [Maintainer]
- Shaz Qadeer (qadeer@microsoft.com)
- Cheng Huang (cheng.huang@microsoft.com)

## Publications
- **[Uncovering Bugs in Distributed Storage Systems During Testing (not in Production!)](https://www.usenix.org/node/194442)**. Pantazis Deligiannis, Matt McCutchen, Paul Thomson, Shuo Chen, Alastair F. Donaldson, John Erickson, Cheng Huang, Akash Lal, Rashmi Mudduluru, Shaz Qadeer and Wolfram Schulte. In the *14th USENIX Conference on File and Storage Technologies* (FAST), 2016.
- **[Asynchronous Programming, Analysis and Testing with State Machines](https://dl.acm.org/citation.cfm?id=2737996)**. Pantazis Deligiannis, Alastair F. Donaldson, Jeroen Ketema, Akash Lal and Paul Thomson. In the *36th Annual ACM SIGPLAN Conference on Programming Language Design and Implementation* (PLDI), 2015.
