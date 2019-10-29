[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://raw.githubusercontent.com/p-org/PSharp/master/LICENSE)
[![NuGet](https://img.shields.io/nuget/v/Microsoft.PSharp.svg)](https://www.nuget.org/packages/Microsoft.PSharp/)
[![Build status](https://dev.azure.com/foundry99/Coyote/_apis/build/status/PSharp/PSharp-Windows-CI)](https://dev.azure.com/foundry99/Coyote/_build/latest?definitionId=63)

P# is a framework for rapid development of reliable asynchronous software. P# is used by several teams in [Azure](https://azure.microsoft.com/) to design, implement and automatically test production distributed systems and services.

## Features
The P# framework provides:
- An actor-based programming model for building event-driven asynchronous applications. The unit of concurrency in P# is an asynchronous communicating state-machine, which is basically an actor that can create new machines, send and receive events, and transition to different states. Using P# machines, you can express your design and code at a higher level that is a natural fit for many cloud services.
- An efficient, lightweight runtime that is build on top of the Task Parallel Library (TPL). This runtime can be used to deploy a P# program in production. The P# runtime is very flexible and can work with any communication and storage layer.
- The capability to easily write safety and liveness specifications (similar to TLA+) programmatically in C#.
- A systematic testing engine that can control the P# program schedule, as well as all declared sources of nondeterminism (e.g. failures and timeouts), and systematically explore the actual executable code to discover bugs (e.g. crashes or specification violations). If a bug is found, the P# testing engine will report a deterministic reproducible trace that can be replayed using the Visual Studio Debugger.

## Getting started
Read the P# programming guide and then read about various features and topics [here](Docs/README.md).

## How to build
Follow the [instructions](Docs/BuildInstructions.md) to build P# from source, or just install our latest P# [NuGet package](https://www.nuget.org/packages/Microsoft.PSharp/).

## How to contribute
We welcome contributions! However, before you start contributing, please read carefully the [development guidelines](Docs/Contributing.md).

## Contact us
If you are interested in using P# in your project, or have any P# related questions, please send us an [email](mailto:pdev@microsoft.com) or open a new [issue](https://github.com/p-org/PSharp/issues).
