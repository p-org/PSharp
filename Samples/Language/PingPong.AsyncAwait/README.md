PingPong
========
This is a simple implementation of a ping-pong application in P#. This sample showcases how to use async-await in the P# high-level syntax.

## How to test

To test the produced executable use the following command:
```
PSharpTester.exe /test:PingPong.AsyncAwait.exe /i:100
```
With the above command, the P# tester will systematically test the program for 100 testing iterations.

Note that this program is pretty simple: there are no bugs to be found, and the execution is pretty much deterministic. Please check our other samples for more advanced examples.
