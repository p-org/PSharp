Overview
========
P# is a framework that provides the capability of creating asynchronous state-machines, sending events from one machine to another, and writing assertions about system properties (both safety and liveness).

During testing, the built-in P# testing engine captures and controls all (implicit as well as specified) nondeterminism in the system, thoroughly explores the actual executable code to discover bugs, and reports fully-reproducible bug traces. A P# bug trace provides a global order of all events and transitions, and thus is easier to debug.

Getting started with P#
=======================
First, build P# from source, following the instructions [here](BuildInstructions.md), or install our latest [NuGet package](https://www.nuget.org/packages/Microsoft.PSharp/).

Next, learn about the different ways of using P# [here](Overview.md), and how to write your first P# program [here](WriteFirstProgram.md).

Now you are ready to dive into various features and topics:
- [Machine termination](Features/MachineTermination.md)
- [Specifying models and safety/liveness properties](Features/SafetyLivenessProperties.md)
- [Using async/await in a machine](Features/AsyncAwaitSupport.md)
- [Sharing objects across machines](Features/ObjectSharing.md)
- [Synchronous execution of machines](Features/SynchronousExecution.md)
- [Using timers in P#](Features/Timers.md)
- [Logging](Features/Logging.md) and [tracking operation groups](Features/TrackingOperationGroups.md)

Learn how to use the P# testing infrastructure to write unit-tests, thoroughly check safety and liveness properties, and deterministically reproduce bugs:
- [Unit-testing P# machines in isolation](Testing/UnitTesting.md)
- [Automatically testing P# programs end-to-end and reproducing bugs](Testing/TestingMethodology.md)
- [Effectively checking liveness properties](Testing/LivenessChecking.md)
- [Testing async/await code using P#](Testing/TestingAsyncAwait.md)
- [Code and activity coverage](Testing/CodeCoverageVisualisation.md)
- [Semantics of uncaught exceptions](Testing/UncaughtExceptions.md)

## Tools
The following provides information regarding the available tools in the P# ecosystem:
- [P# Compiler](Tools/Compiler.md)
- [P# Race Detector](Tools/RaceDetection.md)
- [P# Batch Tester](https://github.com/p-org/PSharpBatchTesting)

## Code editing
We provide support for editing the P# language syntax in Visual Studio:
- [Visual Studio Editing Support](CodeEditors/VisualStudioLanguageSupport.md)

## Samples and applications
We provide a collection of samples that show how to use the P# framework to build and systematically test asynchronous, event-driven applications. The P# samples are available in the Git repository, under the Samples directory. You can read more [here](https://github.com/p-org/PSharp/tree/master/Samples).

We have also used P# to test applications on top of Azure Service Fabric and Orleans:
- [Testing Azure Service Fabric Applications](https://github.com/p-org/PSharpModels)
- [Testing Microsoft Orleans Applications](https://github.com/p-org/PSharpModels)

## Publications
List of publications on P#:
- **[Lasso Detection using Partial-State Caching](https://www.microsoft.com/en-us/research/publication/lasso-detection-using-partial-state-caching-2/)**. Rashmi Mudduluru, Pantazis Deligiannis, Ankush Desai, Akash Lal, Shaz Qadeer . In *Formal Methods in Computer-Aided Design* (FMCAD), 2017.
- **[Uncovering Bugs in Distributed Storage Systems During Testing (not in Production!)](https://www.usenix.org/node/194442)**. Pantazis Deligiannis, Matt McCutchen, Paul Thomson, Shuo Chen, Alastair F. Donaldson, John Erickson, Cheng Huang, Akash Lal, Rashmi Mudduluru, Shaz Qadeer and Wolfram Schulte. In the *14th USENIX Conference on File and Storage Technologies* (FAST), 2016.
- **[Asynchronous Programming, Analysis and Testing with State Machines](https://dl.acm.org/citation.cfm?id=2737996)**. Pantazis Deligiannis, Alastair F. Donaldson, Jeroen Ketema, Akash Lal and Paul Thomson. In the *36th Annual ACM SIGPLAN Conference on Programming Language Design and Implementation* (PLDI), 2015.

## Contributing
To start contributing to P#, read our [contribution guidelines](Contributing.md).

## Contact us
If you are interested in using P# in your project, or have any P# related questions, please send us an [email](mailto:pdev@microsoft.com) or open a new [issue](https://github.com/p-org/PSharp/issues).
