P#
====================
A toolkit for **building**, **analyzing**, **systematically testing** and **debugging** asynchronous reactive software, such as web-services and distributed systems.

## Features
The P# framework provides:
- Language extensions to C# for building **event-driven asynchronous** applications, writing **test harnesses**, and specifying **safety and liveness properties**.
- A **systematic testing engine** that can capture and control all specified nondeterminism in the system, systematically explore the actual executable code to discover bugs, and report bug traces. A P# bug trace provides a global order of all communication events, and thus is easier to debug.
- Support for **replaying** bug traces, and **debugging** them using the Visual Studio debugger.

Although P# primarily targets .NET, it has also experimental support for systematically testing native C++ code.

## How to build and use
Check the P# [wiki](https://github.com/p-org/PSharp/wiki):

- [Building P#](https://github.com/p-org/PSharp/wiki/Build-Instructions)
- [P# Compiler](https://github.com/p-org/PSharp/wiki/Compilation)
- [P# Tester](https://github.com/p-org/PSharp/wiki/Testing)
- [P# Replayer/Debugger](https://github.com/p-org/PSharp/wiki/Bug-Reproduction)
- [Samples and Walkthroughs](https://github.com/p-org/PSharp/wiki/Samples-and-Walkthroughs)

## How to contribute

We welcome contributions to the P# project! Before you start contributing, though, please read carefully the [development guidelines](https://github.com/p-org/PSharp/wiki/Contributing-Code).

## Contact us

If you would like to use P# in your project, or have any specific questions, please feel free to contact one of the following members of the P# team:
- Pantazis Deligiannis (p.deligiannis@imperial.ac.uk) [Maintainer]
- Akash Lal (akashl@microsoft.com) [Maintainer]
- Shaz Qadeer (qadeer@microsoft.com)
- Cheng Huang (cheng.huang@microsoft.com)

## Publications
- **[Uncovering Bugs in Distributed Storage Systems During Testing (not in Production!)](https://www.usenix.org/node/194442)**. Pantazis Deligiannis, Matt McCutchen, Paul Thomson, Shuo Chen, Alastair F. Donaldson, John Erickson, Cheng Huang, Akash Lal, Rashmi Mudduluru, Shaz Qadeer and Wolfram Schulte. In the *14th USENIX Conference on File and Storage Technologies* (FAST), 2016.
- **[Asynchronous Programming, Analysis and Testing with State Machines](https://dl.acm.org/citation.cfm?id=2737996)**. Pantazis Deligiannis, Alastair F. Donaldson, Jeroen Ketema, Akash Lal and Paul Thomson. In the *36th Annual ACM SIGPLAN Conference on Programming Language Design and Implementation* (PLDI), 2015.
