[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://raw.githubusercontent.com/p-org/PSharp/master/LICENSE.txt)
[![NuGet](https://img.shields.io/nuget/v/Microsoft.PSharp.svg)](https://www.nuget.org/packages/Microsoft.PSharp/)
[![Build status](https://p-language.visualstudio.com/plang-ci/_apis/build/status/psharp/psharp-win-build-and-test?branchName=master)](https://p-language.visualstudio.com/plang-ci/_build/latest?definitionId=1)

P# is a framework for rapid development of reliable asynchronous software. P# is used by several teams in [Azure](https://azure.microsoft.com/) to design, implement and automatically test production distributed systems and services.

Why should I use P#?
====================
The key value of P# is that it allows you to express your system design at a higher level and specify properties (both safety and liveness) _programmatically_ in your source code. During testing, P# serializes your program, captures and controls all (implicit as well as specified) nondeterminism, and thoroughly explores the executable code (in your local dev machine) to automatically discover deep concurrency bugs. If a bug is found, P# reports a reproducible bug trace that provides a global order of all asynchrony and events in the system, and thus is significantly easier to debug that regular unit-/integration-tests and logs from production or stress tests, which are typically nondeterministic.

Besides testing, P# can be directly used in production as it offers fast, efficient and scalable execution. As a testament of this, P# is being used by several teams in Azure to build mission-critical services.

P# programming models
=====================
For designing and implementing reliable asynchronous software, P# provides the following two programming models:
- **Asynchronous machine tasks**, which follows the [task-based asynchronous pattern](https://docs.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap). This programming model is based on the `MachineTask` type, which represents an asynchronous operation that you can coordinate using the `async` and `await` keywords of [C#](https://docs.microsoft.com/en-gb/dotnet/csharp/).
- **Asynchronous communicating state-machines**, an [actor-based programming model](https://en.wikipedia.org/wiki/Actor_model) that allows you to express your design and concurrency at a higher-level. This programming model is based on the `Machine` type, which represents an asynchronous entity that can create new machines, send events to other machines, and handle received events with user-specified logic.

Getting started with P#
=======================
First, install our latest [NuGet package](https://www.nuget.org/packages/Microsoft.PSharp/), or build P# from source, following the instructions [here](Docs/BuildInstructions.md).

Next, learn about the P# programming models and how to write your first P# program:
- [Asynchronous machine tasks](Docs/ProgrammingModels/MachineTasks.md)
- [Asynchronous communicating state-machines](Docs/ProgrammingModels/Machines.md)

Now you are ready to dive into various features and topics:
- [Machine termination](Docs/Features/MachineTermination.md)
- [Specifying models and safety/liveness properties](Docs/Features/SafetyLivenessProperties.md)
- [Using async/await in a machine](Docs/Features/AsyncAwaitSupport.md)
- [Sharing objects across machines](Docs/Features/ObjectSharing.md)
- [Synchronous execution of machines](Docs/Features/SynchronousExecution.md)
- [Using timers in P#](Docs/Features/Timers.md)
- [Logging](Docs/Features/Logging.md) and [tracking operation groups](Docs/Features/TrackingOperationGroups.md)

Learn how to use the P# testing infrastructure to write unit-tests, thoroughly check safety and liveness properties, and deterministically reproduce bugs:
- [Unit-testing P# machines in isolation](Docs/Testing/UnitTesting.md)
- [Automatically testing P# programs end-to-end and reproducing bugs](Docs/Testing/TestingMethodology.md)
- [Effectively checking liveness properties](Docs/Testing/LivenessChecking.md)
- [Testing async/await code using P#](Docs/Testing/TestingAsyncAwait.md)
- [Code and activity coverage](Docs/Testing/CodeCoverageVisualisation.md)
- [Semantics of uncaught exceptions](Docs/Testing/UncaughtExceptions.md)

The following provides information regarding the available tools in the P# ecosystem:
- [P# Language Syntax Translator](Docs/Tools/LanguageSyntaxRewriter.md)
- [P# Trace Viewer](Docs/Tools/TraceViewer.md)
- [P# Race Detector](Docs/Tools/RaceDetection.md)
- [P# Batch Tester](https://github.com/p-org/PSharpBatchTesting)

We provide support for editing the P# state-machine language syntax in Visual Studio:
- [Visual Studio Editing Support](Docs/CodeEditors/VisualStudioLanguageSupport.md)

## Samples and applications
We provide a collection of samples that show how to use the P# framework to build and systematically test asynchronous, event-driven applications. The P# samples are available in the Git repository, under the Samples directory. You can read more [here](https://github.com/p-org/PSharp/tree/master/Samples).

We have also used P# to test applications on top of Azure Service Fabric and Orleans:
- [Testing Azure Service Fabric Applications](https://github.com/p-org/PSharpModels)
- [Testing Microsoft Orleans Applications](https://github.com/p-org/PSharpModels)

## Publications
List of publications on P#:
- **[Asynchronous Programming, Analysis and Testing with State Machines](https://dl.acm.org/citation.cfm?id=2737996)**. Pantazis Deligiannis, Alastair F. Donaldson, Jeroen Ketema, Akash Lal and Paul Thomson. In the *36th Annual ACM SIGPLAN Conference on Programming Language Design and Implementation* (PLDI), 2015.
- **[Uncovering Bugs in Distributed Storage Systems During Testing (not in Production!)](https://www.usenix.org/node/194442)**. Pantazis Deligiannis, Matt McCutchen, Paul Thomson, Shuo Chen, Alastair F. Donaldson, John Erickson, Cheng Huang, Akash Lal, Rashmi Mudduluru, Shaz Qadeer and Wolfram Schulte. In the *14th USENIX Conference on File and Storage Technologies* (FAST), 2016.
- **[Lasso Detection using Partial-State Caching](https://www.microsoft.com/en-us/research/publication/lasso-detection-using-partial-state-caching-2/)**. Rashmi Mudduluru, Pantazis Deligiannis, Ankush Desai, Akash Lal and Shaz Qadeer. In the *17th International Conference on Formal Methods in Computer-Aided Design* (FMCAD), 2017.
- **Reliable State Machines: A Framework for Programming Reliable Cloud Services**. Suvam Mukherjee, Nitin John Raj, Krishnan Govindraj, Pantazis Deligiannis, Chandramouleswaran Ravichandran, Akash Lal, Aseem Rastogi and Raja Krishnaswamy. In the *33rd European Conference on Object-Oriented Programming* (ECOOP), 2019.

## Contributing
We welcome contributions! However, before you start contributing, please read our [contribution guidelines](Docs/Contributing.md).

## Contact us
If you are interested in using P#, or have any P# related questions, please send us an [email](mailto:pdev@microsoft.com) or open a new [issue](https://github.com/p-org/PSharp/issues).
