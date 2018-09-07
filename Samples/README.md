P# Samples
==========
A collection of samples that show how to use the P# framework to build and systematically test asynchronous event-driven applications.

We provide samples that show how to use (1) P# as a C# framework (see the [Framework](Framework) directory) and (2) P# as a high-level language (see the [Language](Language) directory). It is up to you to decide which P# mode to use to develop your application or service.

## Where to start
If you are new to P#, please check out the **PingPong** and **FailureDetector** samples (inside [Framework](Framework) and [Language](Language)), which give an introduction to using basic and more advanced features of P#. If you have any questions, please get in touch!

## Samples
- **PingPong**, a simple application that consists of a client and a server sending ping and pong messages for a number of turns.
- **FailureDetector**, which demonstrates more advanced features of P# such as monitors (for specifying global safety and liveness properties) and nondeterministic timers which are controlled during testing.

## How to build
To build the framework samples, run the following powershell script (or manually build the `Samples.Framework` solution):
```
powershell -c .\build-framework-samples.ps1
```

To build the language samples, run the following powershell script (or manually build the `Samples.Language` solution):
```
powershell -c .\build-language-samples.ps1
```

## How to run
To execute a sample, simply run the corresponding executable, available in `.\Framework\bin\${framework}\` or `.\Language\bin\${framework}\`.
