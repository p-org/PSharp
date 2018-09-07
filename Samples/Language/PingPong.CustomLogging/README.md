PingPong
========
This is a simple implementation of a ping-pong application in P# that uses a custom logger for testing.

## How to test

To test the produced executable use the following command:
```
PSharpTester.exe /test:PingPong.CustomLogging.exe /i:100
```
With the above command, the P# tester will systematically test the program for 100 testing iterations.

Note that this program is pretty simple: there are no bugs to be found, and the execution is pretty much deterministic. Please check our other samples for more advanced examples.
