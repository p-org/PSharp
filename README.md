P#
====================
A toolkit for **building**, **analyzing** and **systematically testing** asynchronous reactive software, such as web-services and distributed systems.

## Features
The P# framework provides:
- Language extensions to C# for building **event-driven asynchronous** applications, writing **test harnesses**, and specifying **safety and liveness properties**.
- A **systematic testing engine** that can capture and control all the specified nondeterminism in the system, and systematically explore the actual executable code to discover bugs.

Although P# primarily targets .NET, it has also experimental support for systematically testing native C++ code.

## Build instructions
1. Get Visual Studio 2015 (required for Microsoft Roslyn).
2. Clone this project and compile using VS2015.

Optional: Get the [Visual Studio 2015 SDK](https://www.microsoft.com/en-us/download/details.aspx?id=46850) to be able to compile the P# visual studio extension (syntax highlighting). Only for the high-level P# language.

## How to use
A good way to start is by reading the [manual](https://github.com/p-org/PSharp/blob/master/Docs/Manual/manual.pdf) (which is not feature complete yet, but please also feel free to contact the P# dev team with specific questions).

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

## Contact us

If you would like to use P# in your project, or have any specific questions, please feel free to contact one of the following members of the P# team:
- Pantazis Deligiannis (p.deligiannis@imperial.ac.uk)
- Shaz Qadeer (qadeer@microsoft.com)
- Akash Lal (akashl@microsoft.com)
- Cheng Huang (cheng.huang@microsoft.com)

## Publications
- **[Uncovering Bugs in Distributed Storage Systems During Testing (not in Production!)](https://www.usenix.org/node/194442)**. Pantazis Deligiannis, Matt McCutchen, Paul Thomson, Shuo Chen, Alastair F. Donaldson, John Erickson, Cheng Huang, Akash Lal, Rashmi Mudduluru, Shaz Qadeer and Wolfram Schulte. In the *14th USENIX Conference on File and Storage Technologies* (FAST), 2016.
- **[Asynchronous Programming, Analysis and Testing with State Machines](https://dl.acm.org/citation.cfm?id=2737996)**. Pantazis Deligiannis, Alastair F. Donaldson, Jeroen Ketema, Akash Lal and Paul Thomson. In the *36th Annual ACM SIGPLAN Conference on Programming Language Design and Implementation* (PLDI), 2015.
