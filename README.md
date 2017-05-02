[![NuGet](https://img.shields.io/nuget/v/Microsoft.PSharp.svg)](https://www.nuget.org/packages/Microsoft.PSharp/) [![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://raw.githubusercontent.com/p-org/PSharp/master/LICENSE.txt)

P#
====================
A language and toolkit for **building**, **analyzing**, **systematically testing** and **debugging** asynchronous reactive software, such as web-services and distributed systems.

## Features
The P# framework provides:
- Language extensions to C# for building **event-driven asynchronous** applications, writing **test harnesses**, and specifying **safety** and **liveness properties**.
- A **systematic testing engine** that can capture and control all specified nondeterminism in the system, systematically explore the actual executable code to discover bugs, and report bug traces. A P# bug trace provides a global order of all communication events, and thus is easier to debug.
- Support for **replaying** bug traces, and **debugging** them using the Visual Studio debugger.

## Getting started
The best way to build and start using P# is to read our [wiki](https://github.com/p-org/PSharp/wiki).

You can also read the manual and available publications:

- [Manual](https://github.com/p-org/PSharp/blob/master/Docs/Manual/manual.pdf)  
- [Publications](https://github.com/p-org/PSharp/wiki/Publications)

## How to build

To build P#, run the following powershell script from the Visual Studio 2017 developer command prompt:
```
powershell -c .\build.ps1
```

To build the samples, run the above script with the `samples` option:
```
powershell -c .\build.ps1 -samples
```

## How to contribute

We welcome contributions! However, before you start contributing, please read carefully the [development guidelines](https://github.com/p-org/PSharp/wiki/Contributing-Code).

## Contact us

If you are interested in using P# in your project, or have any P# related questions, please send us an [email](pdev@microsoft.com) or open a new [issue](https://github.com/p-org/PSharp/issues).
