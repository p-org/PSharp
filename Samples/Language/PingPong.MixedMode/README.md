PingPong
========
This is a simple implementation of a ping-pong application in P#.

This sample showcases how to write a P# program in mixed-mode: using both the P# high-level language and the C# framework (based on partial classes).

## How to test

To test the produced executable use the following command:
```
PSharpTester.exe /test:PingPong.MixedMode.exe /i:100
```
With the above command, the P# tester will systematically test the program for 100 testing iterations.

Note that this program is pretty simple: there are no bugs to be found, and the execution is pretty much deterministic. Please check our other samples for more advanced examples.
