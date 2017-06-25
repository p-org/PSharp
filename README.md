[![NuGet](https://img.shields.io/nuget/v/Microsoft.PSharp.svg)](https://www.nuget.org/packages/Microsoft.PSharp/) [![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://raw.githubusercontent.com/p-org/PSharp/master/LICENSE.txt)

P#
====================
A framework for **building**, **analyzing**, **systematically testing** and **debugging** asynchronous reactive software. P# is used by engineers in [Azure](https://azure.microsoft.com/) to design, implement and thoroughly test distributed systems and services.

## Features
The P# framework provides:
- Language extensions to C# for building **event-driven asynchronous** applications, writing **test harnesses**, and specifying **safety** and **liveness properties**.
- A **systematic testing engine** that can capture and control all specified nondeterminism in the system, systematically explore the actual executable code to discover bugs, and report bug traces. A P# bug trace provides a global order of all communication events, and thus is easier to debug.
- Support for **replaying** bug traces, and **debugging** them using the Visual Studio debugger.

## Getting started
The best way to [build](https://github.com/p-org/PSharp/wiki/Build-Instructions) and start using P# is to read our [wiki](https://github.com/p-org/PSharp/wiki).

You can also read the manual and available publications:

- [Manual](https://github.com/p-org/PSharp/wiki/Walkthrough-and-Samples)  
- [Publications](https://github.com/p-org/PSharp/wiki/Publications)

## How to build
Follow the instructions in our [wiki](https://github.com/p-org/PSharp/wiki/Build-Instructions).

## How to contribute
We welcome contributions! However, before you start contributing, please read carefully the [development guidelines](https://github.com/p-org/PSharp/wiki/Contributing-Code).

## Contact us
If you are interested in using P# in your project, or have any P# related questions, please send us an [email](mailto:pdev@microsoft.com) or open a new [issue](https://github.com/p-org/PSharp/issues).
