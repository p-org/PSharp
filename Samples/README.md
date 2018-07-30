P# Samples
==========
A collection of samples that show how to use the P# framework to build and systematically test asynchronous, event-driven applications.

## Where to start
If you are new to P#, please check out the **PingPong** and **FailureDetector** samples, which give an introduction to using basic and more advanced features of P#. If you have any questions, please get in touch!

## Samples
- **PingPong**, a simple application that consists of a client and a server sending ping and pong messages for a number of turns.
- **FailureDetector**, which demonstrates more advanced features of P# such as monitors (for specifying global safety and liveness properties) and nondeterministic timers which are controlled during testing.

## How to build
To build all samples, run the following powershell script from the Visual Studio 2017 developer command prompt:
```
powershell -c Scripts\build-samples.ps1
```

## How to run
To execute a sample, simply run the corresponding executable, available in `.\bin\${framework}\`.
