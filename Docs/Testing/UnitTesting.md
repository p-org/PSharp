Unit-testing P# machines in isolation
=====================================
The `MachineTestKit` API provides the capability to _unit-test_ a machine of type `T` _sequentially_ and in _isolation_ from other machines, or the external environment. This is orthogonal from using the `PSharpTester` (see [here](./TestingMethodology.md)) which provides the capability to automatically test a P# program end-to-end (i.e. integration-testing) against concurrency and specifications. We recommend writing both unit-tests and integration-tests to get the most value out of the P# framework.

We will now discuss how to use `MachineTestKit` by going through some simple examples.

Say that you have the following machine `M`, which waits to receive an event of type `E` (via the `Receive` statement) in the `InitOnEntry` handler of the `Start` state `Init`:
```C#
private class E : Event {}

private class M : Machine
{
   [Start]
   [OnEntry(nameof(InitOnEntry))]
   private class Init : MachineState {}

   private async Task InitOnEntry()
   {
         await this.Receive(typeof(E));
   }
}
```

To unit-test the above logic, first import the `Microsoft.PSharp.TestingServices` library:
```C#
using Microsoft.PSharp.TestingServices;
```

Next, create a new `MachineTestKit` instance for the machine `M` in your test method, as seen below. You can pass an optional `Configuration` (e.g. if you want to enable verbosity).
```C#
public void Test()
{
   var test = new MachineTestKit<M>(configuration: Configuration.Create());
}
```

When `MachineTestKit<M>` is instantiated, it creates an instance of the machine `M`, which executes in a special runtime that provides isolation. The internals of the machine (e.g. the queue) are properly initialized, as if the machine was executing in production. However, if the machine is trying to create other machines, it will get a _dummy_ `MachineId`, and if the machine tries to send an event to a machine other than itself, that event will be dropped. Talking to external APIs (e.g. network or storage) might still require mocking (as is the case in regular unit-testing).

The `MachineTestKit` provides two APIs that allow someone to asynchronously (but sequentially) interact with the machine via its inbox, and thus test how the machine transitions to different states and handles events. These two APIs are `StartMachineAsync(Event initialEvent = null)` and `SendEventAsync(Event e)`.

The `StartMachineAsync` method transitions the machine to its `Start` state, passes the optional specified event (`initialEvent`) and invokes its `OnEntry` handler, if there is one available. This method returns a task that completes when the machine reaches quiescence (typically when the event handler finishes executing because there are not more events to dequeue, or when the machine asynchronously waits to receive an event). This method should only be called in the beginning of the unit-test, since a machine only transitions to its `Start` state once.

The `SendEventAsync` method sends an event to the machine and starts its event handler. Similar to `StartMachineAsync`, this method returns a task that completes when the machine reaches quiescence (typically when the event handler finishes executing because there are not more events to dequeue, or when the machine asynchronously waits to receive an event).

The `MachineTestKit<M>` also provides `Assert`, which is a generic assertion that you can use for checking correctness, as well as several other specialized assertions, e.g. for asserting state transitions, or to check if the inbox is empty. We plan to add more assertions in the future.

The following code snippet shows how to use these APIs to test the machine `M` of the above example:
```C#
public async Task Test()
{
   var test = new MachineTestKit<M>();

   await test.StartMachineAsync();
   test.AssertIsWaitingToReceiveEvent(true);

   await test.SendEventAsync(new E());
   test.AssertIsWaitingToReceiveEvent(false);
   test.AssertInboxSize(0);
}
```

The above unit-test creates a new instance of the machine `M`, then transitions the machine to its `Start` state using `StartMachineAsync`, and then asserts that the machine is asynchronously waiting to receive an event of type `E` by invoking the `AssertIsWaitingToReceiveEvent(true)` assertion. Next, the test is sending the expected event `E` using `SendEventAsync`, and finally asserts that the machine is not waiting any event, by calling `AssertIsWaitingToReceiveEvent(false)`, and that its inbox is empty, by calling `AssertInboxSize(0)`.

Besides providing the capability to drive the execution of a machine via the `StartMachineAsync` and `SendEventAsync` APIs, the `MachineTestKit` also allows someone to directly call machine methods. Lets see how this can be done in a simple example. Say that you have the following machine `M`, which has a method `Add(int m, int k)` that takes two integers, adds them, and returns the result:
```C#
private class M : Machine
{
   [Start]
   private class Init : MachineState {}

   internal int Add(int m, int k)
   {
         return m + k;
   }
}
```

To unit-test the above `Add` machine method, the `MachineTestKit<M>` instance gives you access to the machine reference through the `MachineTestKit.Machine` property. You can then use this reference to directly invoke methods of the machine, as seen below.
```C#
public void Test()
{
   var test = new MachineTestKit<M>(configuration: Configuration.Create());
   int result = test.Machine.Add(3, 4);
   test.Assert(result == 7, $"Incorrect result '{result}'");
}
```

Note that directly calling machine methods only works if these methods are declared as `public` or `internal`. This is typically not recommended for machines (and actors, in general), since the only way to interact with them should be by sending messages. However, it can be very useful to unit-test private machine methods, and for this reason the `MachineTestKit` provides the `Invoke` and `InvokeAsync` APIs, which accept the name of the method (and, optionally, parameter types for distinguishing overloaded methods), as well as the parameters to invoke the method, if any. The following example shows how to use these APIs to invoke private machine methods:

```C#
private class M : Machine
{
   [Start]
   private class Init : MachineState {}

   private int Add(int m, int k)
   {
         return m + k;
   }

   private async Task<int> AddAsync(int m, int k)
   {
         await Task.CompletedTask;
         return m + k;
   }
}

public async Task TestAsync()
{
   var test = new MachineTestKit<M>(configuration: Configuration.Create());

   // Use this API to unit-test a private machine method.
   int result = (int)test.Invoke("Add", 3, 4);
   test.Assert(result == 7, $"Incorrect result '{result}'");

   // Use this API to unit-test an overloaded private machine method.
   result = (int)test.Invoke("Add", new Type[] { typeof(int), typeof(int) }, 3, 4);
   test.Assert(result == 7, $"Incorrect result '{result}'");

   // Use this API to unit-test an asynchronous private machine method.
   int result = (int)await test.InvokeAsync("AddAsync", 3, 4);
   test.Assert(result == 7, $"Incorrect result '{result}'");

   // Use this API to unit-test an asynchronous overloaded private machine method.
   result = (int)await test.InvokeAsync("AddAsync", new Type[] { typeof(int), typeof(int) }, 3, 4);
   test.Assert(result == 7, $"Incorrect result '{result}'");
}
```

Note that its possible to use both the `StartMachineAsync` and `SendEventAsync`, as well as invoke directly machine methods by accessing the `MachineTestKit.Machine` property, based on the testing scenario.
