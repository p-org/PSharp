Synchronous execution of machines
=================================
P# offers the following APIs (and overloads) for synchronous execution.
```C#
public Task<MachineId> CreateMachineAndExecute(Type type, Event e = null, Guid? operationGroupId = null);
public Task<bool> SendEventAndExecute(MachineId target, Event e, SendOptions options = null);
```

Both these are `async` methods and must be `awaited` by the caller. The method `CreateMachineAndExecute` when awaited, returns only when the newly created machine becomes idle. That is, it creates the machine, passes it the initial event `e` and then waits for the machine to become idle. (A machine is idle when it is blocked on its inbox for receiving input.) The method `SendEventAndExecute` when awaited has two possible executions. If the `target` machine is running (i.e., it is not idle) then the method only enqueues the event and returns immediately with the return value `false`. If the `target` machine was idle then the method enqueues the event (which causes the `target` machine to start executing) and blocks until the machine becomes idle again. In this case, the method returns `true` indicating that the event has been processed by the `target` machine.

The user should be careful with the use of `Receive` when using these methods. In the absence of `Receive`, the semantics of these methods guarantee that the program cannot deadlock. With a `Receive` the following situation can occur. Lets suppose there are two machines `A` and `B` and the latter is idle. Then machine `A` does `SendEventAndExecute` to pass an event `e` to `B`. Because `B` was idle, `A` will wait until `B` becomes idle again. But if `B` executes a `Receive` while processing the event `e`, expecting another event from `A` then the program deadlocks. (Blocking on a `Receive` is not considered as being idle.)

An expected use case is a program that simply wants to drive a state machine synchronously. The main thread does `CreateMachineAndExecute` to create the state machine, then repeatedly does `SendEventAndExecute` to make the machine process events one after another. 
